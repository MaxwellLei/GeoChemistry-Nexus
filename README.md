<div align="center">

<img src="https://geochemistrynexus-1303234197.cos.ap-hongkong.myqcloud.com/Images/logo.png" width="96" alt="GeoChemistry Nexus Logo">

# GeoChemistry Nexus

**🌍 Next-Gen Geochemistry & Petrology Diagram and Calculation Workbench — A Geoscientist's Best Friend**

[![GitHub Stars](https://img.shields.io/github/stars/MaxwellLei/GeoChemistry-Nexus?style=flat-square&logo=github)](https://github.com/MaxwellLei/GeoChemistry-Nexus/stargazers)
[![Latest Release](https://img.shields.io/github/v/release/MaxwellLei/GeoChemistry-Nexus?style=flat-square)](https://github.com/MaxwellLei/GeoChemistry-Nexus/releases)
[![License: GPL v3](https://img.shields.io/github/license/MaxwellLei/GeoChemistry-Nexus?style=flat-square)](https://github.com/MaxwellLei/GeoChemistry-Nexus/blob/main/LICENSE)
[![.NET 6](https://img.shields.io/badge/.NET-6.0-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com/download/dotnet/6.0)

**English** · [简体中文](./readme/README.zh-CN.md) · [繁體中文](./readme/README.zh-HK.md) · [Deutsch](./readme/README.de-DE.md) · [日本語](./readme/README.ja-JP.md) · [한국어](./readme/README.ko-KR.md)

<br>

📥 [Download Latest](https://github.com/MaxwellLei/GeoChemistry-Nexus/releases) · 📖 [Documentation](https://geochemistry-nexus.pages.dev/) · 💬 [Feedback & Discussion](https://github.com/MaxwellLei/GeoChemistry-Nexus/issues)

</div>

---

## 💡 Why GeoChemistry Nexus

In geochemistry and petrology research, **plotting** and **calculation** are often scattered across different tools: 📋 data preparation in spreadsheets, ✏️ base-map drawing in general vector software, and 🌡️ geothermobarometry in separate scripts or legacy programs. Fragmented workflows, inconsistent formats, and high reproducibility costs are friction points many researchers face daily.

On top of that, many widely used discrimination diagrams, geothermobarometers, and related calculation tools **lack ongoing updates**—newly published algorithms and revisions to classic models are slow to be incorporated, leaving researchers stuck with versions that have stagnated for years. An equally practical problem: when researchers **develop their own** algorithms or diagrams, they often **lack convenient, open channels to distribute them**, so results remain in paper appendices, personal scripts, or small-circle sharing—hard to spread and reuse across the field.

**GeoChemistry Nexus** aims to bring this workflow together—from sample data import, preprocessing, and correction, to discrimination diagram plotting, geothermometer calculation, and CIPW norm calculation, through to export in PNG / SVG and other formats—all within a single interface. Through a **cloud template ecosystem** and **community collaboration**, diagrams and algorithms can keep evolving and reach more researchers quickly.

> 🚀 **Our Vision**: To build an integrated platform combining fundamental geochemistry/petrology diagrams and calculation functions, significantly improving research efficiency and lowering technical barriers, allowing scientists to focus on scientific discovery itself.

<div align="center">
  <img src="./readme/images/software_ui_show.webp" alt="GeoChemistry Nexus Software UI" width="88%" style="border-radius: 10px; box-shadow: 0 4px 12px rgba(0,0,0,0.15);">
  <br>
  <sub>📈 Discrimination Diagrams · 📐 Data Projection · 🧩 Template Editing · 🌐 Multilingual UI</sub>
</div>

---

## 🧭 What You Can Do

| Module | Description |
| :---: | --- |
| 🏠 **Home** | Quick links, practical widgets, and common Earth Science resource shortcuts—fewer window switches |
| 📊 **Data Preprocessing** | Import and organize analytical data to prepare for plotting and calculation |
| 📈 **Discrimination Diagrams** | Plot ternary, scatter, spider diagrams, and more from template libraries; supports data projection and style editing |
| 🌡️ **Geothermometers** | Integrated geothermobarometer templates for common scenarios such as single-mineral calculations |
| 🪨 **CIPW Calculation** | Standard mineral norm calculation to support petrological analysis workflows |
| ☁️ **Template Ecosystem** | Official and community templates updated via the cloud; pack, share, and collaborate on custom templates |

---

## ✨ Core Features

* **🎨 Powerful Custom Plotting**
    * Built-in rich drawing primitives (lines, polygons, text, arrows, function curves, etc.).
    * Supports custom scripts to flexibly define data import and calculation rules, meeting deep customization needs.

* **🌐 Native Multi-Language Support**
    * **Diagram Internationalization**: One template supports one-click switching between languages (e.g., English/Chinese)—create once, publish globally.
    * **Interface Localization**: Full interface support for English (US), Simplified Chinese, German, and more.

* **☁️ Cloud Template Ecosystem**
    * Includes a library of diagram templates maintained by both the official team and the community.
    * Supports dynamic updates, allowing you to get the latest scientific diagram templates without upgrading the software.

* **🤝 Convenient Distribution & Collaboration**
    * Supports packing and exporting custom diagram templates, easily enabling the sharing of research results across teams and institutions.

* **🧮 Professional Geological Calculation Module**
    * Integrated geothermometer templates (including single mineral types), meeting diverse parameter calculation requirements.

---

## ⚡ Quick Start

### 📥 Download & Install

Visit the GitHub **[Releases](https://github.com/MaxwellLei/GeoChemistry-Nexus/releases)** page to download the Windows installer (current version **v0.7.1**), then follow the setup wizard.

### 💻 System Requirements

| Item | Requirement |
| --- | --- |
| 🖥️ Operating System | Windows 7 SP1 or higher (Windows 10 / 11 recommended) |
| ⚙️ Runtime | [.NET 6.0 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/6.0) |
| 📦 Architecture | x64 |

> 💡 If .NET Runtime is not installed, the installer or first launch will guide you to download it.

### 🚀 Suggested Getting Started Path

| Step | Action |
| :---: | --- |
| 1️⃣ | Explore the **Home** page to familiarize yourself with the interface and shortcuts |
| 2️⃣ | Open an official template in **Discrimination Diagrams** with sample data and create your first plot |
| 3️⃣ | Try calculations in the **Geothermometers** or **CIPW** module as needed |
| 4️⃣ | Export as PNG / JPG / BMP / SVG, and read the [Official Documentation](https://geochemistry-nexus.pages.dev/) for advanced usage |

---

## 🛠️ Technical Overview

| Category | Details |
| --- | --- |
| 💻 Language & Framework | C# · WPF · .NET 6 |
| 🏗️ Architecture | MVVM (CommunityToolkit.Mvvm) |
| 📊 Plotting & Export | ScottPlot; supports PNG / JPG / BMP / SVG |
| 📜 Script Engine | Jint (custom logic within templates) |
| 📄 License | [GNU GPL v3](./LICENSE) |

> 🔧 For local build and contribution details, see the `src/` directory; Issues and Pull Requests are welcome on GitHub.

---

## 🗺️ Roadmap

The goal is not just a "plotting app," but an integrated research toolchain spanning **data · diagrams · calculation · collaboration**.

| Phase | Direction |
| --- | --- |
| 🔥 **In Progress** | Continuously expand common geochemistry / petrology diagrams; improve geothermometer and geobarometer templates |
| 📅 **Near Term** | Isotope geochronology plotting; richer geochemical calculation tools |
| 🔭 **Long Term** | Research community & template sharing · ML-assisted analysis · loadable "new diagram" discrimination models · RAG-based AI research assistant |

📌 See the full roadmap at [Official Documentation · Roadmap](https://geochemistry-nexus.pages.dev/docs/Roadmap).

---

## 👋 Get Involved & Contact

Whether you excel in **geological theory**, **algorithm development**, **template design**, or **C# development**, you're welcome to join:

| How | Details |
| --- | --- |
| 🐛 Report Issues | Submit bug reports or feature requests → [GitHub Issues](https://github.com/MaxwellLei/GeoChemistry-Nexus/issues) |
| 💻 Contribute Code | Fork and open a Pull Request |
| 🌍 Help Translate | Localize the interface and documentation (Chinese / English / German / Japanese / Korean, etc.) |
| 📈 Share Templates | Upload your own discrimination diagram templates for the community |

### 💬 Community Channels

| Channel | Info |
| --- | --- |
| 📧 Email | `maxwelllei@qq.com` |
| 💬 QQ Group | `1076647740` |
| 🎮 Discord | https://discord.gg/mRm8dbwa4W |

---

## ⭐ Star History

<p align="center">
  <a href="https://star-history.com/#MaxwellLei/GeoChemistry-Nexus&Date">
    <img src="https://api.star-history.com/svg?repos=MaxwellLei/GeoChemistry-Nexus&type=timeline" alt="Star History">
  </a>
</p>

<div align="center">

✨ If GeoChemistry Nexus helps your research, please **Star** the project to support ongoing updates ✨

</div>
