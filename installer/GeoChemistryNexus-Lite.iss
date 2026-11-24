; 精简版安装包入口：不内置 .NET 10 桌面运行时。
; 所有内容与完整版共用 GeoChemistryNexus.iss，仅通过 LITE 宏切换。
; 在 Inno Setup 中编译本文件即可得到 GeoChemistryNexus-Setup-<版本>-x64-lite.exe；
; 编译完整版请直接编译 GeoChemistryNexus.iss。
#define LITE
#include "GeoChemistryNexus.iss"
