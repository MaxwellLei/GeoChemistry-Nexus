---
sidebar_position: 1
---

import useBaseUrl from '@docusaurus/useBaseUrl';

# Make a diagram template

For a diagram that is not yet in the online diagram template library, you can make a custom diagram template. Package the custom template as a template package, and you can share it quickly with other researchers.

You can also offer your template to the developer for inclusion in the online diagram template library. We are sincerely grateful for every contribution.

## Create quickly

You can create a new diagram template from the menu bar with `File` -> `New Diagram Template`, as shown below:

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/new_graphic_template.webp')}
    alt="Diagram settings"
    style={{width: '50%'}}
  />
</div>

After you click New Diagram Template, a dialog for creating a new diagram template appears:

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/new_graphic_template_interface.webp')}
    alt="New diagram interface"
    style={{width: '100%'}}
  />
</div>

A new custom diagram template has three main parts to configure:

1. **Default supported languages**: you can choose a built-in language shortcut from the selection box on the right. We provide ten languages, including Simplified Chinese, Traditional Chinese, American English, Japanese, Russian, Korean, and German.

    > The default language is the fallback used when another language is untranslated or an error occurs. In the new version, we no longer allow users to define custom language codes. If you need support for your language, contact the developer.

2. **Diagram template category (hierarchy)**: we provide a built-in shortcut category structure. This setting controls where your template sits in the hierarchy of the diagram template list.

3. **Diagram template type**: two types are supported today: a **two-dimensional coordinate system** and ternary diagrams.

When the settings are complete, click OK to enter the custom drawing interface. From here, focus on the Edit toolbar. After you click Edit, the system shows a confirmation dialog before the chart is edited. After you confirm, you enter edit mode, where you can view and use the tools on the edit toolbar.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/empty_diagram_template.webp')}
    alt="Empty diagram"
    style={{width: '100%'}}
  />
</div>

## Draw the diagram

In edit mode, the following actions are available:

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/graphical_toolbar.webp')}
    alt="Draw the diagram"
    style={{width: '100%'}}
  />
</div>

* **Save**: save the diagram template. After you click it, the program generates a matching thumbnail from the current drawing state by default.
* **Save As**: save the diagram template to a different file location.
* **Add Line**: when enabled, you enter Add Line mode. Click the first point in the plot area to start the line, and click the second point to finish the line object.
* **Add Text**: also called an annotation. When enabled, you enter Add Text mode. Click a location on the plot to create it. The default text is `Text`. You can change the position or the content in the properties section of the layer panel.
* **Add Polygon**: when enabled, you enter Add Polygon mode. Add a closed polygon by left-clicking vertices in sequence, then right-click to close the shape.
* **Add Arrow**: when enabled, you enter Add Arrow mode. Adding an arrow is similar to creating a line.
* **Add Function**: clicking this adds a default function, `sin(x)`, with a domain of [-10, 10]. You can customize the formula in the property panel.
* **Undo/Redo**: these are disabled when no drawing object has been created or deleted. By default, only the last 10 actions are stored in the history.
* **Delete**: delete a drawing object. First select the object (for example, text), then click Delete to remove it.

### Add a line

Below is an example of the property panel for adding a line. Through the property panel you can adjust the line position and other properties precisely.

**The Pick Coordinates button to the right of each coordinate lets you readjust and capture a coordinate in the plot area.** Once it is triggered, the next left-click in the plot area picks that click location as the coordinate.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/add_line.webp')}
    alt="Add a line"
    style={{width: '100%'}}
  />
</div>

### Add an arrow

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/add_arrow.webp')}
    alt="Add an arrow"
    style={{width: '100%'}}
  />
</div>

### Add a polygon

**After Add Polygon is enabled, left-click in the canvas to confirm polygon vertices in sequence. A preview of the polygon is shown while you do this. Right-click to finish creating the polygon.** A polygon object has a vertex list. You can select, modify, or delete any vertex at any time.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/add_polygon.webp')}
    alt="Add a polygon"
    style={{width: '100%'}}
  />
</div>

### Add text

After Add Text is enabled, left-click anywhere in the canvas to create a text object at that location. The status bar at the bottom of the canvas shows which language the content of the current text object belongs to.

Because diagrams support multiple languages natively, translation of multilingual content is explained in detail later.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/add_text.webp')}
    alt="Add text"
    style={{width: '100%'}}
  />
</div>

### Add a function

Adding a function differs from the objects above. **After you click Add Function, a default function object is added immediately.** The default function is `sin(x)`. You can change the formula in the property panel to define your function.

For a function object, the two most important parameters are the **domain** and the **sampling points**.

* **The domain defines the range in which the function is displayed on the canvas.**
* **The sampling points control the drawing precision of the function on the canvas**, which in turn affects the accuracy of the mouse hit-test. The default is `1000`. The higher the precision, the finer the rendering, but it also increases the program’s rendering load.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/add_function.webp')}
    alt="Add a function"
    style={{width: '100%'}}
  />
</div>

## Complete the diagram template

After the basic graphics are drawn, a complete template still needs:

1. **Data and script**: defines the template’s input data and the data-calculation algorithm, that is, how the data are calculated and plotted.
2. **Diagram document**: the help text that explains the diagram.
3. **Languages**: if the diagram template is set to more than one language, those parts must be translated. That includes the diagram document above and text objects on the diagram canvas.

You can complete the template on the diagram operation page, but we still recommend doing it in the diagram editor window, which is more convenient.

### Data and script

Data and script are a key part of a diagram template, because they define how data in the diagram are calculated and finally shown on the plot.

Two parameters are required: the template’s **variable parameters** and the **calculation algorithm (script)**, as shown below:

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/diagram_data_and_script.webp')}
    alt="Diagram data and script"
    style={{width: '100%'}}
  />
</div>

For the template’s **variable parameters**, you can add columns at the top to represent the input columns the diagram needs. There are two optional parameters.

1. Unit is the unit of the input column. It is for display only and does not affect the data algorithm.
2. Group is the group of the input column. It is for display only and does not affect the data algorithm.

**The column titles you define in the data table become registered variables that the script below can call directly.** You do not need to define the input-column variables again in the script.

The **calculation algorithm (script)** is written in `JavaScript` by default. The default script example is `return [X,Y]`, which returns the input columns `X` and `Y` defined in the data table to the two-dimensional axes.

For example, for a TAS diagram the parameters should be `SiO2, Na2O, K2O`. The script is:

```javascript
// Calculate using the variables K2O + Na2O
var result1 = K2O + Na2O;
// Use SiO2
var result2 = SiO2;
// Return the two calculated values. For the default two-dimensional plot, there are only two return values.
// The first position is the X return value, and the second is the Y return value.
return [result2, result1]
```

Or you can write the script as follows:

```javascript
var result = K2O + Na2O
return [SiO2, result]
```

Note that the positions of the return values are fixed. In `[x, y]`, the first value is always X (bottom axis) and the second is Y (left axis).

:::info

For a ternary diagram, the final return format is `[A, B, C]`, where the first value is A (bottom axis), the second is B (left axis), and the third is C (right axis).

:::

### Diagram document

The diagram document is a necessary step so other researchers can quickly learn the basic information and how to use the diagram.

Write the diagram document on the help document page. We provide a simple toolbar for ordinary document needs. You can also write the content in `Office Word` and paste it here. Most of that formatting is compatible.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/diagram_help_document.webp')}
    alt="Diagram help document"
    style={{width: '100%'}}
  />
</div>

For the format of the diagram document, we recommend the following standard:

* **Introduction**: explain the basic concept and function of the diagram template, so users can understand it quickly.
* **Data format**: specify the input data format and column titles required to read valid data.
* **References**: list the references used to create the diagram template and its content.
* **Contributors**: the names of the people who took part in making the diagram template. You are encouraged to include your personal website.

### Languages

We provide two ways to set multiple languages:

The first is the **Switch Language** option in the top toolbar of the diagram page. It lets you set specific content for a second language.

The second is to edit multilingual content in the diagram editor window, as shown below.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/illustrated_multilingual.webp')}
    alt="Diagram data and script"
    style={{width: '100%'}}
  />
</div>
