using System;

namespace GeoChemistryNexus.Helpers
{
    public static class HomeIconHelper
    {
        public const string DefaultIcon = "\uE774";

        public static bool IsUrlIcon(string icon)
        {
            if (string.IsNullOrWhiteSpace(icon))
                return false;

            return icon.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || icon.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
        }

        public static string ResolveIcon(string icon)
        {
            if (string.IsNullOrWhiteSpace(icon))
                return DefaultIcon;

            return icon.Trim();
        }
    }
}
