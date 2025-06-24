using Newtonsoft.Json.Linq;
using Serilog;
using YandexDisk.Client;
using YandexDisk.Client.Clients;
using YandexDisk.Client.Http;

namespace DBBackup.Cloud
{
    public class YandexDiskService : ICloudService
    {
        private readonly string _oauthToken;
        private readonly IDiskApi _diskApi;

        private const string _yandexIdInfoUrlTemplate = "https://login.yandex.ru/info?format=json&oauth_token={0}";
        public YandexDiskService(string oauthToken)
        {
            _oauthToken = oauthToken;
            _diskApi = new DiskHttpApi(_oauthToken);
        }

        public async Task<bool> CheckConnectionAsync()
        {
            string yandexIdInfoUrl = string.Format(_yandexIdInfoUrlTemplate, _oauthToken);
            try
            {
                HttpResponseMessage response = await new HttpClient().GetAsync(yandexIdInfoUrl);
                if (response.IsSuccessStatusCode) return true;
                else
                {
                    Log.Error("Error while authentication in Yandex ID: {Error}", await response.Content.ReadAsStringAsync());
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error connecting Yandex ID: {Error}", ex.ToString());
                return false;
            }
        }

        public async Task SendFileAsync(string localPath, string cloudPath)
        {
            await _diskApi.Files.UploadFileAsync(
                path: cloudPath,
                overwrite: false,
                localFile: localPath,
                cancellationToken: CancellationToken.None);
        }
    }
}
