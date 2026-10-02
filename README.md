# Extender Copilot Cowork con plugins y MCP

Material de preparacion para una charla sobre extensibilidad de Microsoft 365 Copilot Cowork con skills, conectores remotos MCP, Microsoft Entra SSO y Azure OpenAI.

Consulta la [arquitectura y el alcance](docs/01-arquitectura-y-alcance.md), el [runbook de demo](docs/02-runbook-demo.md), los [controles de seguridad](docs/03-seguridad-y-gobierno.md) y las [fuentes de Learn](docs/04-fuentes-learn.md).

## Arquitectura de la demo

```text
Copilot Cowork
    |
    +-- skill Sanctuary Intelligence
    |
    +-- conector MCP remoto HTTPS
              |
              +-- Dev Tunnel local
                        |
                        +-- Sanctuary Intelligence MCP :3001
                                  |
                                  +-- Sanctuary Intelligence API :5100
                                            |
                                            +-- Azure OpenAI / Foundry
```

El MCP implementa Streamable HTTP en `/mcp`, descubre cinco herramientas y llama a la API local. La API usa Azure OpenAI cuando se configura y utiliza el cliente simulado cuando no hay endpoint.

## Dos variantes del plugin

| Variante | Paquete | Autenticacion | Uso |
| --- | --- | --- | --- |
| Anonima | `cowork-plugin-anonymous/` | Sin JWT | Desarrollo y demo privada local. |
| Entra SSO | `cowork-plugin/` | JWT de Microsoft Entra | Demo segura y base para un piloto. |

Cada paquete tiene un manifiesto, identidad e iconos propios. Ambos incluyen la skill `sanctuary-intelligence` y un descriptor estatico de herramientas en `tools/sanctuary-tools.json`, requerido por el validador de Cowork.

## Preparacion inicial

### Dependencias

- .NET 10 SDK.
- Microsoft Dev Tunnels CLI y una sesion iniciada: `devtunnel user login`.
- Bash: `curl`, `jq` o Node.js, y `zip` o Python 3.
- PowerShell: `pwsh` y `Compress-Archive`.

### Crear el tunel persistente

El tunel se crea una sola vez. Los scripts lo alojan automaticamente en cada ejecucion.

```bash
devtunnel create --allow-anonymous
devtunnel port create <TUNNEL_ID> -p 3001 --protocol http
```

El acceso anonimo del tunel solo permite que Microsoft 365 alcance el servicio local. No sustituye la autenticacion Entra del MCP ni de la API.

### Configurar el entorno local

```bash
cp script/.env.example script/.env
```

Completa en `script/.env` el `TUNNEL_ID`. Para Azure OpenAI real, configura tambien:

```dotenv
AZURE_OPENAI_ENDPOINT=https://<resource>.services.ai.azure.com/openai/v1
AZURE_OPENAI_DEPLOYMENT_NAME=<deployment-name>
AZURE_OPENAI_API_KEY=<api-key>
```

El archivo `.env` esta ignorado por Git. Puede contener la API key local, asi que no debe compartirse ni añadirse al repositorio. Los scripts la inyectan solo en el proceso API; `appsettings.json` no contiene secretos ni valores de Azure OpenAI.

## Ejecutar la demo anonima

```bash
bash script/setup-anonymous-demo.sh
```

```powershell
pwsh -File ./script/setup-anonymous-demo.ps1
```

Este flujo inicia API, MCP y Dev Tunnel, actualiza el manifiesto anonimo y crea o reutiliza un ZIP versionado en `dist/`. API y MCP quedan abiertos para permitir la demo sin SSO.

Sube el ZIP `sanctuary-cowork-plugin-anonymous-<version>.zip` en Cowork desde **Customize** > **Plugins** > **Add plugin** > **Only you**.

## Ejecutar la demo con Entra SSO

Antes de ejecutar, crea la configuracion SSO en Teams Developer Portal o Agents Toolkit y completa en `script/.env`:

```dotenv
ENTRA_TENANT_ID=<tenant-id>
ENTRA_CLIENT_ID=<application-client-id>
ENTRA_APPLICATION_ID_URI=<application-id-uri-generated-by-teams>
ENTRA_SSO_REGISTRATION_ID=<teams-sso-registration-id>
```

En el registro de aplicacion Entra debes:

1. Agregar el Application ID URI generado por Teams a `identifierUris`.
2. Agregar `https://teams.microsoft.com/api/platform/v1.0/oAuthConsentRedirect` como redirect URI web.
3. Autorizar el cliente Enterprise Token Store `ab3be6b7-f5df-413d-ac2d-abf1e3fd9c0b` para el scope delegado de la API.

Ejecuta el flujo:

```bash
bash script/setup-entra-sso-demo.sh
```

```powershell
pwsh -File ./script/setup-entra-sso-demo.ps1
```

En este modo, Cowork entrega un token Bearer al MCP. MCP y API validan la misma audiencia Entra, la API exige autenticacion en `/api/*` y el MCP reenvia el bearer a la API. Las rutas `/` y `/health` permanecen anonimas para los health checks locales.

Sube el ZIP `sanctuary-cowork-plugin-entra-<version>.zip` en Cowork y completa el consentimiento al activarlo.

## Comandos de control

Los scripts `setup-*` son los puntos de entrada. Internamente usan `prepare-cowork-plugin` para iniciar servicios, alojar el tunel, actualizar el manifiesto y crear el ZIP.

```bash
bash script/setup-anonymous-demo.sh --stop
bash script/setup-entra-sso-demo.sh --stop
```

```powershell
pwsh -File ./script/setup-anonymous-demo.ps1 -Stop
pwsh -File ./script/setup-entra-sso-demo.ps1 -Stop
```

La parada detiene API, MCP y Dev Tunnel sin eliminar el tunel persistente, los ZIP ni el archivo `.env`.

Consulta [script/README.md](script/README.md) para requisitos, comportamiento idempotente, logs y uso de un archivo de entorno alternativo.

## Punto de rigor

Microsoft Learn documenta Cowork como un paquete Microsoft 365 con `agentSkills` y, opcionalmente, `agentConnectors.remoteMcpServer`. No se debe afirmar que Cowork enruta automaticamente estos conectores mediante Foundry AI gateway: esa integracion requiere una validacion practica separada y se considera un tramo preview de la charla.