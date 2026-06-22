<div align="center">

<img src="https://geochemistrynexus-1303234197.cos.ap-hongkong.myqcloud.com/Images/logo.png" width="96" alt="GeoChemistry Nexus Logo">

# GeoChemistry Nexus

**🌍 次世代の地球化学・岩石学 図解・計算ワークベンチ — 地球科学者の頼れるパートナー**

[![GitHub Stars](https://img.shields.io/github/stars/MaxwellLei/GeoChemistry-Nexus?style=flat-square&logo=github)](https://github.com/MaxwellLei/GeoChemistry-Nexus/stargazers)
[![Latest Release](https://img.shields.io/github/v/release/MaxwellLei/GeoChemistry-Nexus?style=flat-square)](https://github.com/MaxwellLei/GeoChemistry-Nexus/releases)
[![License: GPL v3](https://img.shields.io/github/license/MaxwellLei/GeoChemistry-Nexus?style=flat-square)](https://github.com/MaxwellLei/GeoChemistry-Nexus/blob/main/LICENSE)
[![.NET 6](https://img.shields.io/badge/.NET-6.0-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com/download/dotnet/6.0)

[English](../README.md) · [简体中文](./README.zh-CN.md) · [繁體中文](./README.zh-HK.md) · [Deutsch](./README.de-DE.md) · **日本語** · [한국어](./README.ko-KR.md)

<br>

📥 [最新版をダウンロード](https://github.com/MaxwellLei/GeoChemistry-Nexus/releases) · 📖 [オンラインドキュメント](https://geochemistry-nexus.pages.dev/) · 💬 [フィードバックと議論](https://github.com/MaxwellLei/GeoChemistry-Nexus/issues)

</div>

---

## 💡 なぜ GeoChemistry Nexus なのか

地球化学・岩石学の研究では、**作図**と**計算**が異なるツールに分散していることがよくあります：📋 データ整理は表計算ソフト、✏️ 底図の作成は汎用ベクターツール、🌡️ 温圧計計算は別のスクリプトや古いプログラム——。ワークフローの分断、フォーマットの不統一、再現コストの高さは、多くの研究者が日常直面する摩擦です。

さらに、よく使われる判別図、温圧計、関連計算ツールの多くは **継続的な更新が不足** しており、新しく発表されたアルゴリズムや古典モデルの改訂がなかなか取り込まれず、研究者は長年停滞した旧バージョンを使い続けることになりがちです。より現実的な問題として、研究者が **自ら新しいアルゴリズムや図解を開発** した場合、**手軽でオープンな配布チャネル** が不足し、成果が論文付録や個人スクリプト、限られた共有のまま——分野内への迅速な普及と再利用が難しい、というケースも少なくありません。

**GeoChemistry Nexus** は、この一連の流れを一つの場所に集約することを目指します——試料データのインポート、前処理・補正から、判別図の作成、地質温度計計算、CIPW 標準鉱物計算、PNG / SVG などへのエクスポートまで、可能な限り同一インターフェースで完結。**クラウドテンプレート・エコシステム** と **コミュニティ協力** により、図解とアルゴリズムを継続的に更新し、より多くの研究者へ素早く届けます。

> 🚀 **私たちのビジョン**：基礎的な地球化学・岩石学の図解と計算機能を融合させた統合プラットフォームを構築し、研究効率を飛躍的に向上させ、技術的な障壁を下げることで、科学者が科学的発見そのものに集中できるようにすることです。

<div align="center">
  <img src="./images/software_ui_show.webp" alt="GeoChemistry Nexus ソフトウェア画面" width="88%" style="border-radius: 10px; box-shadow: 0 4px 12px rgba(0,0,0,0.15);">
  <br>
  <sub>📈 判別図 · 📐 データ投影 · 🧩 テンプレート編集 · 🌐 多言語 UI</sub>
</div>

---

## 🧭 できること

| モジュール | 説明 |
| :---: | --- |
| 🏠 **ホーム** | クイックリンク、実用ウィジェット、地球科学リソースへの入口——ウィンドウ切り替えを削減 |
| 📊 **データ前処理** | 分析データのインポートと整理、作図・計算の準備 |
| 📈 **判別図** | テンプレートライブラリから三角図、散布図、スパイダー図などを作成；データ投影とスタイル編集に対応 |
| 🌡️ **地質温度計** | 単鉱物計算など一般的なシナリオ向けの地質温圧計テンプレートを統合 |
| 🪨 **CIPW 計算** | 標準鉱物 norm 計算、岩石学分析ワークフローを支援 |
| ☁️ **テンプレート・エコシステム** | 公式・コミュニティテンプレートのクラウド更新；カスタムテンプレートのパッケージ化・共有・協力 |

---

## ✨ 主な機能

* **🎨 強力なカスタム作図機能**
    * 豊富な描画ツール（線、多角形、テキスト、矢印、関数曲線など）を内蔵。
    * カスタムスクリプトをサポートし、データのインポートや計算ルールを柔軟に設定することで、高度なカスタマイズ要件に対応します。

* **🌐 ネイティブな多言語サポート**
    * **図解の国際化**：同一のテンプレートで日・英・中などの言語をワンクリックで切り替え可能。一つの成果を世界へ発信できます。
    * **インターフェースのローカライズ**：ソフトウェアの UI は、英語（US）、簡体字中国語、ドイツ語などを全面的にサポートしています。

* **☁️ クラウドテンプレート・エコシステム**
    * 公式チームとコミュニティが共同で管理する図解テンプレートライブラリを内蔵。
    * 動的な更新をサポートしており、ソフトウェアをアップグレードすることなく、最新の研究用図解テンプレートをリアルタイムで取得可能です。

* **🤝 手軽な配布とコラボレーション**
    * カスタム図解テンプレートのパッケージ化とエクスポートをサポートし、チームや機関を超えた研究成果の共有を容易に実現します。

* **🧮 専門的な地質計算モジュール**
    * 単鉱物など多種多様な地質温度計（Geothermometer）テンプレートを統合し、多様なパラメータ計算のニーズに対応します。

---

## ⚡ クイックスタート

### 📥 ダウンロードとインストール

GitHub の **[Releases](https://github.com/MaxwellLei/GeoChemistry-Nexus/releases)** ページから Windows インストーラー（現在のバージョン **v0.7.1**）をダウンロードし、ウィザードに従ってインストールしてください。

### 💻 システム要件

| 項目 | 要件 |
| --- | --- |
| 🖥️ OS | Windows 7 SP1 以上（Windows 10 / 11 推奨） |
| ⚙️ 実行環境 | [.NET 6.0 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/6.0) |
| 📦 アーキテクチャ | x64 |

> 💡 .NET Runtime が未インストールの場合、インストーラーまたは初回起動時にダウンロードを案内します。

### 🚀 おすすめの始め方

| ステップ | 操作 |
| :---: | --- |
| 1️⃣ | **ホーム** でインターフェースとショートカットに慣れる |
| 2️⃣ | サンプルデータで **判別図** の公式テンプレートを開き、最初の作図を完了する |
| 3️⃣ | 必要に応じて **地質温度計** または **CIPW** モジュールで計算を試す |
| 4️⃣ | PNG / JPG / BMP / SVG でエクスポートし、[公式ドキュメント](https://geochemistry-nexus.pages.dev/) で応用を学ぶ |

---

## 🛠️ 技術概要

| カテゴリ | 内容 |
| --- | --- |
| 💻 言語・フレームワーク | C# · WPF · .NET 6 |
| 🏗️ アーキテクチャ | MVVM（CommunityToolkit.Mvvm） |
| 📊 作図・エクスポート | ScottPlot；PNG / JPG / BMP / SVG に対応 |
| 📜 スクリプトエンジン | Jint（テンプレート内カスタムロジック） |
| 📄 ライセンス | [GNU GPL v3](../LICENSE) |

> 🔧 ローカルビルドと貢献の詳細は `src/` ディレクトリを参照；Issue と Pull Request は GitHub で歓迎します。

---

## 🗺️ ロードマップ

目標は単なる「作図ソフト」ではなく、**データ · 図解 · 計算 · 協力** を一体化した研究ツールチェーンの構築です。

| 段階 | 方向性 |
| --- | --- |
| 🔥 **進行中** | 一般的な地球化学・岩石学図解の継続的拡充；地質温度計・地圧計テンプレートの改善 |
| 📅 **近期計画** | 同位体年代学関連の作図；より豊富な地球化学計算ツール |
| 🔭 **中長期** | 研究コミュニティとテンプレート共有 · ML 支援分析 · 読み込み可能な「新しい図解」判別モデル · RAG ベースの AI 研究アシスタント |

📌 完全なロードマップ：[公式ドキュメント · Roadmap](https://geochemistry-nexus.pages.dev/docs/Roadmap)

---

## 👋 参加と連絡先

**地質学理論**、**アルゴリズム開発**、**テンプレートデザイン**、**C# 開発** のいずれかを得意とする方、ぜひご参加ください：

| 方法 | 内容 |
| --- | --- |
| 🐛 問題を報告 | Bug 報告や機能要望 → [GitHub Issues](https://github.com/MaxwellLei/GeoChemistry-Nexus/issues) |
| 💻 コードを貢献 | Fork して Pull Request を作成 |
| 🌍 翻訳を支援 | UI とドキュメントのローカライズ（中 / 英 / 独 / 日 / 韓 など） |
| 📈 テンプレートを共有 | 自作の判別図テンプレートをコミュニティに提供 |

### 💬 コミュニティチャネル

| チャネル | 情報 |
| --- | --- |
| 📧 メール | `maxwelllei@qq.com` |
| 💬 QQ グループ | `1076647740` |
| 🎮 Discord | https://discord.gg/mRm8dbwa4W |

---

## ⭐ Star 履歴

<p align="center">
  <a href="https://star-history.com/#MaxwellLei/GeoChemistry-Nexus&Date">
    <img src="https://api.star-history.com/svg?repos=MaxwellLei/GeoChemistry-Nexus&type=timeline" alt="Star History">
  </a>
</p>

<div align="center">

✨ GeoChemistry Nexus が研究のお役に立てば、継続的な更新のために **Star** をお願いします ✨

</div>
