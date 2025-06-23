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

            Connection connection = settings.Connection;

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(config)
                .CreateLogger();

           await AutoBackupSheduler<PostgresBackupService>.StartAutoBackup(connection, settings.AutoBackups, settings.Email);
        }
    }
}
