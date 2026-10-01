using CommunityToolkit.Mvvm.ComponentModel;
using GeoChemistryNexus.Models;
using System;

namespace GeoChemistryNexus.ViewModels
{
    public partial class CosPublishTargetItemViewModel : ObservableObject
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");

        [ObservableProperty]
        private string name = string.Empty;

        [ObservableProperty]
        private bool enabled = true;

        [ObservableProperty]
        private string publicBaseUrl = string.Empty;

        [ObservableProperty]
        private string secretId = string.Empty;

        [ObservableProperty]
        private string region = OfficialContentEndpoints.DefaultRegion;

        [ObservableProperty]
        private string bucket = string.Empty;

        public string ProtectedSecretKey { get; set; } = string.Empty;

        public int SortOrder { get; set; }

        public string ListLabel =>
            string.IsNullOrWhiteSpace(Name)
                ? (string.IsNullOrWhiteSpace(Bucket) ? Id : Bucket.Trim())
                : Name.Trim();

        partial void OnNameChanged(string value) => OnPropertyChanged(nameof(ListLabel));

        partial void OnBucketChanged(string value) => OnPropertyChanged(nameof(ListLabel));

        public static CosPublishTargetItemViewModel FromModel(CosPublishTarget target)
        {
            return new CosPublishTargetItemViewModel
            {
                Id = string.IsNullOrWhiteSpace(target.Id) ? Guid.NewGuid().ToString("N") : target.Id,
                Name = target.Name ?? string.Empty,
                Enabled = target.Enabled,
                PublicBaseUrl = target.PublicBaseUrl ?? string.Empty,
                SecretId = target.SecretId ?? string.Empty,
                Region = string.IsNullOrWhiteSpace(target.Region)
                    ? OfficialContentEndpoints.DefaultRegion
                    : target.Region,
                Bucket = target.Bucket ?? string.Empty,
                ProtectedSecretKey = target.ProtectedSecretKey ?? string.Empty,
                SortOrder = target.SortOrder
            };
        }

        public CosPublishTarget ToModel()
        {
            return new CosPublishTarget
            {
                Id = Id,
                Name = Name?.Trim() ?? string.Empty,
                Enabled = Enabled,
                PublicBaseUrl = OfficialContentEndpoints.NormalizeBaseUrl(PublicBaseUrl),
                SecretId = SecretId?.Trim() ?? string.Empty,
                ProtectedSecretKey = ProtectedSecretKey ?? string.Empty,
                Region = string.IsNullOrWhiteSpace(Region)
                    ? OfficialContentEndpoints.DefaultRegion
                    : Region.Trim(),
                Bucket = Bucket?.Trim() ?? string.Empty,
                SortOrder = SortOrder
            };
        }
    }
}
