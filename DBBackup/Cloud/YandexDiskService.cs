using YandexDisk.Client;
using YandexDisk.Client.Clients;
using YandexDisk.Client.Http;

namespace DBBackup.Cloud
{
    public class YandexDiskService : ICloudService
    {
        private readonly string _oauthToken;
        private readonly IDiskApi _diskApi;
        public YandexDiskService(string oauthToken)
        {
            _oauthToken = oauthToken;
            _diskApi = new DiskHttpApi(_oauthToken);
        }

        public async Task SendFile(string localPath, string cloudPath)
        {
            await _diskApi.Files.UploadFileAsync(
                path: cloudPath,
                overwrite: false,
                localFile: localPath,
                cancellationToken: CancellationToken.None);
        }
    }
}
