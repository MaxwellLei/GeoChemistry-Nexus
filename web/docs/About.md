---
sidebar_position: 6
---

# ℹ️ About

:::info

**“I could not find the sports car of my dreams, so I built one myself.” — Ferdinand Porsche**

:::

**GeoChemistry Nexus** is the first project I plan to maintain for a very long time. I intend to keep updating it for at least the next five years.

It was originally named **Geo-Thermometer**. At first I only wanted a basic program related to geothermometers, so that calculation and plotting would be easier. As the project moved forward, especially after the plotting module was finished, I realized a larger goal was possible. The project then grew in both concept and function, from Geo-Thermometer into GeoChemistry Nexus.

## Why this name

**GeoChemistry** means geochemistry. **Nexus** means a connection point, a hub, or a place where things meet.

We want to bring scattered geochemical calculation methods, data-processing tools, mineral-chemistry analysis, geological diagrams, and related work onto one platform.

## Why GeoChemistry Nexus is needed

In geochemistry and petrology, **plotting** and **calculation** are often split across different tools. Spreadsheet software prepares the data, a general vector tool draws the base map, and thermobarometry needs yet another script or an old program. Broken workflows, inconsistent formats, and a high cost of reproduction are everyday friction for many researchers.

Many commonly used discrimination diagrams, thermobarometers, and related calculation tools have also **gone without sustained updates for a long time**. Newly published algorithms and revisions of classic models are hard to bring in promptly, so researchers often have to keep using versions that have stood still for years. A more practical problem is this: after a researcher **builds** a new algorithm or diagram, there is often **no convenient, open channel to distribute it**. The result stays in a paper appendix, a personal script, or a small private share, and does not spread or get reused quickly in the field.

![plot_home_ui](/img/about-illustrations/02-research-tool-pain-points.png)

I have also seen practices in research that are not rigorous enough. For example, some people sketch geological base maps roughly in CorelDRAW. The visual difference may be small, but from the standpoint of scientific research the practice goes against a rigorous scientific spirit.

So I asked: **why not build a tool myself?**

A tool that can bring together a large set of geological and geochemical diagram templates, support a wide range of geothermometer and geobarometer calculations, and extend to geochemical calculations and machine-learning features that are in use now. When no ready-made template exists, it provides visual drawing tools so a template can be created and shared. With a modern interface and multiple languages, researchers can do most of the basic scientific plotting and calculation inside one program.

**GeoChemistry Nexus** aims to pull that chain into one place. From sample import, preprocessing, and correction, through discrimination diagrams, geothermometer calculation, and CIPW normative mineral calculation, to export in PNG, SVG, and other formats, as much of the work as possible is done in the same interface. Through a **cloud template ecosystem** and **community collaboration**, diagrams and algorithms can keep iterating and reach more researchers quickly.

## How development started

I happen to know programming languages such as C# and Python, and I know desktop software development. In December 2024 I started building a demo. In the first round of technology choices, I thought carefully about this question: **which language and architecture could best achieve this goal?**

My first idea was Python. It would let me prototype quickly, which is good news for a developer. From a user’s point of view it has serious drawbacks. Execution is inefficient. The difference is small when the data set is small, but a performance gap remains. It also produces a very large installer. I once rewrote, in Python, a simple program that had originally been written in WPF. Even when it was packaged in a brand-new virtual environment, the result was hundreds of megabytes, while the .NET package was only a little over ten megabytes. The gap is huge. **Making users accept an install of hundreds of megabytes, or even gigabytes, plus complex setup for a simple feature is not a good choice.**

I then considered web development. The advantages are obvious: nothing to install, and it opens and runs. The problem is that the long-term cost of a server was more than I could maintain on my own. I therefore moved the focus back to client development.

![plot_home_ui](/img/about-illustrations/03-technology-choice-path.png)

Client development has many technical options. Compared with Qt, which is based on C++, I know .NET better. Given the high execution efficiency of .NET and how widely Windows is used, I chose .NET as the main development platform. At first I also considered Avalonia, so the software could support Windows, Linux, and macOS. During development I found that some project dependencies were incompatible with Avalonia, and the cost of migration was very high. On my own, it was hard to rebuild those modules at an early stage. At least for now, the work remains focused on Windows. A cross-platform version may arrive someday.

## Contact me

If you are interested in software development and want to collaborate, or if you want to offer suggestions and feedback, diagram templates, or algorithm support, or even to discuss commercial cooperation, contact me at any time at **[maxwelllei@qq.com](mailto:maxwelllei@qq.com)**.

*If you find any shortcoming or problem while using the software, please send feedback at any time. The software has more and more features, and they are increasingly complex, so testing cannot cover every case. Something may be missed or inaccurate. I will do my best to correct and improve it, and to give you a more stable and reliable experience.*

![plot_home_ui](/img/about-illustrations/04-long-term-maintenance.png)
