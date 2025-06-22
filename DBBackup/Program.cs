using DBBackup.AutoBackup;
using DBBackup.Configuration;
using DBBackup.Postgres;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace DBBackup
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            IConfigurationRoot config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.Development.json")
                .Build();

            Settings? settings = config.Get<Settings>() ?? throw new Exception("Invalid settings");

            AutoBackupSettings backup = settings.AutoBackups.First();
            Connection connection = settings.Connection;
            Database database = new Database()
            {
                Connection = connection,
                DatabaseName = backup.Database
            };

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(config)
                .CreateLogger();

            //Console.WriteLine(new PostgresBackupService().CheckConnection(database));
            //await new PostgresBackupService().RestoreDatabaseAsync("backup1.sql", database);

            DateTime start = backup.Start;
            TimeSpan interval = backup.Period; 
            await AutoBackupSheduler<PostgresBackupService>.StartAutoBackup(database, start, interval);
        }
    }
}
