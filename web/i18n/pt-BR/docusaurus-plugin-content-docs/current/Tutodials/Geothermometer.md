---
sidebar_position: 3
---

import useBaseUrl from '@docusaurus/useBaseUrl';

# Geotermobarômetro

Nas versões iniciais, este módulo se chamava **GTM**. Depois passou a se chamar geotermômetro e, hoje, o nome oficial é geotermobarômetro.

As versões iniciais incluíam geotermômetros de minerais como zircão, esfalerita, quartzo, xenotímio, clorita e biotita. O geotermobarômetro da nova versão ainda está sendo adaptado e será sincronizado em breve.

## Interface

O módulo de geotermobarômetro se divide em duas partes:

1. À esquerda fica a **lista de modelos de geotermobarômetro**. Ali você procura um modelo adequado ou cria o seu próprio modelo de cálculo.
2. À direita fica a **documentação de ajuda do modelo de geotermobarômetro selecionado**.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/geothermobarometer_interface.webp')}
    alt="Interface do geotermobarômetro"
    style={{width: '100%'}}
  />
</div>

## Funções

Desde o início, o módulo foi pensado **no formato do Excel**, para reduzir o custo de aprendizado. **Há duas formas principais de usar o geotermobarômetro**:

1. **Modelo de geotermobarômetro online**: o **modelo online** padrão não pode ser modificado, mas você pode ver a lógica de implementação do algoritmo. Sem atualizar o software, e com conexão à internet, você obtém os modelos de geotermobarômetro mais recentes. Você pode pesquisar, filtrar e escolher o modelo adequado e preencher os dados e calcular conforme o guia. **Esta forma é mais direta e mais rápida, e não exige conhecimento de programação.**
2. **Janela de cálculo independente**: pelo menu superior, **Arquivo -> Abrir planilha livre**, você abre a janela de cálculo independente. Nela, é permitido programar de forma personalizada e combinar diferentes geotermobarômetros. As funções correspondentes a cada geotermobarômetro estão na seção de tutoriais avançados. **Esta forma é mais complexa e relativamente mais flexível.**

O módulo também permite criar o seu próprio geotermobarômetro personalizado. O modelo pronto pode ser exportado e distribuído para que outros pesquisadores o usem rapidamente.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/export_geothermobarometer_template.webp')}
    alt="Exportar geotermobarômetro"
    style={{width: '45%'}}
  />
</div>

## Exemplo de uso

### Usar um modelo de geotermobarômetro online

Primeiro, dentro do módulo de geotermobarômetro, filtre e selecione o modelo de cálculo na barra lateral esquerda:

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/select_and_apply_thermobarometer_template.webp')}
    alt="Selecionar geotermobarômetro"
    style={{width: '100%'}}
  />
</div>

No painel direito, você consulta a documentação daquele modelo. A barra superior também mostra o título e as etiquetas do modelo selecionado. Depois de escolher o modelo, clique no botão **Aplicar**, na barra superior, para entrar na interface de cálculo.

Na interface do geotermobarômetro aparecem as colunas de entrada correspondentes. Você também pode clicar em **Exemplo**, na barra superior, para carregar dados de exemplo. Preencha os dados conforme o que o título de cada coluna pede e, em seguida, **copie a fórmula de cálculo** ou **use a alça de preenchimento “clicar e arrastar”** (como no Excel) para concluir o cálculo automaticamente, como na animação abaixo.

> A coluna `Sample` não entra no cálculo. Ela serve apenas como anotação para identificar a amostra dos dados.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/calculate_thermobarometer_using_formula.webp')}
    alt="Calcular o geotermobarômetro com fórmula"
    style={{width: '80%'}}
  />
</div>

A versão atual do geotermobarômetro também permite ver os detalhes internos do cálculo.

Selecione a linha de dados com o mouse e clique em **Inspecionar**, na parte superior. Uma barra lateral mostra os resultados intermediários.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/thermobarometer_calculation_details.webp')}
    alt="Detalhes do cálculo do geotermobarômetro"
    style={{width: '100%'}}
  />
</div>

### Janela independente e funções personalizadas

Como descrito acima, você abre a janela livre independente pelo menu **Arquivo -> Abrir planilha livre**.

A tabela da janela independente é uma planilha vazia, com alguns botões simples de ferramenta.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/thermobarometer_free_form.webp')}
    alt="Planilha livre de cálculo do geotermobarômetro"
    style={{width: '100%'}}
  />
</div>

Usar a tabela da janela independente é como **usar uma planilha do Excel no dia a dia: você pode digitar qualquer valor e chamar uma função com `=nomeDaFunção(parâmetros)`**.

A diferença é que os cálculos de geotermobarômetro estão registrados como funções de cálculo correspondentes, e você pode personalizar o conteúdo com mais flexibilidade. Você pode até **comparar, na mesma planilha, os resultados de vários geotermobarômetros**.

:::note

Na tabela da janela independente, **não é possível ver o processo intermediário de cálculo da linha de dados correspondente**.

:::
