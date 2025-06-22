using DBBackup.Email;

namespace DBBackup.Configuration
{
    public class AutoBackupEmailSettings
    {
        public EmailNotificationLevel Level { get; set; }
        public string Address { get; set; }
    }
}
