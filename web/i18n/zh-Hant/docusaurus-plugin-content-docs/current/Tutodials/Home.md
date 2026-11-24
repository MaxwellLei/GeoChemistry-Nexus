---
sidebar_position: 1
---

import useBaseUrl from '@docusaurus/useBaseUrl';

# 首頁

我們希望首頁能夠成為連接地球科學研究資源與日常工作流程的便捷入口。透過整合地球科學領域常用網站的快速存取入口，並提供輕量、實用的小工具，輔助使用者完成日常操作與簡單計算，將原本分散的功能模組、線上資源與網站工具有機地聯繫起來，構建一個互聯互通、高效便捷的綜合服務入口。

![plot_home_ui](/img/v0.9.0/en-US/Software_main_interface.webp)

首頁主要分為三個部分：**公告**、**快速連結** 和 **小工具**。

* **公告**：負責接收伺服器相關通知和推播
* **快速連結**：負責整合地球科學領域入口，分為兩大類。**推薦連結**和**個人連結**。推薦連結個人無法修改，是由伺服器進行內建推薦。當然，**如果你希望你的網站被加到首頁推薦中，請透過「關於」頁面聯絡我們**。個人連結是使用者自行新增、刪除，管理自己偏好的連結。
* **小工具**：小工具的設計目標是提供簡單、獨立且基礎的計算或操作功能，以補充常用的周邊工具，方便使用者日常使用。以後會做成可擴充的外掛形式，目前無法自由擴充。

## 小工具

小工具目前主要包含：**鹼度計算器、硬度計算器、黑體輻射計算器、地球化學週期表、年代地層速查卡、元素／氧化物換算器、地質常用單位換算器**。

**其中我們非常感謝 [Kurt Hollocher(Union College)](https://muse.union.edu/hollochk/kurt-hollocher/geochemistry/) 所公開的鹼度計算、硬度計算、黑體輻射計算方法參考**。

### 鹼度計算器

根據鹽酸滴定終點計算水樣鹼度。輸入 HCl 濃度、水樣體積和消耗的 HCl 體積後，給出鹼度（meq/L），並換算為 HCO₃⁻、CO₃²⁻、CaCO₃ 和 0.5(CaCO₃) 四種表示形式下的 g/L、meq/L、莫耳濃度和 ppm。

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/alkalinity_calculator.webp')}
    alt="鹼度計算器"
    style={{width: '60%'}}
  />
</div>

### 硬度計算器

根據鎂、鈣濃度計算水的總硬度。輸入 Mg、Ca 的濃度（ppm）後，換算為以 CaCO₃ 計的硬度（ppm），並按 USGS 分級標出軟水、中硬水、硬水或極硬水。

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/hardness_calculator.webp')}
    alt="硬度計算器"
    style={{width: '60%'}}
  />
</div>

### 黑體輻射計算器

根據溫度計算黑體輻射。輸入克耳文溫度後，給出史特凡–波茲曼總輻射功率和維恩峰值波長，並按普朗克公式繪製歸一化光譜曲線。

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/black_body_radiation_calculator.webp')}
    alt="黑體輻射計算器"
    style={{width: '70%'}}
  />
</div>

### 地球化學週期表

互動式週期表，可按地球化學分組查閱元素。點選元素可查看原子序、原子量、電子組態、地殼豐度、氧化態和常見氧化物分子量，也可按稀土、高場強、大離子親石、鉑族，以及親石、親鐵、親銅、親氣等分組醒目提示。

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/geochemical_periodic_table.webp')}
    alt="地球化學週期表"
    style={{width: '70%'}}
  />
</div>

### 年代地層速查卡

以 ICS 國際地質年代表為基礎的年代地層速查。可按中英文名稱檢索，或輸入年齡（Ma）反查所處地層單元，並沿層級向下查閱，查看界線年齡和 IUGS 官方配色。

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/chronostratigraphic_quick_reference_card.webp')}
    alt="年代地層速查卡"
    style={{width: '60%'}}
  />
</div>

### 元素／氧化物換算器

在元素與氧化物質量分數之間換算。選擇 Si/SiO₂、Al/Al₂O₃、Fe/Fe₂O₃ 或 FeO 等配對後，輸入一側的 wt%，按化學計量因子得到另一側結果，涵蓋全岩主量與常見微量氧化物。

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/element_oxide_converter.webp')}
    alt="元素／氧化物換算器"
    style={{width: '60%'}}
  />
</div>

### 地質常用單位換算器

在地質工作常用單位之間換算。涵蓋品位濃度（g/t、ppm、wt%、ppb、oz/t）、壓力應力（GPa、kbar、MPa、bar、atm、psi）、微區尺度（mm、μm、nm、Å、pm）、溫度（℃、K、℉）和同位素年代（Ga、Ma、ka、yr），並附常見地質參照值。

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/geological_common_unit_converter.webp')}
    alt="地質常用單位換算器"
    style={{width: '60%'}}
  />
</div>
