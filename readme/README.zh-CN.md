<div align="center">

<img src="https://geochemistrynexus-1303234197.cos.ap-hongkong.myqcloud.com/Images/logo.png" width="96" alt="GeoChemistry Nexus Logo">

# GeoChemistry Nexus

**🌍 新一代地球化学与岩石学图解和计算工作台，地球科学家的好帮手**

[![GitHub Stars](https://img.shields.io/github/stars/MaxwellLei/GeoChemistry-Nexus?style=flat-square&logo=github)](https://github.com/MaxwellLei/GeoChemistry-Nexus/stargazers)
[![Latest Release](https://img.shields.io/github/v/release/MaxwellLei/GeoChemistry-Nexus?style=flat-square)](https://github.com/MaxwellLei/GeoChemistry-Nexus/releases)
[![License: GPL v3](https://img.shields.io/github/license/MaxwellLei/GeoChemistry-Nexus?style=flat-square)](https://github.com/MaxwellLei/GeoChemistry-Nexus/blob/main/LICENSE)
[![.NET 6](https://img.shields.io/badge/.NET-6.0-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com/download/dotnet/6.0)

[English](../README.md) · **简体中文** · [繁體中文](./README.zh-HK.md) · [Deutsch](./README.de-DE.md) · [日本語](./README.ja-JP.md) · [한국어](./README.ko-KR.md)

<br>

📥 [下载最新版](https://github.com/MaxwellLei/GeoChemistry-Nexus/releases) · 📖 [在线文档](https://geochemistry-nexus.pages.dev/) · 💬 [反馈与讨论](https://github.com/MaxwellLei/GeoChemistry-Nexus/issues)

</div>

---

## 💡 为什么需要 GeoChemistry Nexus

在地球化学与岩石学研究中，**成图**与**计算**往往分散在不同工具里：📋 数据整理靠表格软件，✏️ 底图绘制靠通用矢量工具，🌡️ 温压计计算又要另开脚本或老旧程序。流程割裂、格式不统一、复现成本高，是不少研究者日常面对的摩擦。

另一方面，许多常用的判别图解、温压计及相关计算工具 **长期缺乏持续更新**——新发表的算法与经典模型的修订难以及时纳入，研究者往往只能沿用停滞多年的旧版本。更现实的问题是：当科研工作者 **自行开发** 新的算法或图解后，往往 **缺少便捷、开放的推送渠道**，成果停留在论文附录、个人脚本或小范围分享中，难以在同领域快速普及与复用。

**GeoChemistry Nexus** 希望把这条链路收拢到一处——从样品数据导入、预处理与校正，到判别图解绘制、地温计计算与 CIPW 标准矿物计算，再到 PNG / SVG 等格式导出，尽量在同一套界面里完成；同时通过 **云端模板生态** 与 **社区协作机制**，让图解与算法能够持续迭代、快速触达更多研究者。

> 🚀 **我们的愿景**：构建一个融合基础地球化学与岩石学图解、计算功能的一体化平台，显著提升科研效率，降低技术门槛，让科学家专注于科学发现本身。

<div align="center">
  <img src="./images/software_ui_show.webp" alt="GeoChemistry Nexus 软件界面" width="88%" style="border-radius: 10px; box-shadow: 0 4px 12px rgba(0,0,0,0.15);">
  <br>
  <sub>📈 判别图解 · 📐 数据投影 · 🧩 模板编辑 · 🌐 多语言界面</sub>
</div>

---

## 🧭 能做什么

| 模块 | 说明 |
| :---: | --- |
| 🏠 **主页** | 快捷链接、实用小组件与常用地球科学资源入口，减少窗口切换 |
| 📊 **数据预处理** | 导入与整理分析数据，为后续成图与计算做准备 |
| 📈 **判别图解** | 基于模板库绘制三角图、散点图、蛛网图等，支持数据投影与样式编辑 |
| 🌡️ **地温计** | 集成多种地质温压计模板，面向单矿物等常见计算场景 |
| 🪨 **CIPW 计算** | 标准矿物 norm 计算，配合岩石学分析流程 |
| ☁️ **模板生态** | 官方与社区模板云端更新；支持自定义模板打包、分享与协作 |

---

## ✨ 核心特性

* **🎨 强大的自定义绘图**
    * 内置丰富的绘图元工具（线条、多边形、文本、箭头、函数曲线等）。
    * 支持自定义脚本，灵活设定数据导入与运算规则，满足深度定制需求。

* **🌐 原生多语言支持**
    * **图解国际化**：同一模板支持中英等多语言一键切换，一份成果，全球发布。
    * **界面本地化**：软件界面全面支持英语（US）、简体中文、德语等。

* **☁️ 云端模板生态**
    * 内置由官方与社区共同维护的图解模板库。
    * 支持动态更新，无需升级软件即可实时获取最新的科研图解模板。

* **🤝 便捷的分发与协作**
    * 支持将自定义图解模板打包导出，轻松实现跨团队、跨机构的科研成果共享。

* **🧮 专业地质计算模块**
    * 集成单矿物等多种类型的地质温度计模板，满足多样化的参数计算需求。

---

## ⚡ 快速开始

### 📥 下载安装

前往 GitHub **[Releases](https://github.com/MaxwellLei/GeoChemistry-Nexus/releases)** 页面，下载 Windows 安装包（当前版本 **v0.7.1**），按向导完成安装即可。

### 💻 系统要求

| 项目 | 要求 |
| --- | --- |
| 🖥️ 操作系统 | Windows 7 SP1 及以上（推荐 Windows 10 / 11） |
| ⚙️ 运行环境 | [.NET 6.0 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/6.0) |
| 📦 架构 | x64 |

> 💡 若系统未安装 .NET 运行时，安装程序或首次启动时会引导下载。

### 🚀 建议上手路径

| 步骤 | 操作 |
| :---: | --- |
| 1️⃣ | 在 **主页** 熟悉界面与快捷入口 |
| 2️⃣ | 用示例数据在 **判别图解** 中打开官方模板，完成第一次成图 |
| 3️⃣ | 按需进入 **地温计** 或 **CIPW** 模块尝试计算 |
| 4️⃣ | 导出 PNG / JPG / BMP / SVG，并查阅 [官方文档](https://geochemistry-nexus.pages.dev/) 了解进阶用法 |

---

## 🛠️ 技术概览

| 类别 | 说明 |
| --- | --- |
| 💻 语言与框架 | C# · WPF · .NET 6 |
| 🏗️ 架构模式 | MVVM（CommunityToolkit.Mvvm） |
| 📊 绘图与导出 | ScottPlot；支持 PNG / JPG / BMP / SVG |
| 📜 脚本引擎 | Jint（模板内自定义逻辑） |
| 📄 授权协议 | [GNU GPL v3](../LICENSE) |

> 🔧 本地编译与贡献说明见仓库内 `src/` 目录；Issue 与 Pull Request 均欢迎在 GitHub 提交。

---

## 🗺️ 发展路线

项目目标不只是一个「画图软件」，而是逐步建成 **数据 · 图解 · 计算 · 协作** 一体化的科研工具链。

| 阶段 | 方向 |
| --- | --- |
| 🔥 **进行中** | 持续扩充常用地球化学 / 岩石学图解；完善地温计与地压计模板 |
| 📅 **近期计划** | 同位素年代学相关成图；更丰富的地球化学计算工具 |
| 🔭 **中长期** | 科研社区与模板共享 · 机器学习辅助分析 · 可加载的「新图解」判别模型 · 基于 RAG 的 AI 科研助手 |

📌 完整路线图见 [官方文档 · Roadmap](https://geochemistry-nexus.pages.dev/docs/Roadmap)。

---

## 👋 参与与联系

无论您擅长 **地质理论**、**算法推导**、**模板设计** 还是 **C# 开发**，都欢迎参与：

| 方式 | 说明 |
| --- | --- |
| 🐛 反馈问题 | 提交 Bug 报告或功能建议 → [GitHub Issues](https://github.com/MaxwellLei/GeoChemistry-Nexus/issues) |
| 💻 贡献代码 | Fork 后发起 Pull Request |
| 🌍 协助翻译 | 界面与文档本地化（中 / 英 / 德 / 日 / 韩等） |
| 📈 分享模板 | 上传自研判别图解模板，供社区使用 |

### 💬 社区渠道

| 渠道 | 信息 |
| --- | --- |
| 📧 邮箱 | `maxwelllei@qq.com` |
| 💬 QQ 群 | `1076647740` |
| 🎮 Discord | https://discord.gg/mRm8dbwa4W |

---

## ⭐ Star 趋势

<p align="center">
  <a href="https://star-history.com/#MaxwellLei/GeoChemistry-Nexus&Date">
    <img src="https://api.star-history.com/svg?repos=MaxwellLei/GeoChemistry-Nexus&type=timeline" alt="Star History">
  </a>
</p>

<div align="center">

✨ 如果 GeoChemistry Nexus 对您的研究有帮助，欢迎 **Star** 支持项目持续更新 ✨

</div>
