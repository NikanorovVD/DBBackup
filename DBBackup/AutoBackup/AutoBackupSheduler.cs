using Quartz;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using DBBackup.Configuration;
using System.Text.Json;

namespace DBBackup.AutoBackup
{
    public static class AutoBackupSheduler<BackupServiceT> where BackupServiceT : IBackupService, new()
    {
        public static async Task StartAutoBackup(Database database, DateTime start, TimeSpan interval, AutoBackupEmailSettings autoBackupEmailSettings, EmailSettings emailSettings)
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


            IJobDetail job = JobBuilder.Create<BackupJob<BackupServiceT>>()
                .WithIdentity("BackupJob")              
                .UsingJobData("Database", JsonSerializer.Serialize(database))
                .UsingJobData("AutoBackupEmailSettings", JsonSerializer.Serialize(autoBackupEmailSettings))
                .UsingJobData("EmailSettings", JsonSerializer.Serialize(emailSettings))
                .Build();


            ITrigger trigger = TriggerBuilder.Create()
                .WithIdentity("BackupTrigger")
                .StartAt(start)
                .WithSimpleSchedule(s =>
                   s.WithInterval(interval)
                   .RepeatForever())
                .Build();

            await scheduler.ScheduleJob(job, trigger);          
            Task host = builder.RunAsync();

            bool dbAccessible = new BackupServiceT().CheckConnection(database);
            if (!dbAccessible) Log.Error("Can not access Database {Database}", database.DatabaseName);

            await host;
        }
    }
}
