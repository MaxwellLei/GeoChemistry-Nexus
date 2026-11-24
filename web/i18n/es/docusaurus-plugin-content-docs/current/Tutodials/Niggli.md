---
sidebar_position: 6
---

import useBaseUrl from '@docusaurus/useBaseUrl';

# Cálculo de la norma de Niggli

El **cálculo de la norma de Niggli** produce dos conjuntos de resultados para la misma fila de óxidos de roca total:

1. **Valores de Niggli**: `si`, `al`, `fm`, `c`, `alk`, `k`, `mg`, `qz`, y además `ti` y `p`, que solo se muestran en los detalles del cálculo.
2. **Minerales normativos moleculares de Barth–Niggli (Catanorm)**: minerales normativos expresados en porcentaje catiónico (cat%), la composición de los miembros extremos y el recorrido de desilicatación cuando hay subsaturación en sílice.

El Catanorm lo establecieron Paul Niggli (1948) y Tom F. W. Barth (1948). Asigna según equivalentes catiónicos y reduce el peso que elementos pesados como el hierro tienen en el porcentaje en masa, por lo que se aproxima más que el wt% de la CIPW a las proporciones volumétricas de los minerales en lámina delgada. Los minerales normativos en porcentaje en masa se ven en [Cálculo de la norma CIPW](./CIPW).

El algoritmo toma como referencia el flujo de minerales normativos moleculares de GCDkit y se ha vuelto a implementar; el producto de la desilicatación ulterior de la leucita se anota en la kaliofilita `Kp`.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/niggli.webp')}
    alt="Interfaz de Niggli"
    style={{width: '100%'}}
  />
</div>

## Interfaz

La barra superior es la misma que en CIPW: **Fe³⁺/ΣFe**, **Ayuda**, **Ejemplo**, **Exportar**, **Borrar**, **Calcular**. El intervalo de Fe³⁺/ΣFe es de `0` a `1`, y el valor predeterminado es `0.15` (Le Maitre, 2002).

**Ejemplo** rellena tres filas: granito, basalto y fonolita (Phonolite). Las dos primeras filas son rocas volcánicas habituales, relativamente ricas en sílice; la fonolita sirve para ver resultados subsaturados del tipo de la nefelina y la leucita.

La hoja de cálculo se divide de izquierda a derecha en cinco tramos:

| Zona | Grupo | Unidad | Contenido |
| --- | --- | --- | --- |
| Muestra | — | — | Nombre de la muestra; no participa en el cálculo y permanece visible al desplazar en horizontal |
| Óxidos | Elementos mayores; otros óxidos / volátiles | wt% | Zona de entrada |
| Valores de Niggli | Valores de Niggli | Adimensional | `si`, `al`, `fm`, `c`, `alk`, `k`, `mg`, `qz` |
| Diagnóstico | Diagnóstico | — | Estado de saturación en sílice, estado de saturación en alúmina, suma mineral |
| Minerales normativos | Minerales normativos | cat% | Porcentaje catiónico. La última columna es `Sum(suma)` |

La columna de muestra está inmovilizada. La tabla tiene inicialmente 500 filas y admite rellenar hacia abajo con un doble clic en el controlador de relleno. Después de cambiar la entrada, las filas ya calculadas pasan al estado caducado y hay que volver a pulsar **Calcular**.

### Estado de la cabecera de fila

| Punto | Significado |
| --- | --- |
| Hueco gris | Hay entrada, pero todavía no se ha calculado |
| Relleno verde azulado | El cálculo ha tenido éxito y no hay advertencias |
| Relleno naranja | El cálculo ha tenido éxito, pero se ha usado FeOT para estimar el estado de valencia del hierro |
| Relleno rojo | El cálculo ha fallado. La columna de diagnóstico escribe «Error de cálculo» y la causa |
| Hueco naranja | Después del cálculo se han modificado óxidos y el resultado ha caducado |

Las muestras en las que se han rellenado a la vez FeO y Fe₂O₃ muestran un punto verde azulado cuando el cálculo tiene éxito. Cuando solo hay FeOT aparece una advertencia del estado de valencia del hierro, y la cabecera de fila es naranja.

## Entrada

Cada fila es una muestra, y la unidad es el porcentaje en masa de los óxidos (wt%). Solo se leen las celdas mayores que 0; las celdas en blanco y el 0 se consideran no proporcionados.

Elementos mayores, hasta `K2O`:

`SiO2`, `TiO2`, `Al2O3`, `Fe2O3`, `FeO`, `FeOT`, `MnO`, `MgO`, `CaO`, `Na2O`, `K2O`

Otros óxidos / volátiles:

`CO2`, `P2O5`, `F`, `S`

Este módulo no lee `ZrO2`, `Cr2O3`, `Cl` ni `SO3`. Si esas cuatro partidas deben entrar en los minerales normativos, use el módulo CIPW.

### Tratamiento del hierro

El hierro se trata antes de convertirlo en cationes. Al repartirlo, el hierro se cuenta primero como átomos de Fe: el número de moles de hierro total = masa de hierro / masa molar de FeO; después se divide en férrico y ferroso según Fe³⁺/ΣFe, y la mitad del hierro férrico se escribe en Fe₂O₃.

| Situación de relleno | Tratamiento |
| --- | --- |
| Hay a la vez `Fe2O3` y `FeO` | Se usan los valores medidos. Aunque al lado también esté `FeOT`, ya no se reparte |
| Hay `FeOT`, y no están presentes a la vez Fe₂O₃ y FeO | Se reparte `FeOT` según la relación actual, y se advierte en los detalles |
| Solo hay `Fe2O3`, sin `FeO` ni `FeOT` | Ese Fe₂O₃ se toma como hierro total y, después, se reparte según la relación en Fe₂O₃ y FeO |
| Solo hay `FeO`, sin `Fe2O3` ni `FeOT` | Ese FeO se toma como hierro total y, después, se reparte según la relación en Fe₂O₃ y FeO |

Por tanto, si solo se ha analizado FeO o solo se ha analizado Fe₂O₃, el resultado cambia con la relación de la barra superior. Si ambos son valores medidos, rellene las dos columnas.

El `MnO` se conserva hasta la conversión a cationes. Entra en `fm` y `mg` de los valores de Niggli y, al entrar en el Catanorm, se incorpora al Fe²⁺.

## Valores de Niggli

Los parámetros se calculan con el **número molecular** de los óxidos; número molecular = wt% / masa molar. Aquí `al` es el número molecular de Al₂O₃: una parte de Al₂O₃ cuenta como 1, y no es la misma base que en el Catanorm posterior, donde «una parte de Al₂O₃ cuenta como 2 Al».

```text
al  = n(Al2O3)
fm  = n(FeO) + 2 n(Fe2O3) + n(MnO) + n(MgO)
c   = n(CaO)
alk = n(Na2O) + n(K2O)
base = al + fm + c + alk

si  = 100 × n(SiO2) / base
al, fm, c y alk se convierten igualmente en partes de 100 respecto de base
ti  = 100 × n(TiO2) / base
p   = 100 × n(P2O5) / base
k   = n(K2O) / (n(Na2O) + n(K2O))
mg  = n(MgO) / (n(MgO) + n(FeO) + 2 n(Fe2O3) + n(MnO))
```

Índice de cuarzo `qz`:

```text
cuando al > alk,    qz = si − (100 + 4 al)
cuando al ≤ alk,    qz = si − (100 + 3 al + alk)
```

La segunda se usa en las muestras en las que el aluminio no supera a los álcalis. La ventana «Ayuda» del programa solo muestra la primera.

`si`, `al`, `fm`, `c`, `alk`, `ti`, `p` y `qz` conservan 2 decimales, y `k` y `mg` conservan 3 decimales. La tabla solo incluye las ocho columnas del primer grupo; `ti` y `p` están en los detalles del cálculo. Cuando `qz` es positivo, en los detalles se añade el signo positivo.

En la tabla, `al + fm + c + alk` debe ser 100. `si` puede ser muy superior a 100, porque el silicio no forma parte de esas 100 partes. `k` y `mg` son relaciones de 0 a 1.

## Columnas de diagnóstico

| Columna | Significado |
| --- | --- |
| **Saturación en sílice** | Sobresaturado en sílice, saturado en sílice, subsaturado en sílice |
| **Estado de alúmina** | Peraluminoso, metaluminoso, peralcalino |
| **Suma mineral** | Suma de los porcentajes catiónicos del Catanorm, conservada con 2 decimales |

El estado de saturación en sílice se determina por los minerales finales, y el umbral es `0.001 cat%`:

- Si hay cuarzo `Q`: sobresaturado en sílice.
- Si no hay cuarzo, pero hay nefelina `Ne`, leucita `Lc`, kaliofilita `Kp` u olivino `Ol`: subsaturado en sílice.
- El resto: saturado en sílice.

El signo de `qz` sirve para ver si el silicio está en exceso o en defecto respecto de la base de Niggli. La «saturación en sílice» de la tabla se fija en los minerales normativos. Las dos columnas se pueden contrastar; su base de decisión es distinta.

El estado de saturación en alúmina se juzga con los cationes normalizados, y ocurre antes de la asignación de los minerales:

- **Peraluminoso**: Al > Na + K + Ca
- **Metaluminoso**: Na + K < Al ≤ Na + K + Ca
- **Peralcalino**: Al ≤ Na + K

## Minerales normativos

La unidad de las columnas de minerales es el porcentaje catiónico. `Plag` es igual a `Ab + An`. El total `Sum` cuenta `Plag` y no vuelve a sumar `Ab` y `An`. Para comprobar el grado de cierre, mire `Sum` o la suma mineral de la columna de diagnóstico.

| Abreviatura | Mineral | Abreviatura | Mineral |
| --- | --- | --- | --- |
| `Q` | Cuarzo | `Hy` | Ortopiroxeno |
| `C` | Corindón | `Ol` | Olivino |
| `Or` | Ortoclasa | `Cs` | Rankinita |
| `Plag` | Plagioclasa | `Wo` | Wollastonita |
| `Ab` | Albita | `Mt` | Magnetita |
| `An` | Anortita | `Hm` | Hematites |
| `Lc` | Leucita | `Il` | Ilmenita |
| `Ne` | Nefelina | `Tn` | Titanita |
| `Kp` | Kaliofilita | `Pf` | Perovskita |
| `Ac` | Egirina | `Ru` | Rutilo |
| `Ns` | Metasilicato sódico | `Ap` | Apatito |
| `Ks` | Metasilicato potásico | `Fr` | Fluorita |
| `Di` | Diópsido | `Py` | Pirita |
| | | `Cc` | Calcita |

Las proporciones de los miembros extremos no ocupan columnas de resultados propias; se escriben todas en los detalles del cálculo.

## Detalles del cálculo

Después de seleccionar una fila correcta, el inspector de la derecha muestra seis tramos:

1. **Clasificación de la roca y estado de saturación**: estado de saturación en sílice, estado de saturación en alúmina y suma de los minerales normativos. Cuando es subsaturado se resalta la saturación en sílice, y cuando es peralcalino se resalta la saturación en alúmina.
2. **Valores de Niggli**: `si`, `al`, `fm`, `c`, `alk`, `ti`, `p`, `k`, `mg`, `qz`.
3. **Miembros extremos de los minerales**, que solo aparecen cuando existe el mineral correspondiente:
   - Plagioclasa `An% = 100 × An / (Ab + An)`
   - El diópsido se fija en Wo 50 %, y el 50 % restante se reparte en En y Fs según la relación magnesio-hierro
   - Olivino Fo y Fa; Fo es igual al índice de magnesio siguiente
   - Índice de magnesio `Mgr = 100 × En / (En + Fs)`, calculado antes de incorporar el ortopiroxeno al diópsido
4. **Recorrido de las reacciones de desilicatación**: cuando sobra silicio, se indica el cat% del cuarzo; cuando hay déficit de silicio, se escribe paso a paso qué mineral se ha consumido y si el déficit ya se ha eliminado.
5. **Minerales normativos principales**: minerales mayores que `1 cat%`, de mayor a menor, sin el `Plag` repetido. Las entradas mayores que `10 cat%` se resaltan.
6. **Advertencias**: la implementación actual escribe una advertencia cuando el estado de valencia del hierro se estima a partir de FeOT.

## Proceso de cálculo

### 1. Base catiónica

Cada óxido se convierte primero en cantidad catiónica: `(wt% / masa molar) × número de cationes de la fórmula`. El número de cationes de SiO₂, TiO₂, FeO, MnO, MgO, CaO, CO₂, F y S es 1; el de Al₂O₃, Fe₂O₃, Na₂O, K₂O y P₂O₅ es 2.

Después de sumar todos los cationes, se escalan a 100, y esa cantidad sirve de reserva de asignación del Catanorm. Cuando el total se aproxima a 0, esa fila falla y se indica que la suma catiónica es insuficiente. En este paso **ya no** se normalizan los wt% de los óxidos a 100; el H₂O no indicado y la pérdida por calcinación no entran en ese 100.

### 2. Minerales accesorios y sales

Primero, el Mn se incorpora al Fe²⁺. Después, según la estequiometría catiónica:

| Orden | Mineral | Punto de la estequiometría |
| --- | --- | --- |
| 1 | Calcita `Cc` | Cantidad de mineral = `2 × CO2`, y consume la misma cantidad de Ca |
| 2 | Apatito `Ap` | Cuando P es escaso respecto de F, `Ap = 3P` y se consume una parte de F; en caso contrario, todo el F restante se incorpora al apatito |
| 3 | Fluorita `Fr` | F restante: `Fr = 1.5 × F`, y consume `0.5 × F` de Ca |
| 4 | Pirita `Py` | `Py = 1.5 × S`, y consume `0.5 × S` de Fe²⁺ |
| 5 | Ilmenita `Il` | El menor entre Ti y Fe²⁺ determina la cantidad producida, y la cantidad de mineral es el doble de esa cantidad |

CO₂, P, F y S ocupan de forma preferente Ca o Fe²⁺. Cuando estos volátiles superan claramente a los cationes con los que pueden combinarse, los minerales cálcicos y magnésicos posteriores quedan distorsionados, y conviene comprobarlos con la suma mineral y el registro de desilicatación.

### 3. Feldespatos, precursores de feldespatoides y minerales de titanio

La unidad de asignación es el catión. La ortoclasa, la albita y la anortita, cuando el miembro extremo es suficiente, se agrupan todas de 5 cationes.

1. K, junto con Al y Si, genera ortoclasa `Or` (cada K se combina con 1 Al y 3 Si). El K restante genera metasilicato potásico `Ks`.
2. Na, junto con Al y Si, genera albita `Ab`. El Na restante genera, además, egirina `Ac` con Fe³⁺ (cada Na se combina con 1 Fe³⁺ y 2 Si). El Na que todavía queda genera metasilicato sódico `Ns`.
3. El Al restante genera anortita `An` con Ca. Cuando el Ca es insuficiente, se limita por el aluminio: cada Al limitante corresponde a 0.5 Ca y 1 Si.
4. El Ti restante genera titanita `Tn` con Ca y Si (3 cationes). El Ti que todavía queda es rutilo `Ru`.
5. El Al que queda al final es corindón `C`.

### 4. Óxidos de hierro y piroxenos

Fe³⁺ y Fe²⁺ generan magnetita `Mt`: cuando Fe³⁺ no supera el doble de Fe²⁺, `Mt = 1.5 × Fe³⁺`. El Fe³⁺ restante es hematites `Hm`.

El Ca, el Mg y el Fe²⁺ restantes se convierten respectivamente en wollastonita `Wo`, moléculas de enstatita y moléculas de ferrosilita. Cada una es el doble del número de cationes y consume una parte igual de Si. La suma de las moléculas de enstatita y de ferrosilita constituye el ortopiroxeno `Hy`. En ese momento se calcula el índice de magnesio.

`Wo` y `Hy` se combinan de nuevo, con el mismo número de cationes, en diópsido `Di`: el de menor cantidad entra por completo en `Di`, y el número de cationes de `Di` es el doble de esa cantidad; el de mayor cantidad permanece como `Wo` o `Hy`.

### 5. Cuarzo o desilicatación en siete pasos

Cuando el Si restante es ≥ 0, todo pasa a cuarzo `Q` y el cálculo termina aquí.

Cuando Si < 0, el déficit es `D = −Si`. Cada paso de los siguientes o bien reduce el déficit a 0 y termina, o bien agota ese mineral y pasa al paso siguiente. Los coeficientes son porcentajes catiónicos:

| Paso | Reacción | Por cada parte de déficit de Si eliminada |
| --- | --- | --- |
| 1 | Ortopiroxeno → olivino | Se consumen 4 `Hy` y se generan 3 `Ol` |
| 2 | Titanita → perovskita | Se consumen 3 `Tn` y se generan 2 `Pf` |
| 3 | Albita → nefelina | Se consumen 2.5 `Ab` y se generan 1.5 `Ne` |
| 4 | Ortoclasa → leucita | Se consumen 5 `Or` y se generan 4 `Lc` |
| 5 | Leucita → kaliofilita | Se consumen 4 `Lc` y se generan 3 `Kp` |
| 6 | Wollastonita → rankinita | Se consumen 4 `Wo` y se generan 3 `Cs` |
| 7 | Diópsido → rankinita + olivino | Se consumen 4 `Di` y se generan 1.5 `Cs` y 1.5 `Ol` |

El producto del paso 5 es `Kp`. Cuando termina la desilicatación, la plagioclasa es `Plag = Ab + An`, y después se juzga el estado de saturación en sílice según las reglas anteriores.

## Referencias

- Niggli, P. (1948). *Gesteine und Minerallagerstätten*. Birkhäuser, Basel.
- Barth, T. F. W. (1948). The distribution of oxygen in the lithosphere. *The Journal of Geology*, 56(1), 41–49.
- Janoušek, V., Farrow, C. M., Erban, V. (2006). Interpretation of whole-rock geochemical data in igneous geochemistry: introducing Geochemical Data Toolkit (GCDkit). *Journal of Petrology*, 47(6), 1255–1259.
- Le Maitre, R. W. (ed.) (2002). *Igneous Rocks: A Classification and Glossary of Terms* (2nd ed.). Cambridge University Press.
- GCDkit: [https://www.gcdkit.org/](https://www.gcdkit.org/)
