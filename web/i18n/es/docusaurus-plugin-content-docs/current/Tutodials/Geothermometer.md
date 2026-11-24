---
sidebar_position: 3
---

import useBaseUrl from '@docusaurus/useBaseUrl';

# Geotermobarómetro

En las primeras versiones, este módulo se abreviaba como **GTM**. Después pasó a llamarse geotermómetro y, en la actualidad, el nombre oficial es: geotermobarómetro.

Las primeras versiones incluían el cálculo de geotermómetros de minerales como circón, esfalerita, cuarzo, xenotima, clorita y biotita. La nueva versión del geotermobarómetro todavía se está actualizando y adaptando, y se sincronizará en breve.

## Interfaz

El módulo de geotermobarómetro puede dividirse, en conjunto, en dos partes:

1. A la izquierda está la **lista de plantillas de geotermobarómetros**. Aquí puede buscar la plantilla que le convenga, o crear su propia plantilla de cálculo de geotermobarómetro.
2. A la derecha está el **documento de ayuda de la plantilla de geotermobarómetro seleccionada en ese momento**

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/geothermobarometer_interface.webp')}
    alt="Interfaz del geotermobarómetro"
    style={{width: '100%'}}
  />
</div>

## Funciones

El módulo de geotermobarómetro se diseñó desde el principio **imitando la forma de Excel**, para reducir el coste de aprendizaje. **Hay principalmente dos formas de usar el geotermobarómetro**:

1. **Plantillas de geotermobarómetro en línea**: de forma predeterminada, el usuario no puede modificar las **plantillas de geotermobarómetro en línea**, pero puede consultar la lógica de implementación del algoritmo. Puede obtener por red más plantillas recientes de geotermobarómetros sin actualizar el programa. El investigador puede buscar, filtrar y elegir la plantilla que le convenga, y rellenar los datos y calcular según el contenido de la guía. **Esta forma es más directa y más rápida, y no requiere conocimientos de programación**.
2. **Ventana de cálculo independiente**: el investigador puede abrir la ventana de cálculo independiente desde la barra de menús superior, **Archivo -> Abrir hoja independiente**. En la ventana independiente, el investigador puede programar de forma personalizada y combinar distintos geotermobarómetros en el cálculo. Para las funciones que corresponden a cada geotermobarómetro, consulte la parte de tutoriales avanzados. **Esta forma es más compleja y, en comparación, más flexible**.

Por supuesto, el módulo de geotermobarómetro también permite al investigador crear su propio geotermobarómetro personalizado. La plantilla de geotermobarómetro ya elaborada se puede exportar y distribuir para que otros investigadores la usen con rapidez.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/export_geothermobarometer_template.webp')}
    alt="Exportar geotermobarómetro"
    style={{width: '45%'}}
  />
</div>

## Ejemplo de uso

### Usar una plantilla de geotermobarómetro en línea

En primer lugar, dentro del módulo de geotermómetro, filtre y seleccione la plantilla de cálculo correspondiente en la barra lateral izquierda:

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/select_and_apply_thermobarometer_template.webp')}
    alt="Seleccionar un geotermobarómetro"
    style={{width: '100%'}}
  />
</div>

Puede consultar el documento explicativo de esa plantilla en el panel derecho, y la barra superior también muestra el título y las etiquetas de la plantilla de geotermobarómetro seleccionada. Cuando haya elegido la plantilla de geotermobarómetro correspondiente, puede pulsar el botón **Aplicar** de la barra superior para entrar en la interfaz de cálculo del geotermobarómetro.

En la interfaz del geotermobarómetro se ven las columnas de entrada correspondientes. También puede pulsar el botón **Ejemplo** de la barra superior para cargar datos de ejemplo. Solo tiene que rellenar los datos que exige el título de cada columna y, después, **copiar la fórmula de cálculo** o **usar el controlador de relleno de «clic y arrastre»** (como en Excel) para completar el cálculo de forma automática (como se muestra en el ejemplo animado siguiente).

> Hay que señalar que la columna `Sample` no participa en el cálculo; solo sirve de anotación para identificar la muestra de los datos

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/calculate_thermobarometer_using_formula.webp')}
    alt="Calcular el geotermobarómetro con una fórmula"
    style={{width: '80%'}}
  />
</div>

Por supuesto, la versión actual del geotermobarómetro ya permite consultar los detalles internos del cálculo.

Puede seleccionar con el ratón la fila de datos correspondiente, pulsar el botón **Inspeccionar** de la parte superior y consultar los detalles de los resultados intermedios en la barra lateral que aparece.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/thermobarometer_calculation_details.webp')}
    alt="Detalles del cálculo del geotermobarómetro"
    style={{width: '100%'}}
  />
</div>

### Ventana independiente: funciones personalizadas

Como se ha explicado antes, se puede abrir la ventana libre independiente desde la barra de menús, **Archivo -> Abrir hoja libre**.

La tabla de la ventana independiente es una tabla vacía y contiene algunos botones sencillos de barra de herramientas.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/thermobarometer_free_form.webp')}
    alt="Tabla de cálculo libre del geotermobarómetro"
    style={{width: '100%'}}
  />
</div>

Usar la tabla de la ventana independiente es **igual que usar a diario una hoja de Excel: se puede introducir cualquier valor numérico y llamar a una función escribiendo `=NombreDeLaFunción(parámetros)`**.

La diferencia es que nuestros cálculos de geotermobarómetro están registrados como funciones de cálculo correspondientes, y el investigador puede personalizar con más flexibilidad el contenido del cálculo según lo que necesite. Incluso puede **comparar en la misma tabla los resultados de varios geotermobarómetros**.

:::note

Hay que tener en cuenta que: **en la tabla de la ventana independiente no se puede consultar el proceso de cálculo intermedio de la fila de datos correspondiente**.

:::
