---
sidebar_position: 5
---

import useBaseUrl from '@docusaurus/useBaseUrl';

# Cálculo CIPW de minerais normativos

O **cálculo CIPW de minerais normativos** converte os óxidos de elementos maiores de rocha total (wt%) em um conjunto idealizado de minerais normativos. O resultado serve à comparação de rochas ígneas e à discriminação da saturação em sílica e em alumina. A norma CIPW foi proposta por Cross, Iddings, Pirsson e Washington em 1902. Este módulo calcula em lote numa planilha no estilo do Excel e apresenta o diagnóstico e o balanço de massa de cada linha.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/cipw.webp')}
    alt="Interface CIPW"
    style={{width: '100%'}}
  />
</div>

## Interface

A página é formada pela barra superior e pela planilha de dados. À esquerda da barra fica **Fe³⁺/ΣFe**; à direita ficam **Explicação**, **Exemplo**, **Exportar**, **Limpar** e **Calcular**.

A planilha se divide, da esquerda para a direita, em quatro trechos. Acima dos cabeçalhos de coluna há nomes de grupo:

| Área | Grupo | Unidade | Função |
| --- | --- | --- | --- |
| Amostra | — | — | Nome da amostra. Serve apenas como identificador e não entra no cálculo. A coluna fica congelada e permanece visível na rolagem horizontal |
| Óxidos | Elementos maiores; Outros óxidos / voláteis | wt% | Área de entrada, em branco |
| Diagnóstico | Diagnóstico | — | `Saturação em SiO2`, `Estado Al2O3`, `Erro de balanço de massa (%)` |
| Minerais normativos | Minerais normativos CIPW | wt% | Área de resultados em tom claro. O cabeçalho traz a abreviação e o nome do mineral, por exemplo `Q(quartzo)` |

Entre a área de entrada e a de resultados há uma coluna estreita de separação. As colunas de resultado ficam vazias antes do cálculo. A tabela começa com 500 linhas. Um clique duplo na alça de preenchimento preenche para baixo, como no Excel.

### Barra superior

| Controle | Função |
| --- | --- |
| **Fe³⁺/ΣFe** | Somente quando se usa o ferro total `FeOT`, reparte FeO e Fe₂O₃ por essa razão. Intervalo `0`–`1`, padrão `0.15` (Le Maitre, 2002), exibido com duas casas decimais |
| **Explicação** | Abre a descrição do algoritmo dentro do software |
| **Exemplo** | Preenche três linhas de exemplo: granito, basalto e andesito, com Fe₂O₃, FeO e MnO medidos |
| **Exportar** | Grava a tabela atual como CSV em UTF-8. O nome de arquivo padrão é `CIPW_Results.csv` |
| **Limpar** | Depois da confirmação, apaga todas as entradas, os resultados e os detalhes do cálculo |
| **Calcular** | Calcula todas as linhas que contêm óxidos válidos |

### Estado do cabeçalho da linha

O ponto ao lado do número da linha indica o estado daquela linha:

| Ponto | Significado |
| --- | --- |
| Cinza vazado | Já há entrada, mas ainda não houve cálculo |
| Verde-azulado preenchido | Cálculo bem-sucedido, sem avisos, e o balanço de massa foi aprovado |
| Laranja preenchido | Cálculo bem-sucedido, mas há avisos, ou o erro de balanço de massa é maior que `1×10⁻⁶` |
| Vermelho preenchido | O cálculo da linha falhou. A coluna de diagnóstico traz «Erro» e o motivo |
| Laranja vazado | Os óxidos foram alterados depois do cálculo, e o resultado atual está desatualizado |

Todos os dados de exemplo contêm MnO. O MnO é incorporado ao FeO e gera um aviso, por isso as linhas de exemplo ficam laranja depois do cálculo. Esse resultado é o esperado. Alterar o nome da amostra não desatualiza o resultado; alterar qualquer óxido de entrada desatualiza.

## Entrada

Cada linha é uma amostra de rocha total. Preencha a porcentagem em massa dos óxidos (wt%). O software lê apenas células **maiores que 0**. Células em branco e 0 são tratadas como não informadas. Linhas vazias são ignoradas automaticamente.

Você pode colar direto de um programa de planilhas. A ordem das colunas é fixa: alinhe a colagem pelos cabeçalhos abaixo.

### Colunas de óxidos

Elementos maiores:

`SiO2`, `TiO2`, `Al2O3`, `Fe2O3`, `FeO`, `FeOT`, `MnO`, `MgO`, `CaO`, `Na2O`, `K2O`, `P2O5`

Outros óxidos / voláteis:

`ZrO2`, `Cr2O3`, `CO2`, `S`, `F`, `Cl`, `SO3`

`FeOT` é o ferro total expresso como equivalente de FeO. `S`, `F` e `Cl` são informados como porcentagem em massa do elemento.

### Tratamento do ferro

| Situação informada | Tratamento |
| --- | --- |
| Há `FeO` e `Fe2O3` ao mesmo tempo, e não há `FeOT` | Usa a partição medida. O modo de tratamento do ferro é «Valor medido» |
| Há apenas `FeO` ou apenas `Fe2O3` | O item ausente entra como 0. O modo é «Suposição parcial», e um aviso é emitido |
| Há apenas `FeOT` | Reparte pela razão Fe³⁺/ΣFe. O modo é «Estimado a partir de FeOT» |
| `FeOT` aparece junto com `FeO` ou `Fe2O3` | `FeOT` é ignorado. O modo é «Entrada inconsistente», e um aviso é emitido |
| Não há nenhum dado de ferro | FeO e Fe₂O₃ entram como 0. O modo é «Ausente», e um aviso é emitido |

A repartição quando existe apenas `FeOT` é:

```text
Fe2O3 = FeOT × (Fe³⁺/ΣFe) × 1.11134
FeO   = FeOT × (1 − Fe³⁺/ΣFe)
```

O coeficiente `1.11134` corresponde à razão entre a massa molar de Fe₂O₃ e o dobro da massa molar de FeO. Ele converte o ferro trivalente, expresso como equivalente de FeO, de volta à porcentagem em massa de Fe₂O₃.

Antes do cálculo, `MnO` é convertido em equivalente de FeO pela razão de massas molares e somado ao FeO:

```text
Equivalente de FeO = MnO × 71.844 / 70.937
```

Sempre que MnO é informado, os detalhes do cálculo incluem «MnO foi convertido em equivalente de FeO pela razão molar».

## Saída

### Colunas de diagnóstico

| Coluna | Valores |
| --- | --- |
| **Saturação em SiO2** | Supersaturado, Saturado, Subsaturado |
| **Estado Al2O3** | Peralcalino, Metaluminoso, Peraluminoso |
| **Erro de balanço de massa (%)** | Desvio de fechamento entre a massa dos minerais e os óxidos de entrada. Abaixo de `0.01`, usa notação científica; caso contrário, mantém 4 casas decimais |

O significado dos três estados de saturação:

- **Supersaturado**: ainda resta SiO₂ depois da distribuição, registrado como quartzo `Q`.
- **Saturado**: o SiO₂ se esgota exatamente. Não há quartzo e também não é preciso dessilicar.
- **Subsaturado**: o SiO₂ é insuficiente e entram as reações de dessilicação abaixo. Se a cadeia inteira ainda não eliminar o déficit de sílica, um aviso adicional é incluído, com o déficit residual (mol/100 g). Os minerais normativos, nesse caso, não são confiáveis.

O estado de saturação em alumina é avaliado antes da distribuição dos feldspatos, com o número de mols de óxidos ainda restante naquele momento:

- **Peralcalino**: Na₂O + K₂O > Al₂O₃
- **Peraluminoso**: Al₂O₃ / (CaO + Na₂O + K₂O) > 1
- **Metaluminoso**: os demais casos

Apatita, calcita, fluorita, halita e thenardita já consumiram o CaO ou o Na₂O correspondentes, por isso o que se usa aqui é a quantidade restante.

### Minerais normativos

Os resultados seguem a ordem abaixo. Os nomes correspondem aos cabeçalhos do software.

| Abreviação | Mineral | Abreviação | Mineral |
| --- | --- | --- | --- |
| `Q` | Quartzo | `Fo` | Forsterita |
| `Or` | Ortoclásio | `Fa` | Fayalita |
| `Ab` | Albita | `Mt` | Magnetita |
| `An` | Anortita | `Hm` | Hematita |
| `Le` | Leucita | `Ilm` | Ilmenita |
| `Ne` | Nefelina | `Cm` | Cromita |
| `Kp` | Pseudoleucita | `Ru` | Rutilo |
| `Cor` | Coríndon | `Tn` | Titanita |
| `Ac` | Egirina | `Pf` | Perovskita |
| `Di` | Diopsídio | `Z` | Zircão |
| `Hd` | Hedenbergita | `Ap` | Apatita |
| `Wo` | Wollastonita | `Cc` | Calcita |
| `Cs` | Larnita | `Py` | Pirita |
| `En` | Enstatita | `Fl` | Fluorita |
| `Fs` | Ferrosilita | `Hl` | Halita |
| `ns` | Metassilicato de sódio | `Th` | Thenardita |
| `ks` | Metassilicato de potássio | | |

Ortoclásio, albita e egirina são contados internamente em unidades de fórmula: 1 mol de K₂O gera 2 mol de `Or`, e 1 mol de Na₂O gera 2 mol de `Ab` ou de `Ac`. O que se escreve é sempre a porcentagem em massa.

## Detalhes do cálculo

Depois de selecionar uma linha calculada com sucesso, o inspetor à direita mostra:

- O nome da amostra. Se a coluna de amostra estiver vazia, aparece «Linha N».
- A saturação em sílica e a saturação em alumina. No caso subsaturado, a saturação em sílica fica em destaque.
- O **modo de tratamento do ferro**. Qualquer modo que não seja «Valor medido» fica em destaque.
- O **erro de balanço de massa**, com 6 casas decimais. Acima de `1×10⁻⁶`, fica em destaque.
- O **total de minerais**, isto é, a soma dos wt% dos minerais normativos, com 4 casas decimais. O oxigênio liberado na formação de pirita, fluorita e halita não entra nesse total, mas entra no balanço de massa.
- **Minerais principais**: minerais com teor maior que `0.01 wt%`, do maior para o menor. Entradas maiores que `10 wt%` ficam em destaque.
- **Avisos**: ferro ausente ou inconsistente, estimativa a partir de FeOT, conversão de MnO, déficit residual de sílica e outros.

No canto superior direito do inspetor, é possível exportar esses detalhes como CSV ou copiá-los para a área de transferência.

## Processo de cálculo

A ordem abaixo é a do código de cálculo atual. A apatita ocupa o cálcio primeiro; só depois se formam os minerais de voláteis. Titanita e rutilo ficam depois da anortita.

As massas molares dos minerais são todas derivadas das massas molares dos óxidos, segundo a fórmula química, para evitar desequilíbrio de massa por arredondamento manual. A tolerância numérica do cálculo é `1×10⁻¹²`. Se qualquer etapa tornar claramente negativo um óxido que não seja SiO₂, a linha falha.

### 1. Normalização anidra, ferro e manganês

Todos os óxidos informados e não negativos são normalizados a 100%. O ferro é tratado como na seção anterior, o MnO é convertido em equivalente de FeO e, em seguida, **há uma nova normalização**. Se o total não for maior que 0, a linha falha.

### 2. Conversão em número de mols

O número de mols de cada óxido = wt% normalizado / massa molar. Toda a distribuição seguinte ocorre na escala mol/100 g.

### 3. Apatita

A apatita ocupa o cálcio primeiro. 1 mol de `P2O5` combina com `10/3` mol de `CaO` (sem OH nem F):

```text
Ap = min(P2O5, CaO / (10/3))
```

### 4. Minerais de voláteis

Na ordem abaixo, forma-se tanto quanto for possível combinar:

| Mineral | Estequiometria |
| --- | --- |
| Calcita `Cc` | `min(CO2, CaO)` |
| Fluorita `Fl` | `min(F/2, CaO)`, isto é, 1 mol de CaO combina com 2 mol de F |
| Pirita `Py` | `min(S/2, FeO)`, isto é, 1 mol de FeO combina com 2 mol de S |
| Halita `Hl` | `min(Cl, 2×Na2O)` |
| Thenardita `Th` | `min(SO3, Na2O)` |

### 5. Zircão, cromita e ilmenita

- Zircão `Z`: todo o `ZrO2` com a mesma quantidade de `SiO2`.
- Cromita `Cm`: `min(Cr2O3, FeO)`.
- Ilmenita `Ilm`: `min(TiO2, FeO)`.

Esta etapa consome apenas o titânio que pode se combinar com FeO. O TiO₂ restante fica para depois da anortita.

### 6. Feldspatos, coríndon e álcalis restantes

Primeiro registra-se o estado de saturação em alumina e depois se distribui:

| Mineral | Condição e produto |
| --- | --- |
| Ortoclásio `Or` | `min(K2O, Al2O3)`, consome 6 vezes SiO₂, e o número de mols do produto é o dobro desse valor |
| Albita `Ab` | `min(Na2O, Al2O3)`, também consome 6 vezes SiO₂, e o produto é o dobro |
| Anortita `An` | `min(CaO, Al2O3)`, consome 2 vezes SiO₂ |
| Coríndon `Cor` | Al₂O₃ restante |
| Egirina `Ac` | `min(Na2O restante, Fe2O3)`, consome 4 vezes SiO₂, e o produto é o dobro |
| Metassilicato de sódio `ns` | Na₂O restante com a mesma quantidade de SiO₂ |
| Metassilicato de potássio `ks` | K₂O restante com a mesma quantidade de SiO₂ |

Na etapa dos feldspatos, o SiO₂ pode ser registrado primeiro como valor negativo, para a dessilicação posterior tratar o conjunto.

### 7. Titanita e rutilo

A titanita usa apenas o CaO que resta depois da anortita e **não usa o SiO₂ restante daquele momento para limitar a titanita**:

```text
Tn = min(TiO2 restante, CaO restante)
```

`Tn` consome, ao mesmo tempo, a mesma quantidade de SiO₂. O TiO₂ que ainda restar no fim é registrado como rutilo `Ru`.

### 8. Óxidos de ferro

```text
Mt = min(Fe2O3, FeO)
Hm = Fe2O3 restante
```

### 9. Piroxênio e wollastonita

Defina `MgFe = MgO + FeO`. O total de clinopiroxênio é `min(CaO, MgFe)` e, em seguida, é repartido em diopsídio `Di` e hedenbergita `Hd` pela razão magnésio/ferro. Cada porção de piroxênio consome 2 porções de SiO₂.

Todo o MgO e o FeO restantes tornam-se enstatita `En` e ferrosilita `Fs`, cada um consumindo 1 porção de SiO₂. Depois que o magnésio e o ferro se esgotam, se ainda houver CaO, ele se torna integralmente wollastonita `Wo`.

### 10. Saturação em sílica e dessilicação

A ordem da dessilicação é: hiperstênio → titanita → albita → ortoclásio → wollastonita → diopsídio/hedenbergita → leucita. Cada etapa consome apenas o SiO₂ que ainda falta naquele momento.

| Ordem | Reação | SiO₂ liberado |
| --- | --- | --- |
| 1 | 2 enstatita/ferrosilita → 1 forsterita/fayalita | cada 2 mol de piroxênio liberam 1 mol |
| 2 | Titanita `Tn` → perovskita `Pf` | 1 mol de `Tn` libera 1 mol |
| 3 | Albita `Ab` → nefelina `Ne` | 1 mol de `Ab` libera 2 mol |
| 4 | Ortoclásio `Or` → leucita `Le` | 1 mol de `Or` libera 1 mol |
| 5 | 2 wollastonita `Wo` → 1 larnita `Cs` | cada 2 mol de `Wo` liberam 1 mol |
| 6 | Diopsídio/hedenbergita → larnita + olivina | 1 mol de piroxênio libera 1 mol; o magnésio entra em `Fo` e o ferro entra em `Fa` |
| 7 | Leucita `Le` → pseudoleucita `Kp` | 1 mol de `Le` libera 1 mol |

Quando resta SiO₂, forma-se quartzo e o estado é supersaturado. Quando o SiO₂ é exatamente zero, o estado é saturado. Entrar na tabela acima significa subsaturado.

### 11. Porcentagem em massa e balanço de massa

A massa do mineral normativo = número de mols × massa molar derivada da fórmula química e, em seguida, é convertida em wt% em relação ao total de óxidos normalizado.

O S, o F e o Cl da pirita, da fluorita e da halita substituem o oxigênio dos óxidos. O oxigênio liberado não entra em nenhum mineral e é somado de volta no balanço de massa:

```text
Número de mols de oxigênio liberado = Fl + Py + Hl / 2
Erro de balanço de massa = |100 − (soma dos wt% dos minerais + wt% do oxigênio liberado)|
```

Quando o erro é maior que `1×10⁻⁶`, a linha é registrada com aviso.

## Referências

- Cross, W., Iddings, J. P., Pirsson, L. V., Washington, H. S. (1902). A quantitative chemico-mineralogical classification and nomenclature of igneous rocks. *The Journal of Geology*, 10, 555–690.
- Kelsey, C. H. (1965). Calculation of the C.I.P.W. norm. *Mineralogical Magazine*, 34, 276–282.
- Verma, S. P., Torres-Alvarado, I. S., Velasco-Tapia, F. (2003). A revised CIPW norm. *Schweizerische Mineralogische und Petrographische Mitteilungen*, 83, 197–216.
- Philpotts, A. R., Ague, J. J. (2009). *Principles of Igneous and Metamorphic Petrology* (2nd ed.). Cambridge University Press.
- Le Maitre, R. W. (ed.) (2002). *Igneous Rocks: A Classification and Glossary of Terms* (2nd ed.). Cambridge University Press.
