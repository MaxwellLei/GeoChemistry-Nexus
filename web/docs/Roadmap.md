---
sidebar_position: 3
---

# 🛣️ Roadmap

GeoChemistry Nexus aims to be more than just a "plotting software"—it is gradually becoming an integrated **data · diagrams · calculation · collaboration** toolchain for geochemistry and petrology research.

## ✅ Completed


### Core Workflow
- [x] **Home**: Quick links, practical widgets
- [x] **Data Preprocessing**: Major oxide column recognition, Fe valence estimation / back-calculation, outlier, missing value, and detection limit handling strategies
- [x] **Discriminant Diagrams**: Plot ternary diagrams, scatter plots, spider diagrams, etc. based on template library; supports data projection and style editing
- [x] **Geothermometer (GTM)**: Built-in multi-mineral calculation templates and Excel-like custom functions
- [x] **CIPW Norm Mineral Calculation**: Whole-rock major element norm calculation and result export
### Platform Capabilities
- [x] **Template Ecosystem**: Official templates updated dynamically from the cloud; JSON / ZIP format custom template creation, import, and packaged sharing
- [x] **Multilingual**: Interface localization (Chinese / English / German, etc.); one-click multilingual switching for diagram templates
- [x] **Export & Integration**: PNG / JPG / BMP / SVG export; supports collaboration with third-party software such as CorelDRAW, Inkscape, and Adobe Illustrator



## 🔥 In Progress
- [ ] **Continuous expansion of diagram template library**  
  Focus on adding geochemical discriminant diagrams such as tectonic setting discrimination diagrams and rock classification diagrams.
- [ ] **Geothermometer module improvements**  
  Expand commonly used geothermometer / geobarometer templates.
- [ ] **Documentation & localization**  
  Synchronously update Chinese / English / German and other documentation to reduce onboarding costs for new users.

## 📅 Near-Term Plans
### Isotope Geochronology Plotting
- [ ] U-Pb concordia diagram (Concordia)
- [ ] Isochron diagrams for Rb-Sr, Sm-Nd, Lu-Hf, U-Pb, etc.
- [ ] Initial isotope ratio related diagrams (e.g., (⁸⁷Sr/⁸⁶Sr)ᵢ vs εNd(t))


## 🔭 Long-Term Vision
### Machine Learning-Assisted Analysis
Introduce common machine learning workflows to assist geochemical data discrimination and classification, outputting model evaluation, variable importance, and visualizations such as ROC curves and confusion matrices—**without requiring users to configure a Python environment themselves**.
### "New Diagram"-Style Discrimination Models
Support loading extensible machine learning discrimination models (e.g., new-type discrimination diagrams trained on big data), allowing researchers to directly apply "new diagrams" for analysis without mastering the full ML development stack.
### AI Research Assistant (Phased)
1. **Phase 1**: RAG Q&A based on built-in help documentation and knowledge base, providing operation suggestions based on keywords and scenarios  
2. **Phase 2**: Further assist data processing and analysis workflows, simplifying repetitive operations
### Cross-Platform & Other
- [ ] Evaluate Avalonia and other options, gradually implement Linux / macOS support (currently Windows-focused)
- [ ] Continuously optimize performance, template formats, and developer extension interfaces
