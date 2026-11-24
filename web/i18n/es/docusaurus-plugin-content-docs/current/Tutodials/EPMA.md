---
sidebar_position: 4
---

import useBaseUrl from '@docusaurus/useBaseUrl';

# Cálculo mineral de EPMA

**El cálculo mineral de EPMA** trata composiciones minerales obtenidas por microsonda electrónica. Convierte el contenido de óxidos de un conjunto de puntos de análisis, según el método mineral elegido, en resultados de química mineral para la identificación de minerales, la comparación composicional y la interpretación geológica posterior.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/epma_mineral_calculation.webp')}
    alt="Interfaz de EPMA"
    style={{width: '100%'}}
  />
</div>

## Interfaz

El cálculo mineral de EPMA se divide en dos interfaces: la **biblioteca de plantillas de cálculo mineral de EPMA** y la **interfaz de cálculo de EPMA**.

En la biblioteca puede buscar, filtrar y elegir un cálculo de EPMA adecuado. En el panel de explicación de la derecha puede ver la información básica de esas plantillas. Pulse **Entrar en el cálculo** para calcular.

## Ejemplo de uso

Por ejemplo, elija una plantilla de EPMA de olivino y entre en la interfaz de cálculo. La página se divide en una parte superior y otra inferior. En la barra superior puede abrir la documentación detallada de esa plantilla o cargar datos de ejemplo.

Introduzca los datos que pide la tabla y pulse **Ejecutar cálculo** en la barra superior.

La zona blanca grisácea es el área de entrada y la zona azul violeta es el área de resultados. Puede revisar la salida con libertad o pulsar **Inspeccionar** para ver el proceso de cálculo intermedio.

El punto de color delante de cada encabezado de fila indica el estado de cálculo de esa fila:

* **Gris**: la fila tiene datos, pero aún no se ha calculado
* **Verde**: la fila se ha calculado con normalidad, sin advertencias ni errores
* **Naranja**: el cálculo ha terminado, pero hay una advertencia
* **Rojo**: el cálculo de esa fila ha fallado
