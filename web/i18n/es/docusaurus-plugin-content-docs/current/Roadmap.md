---
sidebar_position: 3
---

# 🛣️ Hoja de ruta

El objetivo de GeoChemistry Nexus es construir de forma progresiva una cadena de herramientas científicas de geoquímica y petrología que integre **datos · diagramas · cálculo · colaboración**.

## ✅ Completado


### Flujo de trabajo principal
- [x] **Inicio**: enlaces rápidos y herramientas prácticas
- [x] **Diagramas de discriminación**: un gran número de diagramas de discriminación integrados a partir de la biblioteca de plantillas, incluidos, pero no limitados a, diagramas de discriminación de ambiente tectónico, diagramas de clasificación de rocas y otros; además, admite diagramas de araña de elementos traza y de tierras raras, con un gran número de esquemas de normalización integrados; admite la proyección en diagramas múltiples de Harker;
- [x] **Geotermobarómetro**: plantillas en línea de cálculo geotermobarométrico de varios minerales y funciones personalizadas al estilo de Excel
- [x] **Cálculo mineral por EPMA**: plantillas de cálculo EPMA de distintos minerales, actualizadas en línea y en tiempo real, listas para usar al descargarlas.
- [x] **Cálculo de la norma CIPW**: cálculo normalizado de datos de elementos mayores de roca total y exportación de los resultados
- [x] **Cálculo de la norma de Niggli**: cálculo normalizado de datos de elementos mayores de roca total y exportación de los resultados
### Capacidades de la plataforma
- [x] **Dependencias de rendimiento**: ocupación de memoria muy baja y dependencia de un espacio de instalación muy pequeño; el programa en local, sin el entorno de ejecución de `.NET`, ocupa solo 9 MB.
- [x] **Ecosistema de plantillas**: actualización dinámica en la nube de las plantillas en línea; creación de plantillas personalizadas, importación y empaquetado para compartir; se pueden consultar los detalles del proceso de cálculo para facilitar la comprobación.
- [x] **Varios idiomas**: localización de la interfaz (chino / inglés / alemán / ruso / japonés / coreano, etc.); cambio de idioma de las plantillas de diagramas con un solo clic
- [x] **Exportación e integración**: exportación a PNG / JPG / BMP / SVG; colaboración con programas de terceros como CorelDRAW, Inkscape y Adobe Illustrator

## 📅 Plan próximo
### Diagramas de geocronología isotópica
- [ ] Diagrama de concordia U-Pb (Concordia)
- [ ] Diagramas de isócrona de Rb-Sr, Sm-Nd, Lu-Hf, U-Pb y otros
- [ ] Diagramas relacionados con relaciones isotópicas iniciales (como (⁸⁷Sr/⁸⁶Sr)ᵢ vs εNd(t))
- [ ] Etc.


## 🔭 Visión a medio y largo plazo
### Análisis asistido por aprendizaje automático
Incorporar flujos habituales de aprendizaje automático para ayudar a la discriminación y la clasificación de datos geoquímicos, y obtener la evaluación del modelo, la importancia de las variables y resultados visuales como curvas ROC y matrices de confusión, **sin que el usuario tenga que configurar por su cuenta un entorno de Python**.
### Modelos de discriminación del tipo «nuevo diagrama»
Admitir la carga de modelos de discriminación de aprendizaje automático ampliables (como nuevos diagramas de discriminación entrenados con grandes volúmenes de datos), para que el investigador aplique directamente el «nuevo diagrama» al análisis, sin necesidad de dominar toda la pila de desarrollo de ML.
### Asistente de investigación con IA (por fases)
1. **Primera fase**: preguntas y respuestas RAG basadas en la documentación de ayuda integrada y en una base de conocimiento, que ofrecen sugerencias de uso según palabras clave y situaciones  
2. **Segunda fase**: ayudar además en el flujo de tratamiento y análisis de los datos, y simplificar las operaciones repetitivas
### Multiplataforma y otros aspectos
- [ ] Evaluar soluciones como Avalonia e ir logrando la compatibilidad con Linux / macOS (en la actualidad, el programa se centra en Windows)
- [ ] Seguir optimizando el rendimiento, el formato de las plantillas y las interfaces de ampliación para desarrolladores
