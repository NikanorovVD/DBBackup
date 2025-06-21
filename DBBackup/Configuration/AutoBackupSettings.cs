namespace DBBackup.Configuration
{
    public class AutoBackupSettings
    {
        public string Database { get; set; }
        public string Type { get; set; }
        public string Period { get; set; }
        public string Time { get; set; }
        public AutoBackupEmailSettings Email { get; set; }
    }
}
