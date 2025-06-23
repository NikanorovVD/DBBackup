using System.Diagnostics;
using System.Text;
using DBBackup.Helpers;
using Npgsql;
using Serilog;

namespace DBBackup.Postgres
{
    public class PostgresBackupService : IBackupService
    {
        public static string GetConnectionString(Database database)
        {
            return string.Format(
                ConnectionStringsTemplates.Postgres,
                database.Connection.User,
                database.Connection.Password,
                database.Connection.Host,
                database.Connection.Port,
                database.DatabaseName
                );
        }

        public bool CheckConnection(Database database)
        {
            string connectionString = GetConnectionString(database);
            using var connection = new NpgsqlConnection(connectionString);
            try
            {
                connection.Open();
                return true;
            }
            catch (Exception ex)
            {
                Log.Error("Error connecting database {Database}: {Exception}", database.DatabaseName, ex.ToString());
                return false;
            }
            finally
            {
                connection.Close();
            }
        }

        public Task BackupDatabaseAsync(Database database, string backupPath)
        {
            // настройка процесса        
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            string agrsTemplate = "--host {0} --port {1} --username {2} --file {3} {4}";
            ProcessStartInfo startInfo = new ProcessStartInfo("pg_dump")
            {
                Arguments = string.Format(agrsTemplate,
                    database.Connection.Host,
                    database.Connection.Port,
                    database.Connection.User,
                    backupPath,
                    database.DatabaseName),
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.GetEncoding(1251),
                StandardErrorEncoding = Encoding.GetEncoding(1251)
            };

            startInfo.EnvironmentVariables["PGPASSWORD"] = database.Connection.Password;

            // выполнение
            using Process process = new Process() { StartInfo = startInfo };

            Log.Information("Start backup for database {Database}", database.DatabaseName);
            DateTime start = DateTime.Now;

            process.Start();
            string stdout = process.StandardOutput.ReadToEnd();
            string stderrx = process.StandardError.ReadToEnd();
            process.WaitForExit();

            // лог
            TimeSpan execTime = DateTime.Now - start;
            bool fileExists = File.Exists(backupPath);
            long fileSize = fileExists ? new FileInfo(backupPath).Length : 0;

            string logTemplate =
                "Backup for database {Database} finish with exit code {ExitCode}." + Environment.NewLine +
                "Execution time: {Time}" + Environment.NewLine +
                (fileExists ? "Backup file path: {Path}" : "File {Path} not exist") + Environment.NewLine +
                (fileExists ? "Backup size: {Size}" : string.Empty);

            Log.Information(logTemplate, database.DatabaseName, process.ExitCode, execTime.ToString(@"hh\:mm\:ss"), backupPath, FileSizeString.Get(fileSize));

            if (process.ExitCode != 0)
                Log.Error("pg_dump error : {Error}", stderrx);

            Log.Debug("std out : {Out}", stdout);
            Log.Debug("std err : {Error}", stderrx);

            if (process.ExitCode != 0 || !fileExists)
            {
                throw new Exception($"Backup fail for database {database.DatabaseName}");
            }
            return Task.CompletedTask;
        }

        public async Task RestoreDatabaseAsync(string backupPath, Database database)
        {
            bool exists = await CheckIfDatabaseExistsAsync(database);

            if (!exists)
            {
                await CreateNewDatabaseAsync(database);
            }

            ApplySqlDump(backupPath, database);
        }

        public static async Task<bool> CheckIfDatabaseExistsAsync(Database database)
        {
            Database postgres = new Database()
            {
                Connection = database.Connection,
                DatabaseName = "postgres"
            };

            string postgresConnectionSrting = GetConnectionString(postgres);
            using var connection = new NpgsqlConnection(postgresConnectionSrting);
            await connection.OpenAsync();

            using var command = new NpgsqlCommand(PGQueries.CheckDbExists, connection);
            command.Parameters.AddWithValue("@dbname", database.DatabaseName);

            bool exists = await command.ExecuteScalarAsync() is true;

            string logMessage = exists ? "Database {Database} was found" : "Database {Database} was not found";
            Log.Information(logMessage, database.DatabaseName);

            return exists;
        }


        public static async Task CreateNewDatabaseAsync(Database database)
        {
            Database postgres = new Database()
            {
                Connection = database.Connection,
                DatabaseName = "postgres"
            };

            string postgresConnectionSrting = GetConnectionString(postgres);
            using var connection = new NpgsqlConnection(postgresConnectionSrting);
            await connection.OpenAsync();

            string query = string.Format(PGQueries.CreatDb, database.DatabaseName, database.Connection.User);
            using var command = new NpgsqlCommand(query, connection);
            await command.ExecuteNonQueryAsync();

            Log.Information("Database {Database} was successfully created", database.DatabaseName);
        }


        public static void ApplySqlDump(string backupPath, Database database)
        {
            // настройка процесса
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            string argsTemplate = "--host {0} --port {1} --dbname {2} --username {3} --file {4}";
            ProcessStartInfo startInfo = new ProcessStartInfo("psql")
            {
                Arguments = string.Format(argsTemplate,
                    database.Connection.Host,
                    database.Connection.Port,
                    database.DatabaseName,
                    database.Connection.User,
                    backupPath),
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.GetEncoding(1251),
                StandardErrorEncoding = Encoding.GetEncoding(1251)
            };

            startInfo.EnvironmentVariables["PGPASSWORD"] = database.Connection.Password;

            // выполнение
            using Process process = new Process() { StartInfo = startInfo };

            Log.Information("Start restore");
            DateTime start = DateTime.Now;

            process.Start();
            string stdout = process.StandardOutput.ReadToEnd();
            string stderrx = process.StandardError.ReadToEnd();
            process.WaitForExit();

            // лог
            TimeSpan execTime = DateTime.Now - start;

            string logTemplate =
                "Restore finish with exit code {ExitCode}." + Environment.NewLine +
                "Execution time: {Time}";

            Log.Information(logTemplate, process.ExitCode, execTime.ToString(@"hh\:mm\:ss"));

            if (process.ExitCode != 0)
                Log.Error("psql error : {Error}", stderrx);

            Log.Debug("std out : {Out}", stdout);
            Log.Debug("std err : {Error}", stderrx);

            if (process.ExitCode != 0)
            {
                throw new Exception($"Restore fail for database {database.DatabaseName}");
            }
        }
    }
}
