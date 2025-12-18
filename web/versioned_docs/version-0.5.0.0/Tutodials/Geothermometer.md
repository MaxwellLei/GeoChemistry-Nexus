---
sidebar_position: 2
---

# 🌡️ Geological Thermometer

The Geological Thermometer page in the software is abbreviated as **GTM**. Currently, this page includes thermometric calculations for minerals such as zircon, sphalerite, quartz, arsenopyrite, chlorite, and biotite.

:::tip

We have completely revamped the UI and implementation logic of the plotting module, making it fundamentally different from versions prior to `v0.5.0.0`.

In the future, we plan to add more features, including but not limited to thermobarometric calculations, data and plot interaction functionalities (referencing Origin plotting software).

:::

## 🌟 Features

Our geological thermometer module now adopts an Excel-like interface, greatly lowering the usage barrier. There are two ways to perform thermometric calculations:

1. Directly use functions in cells, just like using Excel functions. We have pre-defined several calculation functions for thermometers. For detailed information about these functions, please refer to the Advanced Tutorial and Custom Functions sections.
2. Use built-in geological thermometer templates. This method is more straightforward and requires no programming background.

![GTM UI](https://geo-1303234197.cos.ap-hongkong.myqcloud.com/V0_5_0_0/GTM_UI.png)

## Usage Example

### Custom Functions

Custom functions are used exactly like in Excel. Simply input `=FuncName(args)` in a cell to call them. For more details on custom functions, please refer to the Advanced Tutorial.

### Using Built-in Templates

First, in the Geological Thermometer module, select the corresponding thermometer calculation template from the menu bar:

![](https://geo-1303234197.cos.ap-hongkong.myqcloud.com/V0_5_0_0/GTM_Cal.png)

A confirmation window will then pop up to ask whether you want to create a new worksheet for the calculation. If you already have data in your current worksheet and do not want to overwrite it, please select "OK." Otherwise, if you do not have any data, select "Cancel."

![Notification](https://geo-1303234197.cos.ap-hongkong.myqcloud.com/V0_5_0_0/GTM_New_Sheet.png)

A sample thermometer template will then appear. Users simply need to fill in the corresponding data, then use Excel-like drag and fill operations to auto-calculate (as shown in the GIF below).

![](https://geo-1303234197.cos.ap-hongkong.myqcloud.com/V0_5_0_0/Example.gif)

-----

We warmly welcome contributors to join us and help create a better software experience together. 🤝😊
