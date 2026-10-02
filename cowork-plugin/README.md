# Plugin Cowork del Oraculo del Santuario con Entra SSO

Este paquete conecta Copilot Cowork con el servidor MCP de `src/SanctuaryIntelligence.Mcp` y activa la skill `sanctuary-intelligence`.

Antes de empaquetarlo:

1. Publica el servidor MCP en HTTPS y sustituye `mcpServerUrl` en `manifest.json` por su URL terminada en `/mcp`.
2. Crea la configuracion Entra SSO en Teams Developer Portal o Agents Toolkit y sustituye `referenceId` por su identificador.
3. Configura el registro de aplicacion y el servidor segun [el runbook](../docs/02-runbook-demo.md).
4. Copia `color.png` y `outline.png` al directorio de este paquete.

Para crear el ZIP, su contenido debe quedar en la raiz del archivo:

```bash
mkdir -p dist
cd cowork-plugin
zip -r ../dist/sanctuary-cowork-plugin.zip .
```

El script actualiza la URL, incrementa la version y crea el ZIP:

```bash
bash script/prepare-cowork-plugin.sh \
	--plugin entra \
	--tunnel-id <TUNNEL_ID> \
	--auth-config-id <MICROSOFT_ENTRA_SSO_REGISTRATION_ID>
```

Pruebalo primero desde Cowork con **Customize** > **Plugins** > **Add plugin** > **Only you**.