using DBBackup.Cloud;

namespace DBBackup.Configuration
{
    public class CloudSettings
    {
        public CloudType Type {  get; set; }
        public string OAuthToken {  get; set; }
        public string Path {  get; set; }
    }
}
