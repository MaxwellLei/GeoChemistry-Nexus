---
sidebar_position: 1
---

import useBaseUrl from '@docusaurus/useBaseUrl';

# Página inicial

Queremos que a página inicial seja uma entrada prática entre os recursos de pesquisa em geociências e o fluxo de trabalho do dia a dia. Ao reunir atalhos para sites de uso corrente nas geociências e oferecer ferramentas leves e úteis, ela auxilia operações cotidianas e cálculos simples, liga módulos, recursos online e sites que antes estavam dispersos e forma uma entrada de serviços integrada, conectada e eficiente.

![plot_home_ui](/img/v0.9.0/en-US/Software_main_interface.webp)

A página inicial se divide em três partes: **Avisos**, **Links rápidos** e **Widgets**.

* **Avisos**: recebe notificações e mensagens enviadas pelo servidor
* **Links rápidos**: reúne entradas da área de geociências e se divide em duas categorias. **Links recomendados** e **Links pessoais**. Os links recomendados não podem ser alterados por você: são recomendações embutidas, enviadas pelo servidor. **Se você quiser que o seu site entre nas recomendações da página inicial, fale conosco pela página Sobre.** Os links pessoais são os que você adiciona, remove e organiza conforme a sua preferência.
* **Widgets**: o objetivo é oferecer funções simples, independentes e básicas de cálculo ou de operação, para complementar as ferramentas de apoio de uso frequente e facilitar o dia a dia. No futuro, eles devem se tornar plugins extensíveis. Por enquanto, não é possível estendê-los livremente.

## Widgets

Os widgets atuais são, principalmente: **calculadora de alcalinidade, calculadora de dureza, calculadora de radiação de corpo negro, tabela periódica geoquímica, guia cronoestratigráfico, conversor elemento/óxido e conversor de unidades geológicas de uso corrente**.

**Agradecemos especialmente a [Kurt Hollocher (Union College)](https://muse.union.edu/hollochk/kurt-hollocher/geochemistry/) pelas referências publicadas dos métodos de cálculo de alcalinidade, dureza e radiação de corpo negro.**

### Calculadora de alcalinidade

Calcula a alcalinidade de uma amostra de água a partir do ponto final da titulação com ácido clorídrico. Depois de informar a concentração de HCl, o volume da amostra e o volume de HCl consumido, apresenta a alcalinidade (meq/L) e a converte para g/L, meq/L, concentração molar e ppm nas quatro formas HCO₃⁻, CO₃²⁻, CaCO₃ e 0.5(CaCO₃).

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/alkalinity_calculator.webp')}
    alt="Calculadora de alcalinidade"
    style={{width: '60%'}}
  />
</div>

### Calculadora de dureza

Calcula a dureza total da água a partir das concentrações de magnésio e cálcio. Depois de informar as concentrações de Mg e Ca (ppm), converte o resultado para dureza expressa como CaCO₃ (ppm) e indica, pela classificação do USGS, água mole, água moderadamente dura, água dura ou água muito dura.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/hardness_calculator.webp')}
    alt="Calculadora de dureza"
    style={{width: '60%'}}
  />
</div>

### Calculadora de radiação de corpo negro

Calcula a radiação de um corpo negro a partir da temperatura. Depois de informar a temperatura em kelvin, apresenta a potência radiante total de Stefan–Boltzmann e o comprimento de onda de pico de Wien, e traça a curva espectral normalizada pela fórmula de Planck.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/black_body_radiation_calculator.webp')}
    alt="Calculadora de radiação de corpo negro"
    style={{width: '70%'}}
  />
</div>

### Tabela periódica geoquímica

Tabela periódica interativa para consultar elementos por grupos geoquímicos. Ao selecionar um elemento, você vê o número atômico, a massa atômica, a configuração eletrônica, a abundância crustal, os estados de oxidação e a massa molecular dos óxidos comuns. Também é possível destacar grupos como terras-raras, elementos de alta força de campo, elementos litófilos de grande raio iônico e grupo da platina, além de litófilos, siderófilos, calcófilos e atmófilos.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/geochemical_periodic_table.webp')}
    alt="Tabela periódica geoquímica"
    style={{width: '70%'}}
  />
</div>

### Guia cronoestratigráfico

Consulta cronoestratigráfica baseada na Carta Estratigráfica Internacional do ICS. Você pode buscar pelo nome em chinês ou em inglês, ou informar uma idade (Ma) para localizar a unidade estratigráfica correspondente, descer na hierarquia e ver as idades dos limites e as cores oficiais da IUGS.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/chronostratigraphic_quick_reference_card.webp')}
    alt="Guia cronoestratigráfico"
    style={{width: '60%'}}
  />
</div>

### Conversor elemento/óxido

Converte entre a fração em massa do elemento e a do óxido. Depois de escolher um par como Si/SiO₂, Al/Al₂O₃, Fe/Fe₂O₃ ou FeO, informe o wt% de um lado e obtenha o outro pelo fator estequiométrico. Cobre elementos maiores de rocha total e óxidos-traço comuns.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/element_oxide_converter.webp')}
    alt="Conversor elemento/óxido"
    style={{width: '60%'}}
  />
</div>

### Conversor de unidades geológicas de uso corrente

Converte entre unidades de uso corrente no trabalho geológico. Cobre teor e concentração (g/t, ppm, wt%, ppb, oz/t), pressão e tensão (GPa, kbar, MPa, bar, atm, psi), escala de microanálise (mm, μm, nm, Å, pm), temperatura (℃, K, ℉) e idade isotópica (Ga, Ma, ka, yr), e inclui valores de referência geológicos comuns.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/geological_common_unit_converter.webp')}
    alt="Conversor de unidades geológicas de uso corrente"
    style={{width: '60%'}}
  />
</div>
