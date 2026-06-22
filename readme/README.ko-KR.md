<div align="center">

<img src="https://geochemistrynexus-1303234197.cos.ap-hongkong.myqcloud.com/Images/logo.png" width="96" alt="GeoChemistry Nexus Logo">

# GeoChemistry Nexus

**🌍 차세대 지구화학 및 암석학 도표·계산 워크벤치 — 지구과학자의 든든한 파트너**

[![GitHub Stars](https://img.shields.io/github/stars/MaxwellLei/GeoChemistry-Nexus?style=flat-square&logo=github)](https://github.com/MaxwellLei/GeoChemistry-Nexus/stargazers)
[![Latest Release](https://img.shields.io/github/v/release/MaxwellLei/GeoChemistry-Nexus?style=flat-square)](https://github.com/MaxwellLei/GeoChemistry-Nexus/releases)
[![License: GPL v3](https://img.shields.io/github/license/MaxwellLei/GeoChemistry-Nexus?style=flat-square)](https://github.com/MaxwellLei/GeoChemistry-Nexus/blob/main/LICENSE)
[![.NET 6](https://img.shields.io/badge/.NET-6.0-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com/download/dotnet/6.0)

[English](../README.md) · [简体中文](./README.zh-CN.md) · [繁體中文](./README.zh-HK.md) · [Deutsch](./README.de-DE.md) · [日本語](./README.ja-JP.md) · **한국어**

<br>

📥 [최신 버전 다운로드](https://github.com/MaxwellLei/GeoChemistry-Nexus/releases) · 📖 [온라인 문서](https://geochemistry-nexus.pages.dev/) · 💬 [피드백 및 토론](https://github.com/MaxwellLei/GeoChemistry-Nexus/issues)

</div>

---

## 💡 왜 GeoChemistry Nexus인가

지구화학 및 암석학 연구에서 **작도**와 **계산**은 종종 서로 다른 도구에 분산되어 있습니다: 📋 데이터 정리는 스프레드시트, ✏️ 배경 도면 작성은 범용 벡터 소프트웨어, 🌡️ 온압계 계산은 별도 스크립트나 오래된 프로그램——. 워크플로우 단절, 형식 불일치, 높은 재현 비용은 많은 연구자가 매일 마주하는 마찰입니다.

또한 많이 사용되는 판별도, 온압계 및 관련 계산 도구는 **지속적인 업데이트가 부족**하여, 새로 발표된 알고리즘과 고전 모델의 개정이 제때 반영되지 않고, 연구자들은 수년간 정체된 구버전을 사용하는 경우가 많습니다. 더 현실적인 문제는, 연구자가 **새로운 알고리즘이나 도표를 직접 개발**한 뒤 **편리하고 개방적인 배포 채널**이 부족해, 성과가 논문 부록, 개인 스크립트, 소규모 공유에 머물러 동일 분야 내 빠른 보급과 재사용이 어렵다는 점입니다.

**GeoChemistry Nexus**는 이 전체 흐름을 한곳으로 모으고자 합니다——시료 데이터 가져오기, 전처리 및 보정부터 판별도 작성, 지온계 계산, CIPW 표준광물 계산, PNG / SVG 등으로 내보내기까지, 가능한 한 하나의 인터페이스에서 완료합니다. **클라우드 템플릿 생태계**와 **커뮤니티 협업**을 통해 도표와 알고리즘이 지속적으로 발전하고 더 많은 연구자에게 빠르게 전달되도록 합니다.

> 🚀 **우리의 비전**: 기초 지구화학 및 암석학 도표와 계산 기능을 융합한 통합 플랫폼을 구축하여 연구 효율성을 획기적으로 높이고 기술 장벽을 낮춤으로써, 과학자들이 과학적 발견 그 자체에 집중할 수 있도록 돕는 것입니다.

<div align="center">
  <img src="./images/software_ui_show.webp" alt="GeoChemistry Nexus 소프트웨어 UI" width="88%" style="border-radius: 10px; box-shadow: 0 4px 12px rgba(0,0,0,0.15);">
  <br>
  <sub>📈 판별도 · 📐 데이터 투영 · 🧩 템플릿 편집 · 🌐 다국어 UI</sub>
</div>

---

## 🧭 할 수 있는 일

| 모듈 | 설명 |
| :---: | --- |
| 🏠 **홈** | 빠른 링크, 실용 위젯, 지구과학 리소스 바로가기——창 전환 최소화 |
| 📊 **데이터 전처리** | 분석 데이터 가져오기 및 정리, 작도·계산 준비 |
| 📈 **판별도** | 템플릿 라이브러리로 삼각도, 산점도, 스파이더 다이어그램 등 작성; 데이터 투영 및 스타일 편집 지원 |
| 🌡️ **지온계** | 단일 광물 계산 등 일반적인 시나리오를 위한 지질 온압계 템플릿 통합 |
| 🪨 **CIPW 계산** | 표준광물 norm 계산, 암석학 분석 워크플로우 지원 |
| ☁️ **템플릿 생태계** | 공식·커뮤니티 템플릿 클라우드 업데이트; 사용자 정의 템플릿 패키징·공유·협업 |

---

## ✨ 핵심 기능

* **🎨 강력한 커스텀 작도**
    * 풍부한 작도 도구 내장 (선, 다각형, 텍스트, 화살표, 함수 곡선 등).
    * 커스텀 스크립트를 지원하여 데이터 가져오기 및 연산 규칙을 유연하게 설정, 고도의 커스터마이징 요구 충족.

* **🌐 네이티브 다국어 지원**
    * **도표 국제화**: 하나의 템플릿으로 한국어, 영어, 중국어 등 다국어 원클릭 전환 지원. 한 번의 작업으로 전 세계 배포 가능.
    * **인터페이스 현지화**: 소프트웨어 인터페이스는 영어(US), 간체 중국어, 독일어 등을 전면 지원합니다.

* **☁️ 클라우드 템플릿 생태계**
    * 공식 팀과 커뮤니티가 공동으로 유지 관리하는 도표 템플릿 라이브러리 내장.
    * 동적 업데이트를 지원하여 소프트웨어를 업그레이드하지 않고도 최신 연구용 도표 템플릿을 실시간으로 받을 수 있습니다.

* **🤝 편리한 배포 및 협업**
    * 커스텀 도표 템플릿의 패키징 및 내보내기를 지원하여, 팀이나 기관 간의 연구 성과 공유를 쉽게 실현합니다.

* **🧮 전문 지질학 계산 모듈**
    * 단일 광물 등 다양한 유형의 지온계(Geothermometer) 템플릿을 통합하여, 다양한 매개변수 계산 요구를 충족합니다.

---

## ⚡ 빠른 시작

### 📥 다운로드 및 설치

GitHub **[Releases](https://github.com/MaxwellLei/GeoChemistry-Nexus/releases)** 페이지에서 Windows 설치 패키지(현재 버전 **v0.7.1**)를 다운로드한 뒤, 마법사에 따라 설치하세요.

### 💻 시스템 요구 사항

| 항목 | 요구 사항 |
| --- | --- |
| 🖥️ 운영 체제 | Windows 7 SP1 이상 (Windows 10 / 11 권장) |
| ⚙️ 실행 환경 | [.NET 6.0 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/6.0) |
| 📦 아키텍처 | x64 |

> 💡 .NET Runtime이 설치되어 있지 않으면, 설치 프로그램 또는 첫 실행 시 다운로드를 안내합니다.

### 🚀 권장 시작 경로

| 단계 | 작업 |
| :---: | --- |
| 1️⃣ | **홈**에서 인터페이스와 바로가기 익히기 |
| 2️⃣ | 샘플 데이터로 **판별도**에서 공식 템플릿을 열어 첫 작도 완료 |
| 3️⃣ | 필요에 따라 **지온계** 또는 **CIPW** 모듈에서 계산 시도 |
| 4️⃣ | PNG / JPG / BMP / SVG로 내보내고 [공식 문서](https://geochemistry-nexus.pages.dev/)에서 고급 사용법 학습 |

---

## 🛠️ 기술 개요

| 분류 | 설명 |
| --- | --- |
| 💻 언어 및 프레임워크 | C# · WPF · .NET 6 |
| 🏗️ 아키텍처 | MVVM (CommunityToolkit.Mvvm) |
| 📊 작도 및 내보내기 | ScottPlot; PNG / JPG / BMP / SVG 지원 |
| 📜 스크립트 엔진 | Jint (템플릿 내 사용자 정의 로직) |
| 📄 라이선스 | [GNU GPL v3](../LICENSE) |

> 🔧 로컬 빌드 및 기여 안내는 `src/` 디렉터리 참조; Issue와 Pull Request는 GitHub에서 환영합니다.

---

## 🗺️ 로드맵

목표는 단순한 「작도 소프트웨어」가 아니라, **데이터 · 도표 · 계산 · 협업**이 통합된 연구 도구 체인을 구축하는 것입니다.

| 단계 | 방향 |
| --- | --- |
| 🔥 **진행 중** | 일반적인 지구화학·암석학 도표 지속 확충; 지온계·지압계 템플릿 개선 |
| 📅 **단기 계획** | 동位원 연대측정 관련 작도; 더 풍부한 지구화학 계산 도구 |
| 🔭 **중장기** | 연구 커뮤니티 및 템플릿 공유 · ML 보조 분석 · 로드 가능한 「새로운 도표」 판별 모델 · RAG 기반 AI 연구 어시스턴트 |

📌 전체 로드맵: [공식 문서 · Roadmap](https://geochemistry-nexus.pages.dev/docs/Roadmap)

---

## 👋 참여 및 연락처

**지질학 이론**, **알고리즘 개발**, **템플릿 디자인**, **C# 개발** 중 어디에 강점이 있든 환영합니다:

| 방법 | 설명 |
| --- | --- |
| 🐛 문제 보고 | Bug 보고 또는 기능 제안 → [GitHub Issues](https://github.com/MaxwellLei/GeoChemistry-Nexus/issues) |
| 💻 코드 기여 | Fork 후 Pull Request 제출 |
| 🌍 번역 지원 | UI 및 문서 현지화 (중 / 영 / 독 / 일 / 한 등) |
| 📈 템플릿 공유 | 자작 판별도 템플릿을 커뮤니티에 제공 |

### 💬 커뮤니티 채널

| 채널 | 정보 |
| --- | --- |
| 📧 이메일 | `maxwelllei@qq.com` |
| 💬 QQ 그룹 | `1076647740` |
| 🎮 Discord | https://discord.gg/mRm8dbwa4W |

---

## ⭐ Star 추이

<p align="center">
  <a href="https://star-history.com/#MaxwellLei/GeoChemistry-Nexus&Date">
    <img src="https://api.star-history.com/svg?repos=MaxwellLei/GeoChemistry-Nexus&type=timeline" alt="Star History">
  </a>
</p>

<div align="center">

✨ GeoChemistry Nexus가 연구에 도움이 되었다면, 지속적인 업데이트를 위해 **Star**를 눌러 주세요 ✨

</div>
