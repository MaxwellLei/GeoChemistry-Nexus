---
sidebar_position: 2
---

import useBaseUrl from '@docusaurus/useBaseUrl';

# Diagrams

In the diagram module we will bring together more basic geoscience diagram templates, including but not limited to tectonic-setting discrimination diagrams and rock-classification diagrams. **Our final goal is a comprehensive diagram-frame and plotting toolkit for the geosciences, and the greatest convenience we can offer researchers.** 🧪

:::info

If you research geological diagrams or a related field and want to promote your own diagram, please contact us. We will help your diagram reach a wider research community and further improve how widely and how effectively the work is shared.

:::

## Quick start

The diagram module has two main parts: the diagram template library and the diagram operation page. First choose the diagram type you need in the template library, then open the corresponding operation page to enter data, work with the diagram, and export the result.

### Template library

The diagram module is divided into three groups: **Online cloud templates**, **My templates**, and **Tool diagram templates**.

**Online cloud templates** are updated and maintained by us on an ongoing basis. You do not need to update the software. While you are online, you can obtain updated diagram templates and keep the most complete and authoritative template set.

**My templates** are for cases where a custom template is needed for a specific study. You can create these templates yourself and export them so they are easy to share with other researchers, which supports academic exchange and dissemination.

> *In the future we plan to build a dedicated diagram-template community, where users can easily create, upload, share, and download **personal custom templates**, and make the system more flexible and extensible.*

**Tool diagram templates** set the fixed, routine diagrams apart. They currently include the **REE spider diagram, the trace-element spider diagram, and the Harker multi-panel diagram**, plus ordinary two-dimensional and ternary scatter plots.

#### Interface

By default, the plotting module opens on the built-in geoscience template library. The interface has three main parts:

* **Left — diagram categories**: every template level and the templates under it, including the custom template list.
* **Right — diagram cards**: the diagram cards at the current level, including the name and a preview image.
* **Top — navigation bar**: the software uses a borderless design, so the top navigation bar carries part of each page’s function. On the diagram page it shows the current diagram template path, diagram template search, and related functions.

![graphical_interface](/img/v0.9.0/en-US/graphical_interface.webp)

Choose a suitable diagram template and click its card to open the diagram operation page. You can also right-click a diagram template card for more actions, such as export, delete, and favorite.

:::tip

Press the right <kbd>Alt</kbd> key to quickly delete or favorite a diagram template.

:::

#### Top menu bar

The **File** menu in the top bar contains:

1. **New Diagram**: create a custom diagram template.
2. **Open Diagram**: open a diagram template temporarily. Only `.diag.zip` files are supported.
3. **Import Diagram**: import an external diagram template package (`.diag.zip`) into the local My Templates list.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/graphic_menu_bar.webp')}
    alt="Diagram menu bar"
    style={{width: '50%'}}
  />
</div>

### Diagram operation page

#### Interface

The diagram page has five main parts:

- **Toolbar**: shortcut buttons and three function tabs: Plot, Data, and Edit.
- **Layer list (objects)**: the list of drawing elements on the template. Click an element to edit its properties.
- **Drawing canvas**: the central area for viewing the chart, importing data, visual settings, and the template description.
- **Status bar**: basic drawing information, including the current diagram language and coordinates.
- **Property panel**: properties of the selected drawing element, such as color and size, so you can get the visual result you want.

![graphical_operation_interface](/img/v0.9.0/en-US/graphical_operation_interface.webp)

#### Diagram toolbar

The toolbar is a set of **shortcut buttons**. The main ones are:

1. Return to the diagram template library
2. Switch the working state (diagram mode, data mode, edit mode)
3. Diagram property panels, operation tools, and related controls

See the figure below for the detailed functions.

![graphical_toolbar](/img/v0.9.0/en-US/graphical_toolbar.webp)

### Layer list

Drawing elements fall into 7 groups:

- **Line**: defines basic diagram boundaries or line segments.
- **Text**: labels and annotations.
- **Polygon**: closed shapes inside the chart.
- **Arrow**: directed drawing objects.
- **Function**: lets the user enter a custom mathematical function and its domain.
- **Axes**: the axes of the chart.
- **Data point**: elements that represent imported data.

**Default render order (top to bottom): `Data points > Text > Arrows > Functions > Lines > Polygons > Axes`.**

![tutorial_plot5](/img/v0.9.0/en-US/diagrammatic_object_selection_example.webp)

When you select an element in the layer panel, it is highlighted on the canvas, and the other elements become translucent so they are less distracting. The property panel then shows the properties of that element. 🔍

You can change these properties to reach the visual style you want.

:::tip

To deselect every element, **right-click anywhere on the canvas** or use the **Deselect** button on the toolbar.

:::

## Examples

### Example

1. From the **diagram template library**, choose a diagram template and open the diagram operation page. For example, choose the TAS diagram.
2. When you need to enter data, switch from diagram mode to data mode.
   
   ![tutorial_plot6](/img/v0.9.0/en-US/diagram_data_input.webp)


For a TAS diagram, four columns must be entered: `Category`, `SiO2`, `K2O`, and `Na2O`. The `Category` column groups the data and builds the legend. The `Sample` column labels each data sample and does not take part in any calculation (it may be left empty). The other elements use the unit `wt.%`. If you are not sure what data this diagram expects, which units it uses, or how to use the diagram, click the diagram help button at the upper right of the diagram area to open its description.

3. Once you know what is required, enter your data and click the plot button at the upper right of the data panel to draw the data points. If the data input is wrong, correct the data and click plot again. ![tutorial_plot8](/img/v0.9.0/en-US/data_plotting.webp)
4. **Export the diagram**. We provide a **shortcut button that copies the plotted diagram to the clipboard**, and a **formal export**. In the formal export panel, image formats `jpg`, `png`, `bmp`, and `svg` are supported. When a scientific figure needs to be a vector graphic, we recommend `svg`. We also support **sending the result directly to a third-party drawing program**, without the intermediate step of exporting and then importing. The built-in third-party recommendations are **CorelDRAW, Inkscape, and Adobe Illustrator**. We recommend linking to Inkscape for later scientific editing. You can also set another third-party drawing program, provided that program supports the `svg` vector format. In Settings, specify the path of that drawing program.
   ![tutorial_plot8](/img/v0.9.0/en-US/export_diagram.webp)

### Additional notes

A diagram template can switch languages. One diagram can be shown in several language versions, for international use.

For example, a diagram for a paper in a Chinese journal needs the Chinese language version, and publication in an English-language journal such as SCI/IE needs the English version of the diagram. Click the control at the upper right of the diagram toolbar to switch the diagram to the corresponding language in one click.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/switch_graphic_language.webp')}
    alt="Switch diagram language"
    style={{width: '60%'}}
  />
</div>
