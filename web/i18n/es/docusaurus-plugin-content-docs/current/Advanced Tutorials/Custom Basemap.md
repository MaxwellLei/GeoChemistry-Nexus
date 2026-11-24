---
sidebar_position: 1
---

import useBaseUrl from '@docusaurus/useBaseUrl';

# Crear una plantilla de diagrama

Para las plantillas de diagramas que todavía no están en la biblioteca de plantillas de diagramas en línea, puede optar por crear una plantilla de diagrama personalizada. Si personaliza la plantilla y la empaqueta como un paquete de plantilla, puede compartirla con rapidez con otros investigadores.

También puede optar por entregar su plantilla a quien desarrolla el programa para que se incorpore a la biblioteca de plantillas de diagramas en línea. Agradecemos de corazón la contribución de cualquier persona que participe.

## Creación rápida

Ahora puede crear una plantilla de diagrama nueva desde la barra de menús, eligiendo `Archivo` -> `Nueva plantilla de diagrama`, como se muestra a continuación:

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/new_graphic_template.webp')}
    alt="Configuración de diagramas"
    style={{width: '50%'}}
  />
</div>

Después de pulsar «Nueva plantilla de diagrama», aparece una ventana emergente para crear la nueva plantilla de diagrama:

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/new_graphic_template_interface.webp')}
    alt="Interfaz de nuevo diagrama"
    style={{width: '100%'}}
  />
</div>

En una plantilla de diagrama personalizada nueva hay que configurar principalmente tres partes:

1.  **Idiomas admitidos de forma predeterminada**: puede elegir en el cuadro de selección de la derecha las opciones rápidas de idiomas integrados. Ofrecemos diez idiomas, entre ellos chino simplificado, chino tradicional, inglés de Estados Unidos, japonés, ruso, coreano y alemán.

    > Hay que aclarar lo siguiente: la función del idioma predeterminado es servir de idioma de reserva si los demás idiomas no están traducidos o se produce un error. Además, en la versión nueva ya no se permite que el usuario personalice el código de idioma. Si necesita compatibilidad con su idioma, póngase en contacto con quien desarrolla el programa.

2.  **Clasificación de la plantilla de diagrama (jerarquía)**: ofrecemos una estructura de clasificación rápida integrada. Este ajuste influye en la posición jerárquica de su plantilla dentro de la lista de plantillas de diagramas.

3.  **Tipo de plantilla de diagrama**: en la actualidad se admiten dos tipos: **sistema de coordenadas bidimensional** y diagrama ternario.

Cuando termine la configuración, pulse «Aceptar» para entrar en la interfaz de dibujo personalizado. A continuación, centramos la operación en la barra de funciones «Editar». Después de pulsar «Editar», el sistema muestra un cuadro de diálogo de confirmación para editar el diagrama. Tras confirmar, usted entra en el modo de edición, donde puede consultar y usar las distintas herramientas de la barra de funciones de edición.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/empty_diagram_template.webp')}
    alt="Diagrama vacío"
    style={{width: '100%'}}
  />
</div>

## Dibujar el diagrama

En el modo de edición se permiten las operaciones siguientes:

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/graphical_toolbar.webp')}
    alt="Dibujar el diagrama"
    style={{width: '100%'}}
  />
</div>

* **Guardar**: guarda la plantilla de diagrama. Al pulsarlo, el programa genera de forma predeterminada la miniatura correspondiente según el estado actual del dibujo.
* **Guardar como**: guarda la plantilla de diagrama en otra ubicación de archivo.
* **Añadir línea**: al activarlo, se entra en el modo «añadir línea». Haga clic en el primer punto de la zona de dibujo para empezar la línea y en el segundo punto para completar el objeto de línea.
* **Añadir texto**: también se llama anotación. Al activarlo, se entra en el modo «añadir texto». Se crea al hacer clic en una posición concreta del dibujo. El texto predeterminado es `Text`. Puede modificar la posición o el contenido en la parte de propiedades del panel de capas.
* **Añadir polígono**: al activarlo, se entra en el modo «añadir polígono». Añada un polígono cerrado creando vértices con clics sucesivos del botón izquierdo, y cierre la forma con un clic del botón derecho.
* **Añadir flecha**: al activarlo, se entra en el modo «añadir flecha». El proceso de adición es semejante al de crear una línea.
* **Añadir función**: al pulsarlo, añade una función predeterminada `sin(x)`, con un dominio de [-10, 10]. Puede personalizar la fórmula en el panel de propiedades.
* **Deshacer/Rehacer**: si no se ha creado ni eliminado ningún objeto de dibujo, estas funciones quedan desactivadas. De forma predeterminada, el historial solo almacena las últimas 10 operaciones.
* **Eliminar**: elimina un objeto de dibujo. Primero seleccione el objeto (por ejemplo, un texto) y después pulse Eliminar para quitarlo.

### Añadir línea

A continuación hay un ejemplo del panel de propiedades de añadir una línea. Mediante el panel de propiedades puede ajustar con precisión la posición de la línea y otras propiedades.

**El botón «Recoger coordenada» que hay a la derecha de cada coordenada le permite reajustar y capturar las coordenadas de la zona de dibujo**. Una vez activado, el siguiente clic con el botón izquierdo en la zona de dibujo toma automáticamente como coordenada la posición del clic.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/add_line.webp')}
    alt="Añadir línea"
    style={{width: '100%'}}
  />
</div>

### Añadir flecha

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/add_arrow.webp')}
    alt="Añadir flecha"
    style={{width: '100%'}}
  />
</div>

### Añadir polígono

**Después de activar añadir polígono, confirme los vértices del polígono con clics sucesivos del botón izquierdo del ratón en la zona del lienzo. Durante ese tiempo puede ver la zona de vista previa del polígono, y al pulsar el botón derecho del ratón se completa la creación del polígono**. El objeto polígono tiene una lista de vértices; puede seleccionar, modificar y eliminar el vértice correspondiente en cualquier momento.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/add_polygon.webp')}
    alt="Añadir polígono"
    style={{width: '100%'}}
  />
</div>

### Añadir texto

Después de activar añadir texto, haga clic con el botón izquierdo del ratón en cualquier punto de la zona del lienzo para crear un objeto de texto en la posición correspondiente. Hay que señalar que, en la barra de estado de la parte inferior del lienzo, puede ver a qué idioma pertenece el contenido del objeto de texto actual.

Como el diagrama admite de forma nativa varios idiomas, la traducción del contenido multilingüe se explica con detalle más adelante.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/add_text.webp')}
    alt="Añadir texto"
    style={{width: '100%'}}
  />
</div>

### Añadir función

Añadir una función es distinto de los objetos anteriores: **al pulsar el botón de añadir función, se añade de inmediato un objeto de función predeterminado**. La función predeterminada es `sin(x)`. Puede modificar la fórmula de la función en el panel de propiedades para definir su función.

Entre las propiedades del objeto de función, los dos parámetros más importantes son: el **dominio** y los **puntos de muestreo**.

* **El dominio define el intervalo de visualización de la función en el lienzo**.
* **Los puntos de muestreo controlan la precisión con la que se dibuja la función en el lienzo** y, por tanto, influyen en la exactitud del algoritmo de captura y selección con el ratón. El valor predeterminado es `1000`. Cuanto mayor es la precisión, más fino es el renderizado de la función, pero también aumenta la carga de renderizado del programa.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/add_function.webp')}
    alt="Añadir función"
    style={{width: '100%'}}
  />
</div>

## Completar la plantilla de diagrama

Después de dibujar los gráficos básicos, una plantilla completa todavía necesita:

1.  **Datos y script**: sirven para definir los datos de entrada de la plantilla de diagrama y el algoritmo de cálculo de los datos, es decir, para indicar cómo se calculan los datos y se completa la proyección.
2.  **Documento del diagrama**: sirve para explicar el contenido de ayuda de ese diagrama.
3.  **Varios idiomas**: si la plantilla de diagrama está configurada con más de dos idiomas, hay que traducir la parte multilingüe. Incluye el «documento del diagrama» anterior y el contenido de los objetos de texto del lienzo del diagrama, entre otros.

Para completar la plantilla de diagrama, aunque se puede hacer en la interfaz de operaciones del diagrama, le sugerimos que lo haga en la ventana de edición del diagrama, porque resultará más cómodo.

### Datos y script

Los datos y el script son la parte clave de la plantilla de diagrama, porque definen cómo se calculan los datos del diagrama y cómo se presentan finalmente en él.

Necesitan dos parámetros: los **parámetros de variables** de la plantilla de diagrama y el **algoritmo de cálculo (script)**, como se muestra a continuación:

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/diagram_data_and_script.webp')}
    alt="Diagrama: datos y script"
    style={{width: '100%'}}
  />
</div>

Para los **parámetros de variables** de la plantilla de diagrama, puede añadir las columnas correspondientes en la parte superior, para representar las columnas de entrada que necesita ese diagrama. Hay dos parámetros opcionales

1. Unit indica la unidad de la columna de entrada; solo sirve de explicación visual y no influye en el algoritmo de los datos.
2. Group indica el grupo de la columna de entrada; solo sirve de explicación visual y no influye en el algoritmo de los datos.

Hay que señalar que **los títulos de columna que defina en la tabla de datos se toman de forma predeterminada como variables ya registradas a las que el script de abajo puede llamar directamente**. Eso significa que no necesita volver a definir en el script las variables de las columnas de entrada.

Para el **algoritmo de cálculo (script)**, de forma predeterminada se escribe en lenguaje `JavaScript`. El contenido de ejemplo del script predeterminado es `return [X,Y]`, es decir, devuelve a los ejes de coordenadas bidimensionales las columnas de entrada `X` e `Y` definidas en la tabla de datos.

Por ejemplo, para el diagrama TAS, los parámetros deben ser: `SiO2, Na2O, K2O`. El contenido del script será:

```javascript
// Calcular con las variables K2O + Na2O
var result1 = K2O + Na2O;
// Usar SiO2
var result2 = SiO2;
// Devolver dos valores calculados. Atención: en la imagen de coordenadas bidimensionales predeterminada solo hay dos valores de retorno.
// La primera posición representa el valor de retorno de X y la segunda, el valor de retorno de Y.
return [result2, result1]
```

O bien puede escribir el script así:

```javascript
var result = K2O + Na2O
return [SiO2, result]
```

Tenga en cuenta que la posición de los valores devueltos es fija. En `[x, y]`, el primer valor es siempre X (eje inferior) y el segundo es Y (eje izquierdo).

:::info

En un diagrama ternario, el formato final de retorno es `[A, B, C]`, donde el primer valor es A (eje inferior), el segundo es B (eje izquierdo) y el tercero es C (eje derecho).

:::

### Documento del diagrama

El documento del diagrama es un paso necesario para ayudar a otros investigadores a conocer con rapidez la información básica y el uso de ese diagrama.

El documento del diagrama se redacta en la página del documento de ayuda. Ofrecemos funciones sencillas de barra de herramientas para cubrir las necesidades habituales de un documento. Por supuesto, también puede redactar el contenido del documento en `Office Word` y después pegarlo aquí: la mayor parte de sus formatos son compatibles.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/diagram_help_document.webp')}
    alt="Diagrama: documento de ayuda"
    style={{width: '100%'}}
  />
</div>

Para el formato de redacción del documento del diagrama, sugerimos seguir este criterio:

*   **Introducción**: explica el concepto básico y la función de la plantilla de diagrama, para ayudar a comprenderla con rapidez.
*   **Formato de los datos**: especifica el formato de los datos de entrada y los títulos de columna necesarios para leer datos válidos.
*   **Referencias**: enumera las referencias usadas para crear la plantilla de diagrama y su contenido.
*   **Colaboradores**: nombres de las personas que han participado en la elaboración de la plantilla de diagrama. Incluso se le anima a incluir su sitio web personal.

### Varios idiomas

Hemos establecido dos métodos para configurar varios idiomas:

El primero consiste en usar la opción **Cambiar idioma** de la barra de funciones superior de la interfaz del diagrama. Permite definir un contenido concreto para el segundo idioma.

El segundo método consiste en editar el contenido multilingüe en la ventana de edición del diagrama, como se muestra en la figura siguiente.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/illustrated_multilingual.webp')}
    alt="Diagrama: datos y script"
    style={{width: '100%'}}
  />
</div>
