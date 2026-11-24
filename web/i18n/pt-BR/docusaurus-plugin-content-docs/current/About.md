---
sidebar_position: 6
---

# ℹ️ Sobre

:::info

**“Eu não encontrava o carro esportivo dos meus sonhos, então construí um.” — Ferdinand Porsche**

:::

**GeoChemistry Nexus** é o primeiro projeto que pretendo manter por um prazo muito longo: a intenção é continuar atualizando-o por pelo menos cinco anos.

O curioso é que, no início, ele se chamava **Geo-Thermometer**. A princípio, eu só queria desenvolver um programa básico ligado a geotermômetros, para facilitar o cálculo e a plotagem. Com o avanço do projeto, sobretudo depois que o módulo de diagramas ficou pronto, percebi que dava para ir além. O projeto então cresceu em conceito e em funções, e o Geo-Thermometer se transformou no GeoChemistry Nexus.

## Por que esse nome

**GeoChemistry** representa a geoquímica. **Nexus** significa ponto de conexão, eixo ou centro de encontro.

A expectativa é reunir, em uma única plataforma, métodos de cálculo geoquímico, ferramentas de tratamento de dados, análise de química mineral, diagramas geológicos e outros recursos que hoje estão dispersos.

## Por que o GeoChemistry Nexus é necessário

Na pesquisa em geoquímica e petrologia, **a elaboração de figuras** e **o cálculo** costumam ficar espalhados em ferramentas diferentes: a organização dos dados fica em um programa de planilhas, o desenho da base do diagrama em um editor vetorial genérico, e o cálculo do geotermobarômetro em outro script ou em um programa antigo. O fluxo fica partido, os formatos não se conversam e o custo de reproduzir o trabalho é alto. Esse atrito faz parte do dia a dia de muitos pesquisadores.

Por outro lado, muitos diagramas discriminantes, geotermobarômetros e ferramentas de cálculo de uso corrente **passam longos períodos sem atualização**: algoritmos recém-publicados e revisões de modelos clássicos demoram a ser incorporados, e o pesquisador acaba usando uma versão parada há anos. Há ainda um problema mais concreto: quando o pesquisador **desenvolve por conta própria** um algoritmo ou um diagrama novo, muitas vezes **falta um canal aberto e prático para distribuí-lo**. O resultado fica no apêndice do artigo, em um script pessoal ou em um compartilhamento restrito, e demora a se espalhar e a ser reutilizado na mesma área.

![plot_home_ui](/img/about-illustrations/02-research-tool-pain-points.png)

Na pesquisa, também encontrei práticas pouco rigorosas. Por exemplo, há quem desenhe a base de um mapa geológico no CorelDRAW, apenas contornando as formas de maneira aproximada. Visualmente a diferença pode ser pequena, mas, do ponto de vista científico, isso foge do rigor que a pesquisa exige.

Por isso me perguntei: **por que não desenvolver uma ferramenta eu mesmo?**

Uma ferramenta capaz de reunir um grande número de modelos de diagramas geológicos e geoquímicos, de calcular diversos geotermômetros e geobarômetros e de se estender a cálculos geoquímicos e às funções de aprendizado de máquina hoje em uso. Quando não houver um modelo pronto, ela oferece ferramentas visuais de desenho para criar e compartilhar. Com uma interface moderna e suporte a vários idiomas, o pesquisador consegue fazer, em um único programa, a maior parte do trabalho de base de desenho científico e de cálculo.

O **GeoChemistry Nexus** busca reunir essa cadeia em um só lugar: da importação, do pré-processamento e da correção dos dados de amostra à construção de diagramas discriminantes, ao cálculo de geotermômetros e ao cálculo de minerais normativos CIPW, e daí à exportação em PNG, SVG e outros formatos, de preferência na mesma interface. Ao mesmo tempo, o **ecossistema de modelos na nuvem** e os **mecanismos de colaboração da comunidade** permitem que diagramas e algoritmos continuem a ser iterados e cheguem depressa a mais pesquisadores.

## A ideia inicial de desenvolvimento

Por coincidência, eu já conhecia linguagens como C# e Python e tinha prática em desenvolvimento de software para desktop. Em dezembro de 2024, comecei a desenvolver o Demo. Na escolha inicial da tecnologia, pensei com cuidado: **que linguagem e que arquitetura melhor realizam esse objetivo?**

A primeira ideia foi Python. Ele permite prototipar depressa, o que é uma boa notícia para quem desenvolve. Do ponto de vista de quem usa, porém, há defeitos graves. Em primeiro lugar, a eficiência de execução é baixa: com pouco dado o impacto é pequeno, mas a diferença de desempenho existe. Em segundo lugar, o instalador fica enorme. Já reescrevi em Python um programa simples que originalmente estava em WPF. Mesmo empacotado em um ambiente virtual novo, o resultado tinha centenas de MB, enquanto o programa empacotado em .NET ficava na casa de uma dezena de MB. A diferença é grande. **Obrigar quem usa a aceitar uma instalação de centenas de MB, ou até de GB, e uma configuração complicada para uma função simples está longe de ser a melhor escolha.**

Depois considerei o desenvolvimento web. A vantagem é evidente: nada para instalar, abre e usa. O problema é o custo alto e contínuo de um servidor, acima do que eu conseguiria manter sozinho. Por isso, voltei o foco para o desenvolvimento do cliente.

![plot_home_ui](/img/about-illustrations/03-technology-choice-path.png)

Há muitas opções técnicas para o cliente. Em comparação com o Qt, baseado em C++, eu conheço melhor o .NET. Juntando a alta eficiência de execução do .NET e a difusão do Windows, escolhi o .NET como plataforma principal. No início, também pensei em usar Avalonia para oferecer suporte a Windows, Linux e macOS. Durante o desenvolvimento, porém, algumas dependências do projeto se mostraram incompatíveis com o Avalonia, e o custo da migração ficou muito alto. Sozinho, seria difícil reescrever esses módulos logo no começo. Por isso, pelo menos nesta etapa, o trabalho continua concentrado no Windows. Uma versão multiplataforma pode surgir em algum momento no futuro.

## Fale comigo

Se você se interessa por desenvolvimento de software e quer colaborar, ou se deseja enviar sugestões e feedback, modelos de diagramas, apoio a algoritmos ou mesmo uma proposta comercial, escreva a qualquer momento para **[maxwelllei@qq.com](mailto:maxwelllei@qq.com)**.

*Se, ao usar o software, você encontrar alguma limitação ou algum problema, envie um retorno quando puder. As funções crescem e ficam mais complexas, e os testes dificilmente cobrem todos os casos. Algo pode passar ou ficar impreciso. Vou me esforçar para corrigir e otimizar, para oferecer uma experiência mais estável e confiável.*

![plot_home_ui](/img/about-illustrations/04-long-term-maintenance.png)
