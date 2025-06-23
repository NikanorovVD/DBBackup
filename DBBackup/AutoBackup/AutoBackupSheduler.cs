using Quartz;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using DBBackup.Configuration;
using System.Text.Json;
using DBBackup.Email;
using DBBackup.Helpers;
using DBBackup.Cloud;

namespace DBBackup.AutoBackup
{
    public static class AutoBackupSheduler<BackupServiceT> where BackupServiceT : IBackupService, new()
    {
        public static async Task StartAutoBackup(Connection connection, IEnumerable<AutoBackupSettings> autoBackups, EmailSettings emailSettings)
        {
            IHost builder = Host.CreateDefaultBuilder()
                 .UseSerilog()
                 .ConfigureServices((cxt, services) =>
                 {
                     services.AddQuartz();
                     services.AddQuartzHostedService(opt =>
                     {
                         opt.WaitForJobsToComplete = true;
                     });
                 }).Build();

            ISchedulerFactory schedulerFactory = builder.Services.GetRequiredService<ISchedulerFactory>();
            IScheduler scheduler = await schedulerFactory.GetScheduler();

            if (emailSettings != null)
            {
                bool emailAccessible = await new EmailService(emailSettings).CheckConnectionAsync();
                if (!emailAccessible) Log.Error("Can not access SMTP server");
            }

            foreach (AutoBackupSettings autoBackup in autoBackups)
            {
                Database database = new Database()
                {
                    Connection = connection,
                    DatabaseName = autoBackup.Database
                };

                bool pathValid = PathValidator.PathDirExists(autoBackup.Path);
                if (!pathValid) Log.Error("Invalid path: {Path}", autoBackup.Path);

                bool dbAccessible = new BackupServiceT().CheckConnection(database);
                if (!dbAccessible) Log.Error("Can not access Database {Database}", database.DatabaseName);

                if (autoBackup.Cloud != null)
                {
                    ICloudService cloudService = CloudServiceFactory.GetCloudService(autoBackup.Cloud);
                    bool cloudAccessible = await cloudService.CheckConnectionAsync();
                    if (!cloudAccessible) Log.Error("Can not access cloud type {CloudType}", autoBackup.Cloud.Type);
                }

                IJobDetail job = JobBuilder.Create<BackupJob<BackupServiceT>>()
                    .WithIdentity(autoBackup.Database)
                    .UsingJobData("Database", JsonSerializer.Serialize(database))
                    .UsingJobData("Path", autoBackup.Path)
                    .UsingJobData("EmailSettings", JsonSerializer.Serialize(emailSettings))
                    .UsingJobData("AutoBackupEmailSettings", JsonSerializer.Serialize(autoBackup.Email))
                    .UsingJobData("CloudSettings", JsonSerializer.Serialize(autoBackup.Cloud))
                    .Build();


                IEnumerable<ITrigger> triggers = autoBackup.Triggers.Select(triggerSettings =>
                    TriggerBuilder.Create()
                       .WithIdentity($"{autoBackup.Database}_Trigger_{triggerSettings.Start}_{triggerSettings.Period}")
                       .StartAt(triggerSettings.Start)
                       .WithSimpleSchedule(s =>
                          s.WithInterval(triggerSettings.Period)
                          .RepeatForever())
                       .Build()
                    );

                await scheduler.ScheduleJob(
                    jobDetail: job,
                    triggersForJob: triggers.ToList(),
                    replace: false);
            }

            Task host = builder.RunAsync();
            await host;
        }
    }
}
