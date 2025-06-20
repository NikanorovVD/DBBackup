namespace DBBackup
{
    public interface IBackupService
    {
        public bool CheckConnection(Database database);
        public Task BackupDatabaseAsync(Database database, string backupPath);
        public Task RestoreDatabaseAsync(string backupPath, Database database);

    }
}
