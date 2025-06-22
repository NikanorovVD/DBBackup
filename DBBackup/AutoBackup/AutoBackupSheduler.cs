using Quartz;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using DBBackup.Configuration;
using System.Text.Json;
using DBBackup.Email;

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

            if(emailSettings != null)
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

                bool dbAccessible = new BackupServiceT().CheckConnection(database);
                if (!dbAccessible) Log.Error("Can not access Database {Database}", database.DatabaseName);

                IJobDetail job = JobBuilder.Create<BackupJob<BackupServiceT>>()
                    .WithIdentity(autoBackup.Database)
                    .UsingJobData("Database", JsonSerializer.Serialize(database))
                    .UsingJobData("AutoBackupEmailSettings", JsonSerializer.Serialize(autoBackup.Email))
                    .UsingJobData("EmailSettings", JsonSerializer.Serialize(emailSettings))
                    .UsingJobData("Path", autoBackup.Path)
                    .Build();


                ITrigger trigger = TriggerBuilder.Create()
                    .WithIdentity(autoBackup.Database)
                    .StartAt(autoBackup.Start)
                    .WithSimpleSchedule(s =>
                       s.WithInterval(autoBackup.Period)
                       .RepeatForever())
                    .Build();

                await scheduler.ScheduleJob(job, trigger);               
            }

            Task host = builder.RunAsync();          
            await host;
        }
    }
}
