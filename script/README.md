# Scripts de demo local

`prepare-cowork-plugin.sh` y `prepare-cowork-plugin.ps1` preparan una prueba de Copilot Cowork contra el MCP local. Ambos scripts realizan el mismo flujo:

1. Inician la API en `http://localhost:5100` y esperan a que responda `GET /health`.
2. Inician el servidor MCP en `http://localhost:3001` y validan `GET /health` y `GET /tools`.
3. Inician un Microsoft Dev Tunnel persistente ya creado para el puerto `3001`.
4. Obtienen la URL HTTPS publica del tunel y actualizan el `mcpServerUrl` del manifiesto elegido.
5. Incrementan la version de parche del manifiesto y crean un ZIP del plugin en `dist/`.
6. Guardan los logs y los identificadores de proceso en `dist/logs/` para poder detenerlos despues.

`setup-anonymous-demo.sh` y `setup-entra-sso-demo.sh` son los puntos de entrada recomendados en Linux. `setup-anonymous-demo.ps1` y `setup-entra-sso-demo.ps1` son sus equivalentes para PowerShell. Preparan respectivamente la demostracion anonima y la demostracion con Entra SSO, y seleccionan el paquete correcto.

## Requisitos

- .NET 10 SDK.
- Microsoft Dev Tunnels CLI, con una sesion iniciada mediante `devtunnel user login`.
- Un tunel persistente creado con acceso anonimo y un puerto HTTP `3001`:

  ```bash
  devtunnel create --allow-anonymous
  devtunnel port create <TUNNEL_ID> -p 3001 --protocol http
  ```

- Bash: `curl`, uno de estos procesadores JSON: `jq` o Node.js, y uno de estos empaquetadores: `zip` o Python 3.
- PowerShell: `pwsh` y `Compress-Archive` disponibles.

El tunel permite el acceso de Microsoft 365 al servicio local, pero no reemplaza la autenticacion del MCP.

## Archivo de entorno

Copia [`.env.example`](.env.example) como `.env` en este directorio y reemplaza sus placeholders:

```bash
cp script/.env.example script/.env
```

`.env` esta excluido de Git. Contiene identificadores del tunel y de Entra y puede contener la API key local de Azure OpenAI; no lo compartas ni lo subas al repositorio. Puedes indicar otra ruta mediante `SANCTUARY_DEMO_ENV_FILE`.

Para usar Azure OpenAI real, completa tambien `AZURE_OPENAI_ENDPOINT`, `AZURE_OPENAI_DEPLOYMENT_NAME` y `AZURE_OPENAI_API_KEY` en `.env`. Los scripts entregan estos valores solo al proceso API mediante variables de entorno; `src/SanctuaryIntelligence.Api/appsettings.json` no contiene claves ni valores de despliegue.

## Plugin anonimo

Usa `cowork-plugin-anonymous/` y el manifiesto con `authorization.type` igual a `None`. Antes de ejecutarlo, configura `Authentication:Enabled` en `false` dentro de `src/SanctuaryIntelligence.Mcp/appsettings.json`.

```bash
bash script/setup-anonymous-demo.sh
```

```powershell
pwsh -File ./script/setup-anonymous-demo.ps1
```

Este modo es exclusivamente para desarrollo y demostracion privada.

## Plugin Entra SSO

Usa `cowork-plugin/` y `OAuthPluginVault`. Antes de ejecutarlo, configura `Authentication:Enabled` en `true` y proporciona el identificador de la configuracion SSO creada en Teams Developer Portal.

```bash
bash script/setup-entra-sso-demo.sh
```

```powershell
pwsh -File ./script/setup-entra-sso-demo.ps1
```

El `referenceId` del manifiesto es el **Microsoft Entra SSO registration ID**, no el client ID de la aplicacion de Entra.

El script SSO requiere que el registro de aplicacion, el Application ID URI, el redirect URI y la configuracion SSO en Teams Developer Portal ya existan. El ultimo paso genera `ENTRA_SSO_REGISTRATION_ID`; el script valida que este valor se haya indicado en `.env` antes de iniciar la demo.

Los cuatro preparadores aceptan un archivo de entorno alternativo. En Bash usa `SANCTUARY_DEMO_ENV_FILE=/ruta/.env`; en PowerShell usa `-EnvFile /ruta/.env`.

## Resultado y limpieza

Cada ejecucion genera un archivo versionado, como `dist/sanctuary-cowork-plugin-entra-1.0.1.zip` o `dist/sanctuary-cowork-plugin-anonymous-1.0.1.zip`. El manifiesto elegido se actualiza con la URL del tunel y la siguiente version de parche.

Los scripts de preparacion son idempotentes: si la URL publica del tunel, el `referenceId`, los archivos del paquete y el ZIP de la version actual ya coinciden, reutilizan el ZIP existente. Si se actualiza la URL del tunel, la configuracion SSO o cualquier archivo del paquete, actualizan el manifiesto y generan un ZIP con una nueva version de parche.

Para detener los procesos iniciados por los scripts:

```bash
bash script/prepare-cowork-plugin.sh --stop
bash script/setup-anonymous-demo.sh --stop
bash script/setup-entra-sso-demo.sh --stop
```

```powershell
pwsh -File ./script/prepare-cowork-plugin.ps1 -Stop
pwsh -File ./script/setup-anonymous-demo.ps1 -Stop
pwsh -File ./script/setup-entra-sso-demo.ps1 -Stop
```

El MCP solo puede estar en un modo de autenticacion por ejecucion. Detenlo y reinicialo despues de cambiar `Authentication:Enabled`; no instales ambos plugins contra la misma instancia durante la demo.