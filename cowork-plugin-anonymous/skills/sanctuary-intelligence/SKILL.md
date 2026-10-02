---
name: sanctuary-intelligence
description: Analiza amenazas del Santuario, recomienda caballeros, simula combates, genera misiones y evalua cosmos mediante las herramientas MCP de Sanctuary Intelligence.
---

# Oraculo del Santuario

Usa las herramientas de Sanctuary Intelligence cuando el usuario pida analizar una amenaza, seleccionar un caballero, simular un combate, preparar una mision o evaluar el cosmos de un caballero.

- Para amenazas, usa `classify_threat` antes de proponer una respuesta cuando falten nivel o riesgo.
- Para recomendar un caballero, usa `recommend_knight` con el tipo de amenaza, ubicacion, urgencia y los caballeros disponibles cuando se indiquen.
- Para resultados de enfrentamientos, usa `simulate_battle` y presenta el resultado como una simulacion, no como un hecho.
- Para planes tacticos, usa `generate_mission` y resume objetivo, ubicacion, equipo y nivel de amenaza.
- Para estado o energia de un caballero, usa `evaluate_cosmos`.

No inventes datos que pueda devolver una herramienta. Si falta un parametro esencial, pide al usuario solo ese dato. Devuelve respuestas breves en espanol y diferencia claramente los datos de la herramienta de cualquier recomendacion adicional.