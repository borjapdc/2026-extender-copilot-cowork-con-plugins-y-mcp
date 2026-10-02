# Extender Copilot Cowork con plugins y MCP

Material de preparacion para la charla. Se basa exclusivamente en Microsoft Learn consultado el 2026-09-23.

## Punto de rigor

La arquitectura que Microsoft Learn documenta para Cowork es un paquete M365 con `agentSkills` y, opcionalmente, `agentConnectors.remoteMcpServer`. Cowork llama al endpoint MCP remoto declarado en el manifiesto.

Microsoft Foundry AI gateway es una capacidad de vista previa para herramientas MCP creadas en Foundry. La documentacion no afirma que un conector de Cowork se enrute automaticamente a traves de ese gateway. Por tanto, no se debe presentar esa afirmacion en la charla sin una validacion practica en el tenant.

Consulta [la arquitectura y la decision de demo](docs/01-arquitectura-y-alcance.md), el [paso a paso](docs/02-runbook-demo.md), los [controles de seguridad y gobierno](docs/03-seguridad-y-gobierno.md) y las [notas trazables de Learn](docs/04-fuentes-learn.md).

## Resultado demostrable recomendado

1. Un servidor MCP HTTPS propio que implemente Streamable HTTP, JSON-RPC 2.0, `initialize`, `tools/list` y `tools/call`.
2. Un paquete Cowork que combine una skill con ese conector remoto.
3. Microsoft Entra SSO u OAuth para cada usuario, configurado en Enterprise Token Store.
4. Prueba privada del paquete desde Cowork y despues despliegue a un grupo piloto.
5. Un segundo tramo, claramente etiquetado como preview y separado, que muestre la gobernanza de la misma clase de herramienta MCP en Foundry AI gateway/APIM.

No hay secretos, IDs, URLs, iconos ni recursos Azure reales en este repositorio.

## Material ejecutable para la demo

- `src/SanctuaryIntelligence.Api`: API .NET 10 con datos sinteticos y un cliente Azure OpenAI simulado cuando no se configuran credenciales.
- `src/SanctuaryIntelligence.Mcp`: servidor MCP Streamable HTTP en `/mcp` que expone cinco herramientas del Oraculo.
- `http/`: peticiones para validar primero la API y despues el contrato MCP localmente.
- `cowork-plugin/`: paquete Cowork con Entra SSO. Antes de empaquetarlo, hay que configurar su URL HTTPS y el `referenceId`; consulta su [guia](cowork-plugin/README.md).
- `cowork-plugin-anonymous/`: paquete Cowork sin autenticacion, solo para la demo privada local con Dev Tunnels.

### Usar Microsoft Foundry real (sin mock)

La API usa `SanctuaryMockChatClient` solo si `AzureOpenAI:Endpoint` esta vacio, contiene `YOUR-RESOURCE` o no es una URI valida. Para usar un despliegue de Microsoft Foundry, configura `AzureOpenAI:Endpoint` con el endpoint del recurso o proyecto Foundry, `AzureOpenAI:DeploymentName` con el nombre exacto del despliegue y proporciona `AzureOpenAI:ApiKey` mediante variable de entorno (`AzureOpenAI__ApiKey`) o Secret Manager. La API agrega automaticamente `/openai/v1/` si no esta incluido en el endpoint.

Tambien admite autenticacion sin clave mediante `DefaultAzureCredential` cuando no se proporciona `AzureOpenAI:ApiKey`. La identidad debe disponer de permisos de inferencia sobre el recurso Foundry. Reinicia la API despues de cambiar la configuracion.

El MCP no simula las respuestas: sus herramientas llaman a la API indicada por `SanctuaryApi:BaseUrl` (por defecto, `http://localhost:5100`). Arranca la API configurada antes que el MCP y reinicia ambos servicios tras cambiar la configuracion.

Para ejecutar la demo local:

```bash
dotnet run --project src/SanctuaryIntelligence.Api
dotnet run --project src/SanctuaryIntelligence.Mcp
```

### Probar Cowork localmente con Microsoft Dev Tunnels

Para una prueba privada desde Copilot Cowork, Microsoft Dev Tunnels puede publicar el MCP local con una URL HTTPS. Es una opcion de desarrollo y demo; no sustituye un despliegue para piloto o produccion.

Con la API y el MCP en ejecucion, crea un tunel persistente para el puerto HTTP del MCP (`3001`):

```bash
# Solo una vez en Linux
curl -sL https://aka.ms/DevTunnelCliInstall | bash

devtunnel user login
devtunnel create --allow-anonymous
devtunnel port create <TUNNEL_ID> -p 3001 --protocol http
```

El identificador persistente se necesita tambien para los scripts de empaquetado. Arranca el host con `devtunnel host <TUNNEL_ID>` o deja que los scripts lo hagan. Al iniciarlo, copia la URL HTTPS publicada y asignala a `mcpServerUrl` en `cowork-plugin/manifest.json`, con la ruta `/mcp`:

```json
"mcpServerUrl": "https://<tunnel-id>.devtunnels.ms:3001/mcp"
```

El acceso anonimo del tunel permite a Microsoft 365 llegar al servidor local; no autentica al usuario ni sustituye la proteccion del MCP. Para la primera demo se puede mantener `Authentication:Enabled` en `false`. Para usar el conector de Cowork con `OAuthPluginVault`, configura despues Entra SSO y reemplaza tambien el `referenceId` del manifiesto.

Antes de la primera prueba, abre la URL de conexion que muestra `devtunnel host` en el navegador y selecciona **Continue** para habilitar el tunel. Al terminar la prueba, detiene el host con `Ctrl+C`. Consulta la guia oficial: [Debug MCP and API plugins locally](https://learn.microsoft.com/microsoft-365/copilot/extensibility/plugin-debug-local).

### Configurar Microsoft Entra SSO para Cowork

Esta es la opcion recomendada para que Cowork identifique al usuario y el MCP valide su token JWT. El `referenceId` no es el identificador de la aplicacion de Entra: es el identificador de la configuracion SSO que se crea en Teams Developer Portal y se almacena en Enterprise Token Store.

1. Crea primero el tunel persistente de la seccion anterior y ejecuta `devtunnel host <TUNNEL_ID>` para obtener una URL publica estable. El endpoint MCP sera `https://<tunnel-id>.devtunnels.ms:3001/mcp`.
2. En [Microsoft Entra admin center](https://entra.microsoft.com/), crea un registro de aplicacion de cuenta de un solo inquilino, por ejemplo `Sanctuary Intelligence MCP`. Copia su **Application (client) ID** y el **Directory (tenant) ID**.
3. En **Expose an API**, crea un scope delegado, por ejemplo `access_as_user`, y anota su valor completo, normalmente `api://<CLIENT_ID>/access_as_user`.
4. En [Teams Developer Portal](https://dev.teams.microsoft.com/tools), abre **Tools** > **Microsoft Entra SSO client ID registration** > **New client registration**. Indica el nombre de la demo, el endpoint MCP publico como **Base URL**, tu organizacion, `Any Teams app` para la prueba, el client ID de Entra y el scope creado. Al guardar, copia:
	- **Microsoft Entra SSO registration ID**: es el valor de `referenceId`.
	- **Application ID URI**: se debe permitir tanto en Entra como en el MCP.
5. Vuelve al registro de aplicacion en Entra:
	- Agrega el **Application ID URI** generado a `identifierUris` mediante el editor de manifiesto de la aplicacion.
	- En **Authentication** > plataforma **Web**, agrega `https://teams.microsoft.com/api/platform/v1.0/oAuthConsentRedirect` como redirect URI.
	- En **Expose an API** > **Add a client application**, agrega `ab3be6b7-f5df-413d-ac2d-abf1e3fd9c0b`, el client ID de Microsoft Enterprise Token Store, y autoriza el scope delegado.
6. Configura [src/SanctuaryIntelligence.Mcp/appsettings.json](src/SanctuaryIntelligence.Mcp/appsettings.json) y reinicia el MCP:

	```json
	"Authentication": {
	  "Enabled": true,
	  "Instance": "https://login.microsoftonline.com/",
	  "TenantId": "<TENANT_ID>",
	  "ClientId": "<MCP_APP_CLIENT_ID>",
	  "Audience": "<APPLICATION_ID_URI>",
	  "Audiences": ["<APPLICATION_ID_URI>"]
	}
	```

7. Genera el paquete con el tunel persistente y el ID SSO. El script valida los servicios, actualiza `mcpServerUrl` y `referenceId`, incrementa la version de parche y crea el ZIP en `dist/`:

	```bash
	bash script/prepare-cowork-plugin.sh \
	  --tunnel-id <TUNNEL_ID> \
	  --auth-config-id <MICROSOFT_ENTRA_SSO_REGISTRATION_ID>
	```

8. En Cowork, usa **Customize** > **Plugins** > **Add plugin** > **Only you**, sube el ZIP y completa el consentimiento solicitado. Para detener los procesos locales al finalizar:

	```bash
	bash script/prepare-cowork-plugin.sh --stop
	```

`--allow-anonymous` solo permite a Microsoft 365 alcanzar el puerto del tunel. Con `Authentication:Enabled` en `true`, el MCP sigue requiriendo el token de Entra de cada usuario. Consulta la guia oficial: [Configure Microsoft Entra SSO authentication](https://learn.microsoft.com/microsoft-365/copilot/extensibility/plugin-authentication-entra-sso).

### Elegir el paquete de la demo

Los dos paquetes tienen la misma skill, pero usan identidades de aplicacion y mecanismos de autorizacion distintos. Instala solo uno en Cowork para cada tramo de la demo.

| Tramo | Paquete | Configuracion del MCP | Comando de empaquetado |
| --- | --- | --- | --- |
| Local sin autenticacion | `cowork-plugin-anonymous/` | `Authentication:Enabled: false` | `bash script/prepare-cowork-plugin.sh --plugin anonymous --tunnel-id <TUNNEL_ID>` |
| Entra SSO | `cowork-plugin/` | `Authentication:Enabled: true` | `bash script/prepare-cowork-plugin.sh --plugin entra --tunnel-id <TUNNEL_ID> --auth-config-id <SSO_REGISTRATION_ID>` |

El servidor MCP actual expone el mismo endpoint `/mcp` en ambos casos. Por ello, detenlo y reinicialo despues de cambiar `Authentication:Enabled`; no pruebes los dos paquetes simultaneamente contra la misma instancia. La variante anonima es exclusivamente para desarrollo y demostracion privada.