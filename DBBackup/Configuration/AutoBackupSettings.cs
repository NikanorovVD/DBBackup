namespace DBBackup.Configuration
{
    public class AutoBackupSettings
    {
        public string Database { get; set; }
        public string Type { get; set; }
        public TimeSpan Period { get; set; }
        public DateTime Start { get; set; }
        public AutoBackupEmailSettings Email { get; set; }
    }
}
