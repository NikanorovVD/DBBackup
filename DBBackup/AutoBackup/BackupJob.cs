using DBBackup.Cloud;
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
        private IBackupService _backupService = new BackupServiceT();
        private EmailService? _emailService;
        private ICloudService _cloudService;
        private AutoBackupEmailSettings? _autoBackupEmailSettings;

        public async Task Execute(IJobExecutionContext context)
        {
            Database database = JsonSerializer.Deserialize<Database>(context.MergedJobDataMap.GetString("Database")!)!;

            _autoBackupEmailSettings = JsonSerializer.Deserialize<AutoBackupEmailSettings>(context.MergedJobDataMap.GetString("AutoBackupEmailSettings"));

            EmailSettings? emailSettings = JsonSerializer.Deserialize<EmailSettings>(context.MergedJobDataMap.GetString("EmailSettings"));
            if (emailSettings != null) _emailService = new EmailService(emailSettings);

            CloudSettings? cloudSettings = JsonSerializer.Deserialize<CloudSettings>(context.MergedJobDataMap.GetString("CloudSettings"));
            if (cloudSettings != null)
            {
                _cloudService = cloudSettings.Type switch
                {
                    CloudType.Yandex => new YandexDiskService(cloudSettings.OAuthToken)
                };
            }


            string pathTemplate = context.MergedJobDataMap.GetString("Path")!;
            DateTime backupTime = DateTime.Now;
            string path = PathFormatter.ReplaceDateTimePlaceholders(pathTemplate, backupTime);
            path = Path.ChangeExtension(path, "sql");

            bool backupError = false;
            try
            {
                await _backupService.BackupDatabaseAsync(database, path);
            }
            catch (Exception ex)
            {
                backupError = true;
                Log.Error("Error while autobackup for database {Database}: {Error}", database.DatabaseName, ex.ToString());
            }

            if (EmailAvailable())
            {
                if (backupError && _autoBackupEmailSettings.Level >= EmailNotificationLevel.ErrorsOnly)
                {
                    try
                    {
                        await _emailService.SendEmailAboutFail(_autoBackupEmailSettings.Address, database.DatabaseName, DateTime.Now);
                    }
                    catch (Exception emailEx)
                    {
                        Log.Error("Error while sending email: {Error}", emailEx.ToString());
                    }
                }
                else if (!backupError && _autoBackupEmailSettings.Level == EmailNotificationLevel.All)
                {
                    try
                    {
                        await _emailService.SendEmailAboutSuccess(_autoBackupEmailSettings.Address, database.DatabaseName, DateTime.Now);
                    }
                    catch (Exception emailEx)
                    {
                        Log.Error("Error while sending email: {Error}", emailEx.ToString());
                    }
                }
            }

            if (CloudAvailable() && !backupError)
            {
                try
                {
                    string cloudPath = PathFormatter.ReplaceDateTimePlaceholders(cloudSettings.Path, backupTime);
                    cloudPath = Path.ChangeExtension(cloudPath, "sql");
                    await _cloudService.SendFile(path, cloudPath);
                }
                catch(Exception cloudEx)
                {
                    Log.Error("Error while sending file to cloud {Error}", cloudEx.ToString());
                }
            }
        }

        private bool EmailAvailable()
            => _emailService != null && _autoBackupEmailSettings != null;

        private bool CloudAvailable()
            => _cloudService != null;
    }
}
