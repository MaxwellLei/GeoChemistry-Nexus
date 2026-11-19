using System;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using GeoChemistryNexus.Services.Epma;

namespace GeoChemistryNexus;

public partial class App
{
    // Development acceptance uses the same byte reader and Jint checks as import.
    // No catalog initialization, installation, user configuration, or main window is needed.
    private async Task ValidateEpmaPackagesAsync(string[] paths)
    {
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        try
        {
            if (paths.Length == 0) throw new ArgumentException("Usage: GeoChemistryNexus.exe --validate-epma <package.epma.zip> [...]");
            foreach (string path in paths)
            {
                var package = await Task.Run(async () =>
                {
                    var candidate = EpmaScriptPackage.Read(path);
                    await new EpmaScriptRuntime().ValidateAsync(candidate);
                    return candidate;
                });
                Console.WriteLine(JsonSerializer.Serialize(new { packageId = package.Manifest.PackageId,
                    version = package.Manifest.Version, sha256 = package.Sha256 }));
            }
            Shutdown(0);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.ToString());
            Shutdown(1);
        }
    }
}
