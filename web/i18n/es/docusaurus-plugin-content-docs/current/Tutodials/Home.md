---
sidebar_position: 1
---

import useBaseUrl from '@docusaurus/useBaseUrl';

# Inicio

Queremos que la página de inicio sea una entrada cómoda que conecte los recursos de investigación en ciencias de la Tierra con el flujo de trabajo diario. Al reunir accesos directos a sitios web de uso habitual en las ciencias de la Tierra y ofrecer herramientas ligeras y prácticas, ayuda a completar las operaciones diarias y los cálculos sencillos, y enlaza de forma orgánica los módulos de funciones, los recursos en línea y las herramientas web que antes estaban dispersos, para construir una entrada de servicios integral, interconectada, eficiente y cómoda.

![plot_home_ui](/img/v0.9.0/en-US/Software_main_interface.webp)

La página de inicio se divide principalmente en tres partes: **Avisos**, **Enlaces rápidos** y **Herramientas**.

* **Avisos**: se encarga de recibir avisos y notificaciones push relacionados con el servidor
* **Enlaces rápidos**: se encarga de reunir las entradas del ámbito de las ciencias de la Tierra, en dos grandes grupos: **enlaces recomendados** y **enlaces personales**. Usted no puede modificar los enlaces recomendados; son recomendaciones integradas a través del servidor. Por supuesto, **si desea que su sitio web se añada a las recomendaciones de la página de inicio, póngase en contacto con nosotros mediante la página «Acerca de»**. Los enlaces personales los añade, elimina y gestiona usted según sus enlaces preferidos.
* **Herramientas**: el objetivo de diseño de estas herramientas es ofrecer funciones de cálculo u operación sencillas, independientes y básicas, para complementar las herramientas periféricas de uso habitual y facilitar el uso diario. En el futuro se harán en forma de complementos ampliables; por ahora no se pueden ampliar libremente.

## Herramientas

Las herramientas incluyen en la actualidad, sobre todo: **calculadora de alcalinidad, calculadora de dureza, calculadora de radiación de cuerpo negro, tabla periódica geoquímica, tarjeta de consulta rápida cronoestratigráfica, convertidor de elementos y óxidos, y conversor de unidades geológicas habituales**.

**Agradecemos especialmente a [Kurt Hollocher(Union College)](https://muse.union.edu/hollochk/kurt-hollocher/geochemistry/) la referencia pública de los métodos de cálculo de alcalinidad, de dureza y de radiación de cuerpo negro**.

### Calculadora de alcalinidad

Calcula la alcalinidad de una muestra de agua según el punto final de la valoración con ácido clorhídrico. Tras introducir la concentración de HCl, el volumen de la muestra de agua y el volumen de HCl consumido, da la alcalinidad (meq/L) y la convierte a g/L, meq/L, concentración molar y ppm en las cuatro formas de expresión HCO₃⁻, CO₃²⁻, CaCO₃ y 0.5(CaCO₃).

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/alkalinity_calculator.webp')}
    alt="Calculadora de alcalinidad"
    style={{width: '60%'}}
  />
</div>

### Calculadora de dureza

Calcula la dureza total del agua a partir de las concentraciones de magnesio y calcio. Tras introducir las concentraciones de Mg y Ca (ppm), las convierte en dureza expresada como CaCO₃ (ppm) y, según la clasificación del USGS, indica agua blanda, moderadamente dura, dura o muy dura.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/hardness_calculator.webp')}
    alt="Calculadora de dureza"
    style={{width: '60%'}}
  />
</div>

### Calculadora de radiación de cuerpo negro

Calcula la radiación de un cuerpo negro a partir de la temperatura. Tras introducir la temperatura en kelvin, da la potencia total radiada de Stefan-Boltzmann y la longitud de onda del máximo de Wien, y dibuja la curva espectral normalizada según la fórmula de Planck.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/black_body_radiation_calculator.webp')}
    alt="Calculadora de radiación de cuerpo negro"
    style={{width: '70%'}}
  />
</div>

### Tabla periódica geoquímica

Tabla periódica interactiva en la que se pueden consultar los elementos por grupos geoquímicos. Al seleccionar un elemento se pueden ver el número atómico, la masa atómica, la configuración electrónica, la abundancia cortical, los estados de oxidación y la masa molecular de los óxidos habituales. También se pueden resaltar grupos como tierras raras, elementos de alta fuerza de campo, elementos litófilos de ion grande, grupo del platino, y litófilos, siderófilos, calcófilos y atmófilos.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/geochemical_periodic_table.webp')}
    alt="Tabla periódica geoquímica"
    style={{width: '70%'}}
  />
</div>

### Tarjeta de consulta rápida cronoestratigráfica

Consulta rápida cronoestratigráfica basada en la Tabla Cronoestratigráfica Internacional de la ICS. Se puede buscar por el nombre en chino o en inglés, o introducir una edad (Ma) para localizar la unidad estratigráfica correspondiente, profundizar en la jerarquía y ver las edades de los límites y los colores oficiales de la IUGS.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/chronostratigraphic_quick_reference_card.webp')}
    alt="Tarjeta de consulta rápida cronoestratigráfica"
    style={{width: '60%'}}
  />
</div>

### Convertidor de elementos y óxidos

Convierte entre la fracción másica de un elemento y la de su óxido. Tras elegir un par como Si/SiO₂, Al/Al₂O₃, Fe/Fe₂O₃ o FeO, se introduce el wt% de un lado y, mediante el factor estequiométrico, se obtiene el resultado del otro. Cubre los elementos mayores de roca total y los óxidos traza habituales.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/element_oxide_converter.webp')}
    alt="Convertidor de elementos y óxidos"
    style={{width: '60%'}}
  />
</div>

### Conversor de unidades geológicas habituales

Convierte entre unidades de uso habitual en el trabajo geológico. Cubre ley y concentración (g/t, ppm, wt%, ppb, oz/t), presión y esfuerzo (GPa, kbar, MPa, bar, atm, psi), escala de microanálisis (mm, μm, nm, Å, pm), temperatura (℃, K, ℉) y edad isotópica (Ga, Ma, ka, yr), y añade valores de referencia geológicos habituales.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/geological_common_unit_converter.webp')}
    alt="Conversor de unidades geológicas habituales"
    style={{width: '60%'}}
  />
</div>
