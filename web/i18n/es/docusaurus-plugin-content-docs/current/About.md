---
sidebar_position: 6
---

# ℹ️ Acerca de

:::info

**«No encontraba el coche de carreras de mis sueños, así que construí uno yo mismo.» — Ferdinand Porsche**

:::

**GeoChemistry Nexus** es el primer proyecto que pienso mantener a muy largo plazo: tengo la intención de seguir actualizándolo al menos durante los próximos cinco años.

Curiosamente, al principio se llamaba **Geo-Thermometer**. Al comienzo solo quería desarrollar un programa básico relacionado con los geotermómetros, para facilitar el cálculo y el dibujo. Sin embargo, a medida que avanzaba el proyecto, y en especial cuando terminé el módulo de diagramas, me di cuenta de pronto de que podía alcanzar un objetivo más ambicioso. Por eso el proyecto se amplió tanto en el concepto como en las funciones, y pasó de Geo-Thermometer a GeoChemistry Nexus.

## Por qué este nombre

**GeoChemistry** significa geoquímica; **Nexus** significa punto de conexión, nexo o centro de confluencia;

esperamos reunir en una sola plataforma los métodos de cálculo geoquímico dispersos, las herramientas de tratamiento de datos, el análisis de química mineral, los diagramas geológicos y otros recursos.

## Por qué hace falta GeoChemistry Nexus

En la investigación en geoquímica y petrología, **elaborar diagramas** y **calcular** suelen estar repartidos entre herramientas distintas: los datos se organizan con un programa de hojas de cálculo, el mapa base se dibuja con una herramienta vectorial genérica y el cálculo de geotermobarómetros exige abrir otro script o un programa antiguo. Un flujo fragmentado, formatos que no coinciden y un coste alto de reproducir el trabajo son fricciones que muchos investigadores encuentran a diario.

Por otra parte, muchos diagramas de discriminación, geotermobarómetros y herramientas de cálculo de uso habitual **llevan mucho tiempo sin una actualización continua**: es difícil incorporar a tiempo los algoritmos recién publicados y las revisiones de los modelos clásicos, y los investigadores a menudo solo pueden seguir usando versiones antiguas detenidas desde hace años. El problema más concreto es este: cuando un investigador **desarrolla por su cuenta** un algoritmo o un diagrama nuevo, a menudo **le falta un canal cómodo y abierto para distribuirlo**. El resultado se queda en un anexo del artículo, en un script personal o en un intercambio de alcance reducido, y es difícil que se difunda y se reutilice con rapidez en el mismo campo.

![plot_home_ui](/img/about-illustrations/02-research-tool-pain-points.png)

Además, en la investigación también he visto prácticas poco rigurosas. Por ejemplo, hay quien, al dibujar el mapa base de un mapa geológico, traza las formas de manera aproximada con CorelDRAW. Aunque a la vista la diferencia no es grande, desde el punto de vista de la investigación científica esta práctica contradice el rigor científico.

Así que me pregunté: **¿por qué no desarrollar yo mismo una herramienta?**

Una herramienta capaz de reunir un gran número de plantillas de diagramas geológicos y geoquímicos, de admitir el cálculo de diversos geotermómetros y geobarómetros, y de extenderse al cálculo geoquímico y a las funciones de aprendizaje automático que están en auge. Si no existe una plantilla, ofrece herramientas de dibujo visual para crearla y compartirla. Con un diseño de interfaz moderno y compatibilidad con varios idiomas, permite a los investigadores completar en un solo programa la mayor parte del trabajo básico de dibujo científico y de cálculo.

**GeoChemistry Nexus** pretende reunir esa cadena en un solo lugar: desde la importación, el preprocesamiento y la corrección de los datos de las muestras, hasta el dibujo de diagramas de discriminación, el cálculo de geotermómetros y el cálculo de la norma CIPW, y después la exportación en formatos como PNG o SVG, en la medida de lo posible dentro de la misma interfaz. Al mismo tiempo, mediante un **ecosistema de plantillas en la nube** y un **mecanismo de colaboración de la comunidad**, los diagramas y los algoritmos pueden seguir iterándose y llegar con rapidez a más investigadores.

## La idea inicial de desarrollo

Por casualidad, conozco lenguajes de programación como C# y Python, y domino las técnicas de desarrollo de software de escritorio. Por eso, en diciembre de 2024, empecé a desarrollar una demo. En la fase inicial de elección técnica me planteé con seriedad: **¿qué lenguaje y qué arquitectura pueden lograr mejor este objetivo?**

Mi primera idea fue Python: me permitía hacer un prototipo con rapidez, y eso es una buena noticia para quien desarrolla. Sin embargo, desde el punto de vista de quien lo usa, tiene defectos decisivos. En primer lugar, la eficiencia de ejecución es baja (aunque con pocos datos la influencia no es grande, sigue habiendo una diferencia de rendimiento). En segundo lugar, genera un paquete de instalación enorme. En una ocasión reescribí en Python un programa sencillo que al principio estaba escrito en WPF. Aunque lo empaqueté en un entorno virtual completamente nuevo, el resultado ocupaba varios cientos de MB, mientras que el programa empaquetado con .NET solo ocupaba algo más de diez MB: la diferencia es enorme. **Obligar a quien lo usa a soportar una instalación de varios cientos de MB o incluso de GB, y una configuración compleja, por una función sencilla no es la mejor opción**.

Después consideré el desarrollo web. Las ventajas son evidentes: no hace falta instalar nada y se usa al abrirlo. Pero el problema es que el coste elevado y prolongado de un servidor supera mi capacidad personal de mantenimiento. Por eso, al final, volví a centrarme en el desarrollo de cliente.

![plot_home_ui](/img/about-illustrations/03-technology-choice-path.png)

El desarrollo de cliente tiene muchas soluciones técnicas, pero, comparado con QT basado en C++, conozco mejor .NET. Uniendo la alta eficiencia de ejecución de .NET y la difusión de Windows, acabé eligiendo .NET como plataforma principal de desarrollo. Al principio también consideré usar Avalonia para lograr compatibilidad multiplataforma con Windows, Linux y MacOS, pero durante el desarrollo descubrí que algunas dependencias del proyecto no eran compatibles con Avalonia, y el coste de la migración era muy alto. Con mis solas fuerzas, era difícil reescribir esos módulos en una fase temprana. Por eso, al menos en esta etapa, mi trabajo sigue centrado en la plataforma Windows, y la versión multiplataforma quizá se haga realidad algún día.

## Contacto

Si le interesa el desarrollo de software y desea colaborar conmigo, o si quiere aportar sugerencias y comentarios, plantillas de diagramas, apoyo en algoritmos o incluso una colaboración comercial, puede ponerse en contacto conmigo en cualquier momento en **[maxwelllei@qq.com](mailto:maxwelllei@qq.com)**.

*Si al usar el programa encuentra alguna carencia o algún problema, no dude en comentarlo. Como las funciones del programa aumentan y se vuelven cada vez más complejas, las pruebas difícilmente cubren todos los casos, y puede haber omisiones o inexactitudes. Haré lo posible por corregirlas y optimizarlas, para ofrecerle una experiencia de uso más estable y fiable.*

![plot_home_ui](/img/about-illustrations/04-long-term-maintenance.png)
