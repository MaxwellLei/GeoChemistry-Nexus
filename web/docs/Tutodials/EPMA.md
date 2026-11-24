---
sidebar_position: 4
---

import useBaseUrl from '@docusaurus/useBaseUrl';

# EPMA mineral calculation

**EPMA mineral calculation** processes mineral compositions obtained by electron-probe microanalysis. Oxide contents from a batch of analytical spots are converted, using the selected mineral calculation method, into mineral-chemistry results for mineral identification, compositional comparison, and later geological interpretation.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/epma_mineral_calculation.webp')}
    alt="EPMA interface"
    style={{width: '100%'}}
  />
</div>

## Interface

EPMA mineral calculation is divided into two interfaces: the **EPMA mineral-calculation template library** and the **EPMA calculation interface**.

In the template library you can search, filter, and select a suitable mineral EPMA calculation. The description panel on the far right shows basic information about these calculation templates. Click **Enter calculation** to start calculating.

## Example

For example, select an olivine EPMA calculation template and enter the calculation interface. The page is divided into an upper part and a lower part. In the top bar you can open the detailed help document for that template or load example data.

Enter the data required by the table, then click **Run calculation** in the top bar.

The white-gray area is where you enter data. The blue-purple area is where results are shown. You can inspect the output freely, or click **Inspect** to view the intermediate calculation.

The colored dot in front of each row header shows that row's calculation status:

* **Gray**: the row contains data but has not been calculated
* **Green**: the row finished normally, with no warning and no error
* **Orange**: calculation finished, but there is a warning
* **Red**: calculation failed for that row
