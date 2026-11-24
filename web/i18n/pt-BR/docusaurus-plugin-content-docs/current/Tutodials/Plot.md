---
sidebar_position: 2
---

import useBaseUrl from '@docusaurus/useBaseUrl';

# Diagrama

No módulo de diagramas, vamos reunir mais modelos básicos de diagramas das geociências, incluindo, entre outros, diagramas discriminantes de ambiente tectônico e diagramas de classificação de rochas. **O objetivo final é criar, para as geociências, um conjunto completo de ferramentas de moldura e de desenho, e oferecer o máximo de praticidade a quem pesquisa.** 🧪

:::info

Se você pesquisa diagramas geológicos e áreas afins e quer divulgar os seus diagramas, fale conosco. Vamos ajudar o seu diagrama a chegar a um público científico mais amplo e a aumentar o alcance e a influência do resultado acadêmico.

:::

## Início rápido

O módulo de diagramas do software se compõe, principalmente, de duas partes: a biblioteca de modelos de diagrama e a página de operação do diagrama. Primeiro, escolha o tipo de diagrama na biblioteca de modelos. Em seguida, entre na página de operação correspondente para informar os dados, processar o diagrama e exportar o resultado.

### Biblioteca de modelos

O módulo de diagramas se divide em três grandes categorias: **modelos na nuvem online**, **Meus modelos** e **modelos de diagrama utilitários**.

**Modelos na nuvem online**: atualizados e mantidos por nós de forma contínua. Sem atualizar o software, e com conexão à internet, você obtém os modelos de diagrama atualizados e mantém sempre o conjunto mais completo e de referência.

**Meus modelos**: para quando você precisa criar modelos personalizados para uma demanda específica de pesquisa. Você pode criar esses modelos e também exportá-los, para compartilhá-los com facilidade com outros pesquisadores e favorecer o intercâmbio e a divulgação acadêmica.

> *No futuro, planejamos uma comunidade dedicada a modelos de diagrama, na qual será possível criar, enviar, compartilhar e baixar com facilidade diversos **modelos personalizados**, ampliando a flexibilidade e a capacidade de expansão do sistema.*

**Modelos de diagrama utilitários**: separam os diagramas convencionais de forma fixa. Hoje incluem o **diagrama aranha de ETR, o diagrama aranha de elementos-traço, os diagramas de Harker em painéis múltiplos** e os diagramas de dispersão bidimensionais e ternários habituais.

#### Interface

Por padrão, ao entrar, o módulo de plotagem mostra a biblioteca embutida de modelos das geociências. A interface se divide em três partes principais:

*   **Esquerda — classificação dos diagramas**: mostra todos os níveis de modelos e os modelos correspondentes, inclusive a lista de modelos personalizados.
*   **Direita — cartões de diagrama**: mostra os cartões de diagrama do nível atual, com nome e imagem de pré-visualização.
*   **Topo — barra de navegação**: o software usa uma janela sem bordas, e a barra de navegação do topo assume parte das funções de cada página. Na interface de diagramas, ela mostra o caminho do modelo atual e funções como a pesquisa de modelos.

![graphical_interface](/img/v0.9.0/en-US/graphical_interface.webp)

Escolha o modelo adequado e clique no cartão para entrar na página de operação do diagrama. No cartão do modelo, você também pode clicar com o botão direito para exportar, excluir, favoritar e fazer outras operações.

:::tip

Pressione a tecla <kbd>Alt</kbd> direita para excluir ou favoritar rapidamente um modelo de diagrama.

:::

#### Barra de menu superior

O menu **Arquivo**, no topo, contém:

1.  **Novo diagrama**: cria um modelo de diagrama personalizado.
2.  **Abrir diagrama**: abre temporariamente um modelo de diagrama. Aceita apenas arquivos `.diag.zip`.
3.  **Importar diagrama**: importa um pacote externo de modelo de diagrama (`.diag.zip`) para a lista local Meus modelos.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/graphic_menu_bar.webp')}
    alt="Barra de menu do diagrama"
    style={{width: '50%'}}
  />
</div>

### Página de operação do diagrama

#### Interface

A interface do diagrama se divide em cinco partes principais:

-   **Barra de ferramentas**: inclui botões de atalho e três abas de função: desenho, dados e edição.
-   **Lista de camadas (objetos)**: lista dos elementos de desenho do modelo. Clicar em um elemento permite alterar as propriedades dele.
-   **Tela de desenho**: área central para ver o gráfico, importar dados, ajustar a aparência e consultar a explicação do modelo.
-   **Barra de status**: mostra informações básicas do desenho, inclusive o idioma atual do diagrama e as coordenadas.
-   **Painel de propriedades**: mostra as propriedades do elemento de desenho selecionado, como cor e tamanho, para obter o efeito visual desejado.

![graphical_operation_interface](/img/v0.9.0/en-US/graphical_operation_interface.webp)

#### Barra de ferramentas do diagrama

A barra de ferramentas é formada por uma série de **botões de atalho**. Os principais são:

1. Voltar à biblioteca de modelos de diagrama
2. Alternar o estado de operação (modo diagrama, modo dados, modo edição)
3. Painéis de propriedades do diagrama e ferramentas de operação

As funções detalhadas estão na figura abaixo.

![graphical_toolbar](/img/v0.9.0/en-US/graphical_toolbar.webp)

### Lista de camadas

Os elementos de desenho se dividem em 7 categorias:

-   **Line (linha)**: define o contorno básico do diagrama ou um segmento.
-   **Text (texto)**: rótulos e anotações.
-   **Polygon (polígono)**: forma fechada dentro do gráfico.
-   **Arrow (seta)**: objeto de desenho orientado.
-   **Function (função)**: permite informar uma função matemática personalizada e o domínio.
-   **Axes (eixos)**: eixos do gráfico.
-   **Data Point (ponto de dados)**: elemento que representa os dados importados.

**Ordem de renderização padrão (de cima para baixo): `ponto de dados > texto > seta > função > linha > polígono > eixos`.**

![tutorial_plot5](/img/v0.9.0/en-US/diagrammatic_object_selection_example.webp)

Quando você seleciona um elemento no painel de camadas, ele fica em destaque na tela, e os demais ficam semitransparentes para reduzir a interferência. O painel de propriedades passa então a mostrar as propriedades daquele elemento. 🔍

Você pode alterar essas propriedades até chegar ao estilo visual desejado.

:::tip

Para cancelar a seleção de todos os elementos, basta **clicar com o botão direito em qualquer ponto da tela** ou usar o botão **Cancelar seleção** da barra de ferramentas.

:::

## Exemplo de uso

### Exemplo

1. Na **biblioteca de modelos de diagrama**, escolha um modelo e entre na página de operação do diagrama. Por exemplo, o diagrama TAS.
2. Quando for preciso informar dados, mude do modo diagrama para o modo dados.
   
   ![tutorial_plot6](/img/v0.9.0/en-US/diagram_data_input.webp)


No diagrama TAS, é obrigatório informar quatro colunas: `Category`, `SiO2`, `K2O` e `Na2O`. A coluna de título `Category` agrupa os dados e gera a legenda. A coluna `Sample` identifica o rótulo da amostra e não entra em nenhum cálculo (pode ficar vazia). A unidade dos demais elementos é `wt.%`. Se você não souber quais dados o diagrama pede, em que unidade, nem como usá-lo, clique no botão de ajuda do diagrama, no canto superior direito da área do diagrama, para ver a documentação daquele diagrama.

3. Depois de entender o que é necessário, informe os seus dados e clique no botão de plotar, no canto superior direito do painel de dados, para desenhar os pontos. Se a entrada tiver algum problema, corrija os dados e clique em plotar de novo.![tutorial_plot8](/img/v0.9.0/en-US/data_plotting.webp)
4. **Exportar o diagrama**. Há um **botão de atalho que copia o resultado da plotagem para a área de transferência**. Há também uma **exportação formal**. No painel de exportação formal, os formatos de imagem são `jpg`, `png`, `bmp` e `svg`. Quando o desenho científico precisa de vetor, recomendamos o formato `svg`. Também é possível **enviar o resultado diretamente a um software de desenho de terceiros**, sem o passo intermediário de exportar e importar de novo. Os programas de terceiros recomendados de forma embutida são **CorelDRAW, Inkscape e Adobe Illustrator**. Recomendamos o Inkscape para o retoque científico posterior. Também é possível definir outro software de desenho de terceiros, desde que ele aceite o formato vetorial `svg`. Basta indicar, nas configurações, o caminho do programa de desenho.
   ![tutorial_plot8](/img/v0.9.0/en-US/export_diagram.webp)

### Observação complementar

O próprio modelo de diagrama permite trocar o idioma: um mesmo diagrama pode ser exibido em várias versões de idioma, para atender ao uso internacional.

Por exemplo, um diagrama para um artigo em periódico chinês precisa da versão em chinês; para publicação em periódicos SCI/IE em inglês, é preciso a versão em inglês do diagrama. Clique no botão de troca de idioma, no canto superior direito da barra de ferramentas do diagrama, para trocar com um clique a versão de idioma correspondente.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/switch_graphic_language.webp')}
    alt="Alternar o idioma do diagrama"
    style={{width: '60%'}}
  />
</div>
