namespace DBBackup.Configuration
{
    public class AutoBackupSettings
    {
        public string Database { get; set; }
        public string Path { get; set; }
        public string Type { get; set; }
        public IEnumerable<TriggerSettings> Triggers { get; set; }
        public AutoBackupEmailSettings Email { get; set; }
        public CloudSettings Cloud { get; set; }
    }
}
