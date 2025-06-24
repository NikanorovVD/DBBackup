namespace DBBackup.Configuration
{
    public class Settings
    {
        public Connection Connection { get; set; }
        public IEnumerable<AutoBackupSettings> AutoBackups { get; set; }
        public TriggerSettings OldFilesDeletion {  get; set; }
        public EmailSettings Email { get; set; }
    }
}
