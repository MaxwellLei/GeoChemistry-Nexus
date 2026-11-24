---
sidebar_position: 4
---

import useBaseUrl from '@docusaurus/useBaseUrl';

# Cálculo mineral de EPMA

**O cálculo mineral de EPMA** trata composições minerais obtidas por microssonda eletrônica. Os teores de óxidos de um conjunto de pontos de análise são convertidos, pelo método mineral escolhido, em resultados de química mineral. Eles servem para identificação mineral, comparação de composições e interpretação geológica posterior.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/epma_mineral_calculation.webp')}
    alt="Interface de EPMA"
    style={{width: '100%'}}
  />
</div>

## Interface

O cálculo mineral de EPMA se divide em duas interfaces: a **biblioteca de modelos de cálculo mineral de EPMA** e a **interface de cálculo de EPMA**.

Na biblioteca você pode pesquisar, filtrar e escolher um cálculo de EPMA adequado. No painel de explicação à direita é possível ver as informações básicas desses modelos. Clique em **Entrar no cálculo** para calcular.

## Exemplo de uso

Por exemplo, escolha um modelo de cálculo de EPMA de olivina e entre na interface de cálculo. A página se divide em uma parte superior e uma inferior. Na barra superior você pode abrir a documentação detalhada desse modelo ou carregar dados de exemplo.

Depois de inserir os dados exigidos pela tabela, clique em **Executar cálculo** na barra superior.

A área branco-acinzentada é a área de entrada, e a área azul-violeta é a área de resultados. Você pode examinar a saída livremente ou clicar em **Inspecionar** para ver as etapas intermediárias do cálculo.

O ponto colorido antes do cabeçalho de cada linha indica o estado de cálculo dessa linha:

* **Cinza**: a linha tem dados, mas ainda não foi calculada
* **Verde**: a linha foi calculada normalmente, sem aviso e sem erro
* **Laranja**: o cálculo terminou, mas há um aviso
* **Vermelho**: o cálculo dessa linha falhou
