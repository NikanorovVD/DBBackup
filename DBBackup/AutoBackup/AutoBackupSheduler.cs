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
        public static async Task StartAutoBackup(Settings settings)
        {
            Connection connection = settings.Connection;
            IEnumerable<AutoBackupSettings> autoBackups = settings.AutoBackups;
            EmailSettings emailSettings = settings.Email;
            TriggerSettings deleterSettings = settings.OldFilesDeletion;

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

            MetadataService.CreateMetaDir();
            if (emailSettings != null)
            {
                bool emailAccessible = await new EmailService(emailSettings).CheckConnectionAsync();
                if (!emailAccessible) Log.Error("Can not access SMTP server");
            }

            foreach (AutoBackupSettings autoBackup in autoBackups)
            {
                Database database = new Database(connection, autoBackup.Database);

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
                    .UsingJobData("Connection", JsonSerializer.Serialize(connection))
                    .UsingJobData("BackupSettings", JsonSerializer.Serialize(autoBackup))
                    .UsingJobData("EmailSettings", JsonSerializer.Serialize(emailSettings))
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

            if (deleterSettings != null)
            {
                IJobDetail deleteJob = JobBuilder.Create<DeleteOldFilesJob>()
                   .WithIdentity("Deleter")
                   .Build();


                ITrigger deleteTrigger =
                    TriggerBuilder.Create()
                       .WithIdentity($"Deleter_Trigger")
                       .StartAt(deleterSettings.Start)
                       .WithSimpleSchedule(s =>
                          s.WithInterval(deleterSettings.Period)
                          .RepeatForever())
                       .Build();

                await scheduler.ScheduleJob(deleteJob, deleteTrigger);
            }

            Task host = builder.RunAsync();
            await host;
        }
    }
}
