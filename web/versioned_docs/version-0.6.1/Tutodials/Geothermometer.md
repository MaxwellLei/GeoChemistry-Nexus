---
sidebar_position: 3
---

# 🌡️ Geothermometers

The Geothermometer page in the software is abbreviated as **GTM**. This page currently includes geothermometer calculations for minerals such as zircon, sphalerite, quartz, xenotime, chlorite, and biotite.

:::tip

Please note that the current Geothermometer module features a temporary UI. It will be adjusted as the functionality of the thermometers is enhanced.

:::

## 🌟 Features

Our Geothermometer module now utilizes an Excel-like interface, significantly lowering the learning curve. Calculations can be performed in two ways:

1.  **Using functions directly in cells**, identical to using Excel functions. We have pre-defined several calculation functions for various thermometers. For specific function details, please refer to the "Custom Functions" section in the Advanced Tutorials.
2.  **Using built-in geothermometer templates**, which is more direct and faster, requiring no programming knowledge.

![gtm_ui](imgs/gtm_ui.png)

## Usage Examples

### Custom Functions

The use of custom functions is consistent with Excel; simply enter a format like `=FuncName(args)` into a cell to call them.

Refer to the Advanced Tutorials for specific custom function references.

### Using Built-in Templates

First, within the Geothermometer module, select the corresponding calculation template from the menu bar:

![gtm_select_m](imgs/gtm_select_m.png)

A pop-up window will then appear asking to confirm whether to create a new sheet for the calculation.

If your current sheet contains data you do not want to overwrite, please select **OK**. Conversely, if you want to overwrite the current sheet, select **Cancel**.

![Notification](https://geo-1303234197.cos.ap-hongkong.myqcloud.com/V0_5_0_0/GTM_New_Sheet.png)

Next, a sample template for the thermometer will appear. Users only need to fill in the corresponding data and then use the "click-and-drag" fill handle (similar to Excel) to automatically complete the calculations (as shown in the animated example below).

![](https://geo-1303234197.cos.ap-hongkong.myqcloud.com/V0_5_0_0/Example.gif)

## Intermediate Calculation Processes

:::tip

We have already taken this into account. We understand that petrologists need to perform verification or view intermediate processes. We are currently designing a new UI and algorithms to implement this functionality.

:::

---

We cordially invite contributors to join us in creating a better software experience. 🤝😊