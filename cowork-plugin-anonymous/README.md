# Plugin Cowork del Oraculo del Santuario: demo local

Este paquete es la variante sin autenticacion para una demostracion privada con Microsoft Dev Tunnels. Requiere que `Authentication:Enabled` sea `false` en `src/SanctuaryIntelligence.Mcp/appsettings.json`.

No lo distribuyas a un grupo piloto ni lo uses en produccion. Para esas situaciones, usa el paquete SSO en `cowork-plugin/`.

El script de demo actualiza la URL, incrementa la version y crea el ZIP:

```bash
bash script/prepare-cowork-plugin.sh --plugin anonymous --tunnel-id <TUNNEL_ID>
```