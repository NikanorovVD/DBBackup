using Quartz;

namespace DBBackup
{
    public class BackupJob<BackupServiceT> : IJob where BackupServiceT : IBackupService, new()
    {
        private IBackupService _backupService;

        public string User {  get; set; }
        public string Password { get; set; }
        public string Host {  get; set; }
        public int Port { get; set; }
        public string Database {  get; set; }

        public BackupJob()
        {
            _backupService = new BackupServiceT();
        }

        public async Task Execute(IJobExecutionContext context)
        {
            Database database = new()
            {
                Connection = new()
                {
                    User = User,
                    Password = Password,
                    Host = Host,
                    Port = Port
                },
                DatabaseName = Database
            };

            string path = $"dump_{DateTime.Now:yy-MM-dd-HH-mm-ss}.sql"; 
            await _backupService.BackupDatabaseAsync(database, path);
        }
    }
}
