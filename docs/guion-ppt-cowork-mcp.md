# Guion de PPT: Extender Copilot Cowork con plugins y MCP

## Estructura

La presentacion tiene 13 diapositivas en total:

- 1 portada.
- 10 diapositivas de contenido.
- 1 diapositiva de demo practica.
- 1 cierre.

El relato sigue este orden: **por que** extender Cowork, **que** piezas forman la extension, **como** construirlas, **como** protegerlas y **como** llevarlas a un entorno gobernado.

---

## Portada

### Titulo en pantalla

**Extender Copilot Cowork con plugins y MCP: skills, conectores**

### Subtitulo

De una intencion en lenguaje natural a herramientas empresariales gobernadas.

### Mensaje para contar

Cowork puede convertirse en una interfaz para procesos y datos empresariales. La clave no es anadir un chat mas: es conectar Copilot con capacidades reales manteniendo contratos, identidad, autorizacion y gobierno.

### Imagen propuesta

Una composicion limpia con tres elementos conectados: Copilot Cowork, un conector MCP y un sistema empresarial. Evitar iconografia generica de IA; usar iconos oficiales de Microsoft 365, Azure y una API.

---

## Slide 1. Por que extender Cowork

### Titulo en pantalla

**Copilot conoce el contexto; las herramientas ejecutan el trabajo**

### Puntos en pantalla

- Las respuestas generativas no sustituyen datos ni reglas de negocio.
- Los usuarios trabajan donde ya colaboran: Microsoft 365 Copilot.
- Un plugin conecta la intencion con herramientas y sistemas corporativos.
- La extension debe respetar identidad, permisos y gobierno existentes.

### Mensaje para contar

El modelo interpreta una peticion, pero no debe inventar el estado de un pedido, el riesgo de una amenaza o una recomendacion operativa. Para eso necesita herramientas con contratos claros. Cowork reduce el cambio de contexto del usuario; MCP reduce el acoplamiento entre el cliente y los sistemas empresariales.

### Imagen propuesta

Diagrama de antes/despues: usuario alternando entre varias aplicaciones frente a usuario en Cowork con una unica invocacion hacia herramientas.

---

## Slide 2. Que construimos

### Titulo en pantalla

**Un plugin es skill + conector + contrato de herramientas**

### Puntos en pantalla

- `SKILL.md`: cuando y como usar una capacidad.
- `manifest.json`: identidad, conector y autenticacion.
- `mcpToolDescription`: catalogo empaquetado de herramientas.
- MCP: transporte, validacion y orquestacion.
- API: reglas de negocio, datos y modelo.

### Mensaje para contar

Estas capas tienen responsabilidades distintas. La skill guia a Copilot; no contiene secretos ni ejecuta codigo. El manifiesto declara el plugin. MCP implementa un protocolo de herramientas. La API conserva la logica de negocio. Separarlas permite evolucionar sin convertir un prompt en una dependencia de infraestructura.

### Imagen propuesta

Arquitectura en capas horizontales: Skill, Manifest/Connector, MCP, API, Datos/Modelo. Cada capa con una frase de responsabilidad.

---

## Slide 3. Como viaja una peticion

### Titulo en pantalla

**Del lenguaje natural al resultado verificable**

### Puntos en pantalla

1. El usuario formula una intencion en Cowork.
2. La skill orienta la eleccion de herramienta.
3. Cowork invoca el conector MCP remoto.
4. MCP valida parametros y llama a la API.
5. La API consulta datos o invoca Azure OpenAI.
6. El resultado estructurado vuelve a Cowork.

### Mensaje para contar

No es una llamada directa del modelo a una base de datos. Hay un contrato en cada salto. El MCP ofrece `initialize`, `tools/list` y `tools/call`; la API aplica reglas de negocio; la respuesta vuelve con datos que Cowork puede explicar al usuario.

### Imagen propuesta

Diagrama de secuencia con seis flechas numeradas. Destacar el retorno de JSON estructurado desde la herramienta, no solo texto libre.

---

## Slide 4. Como disenar una buena skill y buenas tools

### Titulo en pantalla

**Intenciones claras, schemas estrictos, acciones pequenas**

### Puntos en pantalla

- Nombres de tool con verbo: `classify_threat`, `recommend_knight`.
- `inputSchema` con tipos, descripciones y campos obligatorios.
- Separar lectura de acciones con efecto.
- Pedir el dato que falta, no inventarlo.
- Devolver errores accionables y JSON estructurado.

### Mensaje para contar

Una tool no es un agujero para ejecutar cualquier cosa. Debe hacer una operacion concreta y validada. La skill debe aclarar que parametros son imprescindibles y cuando pedirlos. La autorizacion real se aplica en MCP o API, nunca se deduce solo de una instruccion en lenguaje natural.

### Imagen propuesta

Comparativa visual: una tool generica `run_command` tachada frente a tres tools pequenas con schema y permisos diferenciados.

---

## Slide 5. Demo sin autenticacion: aprender la arquitectura

### Titulo en pantalla

**Primero validamos el camino feliz local**

### Puntos en pantalla

- Plugin anonimo con `authorization.type: None`.
- Dev Tunnel publica MCP localmente por HTTPS.
- API y MCP aceptan llamadas sin JWT.
- Sirve para depurar packaging, tools y respuestas.
- No es apto para piloto ni produccion.

### Mensaje para contar

La variante anonima reduce friccion para una demo privada. Dev Tunnel solo resuelve conectividad HTTPS; no autentica al usuario. Es util para comprobar que Cowork descubre las herramientas, pero deja claro que un endpoint publico sin identidad no debe convertirse en una arquitectura empresarial.

### Imagen propuesta

Diagrama local: Cowork -> Dev Tunnel -> MCP -> API. Poner un aviso visible: "Solo desarrollo/demo privada".

---

## Slide 6. SSO: identidad de Cowork hasta la API

### Titulo en pantalla

**El usuario no desaparece al cruzar el conector**

### Puntos en pantalla

- Entra SSO se configura en Teams Developer Portal.
- Enterprise Token Store guarda la configuracion; el manifiesto usa `referenceId`.
- Cowork entrega un bearer token al MCP.
- MCP y API validan issuer, tenant, audiencia y expiracion.
- La API protege `/api/*`; `/health` permanece anonimo.

### Mensaje para contar

`referenceId` no es un client ID, un secreto ni un token: identifica una configuracion almacenada por Microsoft. En la demo, MCP y API usan la misma audiencia. Una llamada sin token devuelve `401`; una llamada desde Cowork usa la identidad del usuario autenticado.

### Imagen propuesta

Diagrama de seguridad: Usuario -> Cowork -> token JWT -> MCP -> token JWT -> API. Anotar `aud`, `scp`, `iss` y `tid` sobre el token.

---

## Slide 7. Autenticacion no es autorizacion

### Titulo en pantalla

**Un JWT valido no concede acceso ilimitado**

### Puntos en pantalla

- Autenticacion: quien llama.
- Autorizacion: que puede hacer.
- Validar `iss`, `aud`, `exp`, tenant, scopes y roles.
- Aplicar reglas de dominio en la API.
- Usar `401` para token ausente/invalido y `403` para permiso insuficiente.

### Mensaje para contar

El error comun es validar un token y tratarlo como una autorizacion total. La API debe comprobar scopes o roles y despues aplicar reglas del dominio: cliente, region, clasificacion de datos o nivel de riesgo. Los errores de audiencia, como `IDX10214`, se corrigen en el registro Entra, no desactivando la validacion.

### Imagen propuesta

Una puerta con dos controles consecutivos: identidad JWT y politica de negocio. Usar etiquetas `401` y `403` para distinguir ambos casos.

---

## Slide 8. De la demo al patron OBO

### Titulo en pantalla

**En produccion, MCP y API son recursos distintos**

### Puntos en pantalla

- Demo: MCP reenvia el mismo token a la API.
- Produccion: API expone su propia audiencia y scopes.
- MCP usa On-Behalf-Of para obtener un token destinado a la API.
- API solo acepta tokens emitidos para ella.
- MCP usa Managed Identity, certificado o secreto en Key Vault.

### Mensaje para contar

El reenvio del token es aceptable solo en una demo con un unico limite de confianza. OBO evita que una API acepte tokens destinados a MCP y permite minimo privilegio real. Es el momento de separar identidades, permisos, auditoria y ciclos de vida.

### Imagen propuesta

Diagrama de dos tokens: `token para MCP` llega desde Cowork y MCP intercambia OBO para obtener `token para API`. Colorear cada audiencia de forma distinta.

---

## Slide 9. Gobierno: seguridad, observabilidad y datos

### Titulo en pantalla

**El conector es parte del perimetro corporativo**

### Puntos en pantalla

- No registrar tokens, API keys ni prompts sensibles.
- Correlacion entre Cowork, MCP, API y gateway.
- Rate limits, timeouts, tamano de payload y reintentos.
- DLP, Conditional Access, Information Barriers y grupos piloto.
- Mitigacion de prompt injection y validacion de inputs.

### Mensaje para contar

Los argumentos de una conversacion son datos no confiables. Una tool no debe convertir texto libre en SQL, comandos o URLs arbitrarias. La observabilidad debe explicar que ocurrio sin convertirse en una fuga de datos. Antes de publicar, limitar la distribucion a un grupo piloto y medir errores, coste y denegaciones.

### Imagen propuesta

Diagrama de anillos de defensa: Cowork, MCP, API, APIM, Key Vault, Monitor. Añadir un flujo lateral de logs con datos enmascarados.

---

## Slide 10. Azure OpenAI, Foundry y AI Gateway

### Titulo en pantalla

**Gobernar el modelo sin confundir las rutas**

### Puntos en pantalla

- La API consume Azure OpenAI o Foundry mediante endpoint OpenAI-compatible.
- Los secretos viven en variables de entorno o Key Vault.
- APIM / AI Gateway aporta politicas, limites y telemetria.
- Dev Tunnel es solo desarrollo.
- Cowork no se enruta automaticamente por AI Gateway.

### Mensaje para contar

La demo conecta API con el modelo. En un entorno gobernado, APIM o AI Gateway puede imponer cuotas, filtros, correlacion y observabilidad. Pero hay que ser rigurosos: el conector Cowork llama al endpoint configurado en el manifiesto; no debemos afirmar un enrutamiento automatico por AI Gateway sin implementarlo y validarlo. Presentarlo como tramo opcional o preview eleva la credibilidad tecnica.

### Imagen propuesta

Dos arquitecturas lado a lado:

1. **Demo local:** Cowork -> Dev Tunnel -> MCP -> API -> Azure OpenAI.
2. **Objetivo gobernado:** Cowork -> MCP hospedado -> API/APIM o AI Gateway -> Foundry/Azure OpenAI, con Key Vault y Monitor laterales.

---

## Slide de demo practica

### Titulo en pantalla

**De una peticion a una tool MCP en Cowork**

### Secuencia sugerida

1. Mostrar el paquete: skill, manifest y descriptor de tools.
2. Ejecutar una variante con los scripts `setup-*`.
3. Mostrar que API, MCP y Dev Tunnel estan activos.
4. Cargar el ZIP en Cowork con **Only you**.
5. Lanzar una peticion de clasificacion o recomendacion.
6. Mostrar logs MCP y la respuesta estructurada.
7. Si el tiempo lo permite, repetir en SSO y mostrar un `401` local sin bearer frente a una llamada valida desde Cowork.

### Prompt de demo

```text
Clasifica una amenaza de energia cosmica detectada en Leo y propone una respuesta.
```

### Mensaje para contar

La demo no demuestra que el modelo "adivina" una respuesta: demuestra que Cowork selecciona una herramienta, MCP valida un contrato y la API devuelve datos y reglas de negocio de forma controlada.

---

## Cierre

### Titulo en pantalla

**Skills orientan. MCP conecta. La identidad gobierna.**

### Mensajes finales

- Empezar con una tool pequena y de lectura.
- Mantener skill, conector, MCP y API como capas separadas.
- Usar Entra SSO para identidad y OBO cuando API sea un recurso distinto.
- Tratar Dev Tunnel como desarrollo y adoptar hosting, Key Vault y gateway para produccion.
- Medir antes de ampliar: uso, coste, errores, autorizacion y riesgo de datos.

### Imagen propuesta

El mismo diagrama de arquitectura de la portada, ahora con los controles de seguridad visibles: Entra, APIM/AI Gateway, Key Vault y Monitor.