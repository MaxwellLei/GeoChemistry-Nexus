---
sidebar_position: 1
---

import useBaseUrl from '@docusaurus/useBaseUrl';

# Criar um modelo de diagrama

Para diagramas que ainda não estão na biblioteca online de modelos, você pode criar um modelo de diagrama personalizado. Ao personalizar o modelo e empacotá-lo, você pode compartilhá-lo rapidamente com outros pesquisadores.

Você também pode enviar o seu modelo ao desenvolvedor para inclusão na biblioteca online. Agradecemos de coração a contribuição de quem participa.

## Criação rápida

Agora você pode criar um novo modelo de diagrama pelo menu `Arquivo` -> `Novo modelo de diagrama`, como mostrado abaixo:

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/new_graphic_template.webp')}
    alt="Configurações do diagrama"
    style={{width: '50%'}}
  />
</div>

Depois de clicar em **Novo modelo de diagrama**, abre-se uma janela para criar o novo modelo:

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/new_graphic_template_interface.webp')}
    alt="Interface de novo diagrama"
    style={{width: '100%'}}
  />
</div>

Em um modelo de diagrama personalizado novo, há três partes principais para configurar:

1.  **Idiomas com suporte padrão**: você pode escolher, na caixa de seleção à direita, um atalho de idioma já incluído. Oferecemos dez idiomas, entre eles chinês simplificado, chinês tradicional, inglês americano, japonês, russo, coreano e alemão.

    > Cabe esclarecer: o idioma padrão é aquele para o qual o programa recua quando outro idioma não está traduzido ou ocorre um erro. Além disso, na nova versão, não é mais permitido definir códigos de idioma personalizados. Se você precisar de suporte ao seu idioma, entre em contato com o desenvolvedor.

2.  **Classificação do modelo de diagrama (hierarquia)**: há uma estrutura de classificação pronta. Esta opção define a posição hierárquica do seu modelo na lista de modelos de diagramas.

3.  **Tipo de modelo de diagrama**: hoje há dois tipos: **sistema de coordenadas bidimensional** e diagrama ternário.

Depois de concluir as opções, clique em **OK** para entrar na interface de desenho personalizado. Em seguida, o foco passa para a barra de funções **Editar**. Ao clicar em **Editar**, o sistema mostra uma confirmação para editar o diagrama. Depois de confirmar, você entra no modo de edição, onde pode ver e usar as ferramentas da barra de edição.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/empty_diagram_template.webp')}
    alt="Diagrama vazio"
    style={{width: '100%'}}
  />
</div>

## Desenhar o diagrama

No modo de edição, as operações abaixo estão disponíveis:

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/graphical_toolbar.webp')}
    alt="Desenhar o diagrama"
    style={{width: '100%'}}
  />
</div>

* **Salvar**: salva o modelo de diagrama. Ao clicar, o programa gera, por padrão, uma miniatura correspondente ao estado atual do desenho.
* **Salvar como**: salva o modelo de diagrama em outro local de arquivo.
* **Adicionar linha**: ao ativar, entra no modo “adicionar linha”. Clique no primeiro ponto da área de desenho para começar a linha e no segundo ponto para concluir o objeto de linha.
* **Adicionar texto**: também chamado de anotação. Ao ativar, entra no modo “adicionar texto”. Clique em um ponto do desenho para criar o objeto. O texto padrão é `Text`. Você pode alterar a posição ou o conteúdo na seção de propriedades do painel de camadas.
* **Adicionar polígono**: ao ativar, entra no modo “adicionar polígono”. Crie um polígono fechado com cliques sucessivos do botão esquerdo para definir os vértices e clique com o botão direito para fechar a forma.
* **Adicionar seta**: ao ativar, entra no modo “adicionar seta”. O processo é semelhante ao de criar uma linha.
* **Adicionar função**: ao clicar, adiciona uma função padrão `sin(x)`, com domínio [-10, 10]. Você pode personalizar a fórmula no painel de propriedades.
* **Desfazer/Refazer**: se nenhum objeto de desenho foi criado ou excluído, estas funções ficam desativadas. Por padrão, o histórico guarda apenas as últimas 10 operações.
* **Excluir**: exclui um objeto de desenho. Primeiro selecione o objeto (por exemplo, um texto) e depois clique em excluir para removê-lo.

### Adicionar linha

Abaixo há um exemplo do painel de propriedades de uma linha. Por esse painel, você ajusta com precisão a posição da linha e outras propriedades.

**O botão de capturar coordenadas, à direita de cada coordenada, permite reposicionar e capturar de novo um ponto na área de desenho.** Depois de acionado, o próximo clique com o botão esquerdo na área de desenho grava automaticamente as coordenadas do ponto clicado.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/add_line.webp')}
    alt="Adicionar linha"
    style={{width: '100%'}}
  />
</div>

### Adicionar seta

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/add_arrow.webp')}
    alt="Adicionar seta"
    style={{width: '100%'}}
  />
</div>

### Adicionar polígono

**Com a adição de polígono ativada, confirme os vértices com cliques sucessivos do botão esquerdo na área da tela. Enquanto isso, você vê a área de pré-visualização do polígono. O clique com o botão direito conclui a criação do polígono.** O objeto de polígono tem uma lista de vértices: você pode selecionar, alterar e excluir cada vértice a qualquer momento.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/add_polygon.webp')}
    alt="Adicionar polígono"
    style={{width: '100%'}}
  />
</div>

### Adicionar texto

Com a adição de texto ativada, clique com o botão esquerdo em qualquer ponto da tela para criar um objeto de texto nessa posição. Na barra de status, na base da tela, você pode ver a qual idioma pertence o conteúdo do objeto de texto atual.

Como o diagrama oferece suporte nativo a vários idiomas, a tradução desse conteúdo é explicada mais adiante.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/add_text.webp')}
    alt="Adicionar texto"
    style={{width: '100%'}}
  />
</div>

### Adicionar função

Adicionar uma função é diferente dos objetos anteriores. **Ao clicar no botão Adicionar função, um objeto de função padrão é incluído na hora.** A função padrão é `sin(x)`. Você define a sua função alterando a fórmula no painel de propriedades.

Nas propriedades do objeto de função, os dois parâmetros mais importantes são o **domínio** e os **pontos de amostragem**.

* **O domínio define o intervalo em que a função aparece na tela.**
* **Os pontos de amostragem controlam a precisão do traçado da função na tela** e, com isso, a exatidão do algoritmo de captura com o mouse. O valor padrão é `1000`. Quanto maior a precisão, mais fino fica o traçado da função, e maior também a carga de renderização do programa.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/add_function.webp')}
    alt="Adicionar função"
    style={{width: '100%'}}
  />
</div>

## Completar o modelo de diagrama

Depois do desenho básico, um modelo completo ainda precisa de:

1.  **Dados e script**: definem os dados de entrada do modelo e o algoritmo de cálculo, isto é, indicam como os dados são calculados e plotados.
2.  **Documento do diagrama**: traz o texto de ajuda que explica o diagrama.
3.  **Vários idiomas**: se o modelo estiver configurado para mais de um idioma, é preciso traduzir essa parte, inclusive o **documento do diagrama** anterior e os objetos de texto na tela do diagrama.

Você pode completar o modelo na própria interface de operação do diagrama, mas recomendamos fazer isso na janela de edição do diagrama, onde o trabalho fica mais prático.

### Dados e script

Dados e script são a parte central do modelo, porque definem como os dados do diagrama são calculados e, por fim, apresentados no gráfico.

São necessários dois parâmetros: os **parâmetros de variáveis** do modelo e o **algoritmo de cálculo (script)**, como mostrado abaixo:

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/diagram_data_and_script.webp')}
    alt="Diagrama — dados e script"
    style={{width: '100%'}}
  />
</div>

Nos **parâmetros de variáveis** do modelo, você pode adicionar colunas na parte superior para representar as colunas de entrada exigidas pelo diagrama. Há dois parâmetros opcionais:

1. Unit indica a unidade da coluna de entrada. Serve apenas como informação exibida e não afeta o algoritmo dos dados.
2. Group indica o grupo da coluna de entrada. Serve apenas como informação exibida e não afeta o algoritmo dos dados.

Os títulos de coluna que você define na tabela de dados passam a ser, por padrão, **variáveis já registradas, que o script abaixo pode chamar diretamente**. Isso significa que você não precisa declarar de novo, no script, as variáveis das colunas de entrada.

O **algoritmo de cálculo (script)** é escrito, por padrão, em `JavaScript`. O exemplo padrão é `return [X,Y]`, que devolve as colunas de entrada `X` e `Y` da tabela de dados aos eixos bidimensionais.

No diagrama TAS, por exemplo, os parâmetros devem ser `SiO2, Na2O, K2O`. O conteúdo do script fica assim:

```javascript
// Calcular com as variáveis K2O + Na2O
var result1 = K2O + Na2O;
// Usar SiO2
var result2 = SiO2;
// Retornar os dois valores calculados. No gráfico de coordenadas bidimensionais padrão, só há dois valores de retorno.
// A primeira posição é o valor de retorno de X; a segunda é o valor de retorno de Y.
return [result2, result1]
```

Ou você pode escrever o script assim:

```javascript
var result = K2O + Na2O
return [SiO2, result]
```

A posição dos valores de retorno é fixa. Em `[x, y]`, o primeiro valor é sempre X (eixo inferior) e o segundo é Y (eixo esquerdo).

:::info

No diagrama ternário, o formato final de retorno é `[A, B, C]`: o primeiro valor é A (eixo inferior), o segundo é B (eixo esquerdo) e o terceiro é C (eixo direito).

:::

### Documento do diagrama

O documento do diagrama é um passo necessário para que outros pesquisadores entendam rapidamente as informações básicas e o uso daquele diagrama.

Escreva o documento na página de documentação de ajuda. Há uma barra de ferramentas simples para as necessidades comuns de um documento. Você também pode redigir o conteúdo no `Office Word` e colá-lo aqui: a maior parte da formatação é compatível.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/diagram_help_document.webp')}
    alt="Diagrama — documentação de ajuda"
    style={{width: '100%'}}
  />
</div>

Para o formato do documento, sugerimos seguir este padrão:

*   **Introdução**: explica o conceito básico e a função do modelo, para que a pessoa entenda depressa.
*   **Formato dos dados**: indica o formato dos dados de entrada e os títulos de coluna necessários para uma leitura válida.
*   **Referências**: lista as referências usadas para criar o modelo e o seu conteúdo.
*   **Contribuidores**: nomes de quem participou da elaboração do modelo. Você pode incluir também o seu site pessoal.

### Vários idiomas

Há duas formas de configurar vários idiomas:

A primeira é a opção **Alternar idioma**, na barra de funções do topo da interface do diagrama. Ela permite definir o conteúdo específico de um segundo idioma.

A segunda é editar o conteúdo multilíngue na janela de edição do diagrama, como na figura abaixo.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/illustrated_multilingual.webp')}
    alt="Diagrama — dados e script"
    style={{width: '100%'}}
  />
</div>
