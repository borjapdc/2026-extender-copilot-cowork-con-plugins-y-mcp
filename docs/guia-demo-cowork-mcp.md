# Guia detallada de la demo: Copilot Cowork, Skills y MCP

## Objetivo de la charla

**Titulo:** Extender Copilot Cowork con plugins y MCP: skills, conectores

**Descripcion:** En esta charla se extiende Microsoft 365 Copilot Cowork mediante plugins que combinan skills y conectores MCP. La demo muestra una skill `SKILL.md` que orienta a Cowork, un servidor MCP que expone herramientas y una API que usa Azure OpenAI o Microsoft Foundry. Tambien se comparan una variante local sin autenticacion y una variante con Microsoft Entra SSO.

La guia separa tres niveles que no se deben confundir:

1. **Demo anonima:** util para explicar la arquitectura y depurar localmente. No es apta para piloto ni produccion.
2. **Demo SSO:** Cowork entrega un token Entra al MCP; MCP y API validan el token y la API queda protegida.
3. **Produccion con OBO:** MCP valida el token de Cowork y adquiere un token propio para la API. Es la opcion recomendada cuando MCP y API son recursos distintos.

## Componentes del repositorio

```text
cowork-plugin/                 Plugin Cowork con Entra SSO.
cowork-plugin-anonymous/       Plugin Cowork sin autenticacion.
script/                         Automatizacion local y archivo .env.
src/SanctuaryIntelligence.Api/  API .NET que llama al modelo y expone /api/*.
src/SanctuaryIntelligence.Mcp/  Servidor MCP Streamable HTTP en /mcp.
```

El flujo de una herramienta es:

```text
Usuario
  -> Copilot Cowork
  -> skill sanctuary-intelligence
  -> conector MCP HTTPS
  -> servidor MCP
  -> API Sanctuary Intelligence
  -> Azure OpenAI / Microsoft Foundry
```

La skill contiene instrucciones de uso; no ejecuta codigo por si misma. Cowork decide cuando invocar las herramientas MCP segun la skill y las definiciones de herramientas incluidas en el paquete.

## Contrato MCP y estructura del paquete

El servidor MCP implementa Streamable HTTP y expone `/mcp`. Debe responder, como minimo, a:

- `initialize`
- `tools/list`
- `tools/call`
- `notifications/initialized`

Cada paquete contiene:

```text
manifest.json
color.png
outline.png
skills/sanctuary-intelligence/SKILL.md
tools/sanctuary-tools.json
```

`tools/sanctuary-tools.json` contiene el descriptor estatico de herramientas. El manifiesto lo referencia mediante:

```json
"mcpToolDescription": {
  "file": "tools/sanctuary-tools.json"
}
```

Aunque MCP admite descubrimiento dinamico mediante `tools/list`, el validador de paquetes usado en esta demo exige ese descriptor dentro del ZIP. Si el archivo o la referencia faltan, la carga puede fallar con un error relativo a `mcpToolDescription`.

## Diseno del plugin Cowork

### Separar la skill, el conector y la logica de negocio

Cada pieza tiene una responsabilidad distinta:

- **Skill:** explica a Cowork cuando debe usar las herramientas, que datos pedir y como presentar el resultado. Debe contener instrucciones de alto nivel, no secretos ni logica de red.
- **Conector MCP:** declara el endpoint HTTPS, el mecanismo de autenticacion y el catalogo de herramientas.
- **MCP:** valida la solicitud, expone el contrato MCP y orquesta llamadas a servicios internos.
- **API o sistema empresarial:** contiene reglas de negocio, acceso a datos y autorizacion de dominio.

Esta separacion permite evolucionar las herramientas sin convertir la skill en un manual de implementacion. Tambien evita que una instruccion de lenguaje natural se trate como una autorizacion para una accion sensible.

### Reglas para `SKILL.md`

Una skill eficaz es breve y determinista:

1. Da una descripcion concreta y orientada a intencion de usuario.
2. Nombra la herramienta adecuada y los parametros necesarios.
3. Indica que hacer ante parametros ausentes, errores o respuestas incompletas.
4. Distingue datos devueltos por una herramienta de una recomendacion generada por el modelo.
5. No incluye URLs privadas, API keys, tokens, identificadores de tenant ni instrucciones administrativas.

Cuando una skill crece, mueve taxonomias, ejemplos extensos y politicas de negocio a archivos de referencia dentro de `skills/<nombre>/references/`. Mantiene el frontmatter con un nombre que coincida exactamente con el nombre de la carpeta.

### Reglas para `manifest.json`

- Usa una version de manifiesto admitida por Cowork y solo propiedades definidas por su schema. Los manifests recientes son estrictos y rechazan campos validos en otros tipos de aplicacion Teams.
- Asigna un `id` distinto a cada paquete. La variante anonima y la variante SSO no son actualizaciones de la misma aplicacion.
- Mantiene `name.short`, descripciones e iconos distintos para que un usuario sepa cual activa.
- Incluye los iconos referenciados y los archivos de herramientas dentro del ZIP. Una referencia valida a un archivo ausente provoca un fallo de carga.
- Incrementa `version` ante cualquier cambio de manifiesto, skill, icono o descriptor de herramientas. Los scripts detectan cambios del paquete y generan una version de parche nueva.
- Usa URLs HTTPS publicas. `localhost`, IPs privadas y certificados no confiables no funcionan como endpoint remoto de Cowork.

### Herramientas MCP seguras y faciles de usar

Para cada herramienta:

- Usa un nombre estable, descriptivo y con un verbo: `classify_threat`, `recommend_knight` o `generate_mission`.
- Define un `inputSchema` estricto con tipos, descripciones y campos obligatorios.
- Rechaza parametros desconocidos, valores fuera de rango y payloads excesivos en el servidor, aunque el schema exista en el paquete.
- Devuelve JSON estructurado y mensajes de error accionables, sin detalles internos de infraestructura.
- Declara una herramienta separada para cada accion de riesgo. No uses una herramienta generica que acepte comandos arbitrarios.
- Empieza por operaciones de lectura. Para una accion que escriba, borre, publique o tenga efecto financiero, exige confirmacion explicita y aplica autorizacion de negocio en la API.

## Autenticacion, autorizacion y tokenizacion

### No confundir autenticacion con autorizacion

La autenticacion responde a "quien es el usuario o cliente". La autorizacion responde a "que puede hacer en este recurso". Validar una firma JWT no concede por si mismo permiso para ejecutar cualquier herramienta.

En MCP y API valida como minimo:

- Firma e issuer del token.
- Expiracion y tolerancia de reloj limitada.
- Tenant esperado, si el servicio es de un solo tenant.
- Audiencia esperada (`aud`).
- Scopes delegados (`scp`) o roles de aplicacion (`roles`) requeridos para la operacion.
- Reglas de dominio adicionales: pertenencia a equipo, acceso a cliente, region o clasificacion de datos.

No uses un `audience` comodin ni desactives la validacion de audiencia para solucionar errores de SSO. Un error `IDX10214` indica que la audiencia emitida no coincide con la configurada; debe corregirse el registro Entra, la configuracion SSO o la lista permitida del recurso.

### Entra SSO y Enterprise Token Store

En el paquete SSO, el manifiesto usa:

```json
"authorization": {
  "type": "OAuthPluginVault",
  "referenceId": "<teams-sso-registration-id>"
}
```

`referenceId` identifica una configuracion almacenada en Enterprise Token Store. No es:

- El client ID del registro de aplicacion Entra.
- Un client secret.
- Un access token.
- El object ID del registro de aplicacion.

El Application ID URI generado durante el registro SSO debe estar en `identifierUris` de la aplicacion que protege el recurso. Si se configura SSO para un tenant y Azure CLI esta autenticado en otro, las consultas de administracion pueden no encontrar el registro aunque Cowork este intentando adquirir el token en el tenant correcto.

### Reenvio de token: solo para la demo

La demo SSO reenvia el token recibido por MCP hacia la API. Es una simplificacion valida cuando se cumplen simultaneamente estas condiciones:

1. MCP y API pertenecen al mismo limite de confianza.
2. Ambos validan la misma audiencia Entra.
3. La API no representa un recurso independiente ni necesita scopes propios.
4. El token se reenvia solo a la API local conocida, nunca a un host elegido por la entrada del usuario.

El handler del MCP copia exclusivamente una cabecera `Bearer` valida al `HttpClient` de la API. No debe reenviar cabeceras arbitrarias, cookies, tokens de otros esquemas ni datos del usuario a servicios externos.

### API protegida en la demo

Cuando se ejecuta `setup-entra-sso-demo`:

- La API y el MCP reciben `Authentication__Enabled=true` y la misma configuracion Entra desde `.env`.
- Los endpoints `/api/*` requieren autenticacion.
- `/` y `/health` permiten acceso anonimo para la orquestacion local.
- Una llamada sin token a API o MCP debe devolver `401`.

No actives el middleware de autenticacion sin aplicar tambien `RequireAuthorization` o una politica fallback a los endpoints de negocio. De lo contrario la API valida tokens cuando existen, pero sigue aceptando usuarios anonimos.

### OBO: patron recomendado para produccion

Para una arquitectura empresarial, API y MCP deben ser recursos distintos:

```text
Usuario -> Cowork -> token para MCP
MCP -> On-Behalf-Of -> token para API
MCP -> API con token para API
```

El flujo OBO impide que API acepte un token que fue emitido para MCP. La API expone su propio scope y MCP solicita ese scope en nombre del usuario. Para implementarlo correctamente:

1. Registra API y MCP como aplicaciones separadas.
2. Expone un scope de API y otorga a MCP el permiso delegado correspondiente.
3. Configura MCP como cliente confidencial con Managed Identity, certificado o secreto en Key Vault.
4. Valida en API su audiencia propia y el scope requerido.
5. Usa una biblioteca de identidad, como MSAL o Microsoft.Identity.Web, para el canje OBO; no construyas manualmente solicitudes de token.
6. Registra de forma segura el resultado de autorizacion, no el token.

El secreto o certificado de MCP no se distribuye a Cowork. Vive en el entorno de hosting administrado, preferiblemente en Managed Identity y Key Vault.

### OAuth externo, DCR y API keys

- **OAuth 2.0 externo:** registra la aplicacion con el proveedor y guarda la configuracion en Enterprise Token Store. Solicita solo los scopes necesarios; usa `offline_access` cuando se requiera refresh token.
- **Dynamic Client Registration:** es una opcion si el servidor MCP y el cliente la soportan. Verifica el comportamiento en Cowork y no asumas compatibilidad con todas las superficies de Copilot.
- **API keys:** identifican una aplicacion, no un usuario. Nunca se incluyen en skill, manifiesto, ZIP, logs ni prompts. Para integraciones empresariales suelen ser menos adecuadas que Entra SSO, OAuth u OBO.

## Recomendaciones de MCP y API

### Transporte y HTTP

- Usa HTTPS en todo endpoint remoto.
- Implementa Streamable HTTP conforme al protocolo MCP que soporte el cliente objetivo.
- Mantiene `/health` limitado a disponibilidad; no expone configuracion, IDs, modelos ni secretos.
- Configura timeouts en MCP para llamadas a API y en API para llamadas al modelo.
- Aplica limites de tamano a argumentos, respuestas y cargas de archivos.
- Propaga un ID de correlacion, por ejemplo `x-correlation-id`, entre Cowork, MCP, API y gateway.

### Validacion y errores

- Valida la entrada antes de llamar al modelo o a sistemas de negocio.
- No devuelvas stack traces, excepciones de Entra, URI internas ni nombres de recursos al usuario.
- Usa `400` para entrada invalida, `401` para ausencia o invalidez de autenticacion, `403` para permisos insuficientes y `429` para limites de uso.
- Distingue un error de herramienta de una respuesta de negocio negativa. Por ejemplo, "no hay caballero disponible" puede ser una respuesta correcta, no un `500`.

### Proteccion frente a prompt injection y datos

Los argumentos proporcionados por una conversacion no son instrucciones confiables para el servidor. El MCP debe tratar texto de usuario, documentos adjuntos y resultados recuperados como datos no confiables.

- No conviertas texto libre en comandos de shell, SQL o rutas de red.
- Usa listas permitidas, consultas parametrizadas y SDKs estructurados.
- No permitas que una herramienta cambie su propia URL de destino por un parametro de usuario.
- Separa datos de control de instrucciones para el modelo.
- Aplica filtros de clasificacion y DLP antes de devolver datos empresariales al modelo.

## Gobierno, operacion y despliegue

### Observabilidad responsable

Registra:

- ID de correlacion.
- Nombre de herramienta.
- Usuario o subject pseudonimizado cuando la politica lo permita.
- Resultado, latencia, codigo HTTP y motivo de denegacion.

No registres:

- Cabeceras `Authorization`.
- Access tokens, refresh tokens, API keys o secretos.
- Prompts completos, documentos adjuntos o argumentos sensibles por defecto.
- Respuestas con datos personales o empresariales sin una politica de retencion aprobada.

### Publicacion y pilotos

1. Prueba el ZIP con **Only you**.
2. Verifica `initialize`, `tools/list` y `tools/call` en los logs MCP.
3. Publica a un grupo de seguridad piloto desde el centro de administracion Microsoft 365.
4. Documenta propietario, clasificacion de datos, soporte, version y proceso de revocacion.
5. Mide errores, latencia, denegaciones de autorizacion y coste del modelo antes de ampliar la distribucion.

### De Dev Tunnel a produccion

Dev Tunnel es una herramienta de desarrollo. Para un piloto o produccion:

- Aloja API y MCP en servicios HTTPS administrados.
- Usa Managed Identity para acceso a Azure.
- Guarda secretos y certificados en Key Vault.
- Pon APIM o una pasarela equivalente delante de MCP/API para rate limiting, filtros IP, correlacion y proteccion contra abuso.
- Configura alertas, presupuestos, cuotas y retencion de logs.
- Usa despliegues reproducibles e infraestructura como codigo.

### Foundry AI gateway y APIM

Foundry AI gateway y APIM pueden gobernar llamadas a modelos y herramientas MCP. Son utiles para aplicar politicas, limites, telemetria y filtros en un entorno administrado.

No se debe afirmar que Cowork enruta automaticamente un conector MCP por Foundry AI gateway. La ruta depende del endpoint configurado en el manifiesto y de como se haya integrado el gateway. Presenta ese tramo como una integracion separada, valida el endpoint real y comunica las capacidades preview como tales.

## Preparacion comun

### 1. Instalar dependencias

En Linux:

```bash
dotnet --info
devtunnel --version
node --version
python3 --version
```

Se necesita:

- .NET 10 SDK.
- Microsoft Dev Tunnels CLI.
- Bash, `curl` y uno de `jq` o Node.js.
- Uno de `zip` o Python 3 para crear el paquete.

Para PowerShell se necesita `pwsh` y `Compress-Archive`.

### 2. Iniciar sesion en Dev Tunnels

```bash
devtunnel user login
```

La cuenta debe tener el permiso `host`. Una sesion con permisos `create` y `connect`, pero sin `host`, no puede publicar el puerto del MCP.

Si aparece un error de permisos, renueva la sesion:

```bash
devtunnel user logout
devtunnel user login
```

### 3. Crear un tunel persistente una vez

El servidor MCP escucha localmente en el puerto `3001`. Crea un tunel persistente con un puerto HTTP asociado:

```bash
devtunnel create --allow-anonymous
devtunnel port create <TUNNEL_ID> -p 3001 --protocol http
```

Guarda `<TUNNEL_ID>`. Los scripts arrancan `devtunnel host <TUNNEL_ID>` en cada ejecucion y actualizan el manifiesto con la URL HTTPS publicada.

`--allow-anonymous` permite que el servicio Microsoft 365 llegue al tunel. No autentica al usuario y no elimina la necesidad de validar un JWT en MCP o API.

### 4. Crear el archivo local de entorno

```bash
cp script/.env.example script/.env
```

El archivo `.env` esta ignorado por Git. Nunca lo compartas ni lo incluyas en el paquete Cowork.

Para usar Azure OpenAI o Foundry real, completa:

```dotenv
TUNNEL_ID=<persistent-dev-tunnel-id>
AZURE_OPENAI_ENDPOINT=https://<resource>.services.ai.azure.com/openai/v1
AZURE_OPENAI_DEPLOYMENT_NAME=<deployment-name>
AZURE_OPENAI_API_KEY=<api-key>
```

Los scripts inyectan esas variables solo en el proceso API mediante `AzureOpenAI__Endpoint`, `AzureOpenAI__DeploymentName` y `AzureOpenAI__ApiKey`. No almacenes claves en `appsettings.json` ni en el manifiesto del plugin.

Si no se configura un endpoint valido, la API usa el cliente simulado para que la demo local siga siendo funcional.

## Demo 1: plugin sin autenticacion

### Cuando usarla

Usa esta variante para explicar el recorrido de una llamada, depurar herramientas MCP o validar el empaquetado antes de introducir Entra. No la distribuyas a un grupo piloto y no la uses en produccion.

En este modo:

- Cowork puede llegar al MCP a traves del tunel publico.
- MCP acepta llamadas sin bearer token.
- API acepta llamadas sin bearer token.
- El paquete usa `authorization.type: None`.

### Ejecutar la demo anonima

```bash
bash script/setup-anonymous-demo.sh
```

PowerShell:

```powershell
pwsh -File ./script/setup-anonymous-demo.ps1
```

El script:

1. Carga `script/.env`.
2. Inicia API en `http://localhost:5100`.
3. Inicia MCP en `http://localhost:3001`.
4. Comprueba `/health` y `/tools`.
5. Aloja el tunel persistente.
6. Actualiza `cowork-plugin-anonymous/manifest.json` con la URL publica terminada en `/mcp`.
7. Incrementa la version si cambia el paquete y crea un ZIP en `dist/`.

### Cargar y probar el paquete

1. Abre Cowork.
2. Selecciona **Customize** > **Plugins** > **Add plugin**.
3. Selecciona el ZIP `sanctuary-cowork-plugin-anonymous-<version>.zip`.
4. Elige **Only you**.
5. Activa el plugin y formula una peticion alineada con la descripcion de la skill, por ejemplo:

```text
Clasifica una amenaza de energia cosmica detectada en Leo y propone una respuesta.
```

### Validaciones locales

```bash
curl http://localhost:5100/health
curl http://localhost:5100/api/knights
curl http://localhost:3001/health
```

En modo anonimo los endpoints de API y MCP responden sin token.

## Demo 2: plugin con Microsoft Entra SSO

### Resultado esperado

En esta demo se demuestra una cadena de proteccion simple:

```text
Cowork --Bearer token--> MCP --mismo Bearer token--> API
```

MCP y API validan la misma audiencia Entra. La API protege `/api/*`; las rutas `/` y `/health` permanecen anonimas para los checks de los scripts.

Este reenvio del token es una simplificacion adecuada para una demo con un unico recurso de aplicacion. La seccion de OBO explica la opcion recomendada para produccion.

### 1. Crear o reutilizar el registro de aplicacion Entra

1. Abre [Microsoft Entra admin center](https://entra.microsoft.com/).
2. Ve a **App registrations** > **New registration**.
3. Nombre sugerido: `Sanctuary Intelligence MCP`.
4. Selecciona cuentas del directorio actual.
5. Guarda y copia:
   - **Application (client) ID**.
   - **Directory (tenant) ID**.

### 2. Exponer el recurso y el scope delegado

1. En el registro, abre **Expose an API**.
2. Define un Application ID URI, por ejemplo `api://<CLIENT_ID>`.
3. Crea un scope delegado llamado `access_as_user`.
4. Habilita el scope y registra su valor completo:

```text
api://<CLIENT_ID>/access_as_user
```

### 3. Crear la configuracion SSO de Cowork

1. Abre [Teams Developer Portal](https://dev.teams.microsoft.com/tools).
2. Selecciona **Tools** > **Microsoft Entra SSO client ID registration**.
3. Crea un nuevo registro.
4. Indica:
   - Nombre de registro para la demo.
   - URL base publica del MCP, incluyendo `/mcp`.
   - Organizacion permitida.
   - `Any Teams app` durante la prueba privada.
   - Client ID del registro Entra.
   - Scope delegado creado en el paso anterior.
5. Guarda y copia:
   - **Microsoft Entra SSO registration ID**: sera `ENTRA_SSO_REGISTRATION_ID` y el `referenceId` del manifiesto.
   - **Application ID URI** generado: sera `ENTRA_APPLICATION_ID_URI`.

### 4. Finalizar el registro Entra

En el registro de aplicacion Entra:

1. Agrega el Application ID URI generado por Teams a `identifierUris` en el manifiesto de la aplicacion. No elimines URIs existentes.
2. En **Authentication** > plataforma **Web**, agrega el redirect URI:

```text
https://teams.microsoft.com/api/platform/v1.0/oAuthConsentRedirect
```

3. En **Expose an API** > **Add a client application**, agrega el cliente Enterprise Token Store:

```text
ab3be6b7-f5df-413d-ac2d-abf1e3fd9c0b
```

4. Autoriza el scope delegado de la API.

### 5. Completar `.env`

```dotenv
TUNNEL_ID=<persistent-dev-tunnel-id>
ENTRA_TENANT_ID=<tenant-id>
ENTRA_CLIENT_ID=<application-client-id>
ENTRA_APPLICATION_ID_URI=<application-id-uri-generated-by-teams>
ENTRA_SSO_REGISTRATION_ID=<teams-sso-registration-id>
```

No incluyas client secrets ni tokens en este archivo. El flujo de esta demo solo valida bearer tokens; no necesita un secreto de aplicacion.

### 6. Ejecutar la demo SSO

```bash
bash script/setup-entra-sso-demo.sh
```

PowerShell:

```powershell
pwsh -File ./script/setup-entra-sso-demo.ps1
```

El script aplica temporalmente la configuracion Entra a API y MCP, aloja el tunel y crea o reutiliza `sanctuary-cowork-plugin-entra-<version>.zip`.

### 7. Cargar y probar

1. Sube el ZIP SSO desde **Customize** > **Plugins** > **Add plugin** > **Only you**.
2. Activa el plugin.
3. Inicia sesion y concede consentimiento cuando Cowork lo solicite.
4. Ejecuta una herramienta desde Cowork.

Validaciones locales sin token:

```bash
curl -i http://localhost:5100/health
curl -i http://localhost:5100/api/knights
curl -i -X POST http://localhost:3001/mcp \
  -H 'Content-Type: application/json' \
  -d '{"jsonrpc":"2.0","id":1,"method":"tools/list","params":{}}'
```

El resultado esperado es `200` para `/health` y `401` para `/api/knights` y `/mcp`. Al invocar desde Cowork con SSO, MCP valida el token y lo reenvia a la API.

### Diagnostico de SSO

| Sintoma | Comprobacion | Accion |
| --- | --- | --- |
| Cowork muestra error de autenticacion | Revisa `dist/logs/mcp.log`. | Comprueba tenant, client ID, `referenceId` y Application ID URI. |
| `IDX10214` en el log | La firma y expiracion son correctas, pero `aud` no coincide. | Agrega el URI generado por Teams a `identifierUris` y configura la misma audiencia en API y MCP. |
| El tunel no inicia | Dev Tunnel informa que falta scope `host`. | Ejecuta `devtunnel user logout` y `devtunnel user login` con una cuenta autorizada. |
| API devuelve 401 desde MCP | API protege `/api/*`, pero no recibe bearer. | Confirma que el MCP usa el handler de reenvio y que ambos servicios aceptan la misma audiencia. |
| API devuelve 500 al iniciar SSO | El log indica que falta `Instance`. | Asegura `https://login.microsoftonline.com/` en la configuracion Entra de API. |

## Produccion: API separada y On-Behalf-Of

El reenvio del mismo token sirve para la demo, pero no es la arquitectura recomendada cuando MCP y API son recursos de seguridad independientes.

En produccion, usa tres identidades:

```text
Cowork token para MCP
       |
       v
MCP valida el token y usa OBO
       |
       v
Token especifico para API
       |
       v
API valida su propia audiencia y scopes
```

### Configuracion recomendada

1. Registra MCP como una aplicacion o recurso Entra.
2. Registra API como otra aplicacion y expone scopes propios, por ejemplo `api://<API_CLIENT_ID>/access_as_user`.
3. Da a MCP permisos delegados sobre la API.
4. Configura la API para aceptar solo su Application ID URI, validar scopes y exigir autorizacion.
5. Configura MCP como cliente confidencial con certificado preferiblemente, o secreto guardado en Key Vault.
6. Cuando llega el token de Cowork, MCP usa el flujo OBO para solicitar un token para el scope de API.
7. MCP llama a API con el token OBO, no con el token original de Cowork.

Ventajas de OBO:

- La API tiene una audiencia propia y no acepta tokens destinados al MCP.
- Los permisos son de minimo privilegio y se pueden revocar de forma independiente.
- La API puede auditar el usuario delegado y el cliente MCP.
- Es mas facil separar ciclos de vida, roles y gobierno de cada servicio.

No envíes el client secret al paquete Cowork, a la skill, al manifiesto ni al navegador. Usa Managed Identity cuando MCP este alojado en Azure, o un certificado/secret gestionado en Key Vault cuando sea imprescindible.

## OAuth, API keys y Enterprise Token Store

### Microsoft Entra SSO

Es la opcion preferida para recursos corporativos Microsoft Entra. Cowork usa Enterprise Token Store y el manifiesto referencia un `referenceId`; no contiene secretos.

### OAuth 2.0

Usalo para proveedores externos. La configuracion OAuth tambien se almacena en Enterprise Token Store. Si el proveedor requiere renovacion, solicita `offline_access` junto con los scopes necesarios.

### API keys

Una API key no representa a un usuario. No se debe incluir en `manifest.json`, `SKILL.md`, ZIP ni logs. La documentacion de Cowork indica que el soporte para API key en el vault puede tener limitaciones; para servicios empresariales prefiere Entra SSO, OAuth o una pasarela gestionada.

### Dynamic Client Registration

Si el servidor MCP implementa DCR, Cowork puede crear un cliente OAuth en su nombre. Confirma el soporte del cliente y del servidor antes de elegirlo; no asumas que una configuracion valida para Cowork funciona de igual modo en otras superficies de Copilot.

## Azure OpenAI, Foundry y AI Gateway

La API de la demo puede llamar directamente a Azure OpenAI o Microsoft Foundry mediante el endpoint OpenAI-compatible. Dev Tunnel solo es para desarrollo local.

Para produccion, considera:

- Alojar API y MCP en Azure con Managed Identity.
- Guardar secretos y certificados en Key Vault.
- Usar APIM para rate limiting, validacion, correlacion, filtros IP y politicas de datos.
- Aplicar cuotas y presupuestos al recurso de IA.
- Registrar telemetria sin prompts, tokens ni argumentos sensibles por defecto.

Microsoft Foundry AI gateway y APIM pueden gobernar herramientas MCP y llamadas a modelos, pero no se debe afirmar que Cowork enruta automaticamente su conector MCP por ese gateway. Trata ese tramo como una demo separada, valida el endpoint real y comunica cualquier capacidad preview como tal.

## Gobierno y seguridad

Antes de un piloto:

1. Limita la distribucion del plugin a un grupo de seguridad piloto.
2. Expone solo herramientas necesarias y de bajo riesgo; empieza con lectura.
3. Valida entrada de cada herramienta y devuelve salidas estructuradas.
4. No registres `Authorization`, API keys, prompts sensibles ni datos personales.
5. Usa IDs de correlacion entre Cowork, MCP, API y APIM.
6. Define rate limits, timeout, reintentos y limites de tamano de payload.
7. Mantiene la skill breve y mueve detalles a referencias versionadas.
8. Revisa Conditional Access, Information Barriers, DLP y politicas del tenant antes de publicar.
9. Rota claves expuestas y elimina secretos de archivos versionados.
10. Mantiene Dev Tunnel solo para desarrollo; usa hosting HTTPS administrado para piloto y produccion.

## Detener la demo

```bash
bash script/setup-anonymous-demo.sh --stop
bash script/setup-entra-sso-demo.sh --stop
```

PowerShell:

```powershell
pwsh -File ./script/setup-anonymous-demo.ps1 -Stop
pwsh -File ./script/setup-entra-sso-demo.ps1 -Stop
```

La parada conserva el tunel persistente, los ZIP, los logs y `.env`, pero detiene API, MCP y el host de Dev Tunnel.

## Fuentes oficiales

- [Build plugins for Copilot Cowork](https://learn.microsoft.com/microsoft-365/copilot/cowork/cowork-plugin-development)
- [Manage plugins for Copilot Cowork](https://learn.microsoft.com/microsoft-365/copilot/cowork/cowork-manage-plugins)
- [Configure Microsoft Entra SSO authentication](https://learn.microsoft.com/microsoft-365/copilot/extensibility/plugin-authentication-entra-sso)
- [Configure authentication for MCP and API plugins](https://learn.microsoft.com/microsoft-365/copilot/extensibility/plugin-authentication)
- [Debug MCP and API plugins locally](https://learn.microsoft.com/microsoft-365/copilot/extensibility/plugin-debug-local)
- [Microsoft identity platform: On-Behalf-Of flow](https://learn.microsoft.com/entra/identity-platform/v2-oauth2-on-behalf-of-flow)
- [Azure AI Foundry AI gateway](https://learn.microsoft.com/azure/ai-foundry/ai-gateway/overview)