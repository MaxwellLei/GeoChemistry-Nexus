using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Models;
using System;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;

namespace GeoChemistryNexus.Services
{
    public static class ServerAnnouncementService
    {
        public static async Task<string> LoadAnnouncementAsync()
        {
            try
            {
                string json = await UpdateHelper.GetUrlContentAsync(OfficialContentEndpoints.ServerInfoUrl);
                if (string.IsNullOrWhiteSpace(json))
                    return string.Empty;

                var serverInfo = JsonSerializer.Deserialize<ServerInfo>(json);
                return serverInfo?.Announcement?.Trim() ?? string.Empty;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ServerAnnouncementService] Load failed: {ex.Message}");
                return string.Empty;
            }
        }
    }
}
