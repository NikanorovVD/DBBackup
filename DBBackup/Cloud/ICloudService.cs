namespace DBBackup.Cloud
{
    public interface ICloudService
    {
        public Task SendFile(string localPath, string cloudPath);
    }
}
