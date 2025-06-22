using DBBackup.Configuration;
using DBBackup.Email;
using DBBackup.Helpers;
using Quartz;
using Serilog;
using System.Text.Json;

namespace DBBackup.AutoBackup
{
    public class BackupJob<BackupServiceT> : IJob where BackupServiceT : IBackupService, new()
    {
        private IBackupService _backupService;
        private EmailService _emailService;
        private AutoBackupEmailSettings _autoBackupEmailSettings;

        public async Task Execute(IJobExecutionContext context)
        {
            _backupService = new BackupServiceT();

            Database database = JsonSerializer.Deserialize<Database>(context.MergedJobDataMap.GetString("Database")!)!;
            _autoBackupEmailSettings = JsonSerializer.Deserialize<AutoBackupEmailSettings>(context.MergedJobDataMap.GetString("AutoBackupEmailSettings")!)!;

            EmailSettings emailSettings = JsonSerializer.Deserialize<EmailSettings>(context.MergedJobDataMap.GetString("EmailSettings")!)!;
            _emailService = new EmailService(emailSettings);

            string pathTemplate = context.MergedJobDataMap.GetString("Path")!;
            string path = PathFormatter.ReplaceDateTimePlaceholders(pathTemplate, DateTime.Now);
            path = Path.ChangeExtension(path, "sql");

            try
            {
                await _backupService.BackupDatabaseAsync(database, path);

                if (_autoBackupEmailSettings.Level == EmailNotificationLevel.All)
                {
                    await _emailService.SendEmailAboutSuccess(_autoBackupEmailSettings.Address, database.DatabaseName, DateTime.Now);
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error while autobackup for database {Database}: {Error}", database.DatabaseName, ex.ToString());

                if (_autoBackupEmailSettings.Level >= EmailNotificationLevel.ErrorsOnly)
                {
                    await _emailService.SendEmailAboutFail(_autoBackupEmailSettings.Address, database.DatabaseName, DateTime.Now, ex);
                }
            }
        }
    }
}
