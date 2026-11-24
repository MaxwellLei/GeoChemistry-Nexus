---
sidebar_position: 3
---

import useBaseUrl from '@docusaurus/useBaseUrl';

# Geothermobarometer

In early versions this module was abbreviated **GTM**. It was later renamed geothermometer, and its formal name today is geothermobarometer.

Early versions included geothermometer calculations for minerals such as zircon, sphalerite, quartz, xenotime, chlorite, and biotite. The new geothermobarometer is still being updated and adapted, and it will be brought in line before long.

## Interface

The geothermobarometer module has two parts:

1. On the left is the **thermobarometer template list**. Look up a thermobarometer template that fits your work, or create your own thermobarometer calculation template.
2. On the right is the **help document for the thermobarometer template that is currently selected**.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/geothermobarometer_interface.webp')}
    alt="Geothermobarometer interface"
    style={{width: '100%'}}
  />
</div>

## Features

The geothermobarometer module was designed from the start to **follow the form of Excel**, so it would be easier to learn. **There are two main ways to use a thermobarometer**:

1. **Online thermobarometer templates**: users cannot modify the default **online thermobarometer templates**, but they can view how the algorithm is implemented. You can obtain more of the latest geothermobarometer templates over the network without updating the software. Researchers can search, filter, and choose a template that fits, then fill in data and calculate according to the guide. **This way is more direct and faster, and it does not require programming knowledge.**
2. **Independent calculation window**: from the top menu bar, choose **File -> Open Independent Spreadsheet** to open an **independent calculation window**. In that window, researchers may write custom calculations and combine different geothermobarometers. For the function that corresponds to each thermobarometer, see the advanced tutorials. **This way is more complex, and relatively more flexible.**

The geothermobarometer module also lets researchers create their own custom geothermobarometers. A finished geothermobarometer template can be exported and distributed so other researchers can use it quickly.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/export_geothermobarometer_template.webp')}
    alt="Export geothermobarometer"
    style={{width: '45%'}}
  />
</div>

## Examples

### Use an online thermobarometer template

First, in the geothermobarometer module, filter and select the calculation template from the left sidebar:

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/select_and_apply_thermobarometer_template.webp')}
    alt="Select a geothermobarometer"
    style={{width: '100%'}}
  />
</div>

You can read the template’s description in the right panel. The top bar also shows the title and tags of the geothermobarometer template that is currently selected. After you select the thermobarometer template, click **Apply** in the top bar to enter the thermobarometer calculation page.

On the thermobarometer page you can see the corresponding input columns. You can also click **Example** in the top bar to load example data. Enter the data required by the column titles, then **copy the calculation formula** or **use the click-and-drag fill handle** (as in Excel) to finish the calculation automatically, as in the animation below.

> The `Sample` column is not used in the calculation. It is only a note that identifies the sample.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/calculate_thermobarometer_using_formula.webp')}
    alt="Calculate a thermobarometer with a formula"
    style={{width: '80%'}}
  />
</div>

The current version of the thermobarometer can also show the internal calculation details.

Select the data row with the mouse, click **Inspect** at the top, and read the intermediate results in the sidebar that opens.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/thermobarometer_calculation_details.webp')}
    alt="Thermobarometer calculation details"
    style={{width: '100%'}}
  />
</div>

### Independent window: custom functions

As described above, choose **File -> Open Free Spreadsheet** in the menu bar to open the independent free-form window.

The independent window is an empty spreadsheet, with a few simple toolbar buttons.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/thermobarometer_free_form.webp')}
    alt="Thermobarometer free-form calculation sheet"
    style={{width: '100%'}}
  />
</div>

Using the independent window spreadsheet is like **using an Excel spreadsheet in everyday work. You can enter any value, and call a function by typing `=FunctionName(arguments)`**.

The difference is that our geothermobarometer calculations are registered as corresponding calculation functions, so researchers can customize the calculation more flexibly. You can even **compare the results of several thermobarometers in the same spreadsheet**.

:::note

In the independent window spreadsheet, **you cannot view the intermediate calculation steps for a data row**.

:::
