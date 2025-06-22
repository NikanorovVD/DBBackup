using Quartz;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace DBBackup.AutoBackup
{
    public static class AutoBackupSheduler<BackupServiceT> where BackupServiceT : IBackupService, new()
    {
        public static async Task StartAutoBackup(Database database, DateTime start, TimeSpan interval)
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
                .UsingJobData("User", database.Connection.User)
                .UsingJobData("Password", database.Connection.Password)
                .UsingJobData("Host", database.Connection.Host)
                .UsingJobData("Port", database.Connection.Port)
                .UsingJobData("Database", database.DatabaseName)
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
