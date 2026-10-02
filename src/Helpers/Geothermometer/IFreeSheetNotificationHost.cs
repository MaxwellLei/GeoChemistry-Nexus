using System.Threading.Tasks;
using System.Windows;

namespace GeoChemistryNexus.Helpers
{
    public enum FreeSheetCsvExportMode
    {
        Values,
        Formulas
    }

    public interface IFreeSheetNotificationHost
    {
        Window OwnerWindow { get; }

        void ShowInfo(string message);

        void ShowSuccess(string message);

        void ShowWarning(string message);

        void ShowError(string message);

        Task<bool> ShowConfirmAsync(string message, string cancelText, string confirmText);

        Task<FreeSheetCsvExportMode?> ShowExportModeAsync();
    }
}
