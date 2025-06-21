using DBBackup.AutoBackup;
using DBBackup.Configuration;
using DBBackup.Postgres;
using Microsoft.Extensions.Configuration;

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
            Database database = new Database()
            {
                Connection = connection,
                DatabaseName = settings.AutoBackups.First().Database
            };


            await AutoBackupSheduler<PostgresBackupService>.StartAutoBackup(database);
        }
    }
}
