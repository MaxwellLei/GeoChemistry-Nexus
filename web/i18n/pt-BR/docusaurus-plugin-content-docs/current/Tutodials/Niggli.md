---
sidebar_position: 6
---

import useBaseUrl from '@docusaurus/useBaseUrl';

# Cálculo de minerais normativos de Niggli

O **cálculo de minerais normativos de Niggli** produz dois conjuntos de resultados para a mesma linha de óxidos de rocha total:

1. **Parâmetros de Niggli**: `si`, `al`, `fm`, `c`, `alk`, `k`, `mg`, `qz` e, exibidos apenas nos detalhes do cálculo, `ti` e `p`.
2. **Minerais normativos moleculares de Barth–Niggli (Catanorm)**: minerais normativos em porcentagem de cátions (cat%), composição dos membros extremos e o percurso de dessilicação quando a sílica está subsaturada.

O Catanorm foi estabelecido por Paul Niggli (1948) e Tom F. W. Barth (1948). Ele distribui por equivalentes catiônicos e reduz o peso que elementos pesados, como o ferro, ocupam na porcentagem em massa. Por isso, fica mais próximo das relações de volume mineral vistas em lâmina do que o wt% da CIPW. Os minerais normativos em porcentagem em massa estão em [Cálculo CIPW de minerais normativos](./CIPW).

O algoritmo toma como referência o fluxo de minerais normativos moleculares do GCDkit e foi reimplementado. O produto da dessilicação adicional da leucita é registrado como kaliofilita `Kp`.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/niggli.webp')}
    alt="Interface Niggli"
    style={{width: '100%'}}
  />
</div>

## Interface

A barra superior é a mesma da CIPW: **Fe³⁺/ΣFe**, **Instruções**, **Exemplo**, **Exportar**, **Limpar** e **Calcular**. O intervalo de Fe³⁺/ΣFe é `0`–`1`, e o padrão é `0.15` (Le Maitre, 2002).

**Exemplo** preenche três linhas: granito, basalto e fonolito (Phonolite). As duas primeiras são rochas vulcânicas comuns, com sílica mais alta. O fonolito serve para examinar resultados subsaturados, como nefelina e leucita.

A planilha se divide, da esquerda para a direita, em cinco trechos:

| Área | Grupo | Unidade | Conteúdo |
| --- | --- | --- | --- |
| Amostra | — | — | Nome da amostra. Não entra no cálculo e permanece visível na rolagem horizontal |
| Óxidos | Elementos maiores; Outros óxidos / voláteis | wt% | Área de entrada |
| Parâmetros de Niggli | Parâmetros de Niggli | Adimensional | `si`, `al`, `fm`, `c`, `alk`, `k`, `mg`, `qz` |
| Diagnóstico | Diagnóstico | — | Estado de saturação em sílica, estado de saturação em alumina, soma dos minerais |
| Minerais normativos | Minerais normativos | cat% | Porcentagem de cátions. A última coluna é `Sum(soma)` |

A coluna de amostra fica congelada. A tabela começa com 500 linhas e permite preencher para baixo com um clique duplo na alça de preenchimento. Depois que a entrada muda, a linha já calculada passa ao estado desatualizado e é preciso clicar de novo em **Calcular**.

### Estado do cabeçalho da linha

| Ponto | Significado |
| --- | --- |
| Cinza vazado | Já há entrada, mas ainda não houve cálculo |
| Verde-azulado preenchido | Cálculo bem-sucedido, sem avisos |
| Laranja preenchido | Cálculo bem-sucedido, mas o estado de oxidação do ferro foi estimado a partir de FeOT |
| Vermelho preenchido | O cálculo falhou. A coluna de diagnóstico traz «Erro de cálculo» e o motivo |
| Laranja vazado | Os óxidos foram alterados depois do cálculo, e o resultado está desatualizado |

Amostras em que FeO e Fe₂O₃ foram informados juntos ficam com o ponto verde-azulado quando o cálculo é bem-sucedido. Quando há apenas FeOT, aparece o aviso de estado de oxidação do ferro e o cabeçalho da linha fica laranja.

## Entrada

Cada linha é uma amostra, em porcentagem em massa dos óxidos (wt%). Só são lidas células maiores que 0. Células em branco e 0 são tratadas como não informadas.

Elementos maiores, até `K2O`:

`SiO2`, `TiO2`, `Al2O3`, `Fe2O3`, `FeO`, `FeOT`, `MnO`, `MgO`, `CaO`, `Na2O`, `K2O`

Outros óxidos / voláteis:

`CO2`, `P2O5`, `F`, `S`

Este módulo não lê `ZrO2`, `Cr2O3`, `Cl` nem `SO3`. Se esses quatro itens precisarem entrar nos minerais normativos, use o módulo CIPW.

### Tratamento do ferro

O ferro é tratado antes da conversão em cátions. Na repartição, o ferro é primeiro reduzido a átomos de Fe: o número de mols de ferro total = massa de ferro / massa molar de FeO. Em seguida, a razão Fe³⁺/ΣFe separa o ferro trivalente do bivalente, e metade do ferro trivalente é escrita em Fe₂O₃.

| Situação informada | Tratamento |
| --- | --- |
| Há `Fe2O3` e `FeO` ao mesmo tempo | Usa os valores medidos. Mesmo que haja `FeOT` ao lado, não há nova repartição |
| Há `FeOT`, e Fe₂O₃ e FeO não estão ambos presentes | Reparte o `FeOT` pela razão atual e registra um aviso nos detalhes |
| Há apenas `Fe2O3`, sem `FeO` e sem `FeOT` | Trata esse Fe₂O₃ como ferro total e reparte, pela razão, em Fe₂O₃ e FeO |
| Há apenas `FeO`, sem `Fe2O3` e sem `FeOT` | Trata esse FeO como ferro total e reparte, pela razão, em Fe₂O₃ e FeO |

Por isso, quando só o FeO ou só o Fe₂O₃ foi analisado, o resultado varia com a razão da barra superior. Se os dois forem valores medidos, preencha as duas colunas.

`MnO` é mantido até a conversão em cátions. Ele entra em `fm` e `mg` dos parâmetros de Niggli e, no Catanorm, é incorporado a Fe²⁺.

## Parâmetros de Niggli

Os parâmetros são calculados com o **número de moléculas** dos óxidos. Número de moléculas = wt% / massa molar. Aqui, `al` é o número de moléculas de Al₂O₃: uma porção de Al₂O₃ conta como 1. Essa base é diferente da do Catanorm, mais adiante, em que uma porção de Al₂O₃ conta como 2 átomos de Al.

```text
al  = n(Al2O3)
fm  = n(FeO) + 2 n(Fe2O3) + n(MnO) + n(MgO)
c   = n(CaO)
alk = n(Na2O) + n(K2O)
base = al + fm + c + alk

si  = 100 × n(SiO2) / base
al, fm, c e alk são igualmente convertidos em parcelas de 100 em relação a base
ti  = 100 × n(TiO2) / base
p   = 100 × n(P2O5) / base
k   = n(K2O) / (n(Na2O) + n(K2O))
mg  = n(MgO) / (n(MgO) + n(FeO) + 2 n(Fe2O3) + n(MnO))
```

Índice de quartzo `qz`:

```text
quando al > alk     qz = si − (100 + 4 al)
quando al ≤ alk     qz = si − (100 + 3 al + alk)
```

A segunda expressão vale para amostras em que a alumina não ultrapassa os álcalis. A janela **Instruções**, dentro do software, lista apenas a primeira.

`si`, `al`, `fm`, `c`, `alk`, `ti`, `p` e `qz` ficam com 2 casas decimais. `k` e `mg` ficam com 3 casas decimais. A tabela mostra apenas as oito colunas do primeiro grupo. `ti` e `p` aparecem nos detalhes do cálculo. Quando `qz` é positivo, os detalhes acrescentam o sinal de mais.

Na tabela, `al + fm + c + alk` deve ser 100. `si` pode ser muito maior que 100, porque a sílica fica fora dessa partição de 100. `k` e `mg` são razões de 0 a 1.

## Colunas de diagnóstico

| Coluna | Significado |
| --- | --- |
| **Estado de saturação em sílica** | Supersaturado em sílica, Saturado em sílica, Subsaturado em sílica |
| **Estado de saturação em alumina** | Peraluminoso, Metaluminoso, Peralcalino |
| **Soma dos minerais** | Soma das porcentagens de cátions do Catanorm, com 2 casas decimais |

O estado de saturação em sílica é definido pelos minerais finais. O limiar é `0.001 cat%`:

- Com quartzo `Q`: supersaturado em sílica.
- Sem quartzo, mas com nefelina `Ne`, leucita `Lc`, kaliofilita `Kp` ou olivina `Ol`: subsaturado em sílica.
- Nos demais casos: saturado em sílica.

O sinal de `qz` mostra se a sílica está em excesso ou em falta em relação à base de Niggli. O «estado de saturação em sílica» da tabela olha para os minerais normativos. As duas colunas podem ser comparadas: as bases de julgamento são diferentes.

O estado de saturação em alumina usa os cátions normalizados e ocorre antes da distribuição dos minerais:

- **Peraluminoso**: Al > Na + K + Ca
- **Metaluminoso**: Na + K < Al ≤ Na + K + Ca
- **Peralcalino**: Al ≤ Na + K

## Minerais normativos

A unidade das colunas de minerais é a porcentagem de cátions. `Plag` é igual a `Ab + An`. A soma `Sum` inclui `Plag` e não soma `Ab` e `An` uma segunda vez. Para conferir o fechamento, olhe `Sum` ou a soma dos minerais na coluna de diagnóstico.

| Abreviação | Mineral | Abreviação | Mineral |
| --- | --- | --- | --- |
| `Q` | Quartzo | `Hy` | Ortopiroxênio |
| `C` | Coríndon | `Ol` | Olivina |
| `Or` | Ortoclásio | `Cs` | Rankinita |
| `Plag` | Plagioclásio | `Wo` | Wollastonita |
| `Ab` | Albita | `Mt` | Magnetita |
| `An` | Anortita | `Hm` | Hematita |
| `Lc` | Leucita | `Il` | Ilmenita |
| `Ne` | Nefelina | `Tn` | Titanita |
| `Kp` | Kaliofilita | `Pf` | Perovskita |
| `Ac` | Egirina | `Ru` | Rutilo |
| `Ns` | Metassilicato de sódio | `Ap` | Apatita |
| `Ks` | Metassilicato de potássio | `Fr` | Fluorita |
| `Di` | Diopsídio | `Py` | Pirita |
| | | `Cc` | Calcita |

As proporções dos membros extremos não ocupam colunas de resultado próprias. Todas ficam nos detalhes do cálculo.

## Detalhes do cálculo

Depois de selecionar uma linha bem-sucedida, o inspetor à direita mostra seis trechos:

1. **Classificação da rocha e estado de saturação**: estado de saturação em sílica, estado de saturação em alumina e soma dos minerais normativos. No caso subsaturado, a saturação em sílica fica em destaque. No caso peralcalino, a saturação em alumina fica em destaque.
2. **Parâmetros de Niggli**: `si`, `al`, `fm`, `c`, `alk`, `ti`, `p`, `k`, `mg`, `qz`.
3. **Membros extremos dos minerais**, exibidos apenas quando o mineral correspondente existe:
   - Plagioclásio `An% = 100 × An / (Ab + An)`
   - O diopsídio fica fixo em Wo 50%, e os outros 50% se dividem em En e Fs pela razão magnésio/ferro
   - Olivina Fo e Fa; Fo é igual ao índice de magnésio abaixo
   - Índice de magnésio `Mgr = 100 × En / (En + Fs)`, calculado antes de o ortopiroxênio ser incorporado ao diopsídio
4. **Percurso das reações de dessilicação**: quando há sílica em excesso, indica o cat% do quartzo; quando há déficit de sílica, registra passo a passo qual mineral foi consumido e se o déficit já foi eliminado.
5. **Principais minerais normativos**: minerais maiores que `1 cat%`, do maior para o menor, sem repetir `Plag`. Entradas maiores que `10 cat%` ficam em destaque.
6. **Avisos**: a implementação atual grava um aviso quando o estado de oxidação do ferro é estimado a partir de FeOT.

## Processo de cálculo

### 1. Base catiônica

Cada óxido vira primeiro uma quantidade de cátions: `(wt% / massa molar) × número de cátions na fórmula`. O número de cátions de SiO₂, TiO₂, FeO, MnO, MgO, CaO, CO₂, F e S é 1. O de Al₂O₃, Fe₂O₃, Na₂O, K₂O e P₂O₅ é 2.

Todos os cátions são somados e escalados para 100, formando o reservatório de distribuição do Catanorm. Quando o total se aproxima de 0, a linha falha, com o aviso de soma de cátions insuficiente. Nesta etapa, os wt% dos óxidos permanecem sem normalização a 100. H₂O e perda ao fogo não informados ficam fora desse 100.

### 2. Minerais acessórios e sais

Mn entra primeiro em Fe²⁺. Em seguida, a contagem é por cátions:

| Ordem | Mineral | Ponto da estequiometria |
| --- | --- | --- |
| 1 | Calcita `Cc` | Quantidade do mineral = `2 × CO2`, e consome a mesma quantidade de Ca |
| 2 | Apatita `Ap` | Quando P é baixo em relação a F, `Ap = 3P` e parte do F é consumida; caso contrário, todo o F restante entra na apatita |
| 3 | Fluorita `Fr` | F restante: `Fr = 1.5 × F`, e consome `0.5 × F` de Ca |
| 4 | Pirita `Py` | `Py = 1.5 × S`, e consome `0.5 × S` de Fe²⁺ |
| 5 | Ilmenita `Il` | O menor entre Ti e Fe²⁺ define a produção, e a quantidade do mineral é o dobro desse valor |

CO₂, P, F e S ocupam Ca ou Fe²⁺ com prioridade. Quando esses voláteis superam de forma clara os cátions com os quais podem se combinar, os minerais cálcicos e magnesianos seguintes ficam distorcidos. Confira pela soma dos minerais e pelo registro de dessilicação.

### 3. Feldspatos, precursores de feldspatoides e minerais de titânio

A unidade de distribuição é o cátion. Ortoclásio, albita e anortita, quando o membro extremo é suficiente, formam grupos de 5 cátions.

1. K, com Al e Si, forma ortoclásio `Or` (cada K se associa a 1 Al e 3 Si). O K restante forma metassilicato de potássio `Ks`.
2. Na, com Al e Si, forma albita `Ab`. O Na restante forma egirina `Ac` com Fe³⁺ (cada Na se associa a 1 Fe³⁺ e 2 Si). O Na que ainda restar forma metassilicato de sódio `Ns`.
3. O Al restante forma anortita `An` com Ca. Se o Ca for insuficiente, o limite é o alumínio: cada Al limitante corresponde a 0.5 Ca e 1 Si.
4. O Ti restante forma titanita `Tn` com Ca e Si (3 cátions). O Ti que ainda restar é rutilo `Ru`.
5. O Al que restar por último é coríndon `C`.

### 4. Óxidos de ferro e piroxênio

Fe³⁺ e Fe²⁺ formam magnetita `Mt`: quando Fe³⁺ não ultrapassa o dobro de Fe²⁺, `Mt = 1.5 × Fe³⁺`. O Fe³⁺ restante é hematita `Hm`.

O Ca, o Mg e o Fe²⁺ restantes tornam-se, respectivamente, wollastonita `Wo`, molécula de enstatita e molécula de ferrosilita, cada uma com o dobro do número de cátions, e consomem uma porção igual de Si. A soma das moléculas de enstatita e de ferrosilita é o ortopiroxênio `Hy`. O índice de magnésio é calculado nesse momento.

`Wo` e `Hy` combinam-se de novo, com o mesmo número de cátions, para formar diopsídio `Di`: o lado em menor quantidade entra inteiro em `Di`, e o número de cátions de `Di` é o dobro desse valor. O lado em maior quantidade deixa `Wo` ou `Hy`.

### 5. Quartzo ou dessilicação em sete passos

Quando o Si restante é ≥ 0, ele se torna integralmente quartzo `Q` e o cálculo termina aqui.

Quando Si < 0, o déficit é `D = −Si`. Cada passo abaixo ou reduz o déficit a 0 e termina, ou esgota aquele mineral e passa ao passo seguinte. Os coeficientes são porcentagens de cátions:

| Passo | Reação | Para eliminar 1 porção de déficit de Si |
| --- | --- | --- |
| 1 | Ortopiroxênio → olivina | Consome 4 `Hy` e gera 3 `Ol` |
| 2 | Titanita → perovskita | Consome 3 `Tn` e gera 2 `Pf` |
| 3 | Albita → nefelina | Consome 2.5 `Ab` e gera 1.5 `Ne` |
| 4 | Ortoclásio → leucita | Consome 5 `Or` e gera 4 `Lc` |
| 5 | Leucita → kaliofilita | Consome 4 `Lc` e gera 3 `Kp` |
| 6 | Wollastonita → rankinita | Consome 4 `Wo` e gera 3 `Cs` |
| 7 | Diopsídio → rankinita + olivina | Consome 4 `Di` e gera 1.5 `Cs` e 1.5 `Ol` |

O produto do passo 5 é `Kp`. Depois da dessilicação, o plagioclásio é `Plag = Ab + An`, e o estado de saturação em sílica é julgado pelas regras anteriores.

## Referências

- Niggli, P. (1948). *Gesteine und Minerallagerstätten*. Birkhäuser, Basel.
- Barth, T. F. W. (1948). The distribution of oxygen in the lithosphere. *The Journal of Geology*, 56(1), 41–49.
- Janoušek, V., Farrow, C. M., Erban, V. (2006). Interpretation of whole-rock geochemical data in igneous geochemistry: introducing Geochemical Data Toolkit (GCDkit). *Journal of Petrology*, 47(6), 1255–1259.
- Le Maitre, R. W. (ed.) (2002). *Igneous Rocks: A Classification and Glossary of Terms* (2nd ed.). Cambridge University Press.
- GCDkit: [https://www.gcdkit.org/](https://www.gcdkit.org/)
