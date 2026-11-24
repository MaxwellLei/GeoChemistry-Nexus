---
sidebar_position: 2
---

import useBaseUrl from '@docusaurus/useBaseUrl';

# Diagramas

En el módulo de diagramas reuniremos más plantillas de diagramas básicos de las ciencias de la Tierra, incluidos, entre otros, diagramas de discriminación de ambiente tectónico, diagramas de clasificación de rocas y otros. **Nuestro objetivo final es crear para las ciencias de la Tierra un kit completo de herramientas de marco y dibujo de diagramas, y ofrecer la máxima comodidad a quienes investigan.** 🧪

:::info

Si se dedica a la investigación en diagramas geológicos u otros campos relacionados y desea difundir sus propios resultados en forma de diagramas, póngase en contacto con nosotros. Ayudaremos a que sus diagramas lleguen a un grupo investigador más amplio, y a mejorar la eficiencia y la influencia de la difusión de los resultados académicos.

:::

## Inicio rápido

El módulo de diagramas de nuestro programa se compone principalmente de dos partes: la biblioteca de plantillas de diagramas y la interfaz de operaciones del diagrama. Primero puede elegir en la biblioteca de plantillas el tipo de diagrama que necesita y, después, entrar en la interfaz de operaciones correspondiente para introducir datos, tratar el diagrama y exportar los resultados.

### Biblioteca de plantillas

Dividimos el módulo de diagramas en tres grandes categorías: **plantillas en la nube en línea**, **Mis plantillas** y **plantillas de diagramas de herramientas**.

**Plantillas en la nube en línea**: las actualizamos y mantenemos de forma continua. No necesita actualizar el programa: con conexión a la red puede obtener las plantillas de diagramas actualizadas, y así disponer siempre de los recursos de plantillas más completos y autorizados.

**Mis plantillas**: sirve para los casos en los que hay que crear plantillas personalizadas para una necesidad concreta de investigación. No solo puede crear usted mismo esas plantillas, sino también exportarlas para compartirlas con facilidad con otros investigadores, y favorecer así el intercambio y la difusión académicos.

> *En el futuro tenemos previsto crear una comunidad específica de plantillas de diagramas, en la que se puedan crear, cargar, compartir y descargar con facilidad diversas **plantillas personalizadas personales**, para reforzar aún más la flexibilidad y la capacidad de ampliación del sistema.*

**Plantillas de diagramas de herramientas**: separan los diagramas convencionales fijos. En la actualidad incluyen: **diagrama de araña de REE, diagrama de araña de elementos traza, diagramas múltiples de Harker**, y los diagramas de dispersión bidimensionales y ternarios habituales.

#### Interfaz

De forma predeterminada, al entrar en el módulo de diagramas se muestra la biblioteca integrada de plantillas de las ciencias de la Tierra. La interfaz se divide en tres partes principales:

*   **Izquierda: clasificación de diagramas**: muestra todos los niveles de plantillas y las plantillas correspondientes, incluida la lista de plantillas personalizadas.
*   **Derecha: tarjetas de diagramas**: muestra las tarjetas de diagramas del nivel actual, con el nombre y la imagen de vista previa.
*   **Parte superior: barra de navegación**: el programa usa un diseño sin bordes, así que la barra de navegación superior asume parte de las funciones de cada página. En la interfaz de diagramas, la barra de navegación superior muestra la ruta de la plantilla de diagrama actual, la búsqueda de plantillas de diagramas y otras funciones.

![graphical_interface](/img/v0.9.0/en-US/graphical_interface.webp)

Elija la plantilla de diagrama adecuada y haga clic en la tarjeta de la plantilla para entrar en la interfaz de operaciones del diagrama. En la tarjeta de la plantilla también puede hacer clic con el botón derecho para exportar, eliminar, marcar como favorito y realizar más operaciones.

:::tip

Puede eliminar o marcar como favorita una plantilla de diagrama de forma rápida pulsando la tecla <kbd>Alt</kbd> derecha.

:::

#### Barra de menús superior

La barra de menús superior **Archivo** contiene:

1.  **Nueva plantilla de diagrama**: sirve para crear una plantilla de diagrama personalizada.
2.  **Abrir diagrama**: sirve para abrir temporalmente una plantilla de diagrama; solo admite archivos `.diag.zip`.
3.  **Importar diagrama**: sirve para importar un paquete externo de plantilla de diagrama (`.diag.zip`) a la lista local de Mis plantillas.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/graphic_menu_bar.webp')}
    alt="Barra de menús de diagramas"
    style={{width: '50%'}}
  />
</div>

### Interfaz de operaciones del diagrama

#### Interfaz

La interfaz del diagrama se divide en cinco partes principales:

-   **Barra de herramientas**: incluye botones de acceso rápido y tres pestañas de funciones: diagrama, datos y edición.
-   **Lista de capas (objetos)**: lista de los elementos de dibujo de la plantilla. Al hacer clic en un elemento puede modificar sus propiedades.
-   **Lienzo de dibujo**: zona central para ver el diagrama, importar datos, ajustar el aspecto y consultar la explicación de la plantilla.
-   **Barra de estado**: muestra información básica del dibujo, incluido el idioma actual del diagrama y la información de coordenadas.
-   **Panel de propiedades**: muestra las propiedades del elemento de dibujo seleccionado (como el color y el tamaño), para lograr el efecto visual deseado.

![graphical_operation_interface](/img/v0.9.0/en-US/graphical_operation_interface.webp)

#### Barra de herramientas del diagrama

La barra de herramientas se compone de una serie de **botones de acceso rápido**. Principalmente:

1. Volver a la biblioteca de plantillas de diagramas
2. Cambiar el estado de operación (modo diagrama, modo de datos, modo de edición)
3. Diversos paneles de propiedades del diagrama, herramientas de operación, etc.

Las funciones detalladas se ven en la figura siguiente.

![graphical_toolbar](/img/v0.9.0/en-US/graphical_toolbar.webp)

### Lista de capas

Los elementos de dibujo se dividen en 7 grandes clases:

-   **Line (línea)**: define el límite básico del diagrama o un segmento.
-   **Text (texto)**: etiquetas y anotaciones.
-   **Polygon (polígono)**: forma cerrada dentro del diagrama.
-   **Arrow (flecha)**: objeto de dibujo orientado.
-   **Function (función)**: permite introducir una función matemática personalizada y su dominio.
-   **Axes (ejes)**: ejes de coordenadas del diagrama.
-   **Data Point (punto de datos)**: elemento que representa los datos importados.

**Orden de representación predeterminado (de arriba abajo): `punto de datos > texto > flecha > función > línea > polígono > eje`**.

![tutorial_plot5](/img/v0.9.0/en-US/diagrammatic_object_selection_example.webp)

Cuando selecciona un elemento en el panel de capas, se resalta en el lienzo, y los demás elementos pasan a ser semitransparentes para reducir las interferencias. El panel de propiedades muestra entonces las propiedades relacionadas con ese elemento. 🔍

Puede modificar esas propiedades para lograr el estilo visual que desea.

:::tip

Para anular la selección de todos los elementos, basta con **hacer clic con el botón derecho en cualquier lugar del lienzo** o usar el botón **Deseleccionar** de la barra de herramientas.

:::

## Ejemplo de uso

### Ejemplo

1. Elija una plantilla de diagrama en la **biblioteca de plantillas de diagramas** y entre en la página de operación del diagrama. Por ejemplo: elija el diagrama TAS.
2. Cuando haya que introducir datos, hay que pasar del modo diagrama al modo de datos.
   
   ![tutorial_plot6](/img/v0.9.0/en-US/diagram_data_input.webp)


Para el diagrama TAS hay que introducir cuatro columnas de datos: `Category`, `SiO2`, `K2O` y `Na2O`. La columna de título `Category` sirve para agrupar los datos y generar la leyenda; la columna `Sample` identifica la etiqueta de la muestra de los datos y no participa en ningún cálculo (puede estar vacía); la unidad de los demás elementos es `wt.%`. Y, por supuesto, si no tiene claro qué datos hay que introducir en este diagrama, en qué unidad, cómo se usa, etc., puede pulsar el botón de ayuda del diagrama, en la esquina superior derecha de la zona del diagrama, para consultar el documento explicativo de ese diagrama.

3. Cuando conozca los requisitos, introduzca sus datos y pulse el botón de graficar de la esquina superior derecha del panel de datos para dibujar los puntos de datos. Si hay un problema en los datos introducidos, después de modificarlos vuelva a pulsar Graficar.![tutorial_plot8](/img/v0.9.0/en-US/data_plotting.webp)
4. **Exportar el diagrama**. Ofrecemos un **botón de función para copiar rápidamente el resultado de la proyección del diagrama al portapapeles**. También hay una **función de exportación formal**. En el panel de exportación formal se pueden exportar imágenes en los formatos `jpg`, `png`, `bmp` y `svg`. Cuando el dibujo científico necesita un gráfico vectorial, recomendamos el formato `svg`. También admitimos **enviar el resultado directamente a un programa de dibujo de terceros**, sin el paso intermedio de exportar y volver a importar. Los programas de terceros recomendados de forma integrada son: **CorelDRAW, Inkscape y Adobe Illustrator**. Recomendamos usar la vinculación con Inkscape para el retoque científico posterior. Por supuesto, también se puede personalizar otro programa de dibujo de terceros, siempre que ese programa admita el formato vectorial `svg`. El investigador solo tiene que indicar en la configuración la ruta del programa de dibujo.
   ![tutorial_plot8](/img/v0.9.0/en-US/export_diagram.webp)

### Notas complementarias

La propia plantilla de diagrama admite el cambio de idioma: un mismo diagrama puede cambiarse a versiones en varios idiomas, para responder a las necesidades de internacionalización.

Por ejemplo: un diagrama para un artículo en una revista china necesita la versión en chino, y publicarlo en una revista en inglés SCI/IE u otra similar necesita la versión en inglés del diagrama. Puede cambiar con un solo clic a la versión del diagrama en el idioma correspondiente pulsando el cambio de idioma situado en la esquina superior derecha de la barra de herramientas del diagrama.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/switch_graphic_language.webp')}
    alt="Cambiar el idioma del diagrama"
    style={{width: '60%'}}
  />
</div>
