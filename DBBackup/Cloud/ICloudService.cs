namespace DBBackup.Cloud
{
    public interface ICloudService
    {
        public Task<bool> CheckConnectionAsync();
        public Task SendFileAsync(string localPath, string cloudPath);
    }
}
