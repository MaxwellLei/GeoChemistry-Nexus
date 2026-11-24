---
sidebar_position: 5
---

import useBaseUrl from '@docusaurus/useBaseUrl';

# Cálculo de la norma CIPW

El **cálculo de la norma CIPW** convierte los óxidos de elementos mayores de roca total (wt%) en un conjunto de minerales normativos idealizados. El resultado se usa para comparar rocas ígneas y para discriminar la saturación en sílice y la saturación en alúmina. La CIPW Norm la propusieron Cross, Iddings, Pirsson y Washington en 1902. Este módulo calcula por lotes en una tabla al estilo de Excel, y da el diagnóstico y el balance de masa de cada fila.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/cipw.webp')}
    alt="Interfaz CIPW"
    style={{width: '100%'}}
  />
</div>

## Interfaz

La página se compone de la barra superior y de la hoja de datos. A la izquierda de la barra superior está **Fe³⁺/ΣFe**, y a la derecha están **Ayuda**, **Ejemplo**, **Exportar**, **Borrar** y **Calcular**.

La hoja de cálculo se divide de izquierda a derecha en cuatro tramos, y encima de los encabezados de columna hay nombres de grupo:

| Zona | Grupo | Unidad | Función |
| --- | --- | --- | --- |
| Muestra | — | — | Nombre de la muestra; solo sirve de identificador y no participa en el cálculo. Esta columna está inmovilizada y permanece visible al desplazar en horizontal |
| Óxidos | Elementos mayores; otros óxidos / volátiles | wt% | Zona de entrada blanca |
| Diagnóstico | Diagnóstico | — | `Saturación SiO2`, `Estado Al2O3`, `Error de balance de masa (%)` |
| Minerales normativos | Minerales normativos CIPW | wt% | Zona de resultados en color claro. El encabezado es la abreviatura más el nombre, por ejemplo `Q(cuarzo)` |

Entre la zona de entrada y la zona de resultados hay una columna separadora estrecha. Las columnas de resultados están vacías antes del cálculo. La tabla ofrece inicialmente 500 filas. Un doble clic en el controlador de relleno rellena hacia abajo, como en Excel.

### Barra superior

| Control | Función |
| --- | --- |
| **Fe³⁺/ΣFe** | Solo cuando se usa el hierro total `FeOT`, reparte FeO y Fe₂O₃ según esa relación. Intervalo de `0` a `1`, valor predeterminado `0.15` (Le Maitre, 2002), se muestra con dos decimales |
| **Ayuda** | Abre la explicación del algoritmo dentro del programa |
| **Ejemplo** | Rellena tres filas de ejemplo: granito, basalto y andesita, con Fe₂O₃, FeO y MnO medidos |
| **Exportar** | Guarda la tabla actual como CSV en UTF-8, con el nombre de archivo predeterminado `CIPW_Results.csv` |
| **Borrar** | Tras la confirmación, borra todas las entradas, los resultados y los detalles del cálculo |
| **Calcular** | Calcula todas las filas que contienen óxidos válidos |

### Estado de la cabecera de fila

El punto junto al número de fila indica el estado de esa fila:

| Punto | Significado |
| --- | --- |
| Hueco gris | Hay entrada, pero todavía no se ha calculado |
| Relleno verde azulado | El cálculo ha tenido éxito, no hay advertencias y el balance de masa también es correcto |
| Relleno naranja | El cálculo ha tenido éxito, pero hay una advertencia, o el error de balance de masa es mayor que `1×10⁻⁶` |
| Relleno rojo | El cálculo de esa fila ha fallado. La columna de diagnóstico escribe «Error» y la causa |
| Hueco naranja | Después del cálculo se ha vuelto a modificar algún óxido, y el resultado actual ha caducado |

Todos los datos de ejemplo contienen MnO. El MnO se incorpora al FeO y genera una advertencia, así que las filas de ejemplo quedan en naranja después del cálculo: es el resultado esperado. Modificar el nombre de la muestra no hace caducar el resultado; modificar cualquier entrada de óxido, sí.

## Entrada

Cada fila es una muestra de roca total, y se rellena el porcentaje en masa de los óxidos (wt%). El programa solo lee las celdas **mayores que 0**; las celdas en blanco y el 0 se consideran no proporcionados. Las filas vacías se omiten automáticamente.

Se puede pegar directamente desde un programa de hojas de cálculo. El orden de las columnas es fijo; al pegar, alinee según los encabezados siguientes.

### Columnas de óxidos

Elementos mayores:

`SiO2`, `TiO2`, `Al2O3`, `Fe2O3`, `FeO`, `FeOT`, `MnO`, `MgO`, `CaO`, `Na2O`, `K2O`, `P2O5`

Otros óxidos / volátiles:

`ZrO2`, `Cr2O3`, `CO2`, `S`, `F`, `Cl`, `SO3`

`FeOT` es el hierro total expresado como equivalente de FeO. `S`, `F` y `Cl` se rellenan como porcentaje en masa del elemento.

### Tratamiento del hierro

| Situación de relleno | Tratamiento |
| --- | --- |
| Hay a la vez `FeO` y `Fe2O3`, y no hay `FeOT` | Se usa el reparto medido; el modo de tratamiento del hierro es «Medido» |
| Solo hay `FeO` o solo hay `Fe2O3` | El que falta se toma como 0; el modo es «Parcialmente asumido», y se da una advertencia |
| Solo hay `FeOT` | Se reparte según Fe³⁺/ΣFe; el modo es «Estimado a partir de FeOT» |
| `FeOT` aparece a la vez que `FeO` o `Fe2O3` | Se ignora `FeOT`; el modo es «Entrada inconsistente», y se da una advertencia |
| No hay ningún dato de hierro | Tanto FeO como Fe₂O₃ se toman como 0; el modo es «Ausente», y se da una advertencia |

El reparto cuando solo hay `FeOT` es:

```text
Fe2O3 = FeOT × (Fe³⁺/ΣFe) × 1.11134
FeO   = FeOT × (1 − Fe³⁺/ΣFe)
```

El coeficiente `1.11134` corresponde a la relación de masas molares entre Fe₂O₃ y el doble de FeO, y sirve para devolver el hierro férrico desde el equivalente de FeO al porcentaje en masa de Fe₂O₃.

Antes del cálculo, el `MnO` se convierte en equivalente de FeO según la relación de masas molares y se añade al FeO:

```text
equivalente de FeO = MnO × 71.844 / 70.937
```

Siempre que se haya rellenado MnO, en los detalles del cálculo aparecerá «MnO se convirtió a equivalente FeO por relación molar».

## Salida

### Columnas de diagnóstico

| Columna | Valores |
| --- | --- |
| **Saturación SiO2** | Sobresaturado, Saturado, Subsaturado |
| **Estado Al2O3** | Peralcalino, Metaluminoso, Peraluminoso |
| **Error de balance de masa (%)** | Desviación del cierre entre la masa de los minerales y los óxidos de entrada. Cuando es menor que `0.01` se usa notación científica; en caso contrario se conservan 4 decimales |

Significado de las tres saturaciones:

- **Sobresaturado**: después de terminar la asignación todavía queda SiO₂, y se anota como cuarzo `Q`.
- **Saturado**: el SiO₂ se agota exactamente; no hay cuarzo ni hace falta desilicatar.
- **Subsaturado**: el SiO₂ es insuficiente y se entra en las reacciones de desilicatación siguientes. Si toda la cadena de reacciones sigue sin eliminar el déficit de sílice, se añade una advertencia y se indica el déficit residual (mol/100 g). Los minerales normativos de ese caso no son fiables.

El estado de saturación en alúmina se juzga, antes de la asignación de los feldespatos, con el número de moles de óxido que queda en ese momento:

- **Peralcalino**: Na₂O + K₂O > Al₂O₃
- **Peraluminoso**: Al₂O₃ / (CaO + Na₂O + K₂O) > 1
- **Metaluminoso**: los demás casos

El apatito, la calcita, la fluorita, la halita y la thenardita ya han consumido antes el CaO o el Na₂O correspondientes, así que aquí se usa la cantidad restante.

### Minerales normativos

Los resultados se ordenan como se indica a continuación. Los nombres coinciden con los encabezados del programa.

| Abreviatura | Mineral | Abreviatura | Mineral |
| --- | --- | --- | --- |
| `Q` | Cuarzo | `Fo` | Forsterita |
| `Or` | Ortoclasa | `Fa` | Fayalita |
| `Ab` | Albita | `Mt` | Magnetita |
| `An` | Anortita | `Hm` | Hematites |
| `Le` | Leucita | `Ilm` | Ilmenita |
| `Ne` | Nefelina | `Cm` | Cromita |
| `Kp` | Pseudoleucita | `Ru` | Rutilo |
| `Cor` | Corindón | `Tn` | Titanita |
| `Ac` | Egirina | `Pf` | Perovskita |
| `Di` | Diópsido | `Z` | Circón |
| `Hd` | Hedenbergita | `Ap` | Apatito |
| `Wo` | Wollastonita | `Cc` | Calcita |
| `Cs` | Larnita | `Py` | Pirita |
| `En` | Enstatita | `Fl` | Fluorita |
| `Fs` | Ferrosilita | `Hl` | Halita |
| `ns` | Metasilicato sódico | `Th` | Thenardita |
| `ks` | Metasilicato potásico | | |

La ortoclasa, la albita y la egirina se cuentan internamente en unidades de fórmula: 1 mol de K₂O genera 2 mol de `Or`, y 1 mol de Na₂O genera 2 mol de `Ab` o de `Ac`. Lo que se escribe es siempre el porcentaje en masa.

## Detalles del cálculo

Después de seleccionar una fila calculada con éxito, el inspector de la derecha muestra:

- El nombre de la muestra. Si la columna de muestra está vacía, se muestra «Fila N».
- La saturación en sílice y la saturación en alúmina. Cuando es subsaturado, la saturación en sílice se resalta.
- **Modo de tratamiento del hierro**. Se resalta siempre que no sea «Medido».
- **Error de balance de masa**, conservado con 6 decimales. Se resalta cuando supera `1×10⁻⁶`.
- **Total de minerales**, es decir, la suma de los wt% de los minerales normativos, conservada con 4 decimales. El oxígeno liberado al formarse la pirita, la fluorita y la halita no se cuenta en este total, pero sí se cuenta en el balance de masa.
- **Minerales principales**: minerales con un contenido mayor que `0.01 wt%`, ordenados de mayor a menor; las entradas mayores que `10 wt%` se resaltan.
- **Advertencias**: falta o incoherencia de los datos de hierro, estimación a partir de FeOT, conversión de MnO, déficit residual de sílice, etc.

En la esquina superior derecha del inspector se puede exportar este detalle a CSV o copiarlo al portapapeles.

## Proceso de cálculo

A continuación se explica el orden real del código de cálculo actual. El apatito ocupa el calcio en primer lugar, y solo después se forman los minerales de volátiles; la titanita y el rutilo se colocan después de la anortita.

Las masas molares de los minerales se derivan todas de las masas molares de los óxidos según la fórmula química, para evitar que un redondeo manual provoque un desequilibrio de masa. La tolerancia numérica usada en el cálculo es `1×10⁻¹²`. Si en cualquier paso un óxido distinto del SiO₂ pasa a un valor claramente negativo, esa fila falla.

### 1. Normalización anhidra, hierro y manganeso

Todos los óxidos proporcionados y no negativos se normalizan al 100 %. El hierro se trata según la sección anterior, después el MnO se convierte en equivalente de FeO y, a continuación, **se normaliza otra vez**. Si el total no es mayor que 0, esa fila falla.

### 2. Conversión a número de moles

El número de moles de cada óxido = wt% normalizado / masa molar. La asignación posterior se hace toda en la escala mol/100 g.

### 3. Apatito

El apatito ocupa el calcio en primer lugar. 1 mol de `P2O5` se combina con `10/3` mol de `CaO` (sin OH ni F):

```text
Ap = min(P2O5, CaO / (10/3))
```

### 4. Minerales de volátiles

En el orden siguiente, se genera tanto como se pueda combinar:

| Mineral | Estequiometría |
| --- | --- |
| Calcita `Cc` | `min(CO2, CaO)` |
| Fluorita `Fl` | `min(F/2, CaO)`, es decir, 1 mol de CaO se combina con 2 mol de F |
| Pirita `Py` | `min(S/2, FeO)`, es decir, 1 mol de FeO se combina con 2 mol de S |
| Halita `Hl` | `min(Cl, 2×Na2O)` |
| Thenardita `Th` | `min(SO3, Na2O)` |

### 5. Circón, cromita e ilmenita

- Circón `Z`: todo el `ZrO2` y la misma cantidad de `SiO2`.
- Cromita `Cm`: `min(Cr2O3, FeO)`.
- Ilmenita `Ilm`: `min(TiO2, FeO)`.

En este paso solo se consume el titanio que puede combinarse con FeO. El TiO₂ restante se deja para después de la anortita.

### 6. Feldespatos, corindón y álcalis restantes

Primero se registra el estado de saturación en alúmina y después se asigna:

| Mineral | Condición y producto |
| --- | --- |
| Ortoclasa `Or` | `min(K2O, Al2O3)`; consume 6 veces SiO₂, y el número de moles del producto es el doble de ese valor |
| Albita `Ab` | `min(Na2O, Al2O3)`; consume igualmente 6 veces SiO₂, y el producto es el doble |
| Anortita `An` | `min(CaO, Al2O3)`; consume 2 veces SiO₂ |
| Corindón `Cor` | Al₂O₃ restante |
| Egirina `Ac` | `min(Na2O restante, Fe2O3)`; consume 4 veces SiO₂, y el producto es el doble |
| Metasilicato sódico `ns` | Na₂O restante y la misma cantidad de SiO₂ |
| Metasilicato potásico `ks` | K₂O restante y la misma cantidad de SiO₂ |

La etapa de los feldespatos permite que el SiO₂ se anote primero como valor negativo, y lo deja para el tratamiento unificado de la desilicatación posterior.

### 7. Titanita y rutilo

La titanita solo usa el CaO que queda después de la anortita, y **no usa el SiO₂ restante en ese momento para limitar la titanita**:

```text
Tn = min(TiO2 restante, CaO restante)
```

`Tn` consume al mismo tiempo la misma cantidad de SiO₂. El TiO₂ que todavía queda al final se anota como rutilo `Ru`.

### 8. Óxidos de hierro

```text
Mt = min(Fe2O3, FeO)
Hm = Fe2O3 restante
```

### 9. Piroxenos y wollastonita

Sea `MgFe = MgO + FeO`. La cantidad total de clinopiroxeno es `min(CaO, MgFe)`, y después se reparte según la relación magnesio-hierro en diópsido `Di` y hedenbergita `Hd`. Cada parte de piroxeno consume 2 partes de SiO₂.

El MgO y el FeO restantes pasan todos a enstatita `En` y ferrosilita `Fs`, y cada uno consume 1 parte de SiO₂. Después de agotar tanto el magnesio como el hierro, si todavía queda CaO, pasa todo a wollastonita `Wo`.

### 10. Saturación en sílice y desilicatación

El orden de desilicatación es: hipersteno → titanita → albita → ortoclasa → wollastonita → diópsido/hedenbergita → leucita. Cada paso solo consume el SiO₂ que todavía falta en ese momento.

| Orden | Reacción | SiO₂ liberado |
| --- | --- | --- |
| 1 | 2 enstatita/ferrosilita → 1 forsterita/fayalita | Cada 2 mol de piroxeno liberan 1 mol |
| 2 | Titanita `Tn` → perovskita `Pf` | 1 mol de `Tn` libera 1 mol |
| 3 | Albita `Ab` → nefelina `Ne` | 1 mol de `Ab` libera 2 mol |
| 4 | Ortoclasa `Or` → leucita `Le` | 1 mol de `Or` libera 1 mol |
| 5 | 2 wollastonita `Wo` → 1 larnita `Cs` | Cada 2 mol de `Wo` liberan 1 mol |
| 6 | Diópsido/hedenbergita → larnita + olivino | 1 mol de piroxeno libera 1 mol; el magnesio entra en `Fo` y el hierro entra en `Fa` |
| 7 | Leucita `Le` → pseudoleucita `Kp` | 1 mol de `Le` libera 1 mol |

Cuando queda SiO₂ se genera cuarzo, y el estado es sobresaturado. Cuando el SiO₂ es exactamente cero, es saturado. Si se entra en la tabla anterior, es subsaturado.

### 11. Porcentaje en masa y balance de masa

Masa del mineral normativo = número de moles × masa molar derivada de la fórmula química, y después se convierte a wt% respecto del total de óxidos normalizado.

El S, el F y el Cl de la pirita, la fluorita y la halita han sustituido al oxígeno de los óxidos. El oxígeno liberado no entra en ningún mineral, y al hacer el balance de masa se vuelve a sumar:

```text
moles de oxígeno liberado = Fl + Py + Hl / 2
error de balance de masa = |100 − (suma de wt% de los minerales + wt% del oxígeno liberado)|
```

Cuando el error es mayor que `1×10⁻⁶`, esa fila se anota como advertencia.

## Referencias

- Cross, W., Iddings, J. P., Pirsson, L. V., Washington, H. S. (1902). A quantitative chemico-mineralogical classification and nomenclature of igneous rocks. *The Journal of Geology*, 10, 555–690.
- Kelsey, C. H. (1965). Calculation of the C.I.P.W. norm. *Mineralogical Magazine*, 34, 276–282.
- Verma, S. P., Torres-Alvarado, I. S., Velasco-Tapia, F. (2003). A revised CIPW norm. *Schweizerische Mineralogische und Petrographische Mitteilungen*, 83, 197–216.
- Philpotts, A. R., Ague, J. J. (2009). *Principles of Igneous and Metamorphic Petrology* (2nd ed.). Cambridge University Press.
- Le Maitre, R. W. (ed.) (2002). *Igneous Rocks: A Classification and Glossary of Terms* (2nd ed.). Cambridge University Press.
