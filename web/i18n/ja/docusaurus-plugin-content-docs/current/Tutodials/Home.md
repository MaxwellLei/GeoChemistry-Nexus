---
sidebar_position: 1
---

import useBaseUrl from '@docusaurus/useBaseUrl';

# ホーム

ホームは、地球科学の研究リソースと日常の作業の流れとをつなぐ、便利な入口であってほしいと考えています。地球科学でよく使うウェブサイトへのショートカットをまとめ、軽くて実用的な小ツールを用意することで、日常の操作と簡単な計算を助けます。もともと分散していた機能モジュール、オンラインリソース、ウェブサイト上のツールを有機的につなぎ、相互に行き来でき、効率よく便利な総合的なサービス入口を作ります。

![ソフトウェアのメイン画面](/img/v0.9.0/en-US/Software_main_interface.webp)

ホームは主に三つの部分に分かれます。**お知らせ**、**クイックリンク**、**小ツール** です。

* **お知らせ**：サーバーからの通知と配信を受け取ります。
* **クイックリンク**：地球科学分野への入口をまとめます。大きく二つの種類に分かれます。**おすすめリンク** と **個人リンク** です。おすすめリンクは個人では変更できず、サーバーから内蔵のおすすめとして配信されます。**ウェブサイトをホームのおすすめへ追加してほしい場合は、「このソフトウェアについて」のページから連絡してください。** 個人リンクは、ユーザー自身が追加・削除し、好みのリンクを管理するためのものです。
* **小ツール**：小ツールの設計目標は、簡単で独立した基礎的な計算や操作を提供し、よく使う周辺ツールを補って、日常の使用を便利にすることです。将来は拡張可能なプラグイン形式にする予定ですが、現在は自由に拡張できません。

## 小ツール

小ツールには現在、主に次のものが含まれます。**アルカリ度計算、硬度計算、黒体放射計算、地球化学周期表、年代層序早見カード、元素／酸化物換算、地質常用単位換算** です。

**アルカリ度計算、硬度計算、黒体放射計算の方法については、公開されている [Kurt Hollocher (Union College)](https://muse.union.edu/hollochk/kurt-hollocher/geochemistry/) の資料を参考にしています。感謝します。**

### アルカリ度計算

塩酸滴定の終点から、水試料のアルカリ度を計算します。HCl 濃度、水試料の体積、消費した HCl の体積を入力すると、アルカリ度（meq/L）を示し、HCO₃⁻、CO₃²⁻、CaCO₃、0.5(CaCO₃) の四つの表し方について、g/L、meq/L、モル濃度、ppm へ換算します。

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/alkalinity_calculator.webp')}
    alt="アルカリ度計算"
    style={{width: '60%'}}
  />
</div>

### 硬度計算

マグネシウムとカルシウムの濃度から、水の全硬度を計算します。Mg と Ca の濃度（ppm）を入力すると、CaCO₃ としての硬度（ppm）へ換算し、USGS の区分に従って軟水、中硬水、硬水、非常に硬水のいずれかを示します。

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/hardness_calculator.webp')}
    alt="硬度計算"
    style={{width: '60%'}}
  />
</div>

### 黒体放射計算

温度から黒体放射を計算します。ケルビン温度を入力すると、シュテファン＝ボルツマンの全放射パワーとウィーンのピーク波長を示し、プランクの式に従って規格化したスペクトル曲線を描きます。

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/black_body_radiation_calculator.webp')}
    alt="黒体放射計算"
    style={{width: '70%'}}
  />
</div>

### 地球化学周期表

地球化学的なグループ別に元素を調べられる、対話型の周期表です。元素を選ぶと、原子番号、原子量、電子配置、地殻存在度、酸化数、よくある酸化物の分子量を確認できます。希土類、高電界強度、大イオン親石、白金族、ならびに親石、親鉄、親銅、親気などのグループで強調表示することもできます。

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/geochemical_periodic_table.webp')}
    alt="地球化学周期表"
    style={{width: '70%'}}
  />
</div>

### 年代層序早見カード

ICS の国際年代層序表に基づく、年代層序の早見です。中国語名または英語名で検索するか、年代（Ma）を入力して所属する層序単元を逆引きできます。階層を掘り下げ、境界の年代と IUGS の公式配色を確認できます。

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/chronostratigraphic_quick_reference_card.webp')}
    alt="年代層序早見カード"
    style={{width: '60%'}}
  />
</div>

### 元素／酸化物換算

元素と酸化物の質量分率を相互に換算します。Si/SiO₂、Al/Al₂O₃、Fe/Fe₂O₃、FeO などの組を選んだあと、一方の wt% を入力すると、化学量論係数に従って他方の結果を得ます。全岩の主要成分と、よくある微量酸化物を対象とします。

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/element_oxide_converter.webp')}
    alt="元素／酸化物換算"
    style={{width: '60%'}}
  />
</div>

### 地質常用単位換算

地質の作業でよく使う単位を相互に換算します。品位・濃度（g/t、ppm、wt%、ppb、oz/t）、圧力・応力（GPa、kbar、MPa、bar、atm、psi）、微小領域の尺度（mm、μm、nm、Å、pm）、温度（℃、K、℉）、同位体年代（Ga、Ma、ka、yr）を対象とし、よく使う地質の参照値も付けています。

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/geological_common_unit_converter.webp')}
    alt="地質常用単位換算"
    style={{width: '60%'}}
  />
</div>
