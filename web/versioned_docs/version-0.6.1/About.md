---
sidebar_position: 6
---

# ℹ️ About

:::info

**"I couldn't find the sports car of my dreams, so I built it myself." —— Ferdinand Porsche**

:::

**GeoChemistry Nexus** is my first project planned for ultra-long-term maintenance—I intend to keep updating it for at least the next five years.

Interestingly, it was originally named **Geo-Thermometer**. Initially, I simply wanted to develop a basic program related to geological thermometers to facilitate calculation and plotting. However, as the project progressed, especially after the completion of the plotting module, I suddenly realized that a much grander goal was achievable. Consequently, the project expanded in both concept and functionality, evolving from Geo-Thermometer into GeoChemistry Nexus.

In my view, geology and geochemistry are relatively niche disciplines. In this field, I found that many software tools are either in disrepair and have long stopped updating, or are still being updated but have an extremely high barrier to entry, hindering many researchers. Furthermore, I have seen some less-than-rigorous practices in my research—for example, some people use CorelDRAW to roughly sketch shapes when drawing geological diagram basemaps. Although visually there isn't much difference, from a scientific research perspective, this approach violates the rigorous spirit of science.

So I thought: **Why not develop a tool myself?**

A tool that can integrate a large number of geological and geochemical diagram templates, support various geothermometer/barometer calculations, and extend to geochemical calculations and currently popular machine learning functions. If ready-made templates are not available, it provides visual drawing tools to facilitate creation and sharing. Supplemented by modern interface design and multi-language support, it allows researchers to complete most of the basic work of scientific plotting and calculation within a single software.

A visual interface can significantly reduce the difficulty of operation, while multi-language support reduces learning costs; built-in rich templates and long-term updates will make this program a truly valuable and sustainable scientific research tool.

Coincidentally, I am familiar with programming languages such as C# and Python, and I also master development technologies like WPF. Therefore, in December 2024, I began to develop a Demo. In the initial technology selection phase, I seriously considered: what language and architecture would best achieve this goal?

My first thought was Python—it would allow me to quickly prototype, which is good news for developers. However, from the user's perspective, it has fatal flaws: first, low execution efficiency (although the impact is small with small data volumes, there is still a performance gap); second, it brings a massive installation package. I once refactored a simple program originally written in WPF using Python. Even when packaged in a fresh virtual environment, the finished product was as large as hundreds of MB, whereas the .NET packaged program was only a dozen MB—a huge difference. Forcing users to endure hundreds of MB or even GB of installation and complex configuration for simple functions is not the best choice.

Then I considered web development. The advantages are obvious—no installation required, open and use. But the problem lies in: on one hand, my mastery of web technology is not comprehensive enough; on the other hand, the long-term high cost of servers exceeds my personal maintenance capability. Therefore, I ultimately turned my focus back to client-side development.

There are many technical solutions for client-side development, but compared to QT based on C++, I am more familiar with .NET. Combining the high execution efficiency of .NET with the ubiquity of Windows, I finally chose .NET as the main development platform. Initially, I also considered using Avalonia to achieve cross-platform support for Windows, Linux, and MacOS, but during the development process, I found that some project dependencies were not compatible with Avalonia, resulting in very high migration costs. With my personal strength, it is difficult to refactor these modules in the early stages. Therefore, at least at the current stage, my work still focuses on the Windows platform, and cross-platform versions may be realized someday in the future.

## Contact Me

If you are interested in software development and willing to cooperate with me, or wish to provide suggestions and feedback, diagram templates, algorithm support, or even conduct commercial cooperation, please feel free to contact me via **[maxwelllei@qq.com](mailto:maxwelllei@qq.com)**.

*If you find any deficiencies or issues while using the software, please feel free to provide feedback. Due to the increasing functionality and complexity of the software, testing is difficult to cover all situations, and there may be omissions or inaccuracies. I will try my best to correct and optimize it to provide you with a more stable and reliable user experience.*
