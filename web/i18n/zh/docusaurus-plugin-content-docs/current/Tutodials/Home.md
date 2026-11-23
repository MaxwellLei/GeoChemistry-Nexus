---
sidebar_position: 1
---

# 主页

我们希望主页能够成为连接地球科学研究资源与日常工作流程的便捷入口。通过整合地球科学领域常用网站的快捷访问入口，并提供轻量、实用的小工具，辅助用户完成日常操作与简单计算，将原本分散的功能模块、在线资源与网站工具有机地联系起来，构建一个互联互通、高效便捷的综合服务入口。

![plot_home_ui](/img/v0.9.0/en-US/Software_main_interface.webp)

主页主要分为三个部分：**公告**，**快速链接** 和 **小组件**。

* **公告**：负责接受服务器相关通知和推送
* **快速链接**：负责整合地球科学领域入口，分为两个大类。**推荐链接**和**个人链接**，推荐链接个人无法修改，是通过服务器进行内置推荐。当然，**如果你希望你的网站被添加到首页推荐中，请通过“关于”页面联系我们**。个人链接是用户个人进行添加删除管理个人的喜好链接。
* **小组件**：小部件的设计目标是提供简单、独立且基础的计算或操作功能，以补充常用的外围工具，方便用户日常使用。以后会做成可拓展的插件形式，目前无法自由拓展。

## 小组件

小组件目前主要包含：**碱度计算器，硬度计算器，黑体辐射计算器，地球化学周期表，年代地层速查卡，元素/氧化物换算器，地质常用单位换算器**。

**其中我们非常感谢 [Kurt Hollocher(Union Colloege)](https://muse.union.edu/hollochk/kurt-hollocher/geochemistry/) 所公开的碱度计算，硬度计算，黑体辐射计算方法参考**。

### 碱度计算器

根据盐酸滴定终点计算水样碱度。输入 HCl 浓度、水样体积和消耗的 HCl 体积后，给出碱度（meq/L），并换算为 HCO₃⁻、CO₃²⁻、CaCO₃ 和 0.5(CaCO₃) 四种表示形式下的 g/L、meq/L、摩尔浓度和 ppm。

![alkalinity_calculator](/img/v0.9.0/en-US/alkalinity_calculator.webp)

### 硬度计算器

根据镁、钙浓度计算水的总硬度。输入 Mg、Ca 的浓度（ppm）后，换算为以 CaCO₃ 计的硬度（ppm），并按 USGS 分级标出软水、中硬水、硬水或极硬水。

![hardness_calculator](/img/v0.9.0/en-US/hardness_calculator.webp)

### 黑体辐射计算器

根据温度计算黑体辐射。输入开尔文温度后，给出斯特藩–玻尔兹曼总辐射功率和维恩峰值波长，并按普朗克公式绘制归一化光谱曲线。

![black_body_radiation_calculator](/img/v0.9.0/en-US/black_body_radiation_calculator.webp)

### 地球化学周期表

交互式周期表，可按地球化学分组查阅元素。点选元素可查看原子序数、原子量、电子构型、地壳丰度、氧化态和常见氧化物分子量，也可按稀土、高场强、大离子亲石、铂族，以及亲石、亲铁、亲铜、亲气等分组高亮。

![geochemical_periodic_table](/img/v0.9.0/en-US/geochemical_periodic_table.webp)

### 年代地层速查卡

基于 ICS 国际地质年代表的年代地层速查。可按中英文名称检索，或输入年龄（Ma）反查所处地层单元，并沿层级下钻，查看界线年龄和 IUGS 官方配色。

![chronostratigraphic_quick_reference_card](/img/v0.9.0/en-US/chronostratigraphic_quick_reference_card.webp)

### 元素/氧化物换算器

在元素与氧化物质量分数之间换算。选择 Si/SiO₂、Al/Al₂O₃、Fe/Fe₂O₃ 或 FeO 等配对后，输入一侧的 wt%，按化学计量因子得到另一侧结果，覆盖全岩主量和常见微量氧化物。

![element_oxide_converter](/img/v0.9.0/en-US/element_oxide_converter.webp)

### 地质常用单位换算器

在地质工作常用单位之间换算。覆盖品位浓度（g/t、ppm、wt%、ppb、oz/t）、压力应力（GPa、kbar、MPa、bar、atm、psi）、微区尺度（mm、μm、nm、Å、pm）、温度（℃、K、℉）和同位素年代（Ga、Ma、ka、yr），并附常见地质参照值。

![geological_common_unit_converter](/img/v0.9.0/en-US/geological_common_unit_converter.webp)
