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
            Connection connection = JsonSerializer.Deserialize<Connection>(context.MergedJobDataMap.GetString("Connection")!)!;

            AutoBackupSettings backupSettings = JsonSerializer.Deserialize<AutoBackupSettings>(context.MergedJobDataMap.GetString("BackupSettings")!)!;
            Database database  = new Database(connection, backupSettings.Database);

            TimeSpan? deleteAfter = backupSettings.DeleteAfter;
            _autoBackupEmailSettings = backupSettings.Email;

            EmailSettings? emailSettings = JsonSerializer.Deserialize<EmailSettings>(context.MergedJobDataMap.GetString("EmailSettings"));
            if (emailSettings != null) _emailService = new EmailService(emailSettings);

            CloudSettings? cloudSettings = backupSettings.Cloud;
            if (cloudSettings != null)
            {
                _cloudService = CloudServiceFactory.GetCloudService(cloudSettings);
            }

            string pathTemplate = backupSettings.Path;
            DateTime backupTime = DateTime.Now;
            string path = PathFormatter.ReplaceDateTimePlaceholders(pathTemplate, backupTime);
            path = Path.ChangeExtension(path, "sql");

            bool backupError = false;
            bool cloudError = false;

            try
            {
                await _backupService.BackupDatabaseAsync(database, path);
            }
            catch (Exception ex)
            {
                backupError = true;
                Log.Error("Error while autobackup for database {Database}: {Error}", database.DatabaseName, ex.ToString());
            }

            if (!backupError && deleteAfter != null)
            {
                MetadataService.WriteMetadata(new FileMetadata(path, DateTime.Now, DateTime.Now.Add(deleteAfter.Value)));
            }

            if (CloudAvailable() && !backupError)
            {
                try
                {
                    string cloudPath = PathFormatter.ReplaceDateTimePlaceholders(cloudSettings.Path, backupTime);
                    cloudPath = Path.ChangeExtension(cloudPath, "sql");
                    await _cloudService.SendFileAsync(path, cloudPath);
                    Log.Information("Send backup for database {Database} to {CloudType} cloud at path {CloudPath}", database.DatabaseName, cloudSettings.Type, cloudPath);
                }
                catch (Exception cloudEx)
                {
                    cloudError = true;
                    Log.Error("Error while sending file to cloud {Error}", cloudEx.ToString());
                }
            }

            if (EmailAvailable())
            {
                if (backupError && _autoBackupEmailSettings.Level >= EmailNotificationLevel.ErrorsOnly)
                {
                    try
                    {
                        await _emailService.SendEmailAboutBackupFail(_autoBackupEmailSettings.Address, database.DatabaseName, DateTime.Now);
                        Log.Information("Send email about backup fail for database {Database}", database.DatabaseName);
                    }
                    catch (Exception emailEx)
                    {
                        Log.Error("Error while sending email: {Error}", emailEx.ToString());
                    }
                }
                else if (cloudError && _autoBackupEmailSettings.Level >= EmailNotificationLevel.ErrorsOnly)
                {
                    try
                    {
                        await _emailService.SendEmailAboutCloudError(_autoBackupEmailSettings.Address, database.DatabaseName, DateTime.Now);
                        Log.Information("Send email about cloud error for database {Database}", database.DatabaseName);
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
                        await _emailService.SendEmailAboutBackupSuccess(_autoBackupEmailSettings.Address, database.DatabaseName, DateTime.Now);
                        Log.Information("Send email about successful backup for database {Database}", database.DatabaseName);
                    }
                    catch (Exception emailEx)
                    {
                        Log.Error("Error while sending email: {Error}", emailEx.ToString());
                    }
                }
            }
        }

        private bool EmailAvailable()
            => _emailService != null && _autoBackupEmailSettings != null;

        private bool CloudAvailable()
            => _cloudService != null;

    }
}
