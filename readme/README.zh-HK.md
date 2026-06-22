<div align="center">

<img src="https://geochemistrynexus-1303234197.cos.ap-hongkong.myqcloud.com/Images/logo.png" width="96" alt="GeoChemistry Nexus Logo">

# GeoChemistry Nexus

**🌍 新一代地球化學與岩石學圖解和計算工作台，地球科學家的好幫手**

[![GitHub Stars](https://img.shields.io/github/stars/MaxwellLei/GeoChemistry-Nexus?style=flat-square&logo=github)](https://github.com/MaxwellLei/GeoChemistry-Nexus/stargazers)
[![Latest Release](https://img.shields.io/github/v/release/MaxwellLei/GeoChemistry-Nexus?style=flat-square)](https://github.com/MaxwellLei/GeoChemistry-Nexus/releases)
[![License: GPL v3](https://img.shields.io/github/license/MaxwellLei/GeoChemistry-Nexus?style=flat-square)](https://github.com/MaxwellLei/GeoChemistry-Nexus/blob/main/LICENSE)
[![.NET 6](https://img.shields.io/badge/.NET-6.0-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com/download/dotnet/6.0)

[English](../README.md) · [简体中文](./README.zh-CN.md) · **繁體中文** · [Deutsch](./README.de-DE.md) · [日本語](./README.ja-JP.md) · [한국어](./README.ko-KR.md)

<br>

📥 [下載最新版](https://github.com/MaxwellLei/GeoChemistry-Nexus/releases) · 📖 [線上文檔](https://geochemistry-nexus.pages.dev/) · 💬 [反饋與討論](https://github.com/MaxwellLei/GeoChemistry-Nexus/issues)

</div>

---

## 💡 為什麼需要 GeoChemistry Nexus

在地球化學與岩石學研究中，**成圖**與**計算**往往分散在不同工具裡：📋 數據整理靠表格軟件，✏️ 底圖繪製靠通用矢量工具，🌡️ 溫壓計計算又要另開腳本或老舊程式。流程割裂、格式不統一、復現成本高，是不少研究者日常面對的摩擦。

另一方面，許多常用的判別圖解、溫壓計及相關計算工具 **長期缺乏持續更新**——新發表的演算法與經典模型的修訂難以及時納入，研究者往往只能沿用停滯多年的舊版本。更現實的問題是：當科研工作者 **自行開發** 新的演算法或圖解後，往往 **缺少便捷、開放的推送渠道**，成果停留在論文附錄、個人腳本或小範圍分享中，難以在同領域快速普及與復用。

**GeoChemistry Nexus** 希望把這條鏈路收攏到一處——從樣品數據導入、預處理與校正，到判別圖解繪製、地溫計計算與 CIPW 標準礦物計算，再到 PNG / SVG 等格式導出，盡量在同一套介面裡完成；同時通過 **雲端模板生態** 與 **社群協作機制**，讓圖解與演算法能夠持續迭代、快速觸達更多研究者。

> 🚀 **我們的願景**：構建一個融合基礎地球化學與岩石學圖解、計算功能的一體化平台，顯著提升科研效率，降低技術門檻，讓科學家專注於科學發現本身。

<div align="center">
  <img src="./images/software_ui_show.webp" alt="GeoChemistry Nexus 軟件介面" width="88%" style="border-radius: 10px; box-shadow: 0 4px 12px rgba(0,0,0,0.15);">
  <br>
  <sub>📈 判別圖解 · 📐 數據投影 · 🧩 模板編輯 · 🌐 多語言介面</sub>
</div>

---

## 🧭 能做什麼

| 模組 | 說明 |
| :---: | --- |
| 🏠 **主頁** | 快捷連結、實用小組件與常用地球科學資源入口，減少視窗切換 |
| 📊 **數據預處理** | 導入與整理分析數據，為後續成圖與計算做準備 |
| 📈 **判別圖解** | 基於模板庫繪製三角圖、散點圖、蛛網圖等，支援數據投影與樣式編輯 |
| 🌡️ **地溫計** | 整合多種地質溫壓計模板，面向單礦物等常見計算場景 |
| 🪨 **CIPW 計算** | 標準礦物 norm 計算，配合岩石學分析流程 |
| ☁️ **模板生態** | 官方與社群模板雲端更新；支援自訂模板打包、分享與協作 |

---

## ✨ 核心特性

* **🎨 強大的自訂繪圖**
    * 內置豐富的繪圖元工具（線條、多邊形、文字、箭頭、函數曲線等）。
    * 支援自訂腳本，靈活設定數據導入與運算規則，滿足深度客製化需求。

* **🌐 原生多語言支援**
    * **圖解國際化**：同一模板支援中英等多語言一鍵切換，一份成果，全球發布。
    * **介面本地化**：軟件介面全面支援英語（US）、簡體中文、德語等。

* **☁️ 雲端模板生態**
    * 內置由官方與社群共同維護的圖解模板庫。
    * 支援動態更新，無需升級軟件即可實時獲取最新的科研圖解模板。

* **🤝 便捷的分發與協作**
    * 支援將自訂圖解模板打包導出，輕鬆實現跨團隊、跨機構的科研成果共享。

* **🧮 專業地質計算模組**
    * 整合單礦物等多種類型的地質溫度計模板，滿足多樣化的參數計算需求。

---

## ⚡ 快速開始

### 📥 下載安裝

前往 GitHub **[Releases](https://github.com/MaxwellLei/GeoChemistry-Nexus/releases)** 頁面，下載 Windows 安裝包（當前版本 **v0.7.1**），按嚮導完成安裝即可。

### 💻 系統要求

| 項目 | 要求 |
| --- | --- |
| 🖥️ 作業系統 | Windows 7 SP1 及以上（推薦 Windows 10 / 11） |
| ⚙️ 運行環境 | [.NET 6.0 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/6.0) |
| 📦 架構 | x64 |

> 💡 若系統未安裝 .NET 運行時，安裝程式或首次啟動時會引導下載。

### 🚀 建議上手路徑

| 步驟 | 操作 |
| :---: | --- |
| 1️⃣ | 在 **主頁** 熟悉介面與快捷入口 |
| 2️⃣ | 用示例數據在 **判別圖解** 中打開官方模板，完成第一次成圖 |
| 3️⃣ | 按需進入 **地溫計** 或 **CIPW** 模組嘗試計算 |
| 4️⃣ | 導出 PNG / JPG / BMP / SVG，並查閱 [官方文檔](https://geochemistry-nexus.pages.dev/) 了解進階用法 |

---

## 🛠️ 技術概覽

| 類別 | 說明 |
| --- | --- |
| 💻 語言與框架 | C# · WPF · .NET 6 |
| 🏗️ 架構模式 | MVVM（CommunityToolkit.Mvvm） |
| 📊 繪圖與導出 | ScottPlot；支援 PNG / JPG / BMP / SVG |
| 📜 腳本引擎 | Jint（模板內自訂邏輯） |
| 📄 授權協議 | [GNU GPL v3](../LICENSE) |

> 🔧 本地編譯與貢獻說明見倉庫內 `src/` 目錄；Issue 與 Pull Request 均歡迎在 GitHub 提交。

---

## 🗺️ 發展路線

項目目標不只是一個「畫圖軟件」，而是逐步建成 **數據 · 圖解 · 計算 · 協作** 一體化的科研工具鏈。

| 階段 | 方向 |
| --- | --- |
| 🔥 **進行中** | 持續擴充常用地球化學 / 岩石學圖解；完善地溫計與地壓計模板 |
| 📅 **近期計劃** | 同位素年代學相關成圖；更豐富的地球化學計算工具 |
| 🔭 **中長期** | 科研社群與模板共享 · 機器學習輔助分析 · 可加載的「新圖解」判別模型 · 基於 RAG 的 AI 科研助手 |

📌 完整路線圖見 [官方文檔 · Roadmap](https://geochemistry-nexus.pages.dev/docs/Roadmap)。

---

## 👋 參與與聯繫

無論您擅長 **地質理論**、**演算法推導**、**模板設計** 還是 **C# 開發**，都歡迎參與：

| 方式 | 說明 |
| --- | --- |
| 🐛 反饋問題 | 提交 Bug 報告或功能建議 → [GitHub Issues](https://github.com/MaxwellLei/GeoChemistry-Nexus/issues) |
| 💻 貢獻代碼 | Fork 後發起 Pull Request |
| 🌍 協助翻譯 | 介面與文檔本地化（中 / 英 / 德 / 日 / 韓等） |
| 📈 分享模板 | 上傳自研判別圖解模板，供社群使用 |

### 💬 社群渠道

| 渠道 | 信息 |
| --- | --- |
| 📧 郵箱 | `maxwelllei@qq.com` |
| 💬 QQ 群 | `1076647740` |
| 🎮 Discord | https://discord.gg/mRm8dbwa4W |

---

## ⭐ Star 趨勢

<p align="center">
  <a href="https://star-history.com/#MaxwellLei/GeoChemistry-Nexus&Date">
    <img src="https://api.star-history.com/svg?repos=MaxwellLei/GeoChemistry-Nexus&type=timeline" alt="Star History">
  </a>
</p>

<div align="center">

✨ 如果 GeoChemistry Nexus 對您的研究有幫助，歡迎 **Star** 支持項目持續更新 ✨

</div>
