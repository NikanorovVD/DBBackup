using DBBackup.Configuration;

namespace DBBackup.Cloud
{
    public static class CloudServiceFactory
    {
        public static ICloudService GetCloudService(CloudSettings cloudSettings)
        {
            return cloudSettings.Type switch
            {
                CloudType.Yandex => new YandexDiskService(cloudSettings.OAuthToken)
            };
        }
    }
}
