---
sidebar_position: 3
---

# 🛣️ Roteiro

O objetivo do GeoChemistry Nexus é construir, aos poucos, uma cadeia de ferramentas de pesquisa em geoquímica e petrologia que una **dados · diagramas · cálculos · colaboração**.

## ✅ Concluído


### Fluxo de trabalho principal
- [x] **Página inicial**: atalhos e widgets práticos
- [x] **Diagramas discriminantes**: a biblioteca de modelos traz um grande número de diagramas discriminantes, incluindo, entre outros, diagramas de ambiente tectônico e diagramas de classificação de rochas; além disso, diagramas aranha de elementos-traço e de elementos terras-raras, com muitos esquemas de normalização embutidos, e plotagem em painéis múltiplos de diagramas de Harker
- [x] **Geotermobarômetro**: modelos online de cálculo geotermobarométrico de vários minerais e funções personalizadas no estilo do Excel
- [x] **Cálculo mineral por EPMA**: modelos de cálculo EPMA de diferentes minerais, atualizados online em tempo real e prontos para uso após o download
- [x] **Cálculo CIPW de minerais normativos**: cálculo normativo dos elementos maiores de rocha total e exportação dos resultados
- [x] **Cálculo de minerais normativos de Niggli**: cálculo normativo dos elementos maiores de rocha total e exportação dos resultados
### Capacidades da plataforma
- [x] **Desempenho**: uso de memória muito baixo e instalação muito compacta; sem o runtime `.NET`, o programa ocupa apenas 9 MB em disco
- [x] **Ecossistema de modelos**: atualização dinâmica dos modelos online na nuvem; criação, importação e empacotamento de modelos personalizados para compartilhamento; consulta dos detalhes do cálculo para conferência
- [x] **Vários idiomas**: interface localizada (chinês / inglês / alemão / russo / japonês / coreano, entre outros); troca do idioma dos modelos de diagrama com um clique
- [x] **Exportação e integração**: exportação em PNG / JPG / BMP / SVG; colaboração com CorelDRAW, Inkscape, Adobe Illustrator e outros programas de terceiros

## 📅 Planos próximos
### Diagramas de geocronologia isotópica
- [ ] Diagrama concórdia de U-Pb (Concordia)
- [ ] Diagramas de isócrona de Rb-Sr, Sm-Nd, Lu-Hf, U-Pb e outros
- [ ] Diagramas de razões isotópicas iniciais, como (⁸⁷Sr/⁸⁶Sr)ᵢ vs εNd(t)
- [ ] E outros


## 🔭 Visão de médio e longo prazo
### Análise assistida por aprendizado de máquina
Incluir fluxos usuais de aprendizado de máquina para auxiliar a discriminação e a classificação de dados geoquímicos, com avaliação do modelo, importância das variáveis e resultados visuais como curvas ROC e matrizes de confusão, **sem que você precise configurar um ambiente Python**.
### Modelos discriminantes do tipo «novo diagrama»
Permitir o carregamento de modelos discriminantes de aprendizado de máquina extensíveis, como novos diagramas discriminantes treinados com grandes conjuntos de dados, para que o pesquisador aplique diretamente o «novo diagrama» sem precisar dominar toda a pilha de desenvolvimento de aprendizado de máquina.
### Assistente de pesquisa com IA (em etapas)
1. **Primeira etapa**: perguntas e respostas por RAG, com base na documentação de ajuda embutida e em uma base de conhecimento, oferecendo sugestões de operação conforme palavras-chave e o contexto
2. **Segunda etapa**: auxílio adicional ao tratamento de dados e ao fluxo de análise, simplificando operações repetitivas
### Multiplataforma e outros pontos
- [ ] Avaliar soluções como Avalonia e, aos poucos, oferecer suporte a Linux / macOS (hoje o foco é o Windows)
- [ ] Continuar otimizando o desempenho, o formato dos modelos e as interfaces de extensão para desenvolvedores
