---
sidebar_position: 3
---

# 🛣️ Roadmap

GeoChemistry Nexus aims to grow, step by step, into an integrated geochemistry and petrology toolchain of **data, diagrams, calculation, and collaboration**.

## ✅ Completed


### Core workflow
- [x] **Home**: shortcuts and practical widgets
- [x] **Discrimination diagrams**: a large built-in set from the template library, including but not limited to tectonic-setting discrimination diagrams, rock-classification diagrams, and others. It also supports trace-element and rare-earth spider diagrams, with a large set of built-in normalization schemes, and plotting on Harker multi-panel diagrams.
- [x] **Geothermobarometry**: built-in online multi-mineral geothermobarometry templates and Excel-like custom functions
- [x] **EPMA mineral calculation**: built-in EPMA calculation templates for different minerals, updated online in real time and ready to use after download
- [x] **CIPW normative mineral calculation**: normalization of whole-rock major-element data, calculation, and export of results
- [x] **Niggli normative mineral calculation**: normalization of whole-rock major-element data, calculation, and export of results
### Platform capabilities
- [x] **Performance footprint**: very low memory use and a very small install size. Without the `.NET` runtime, the local software package is only 9 MB.
- [x] **Template ecosystem**: online templates update dynamically from the cloud; custom templates can be created, imported, and packaged for sharing; calculation details can be viewed for checking
- [x] **Multiple languages**: interface localization (Chinese / English / German / Russian / Japanese / Korean, and others); one-click language switching for diagram templates
- [x] **Export and integration**: export to PNG / JPG / BMP / SVG; collaboration with third-party software such as CorelDRAW, Inkscape, and Adobe Illustrator

## 📅 Near-term plans
### Isotope geochronology plots
- [ ] U-Pb concordia diagrams (Concordia)
- [ ] Isochron diagrams for Rb-Sr, Sm-Nd, Lu-Hf, U-Pb, and others
- [ ] Diagrams related to initial isotope ratios, such as (⁸⁷Sr/⁸⁶Sr)ᵢ vs εNd(t)
- [ ] And more


## 🔭 Medium- and long-term vision
### Machine-learning-assisted analysis
Bring in common machine-learning workflows to help discriminate and classify geochemical data, and output model evaluation, variable importance, and visual results such as ROC curves and confusion matrices—**without asking the user to configure a Python environment**.
### “New diagram” discrimination models
Support loading extensible machine-learning discrimination models, such as new discrimination diagrams trained on large data sets, so researchers can apply these “new diagrams” directly without mastering the full machine-learning development stack.
### AI research assistant (in stages)
1. **Stage one**: RAG question answering based on the built-in help documents and knowledge base, offering operational suggestions from keywords and the situation
2. **Stage two**: further help with data processing and analysis workflows, and simplify repetitive operations
### Cross-platform and other work
- [ ] Evaluate approaches such as Avalonia and gradually support Linux / macOS (Windows is the focus today)
- [ ] Keep improving performance, the template format, and developer extension interfaces
