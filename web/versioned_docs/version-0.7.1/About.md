---
sidebar_position: 6
---

# ℹ️ About

:::info

**"I couldn't find the sports car of my dreams, so I built it myself." — Ferdinand Porsche**

:::

**GeoChemistry Nexus** is the first project I plan to maintain over the long term—I intend to keep updating it for at least the next five years.

Interestingly, it was originally named **Geo-Thermometer**. At first, I only wanted to develop a basic program related to geothermometers to make calculation and plotting easier. However, as the project progressed, especially after the plotting module was completed, I suddenly realized a much larger goal was within reach. So the project expanded in both concept and functionality, evolving from Geo-Thermometer to GeoChemistry Nexus.

## Why GeoChemistry Nexus Is Needed

In geochemistry and petrology research, **plotting** and **calculation** are often scattered across different tools: data organization relies on spreadsheet software, basemap drawing on general-purpose vector tools, and thermobarometer calculations require separate scripts or legacy programs. Fragmented workflows, inconsistent formats, and high reproducibility costs are friction many researchers face daily.

On the other hand, many commonly used discriminant diagrams, thermobarometers, and related calculation tools **lack ongoing updates for long periods**—newly published algorithms and revisions to classic models are hard to incorporate in time, and researchers often have to rely on versions that have stagnated for years. A more practical problem is that when researchers **develop their own** new algorithms or diagrams, they often **lack convenient, open channels to share them**—results remain in paper appendices, personal scripts, or small-scale sharing, making it difficult to spread and reuse them quickly within the field.

![plot_home_ui](/img/about-illustrations/02-research-tool-pain-points.png)

In addition, I have seen some less rigorous practices in research—for example, some people use CorelDRAW to roughly sketch geological map basemaps. Although the visual difference may be small, from a scientific research perspective, this approach goes against rigorous scientific spirit.

So I thought: **Why not develop a tool myself?**

A tool that can integrate a large number of geological and geochemical chart templates, support various geothermometer/barometer calculations, and extend to geochemical calculations and currently popular machine learning features. When ready-made templates are not available, it provides visual plotting tools to make creation and sharing easier. With modern interface design and multilingual support, it allows researchers to complete most of the foundational scientific plotting and calculation work within a single application.

**GeoChemistry Nexus** aims to bring this workflow together in one place—from sample data import, preprocessing and correction, to discriminant diagram plotting, geothermometer calculation and CIPW norm mineral calculation, to export in formats such as PNG / SVG, all within the same interface as much as possible; while through a **cloud template ecosystem** and **community collaboration mechanism**, diagrams and algorithms can iterate continuously and reach more researchers quickly.

## Initial Development Ideas

Coincidentally, I am familiar with programming languages such as C# and Python, and I have desktop software development skills. So in December 2024, I started developing a demo. In the initial technology selection phase, I seriously considered: **What language and architecture would best achieve this goal?**

My first thought was Python—it would let me prototype quickly, which is good news for developers. However, from the user's perspective, it has fatal flaws: first, low execution efficiency (though the impact is small for small datasets, there is still a performance gap); second, it leads to huge installation packages. I once refactored a simple program originally written in WPF using Python. Even when packaged in a brand-new virtual environment, the result was hundreds of MB, while a .NET packaged program was only a dozen MB—the gap is huge. **Forcing users to endure hundreds of MB or even GB of installation and complex configuration for simple functionality is not the best choice**.

Then I considered web development. The advantages are obvious—no installation required, ready to use. But the problems are: on one hand, my mastery of web technologies is not comprehensive enough; on the other hand, the long-term high cost of servers exceeds my personal maintenance capacity. Therefore, I eventually shifted my focus back to client-side development.

![plot_home_ui](/img/about-illustrations/03-technology-choice-path.png)

Client-side development has many technical options, but compared to C++-based Qt, I am more familiar with .NET. Combined with .NET's high execution efficiency and the popularity of Windows, I ultimately chose .NET as the main development platform. Initially, I also considered using Avalonia for cross-platform support on Windows, Linux, and macOS, but during development, I found that some project dependencies were incompatible with Avalonia, leading to very high migration costs. With my personal capacity alone, it is difficult to refactor these modules in the early stage. Therefore, at least at the current stage, my work remains focused on the Windows platform, and a cross-platform version may be realized someday in the future.

## Contact Me

If you are interested in software development and willing to collaborate with me, or hope to provide suggestions and feedback, diagram templates, algorithm support, or even business cooperation, please feel free to contact me at **[maxwelllei@qq.com](mailto:maxwelllei@qq.com)**.

*If you find any shortcomings or issues while using the software, please feel free to provide feedback. As software features grow increasingly complex, testing is difficult to cover all scenarios, and there may be omissions or inaccuracies. I will do my best to fix and optimize, providing you with a more stable and reliable user experience.*

![plot_home_ui](/img/about-illustrations/04-long-term-maintenance.png)
