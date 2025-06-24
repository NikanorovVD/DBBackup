using DBBackup.AutoBackup;
using DBBackup.Configuration;
using DBBackup.Postgres;
using DBBackup.Validation;
using Microsoft.Extensions.Configuration;
using Serilog;
using System.CommandLine;


namespace DBBackup
{
    internal class Program
    {
        static async Task<int> Main(string[] args)
        {
            Option<FileInfo> configOption = new("--config", "-c")
            {
                Description = "Файл с конфигурацией приложения в формате json",
                DefaultValueFactory = parseResult => new FileInfo("appsettings.json")
            };

            Option<string> databaseOption = new("--database", "-db")
            {
                Description = "Имя базы данных",
                Required = true,
            };

            Option<string> outPathOption = new("--out", "-o")
            {
                Description = "Путь для сохранения бекапа",
                Required = true
            };

            Option<FileInfo> inPathOption = new("--backup", "-b")
            {
                Description = "Путь к бекапу для восстановления",
                Required = true
            };

            Command autoCommand = new("auto", "Запускает процесс автоматического резервного копирования")
            {
                configOption,
            };
            autoCommand.SetAction(async parseResult => await AutoBackup(parseResult.GetValue(configOption)));

            Command backupCommand = new("backup", "Создает резервную копию базы данных")
            {
                configOption,
                databaseOption,
                outPathOption
            };
            backupCommand.SetAction(async parseResult => await Backup(
                parseResult.GetValue(configOption),
                parseResult.GetValue(databaseOption),
                parseResult.GetValue(outPathOption)
                ));

            Command restoreCommand = new("restore", "Восстанавливает базу данных из резервной копии")
            {
                configOption,
                inPathOption,
                databaseOption
            };
            restoreCommand.SetAction(async parseResult => await Restore(
                parseResult.GetValue(configOption),
                parseResult.GetValue(inPathOption),
                parseResult.GetValue(databaseOption)
                ));

            RootCommand rootCommand = new("Приложение для автоматического резервного копирования баз данных");
            rootCommand.Subcommands.Add(autoCommand);
            rootCommand.Subcommands.Add(backupCommand);
            rootCommand.Subcommands.Add(restoreCommand);

            ParseResult parseResult = rootCommand.Parse(args);
            return parseResult.Invoke();
        }

        private static async Task AutoBackup(FileInfo congigFile)
        {
            IConfigurationRoot config = new ConfigurationBuilder()
                .AddJsonFile(congigFile.FullName)
                .Build();

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(config)
                .CreateLogger();

            Settings? settings = config.Get<Settings>() ?? throw new Exception("Invalid settings");
            var validator = new SettingsValidator();
            var validationResult = validator.Validate(settings);

            if (!validationResult.IsValid)
            {
                foreach (var failure in validationResult.Errors)
                {
                    Log.Fatal("Invalid settings: {Error}", failure.ErrorMessage);
                }
                throw new Exception("InvalidSettings");
            }

            Connection connection = new(settings.Connection);
            await AutoBackupSheduler<PostgresBackupService>.StartAutoBackup(settings);
        }

        private static async Task Backup(FileInfo congigFile, string databaseName, string path)
        {
            IConfigurationRoot config = new ConfigurationBuilder()
                .AddJsonFile(congigFile.FullName)
                .Build();

            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .MinimumLevel.Information()
                .CreateLogger();

            ConnectionSettings connectionSettings = config.GetSection("Connection").Get<ConnectionSettings>() ?? throw new Exception("Invalid settings");
            var validator = new ConnectionValidator();
            var validationResult = validator.Validate(connectionSettings);

            if (!validationResult.IsValid)
            {
                foreach (var failure in validationResult.Errors)
                {
                    Log.Fatal("Invalid settings: {Error}", failure.ErrorMessage);
                }
                throw new Exception("InvalidSettings");
            }

            Connection connection = new(connectionSettings);
            Database database = new Database(connection, databaseName);

            IBackupService backupService = new PostgresBackupService();
            await backupService.BackupDatabaseAsync(database, path);
        }

        private static async Task Restore(FileInfo congigFile, FileInfo backupFile, string databaseName)
        {
            IConfigurationRoot config = new ConfigurationBuilder()
                .AddJsonFile(congigFile.FullName)
                .Build();

            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .MinimumLevel.Information()
                .CreateLogger();

            ConnectionSettings connectionSettings = config.GetSection("Connection").Get<ConnectionSettings>() ?? throw new Exception("Invalid settings");
            var validator = new ConnectionValidator();
            var validationResult = validator.Validate(connectionSettings);

            if (!validationResult.IsValid)
            {
                foreach (var failure in validationResult.Errors)
                {
                    Log.Fatal("Invalid settings: {Error}", failure.ErrorMessage);
                }
                throw new Exception("InvalidSettings");
            }

            Connection connection = new(connectionSettings);
            Database database = new Database(connection, databaseName);

            IBackupService backupService = new PostgresBackupService();
            await backupService.RestoreDatabaseAsync(backupFile.FullName, database);
        }
    }
}
