---
sidebar_position: 1
---

import useBaseUrl from '@docusaurus/useBaseUrl';

# 홈

홈이 지구과학 연구 자원과 일상 작업 흐름을 잇는 편리한 입구가 되기를 바랍니다. 지구과학 분야에서 자주 쓰는 웹사이트의 빠른 접근 입구를 모으고, 가볍고 실용적인 작은 도구를 제공하여 일상 조작과 간단한 계산을 돕습니다. 흩어져 있던 기능 모듈, 온라인 자원, 웹사이트 도구를 유기적으로 연결하여, 서로 이어지고 효율적이며 편리한 종합 서비스 입구를 만듭니다.

![plot_home_ui](/img/v0.9.0/en-US/Software_main_interface.webp)

홈은 크게 세 부분으로 나뉩니다. **공지**, **빠른 링크**, **위젯**입니다.

* **공지**: 서버의 관련 알림과 푸시를 받습니다
* **빠른 링크**: 지구과학 분야 입구를 모으며, 두 범주로 나뉩니다. **추천 링크**와 **개인 링크**입니다. 추천 링크는 개인이 수정할 수 없으며, 서버에서 내장 추천으로 제공합니다. **웹사이트를 홈 추천에 추가하려면 "정보" 페이지를 통해 연락해 주십시오**. 개인 링크는 사용자가 직접 추가하고 삭제하며, 선호하는 링크를 관리합니다
* **위젯**: 단순하고 독립적이며 기초적인 계산 또는 조작 기능을 제공하여, 자주 쓰는 주변 도구를 보완하고 일상 사용을 편리하게 하는 것이 목표입니다. 이후에는 확장 가능한 플러그인 형태로 만들 예정이며, 현재는 자유롭게 확장할 수 없습니다

## 위젯

위젯에는 현재 주로 **알칼리도 계산기, 경도 계산기, 흑체 복사 계산기, 지구화학 주기율표, 연대층서 빠른 참조 카드, 원소/산화물 환산기, 지질 상용 단위 환산기**가 있습니다.

**알칼리도 계산, 경도 계산, 흑체 복사 계산 방법은 [Kurt Hollocher(Union College)](https://muse.union.edu/hollochk/kurt-hollocher/geochemistry/)가 공개한 자료를 참고하였으며, 이에 깊이 감사드립니다**.

### 알칼리도 계산기

염산 적정 종말점으로 물 시료의 알칼리도를 계산합니다. HCl 농도, 물 시료 부피, 소비된 HCl 부피를 입력하면 알칼리도(meq/L)를 제시하고, HCO₃⁻, CO₃²⁻, CaCO₃, 0.5(CaCO₃)의 네 가지 표현으로 g/L, meq/L, 몰 농도, ppm을 환산합니다.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/alkalinity_calculator.webp')}
    alt="알칼리도 계산기"
    style={{width: '60%'}}
  />
</div>

### 경도 계산기

마그네슘과 칼슘 농도로 물의 총경도를 계산합니다. Mg, Ca의 농도(ppm)를 입력하면 CaCO₃로 나타낸 경도(ppm)로 환산하고, USGS 등급에 따라 연수, 중간 경수, 경수, 매우 경수를 표시합니다.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/hardness_calculator.webp')}
    alt="경도 계산기"
    style={{width: '60%'}}
  />
</div>

### 흑체 복사 계산기

온도로 흑체 복사를 계산합니다. 켈빈 온도를 입력하면 슈테판-볼츠만 총복사 출력과 빈 피크 파장을 제시하고, 플랑크 공식에 따라 정규화 스펙트럼 곡선을 그립니다.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/black_body_radiation_calculator.webp')}
    alt="흑체 복사 계산기"
    style={{width: '70%'}}
  />
</div>

### 지구화학 주기율표

지구화학 그룹별로 원소를 찾아볼 수 있는 대화형 주기율표입니다. 원소를 선택하면 원자 번호, 원자량, 전자 배치, 지각 존재도, 산화 상태, 흔한 산화물의 분자량을 볼 수 있습니다. 희토류, 고장력 원소, 대이온친석원소, 백금족, 그리고 친석, 친철, 친동, 친기 등의 그룹으로 강조할 수도 있습니다.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/geochemical_periodic_table.webp')}
    alt="지구화학 주기율표"
    style={{width: '70%'}}
  />
</div>

### 연대층서 빠른 참조 카드

ICS 국제 지질 연대표에 기반한 연대층서 빠른 참조입니다. 중국어와 영어 이름으로 검색하거나, 연령(Ma)을 입력하여 해당하는 층서 단위를 역으로 찾을 수 있습니다. 계층을 따라 내려가며 경계 연령과 IUGS 공식 배색을 확인합니다.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/chronostratigraphic_quick_reference_card.webp')}
    alt="연대층서 빠른 참조 카드"
    style={{width: '60%'}}
  />
</div>

### 원소/산화물 환산기

원소와 산화물 질량 분율 사이를 환산합니다. Si/SiO₂, Al/Al₂O₃, Fe/Fe₂O₃ 또는 FeO 등의 짝을 선택한 뒤, 한쪽의 wt%를 입력하면 화학량론 인자로 다른 쪽 결과를 얻습니다. 전암 주원소와 흔한 미량 산화물을 다룹니다.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/element_oxide_converter.webp')}
    alt="원소/산화물 환산기"
    style={{width: '60%'}}
  />
</div>

### 지질 상용 단위 환산기

지질 작업에서 자주 쓰는 단위 사이를 환산합니다. 품위·농도(g/t, ppm, wt%, ppb, oz/t), 압력·응력(GPa, kbar, MPa, bar, atm, psi), 미세 영역 규모(mm, μm, nm, Å, pm), 온도(℃, K, ℉), 동위원소 연대(Ga, Ma, ka, yr)를 다루며, 흔한 지질 참조값도 함께 제공합니다.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/geological_common_unit_converter.webp')}
    alt="지질 상용 단위 환산기"
    style={{width: '60%'}}
  />
</div>
