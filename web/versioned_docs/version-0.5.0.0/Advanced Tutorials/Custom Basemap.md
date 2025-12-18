---
sidebar_position: 1
---

# 🎨 Custom Basemap

For mapping, we have recently redesigned the logic of the basemap files from the ground up. Now we can freely create, modify, import, and delete basemaps through the software.
:::info
We encourage contributors to create relevant basemaps, participate in contributions, and work together to build a better software.
We plan to create a community-like environment shortly, providing services for uploading and downloading basemaps to facilitate widespread dissemination.
:::

## Edit Mode
You can now enter edit mode by clicking on `File` -> `Edit Mode` in the upper left corner, as shown in the image below:
![](https://geo-1303234197.cos.ap-hongkong.myqcloud.com/EditModeMenu.png)
Once you enter edit mode, there will be three changes:
1. The previous toolbar has been replaced with edit mode-specific convenience tools.
   ![](https://geo-1303234197.cos.ap-hongkong.myqcloud.com/EditModeToolBar.png)
2. Edit mode has its own specific project list, meaning that it is different from the drawing list in the default drawing mode. This can be understood as a project list for editing custom basemaps.
3. Certain properties of drawing elements will display relevant properties such as drawing location.
## Custom Basemap
You can now create a new basemap via `File` -> `Edit Mode`, as shown in the image below:
![](https://geo-1303234197.cos.ap-hongkong.myqcloud.com/NewBasemap.png)
Creating a new basemap will open a dialog box, as shown below. You need to fill in the category parameters for the basemap, which can be understood as a "special project name." The entered content must meet the following format requirements:
1. Use the English character `,` as a separator.
2. The primary category is recommended to be in the native language, for example: `English` or `简体中文`.
3. Subsequent categories can be flexible, but it is suggested that the secondary categories are named as `Igneous Rock`, `Metamorphic Rock`, `Sedimentary Rock`, `Others`, etc., tertiary categories are to be named by function (e.g., `Tectonic Environment`), quaternary categories by the author’s name + date (e.g., `Lucy (2020)`), and quinary categories by the required elements (e.g., `TiO2-SiO2-Y`).
For a normal name example: `English,Igneous Rock,Rock Category,TAS`. The final category will be displayed in a straightforward manner based on the following rules:
![](https://geo-1303234197.cos.ap-hongkong.myqcloud.com/NameRule.png)
Please note that after creating a project file, the basemap file will not be created immediately; it will only be created after clicking the save button. The format of the basemap file is `json`. The naming rule for the file is based on the classification path; for this example, it would be `English_Igneous Rock_Rock Category_TAS.json`. 
The storage path of the basemap file is: based on the installation location of the program -> `Data` -> `PlotData` -> `Custom` folder.
It should be noted that a complete basemap file needs to meet the following requirements:
1. **Basemap Drawing**: that is, completing the drawing of the basemap.
2. **Script Settings**: fill in the parameters and execution scripts required for plotting the basemap.
3. **Guide Writing**: write the guide content for the basemap according to a certain format.
### Basemap Drawing
Basemap drawing relies mainly on the toolbar and the property panel.
![](https://geo-1303234197.cos.ap-hongkong.myqcloud.com/EditModeToolBar.png)
* **Add Lines**: Lines, also known as border, can be created by clicking the add line button, then clicking the starting and ending points on the drawing. Although this method may be a bit rough, you can adjust the position in the property panel after selecting the recently created boundary in the layer panel.
* **Add Annotations**: Annotations, also known as text, can be created similarly by clicking the add text button, then clicking a specified position in the drawing. The default annotation text is `Text`, and modifications to the text position or content can also be done from the property position in the layer panel.
* **Add Polygons**: This feature is similar to the previous ones; you can create a closed polygon by consecutively clicking points with the left mouse button and closing the shape with the right mouse button.
* **Script Settings**: Fill in some necessary parameters and rules for plotting. Detailed content will be discussed in future documentation.
* **Save**: Save the basemap; clicking the save button is the only way to create the corresponding basemap file.
* **Exit Edit Mode**: Exit edit mode and return to the default drawing interface.
#### Adding Lines
Here is an example of the property panel for adding lines. Through the property panel, you can make fine adjustments to the position and other properties of the lines. The positioning icon on the right, when triggered, sets the position to the clicked location in the drawing area (this feature is still under development).
![](https://geo-1303234197.cos.ap-hongkong.myqcloud.com/ShuXinMianBan.png)
### Script Settings
Script settings are a crucial part of plotting, determining the custom plotting logic. Two parameters are required: plotting parameters and scripts. As shown in the image below:
![](https://geo-1303234197.cos.ap-hongkong.myqcloud.com/JsSetting.png)
For the script parameters, you need to specify which columns of data in the plotting data file (`xlsx`, `xls`, `csv`) should be read. The input rule is to separate them with English `,`. 
**Currently, the first parameter must be `Group`**; it will categorize different data points during plotting, thereby influencing the display of legend categories. The remaining parameters should be based on the needs of the custom basemap.
Scripts are written in `JavaScript`. For basic syntax details, you can refer to related documentation and we won’t elaborate here. 
For example, parameter input: `Group,SiO2,Na2O,K2O`. The script content is as follows:

```javascript
// Calculate using variables K2O + Na2O
var result1 = K2O + Na2O;
// Calculate using SiO2
var result2 = SiO2;
// Return the two calculated values. Note that for the default two-dimensional coordinate axis image, there are only two return values.
// The first position represents the return value for X, and the second position is for Y.
[result2, result1]
```
### Guide Writing
Guide writing is also an important task as it allows ordinary users to quickly understand the basic information and usage of the basemap. Write the guide content at the position shown in the image below.
![](https://geo-1303234197.cos.ap-hongkong.myqcloud.com/GuideContent.png)
You can choose to write it directly, but we prefer using Office or WPS to create an `rtf` file, write the content within it, and then copy and paste it here. As shown in the image below:
![](https://geo-1303234197.cos.ap-hongkong.myqcloud.com/RTFGuide.png)
For the format of guide writing, we recommend the following standard:

* Intro: Responsible for introducing some basic concepts and functions of the basemap, helping users quickly understand it.
* Data Format: Specifies what the input data format is; that is, what the column headers should be for valid data reading.
* References: Related content referring to the creation of this basemap and its related material.

Contributors: Names or nicknames of individuals involved in the creation of this basemap, depending on your interest; you could even include your personal website, which is highly encouraged.
:::info
Some functions may not yet be fully implemented; we are working to improve them to provide a better user experience. ✨
:::

