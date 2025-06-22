using Quartz;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace DBBackup.AutoBackup
{
    public static class AutoBackupSheduler<BackupServiceT> where BackupServiceT : IBackupService, new()
    {
        public static async Task StartAutoBackup(Database database)
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
                .StartNow()
                .WithSimpleSchedule(x => x
                    .WithIntervalInSeconds(40)
                    .RepeatForever())
                .Build();

            await scheduler.ScheduleJob(job, trigger);
            await builder.RunAsync();
        }
    }
}
