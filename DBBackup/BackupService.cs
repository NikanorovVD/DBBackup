using DBBackup.Helpers;
using Serilog;

namespace DBBackup
{
    public abstract class BackupService: IBackupService
    {       
        public async Task BackupDatabaseAsync(Database database, string backupPath)
        {          
            Log.Information("Start backup for database {Database}", database.DatabaseName);
            DateTime start = DateTime.Now;

            (int exitCode, string stdout, string stderrx) = await RunBackupProcess(database, backupPath);

            TimeSpan execTime = DateTime.Now - start;
            bool fileExists = File.Exists(backupPath);
            long fileSize = fileExists ? new FileInfo(backupPath).Length : 0;
            string fullBackupPath = new FileInfo(backupPath).FullName;

            string logTemplate =
                "Backup for database {Database} finish with exit code {ExitCode}." + Environment.NewLine +
                "Execution time: {Time}" + Environment.NewLine +
                (fileExists ? "Backup file path: {Path}" : "File {Path} was not created") + Environment.NewLine +
                (fileExists ? "Backup size: {Size}" : string.Empty);

            Log.Information(logTemplate, database.DatabaseName, exitCode, execTime.ToString(@"hh\:mm\:ss"), fullBackupPath, FileSizeString.Get(fileSize));

            if (exitCode != 0)
                Log.Error("Backup process error : {Error}", stderrx);

            Log.Debug("std out : {Out}", stdout);
            Log.Debug("std err : {Error}", stderrx);

            if (exitCode != 0 || !fileExists)
            {
                throw new Exception($"Backup fail for database {database.DatabaseName}");
            }
        }

        public async Task RestoreDatabaseAsync(string backupPath, Database database)
        {
            bool exists = await CheckIfDatabaseExistsAsync(database);

            if (!exists)
            {
                await CreateNewDatabaseAsync(database);
            }

            await ApplySqlDumpAsync(backupPath, database);
        }

        public async Task ApplySqlDumpAsync(string backupPath, Database database)
        {
            Log.Information("Start restore");
            DateTime start = DateTime.Now;

            (int exitCode, string stdout, string stderrx) = await RunRestoreProcess(backupPath, database);

            TimeSpan execTime = DateTime.Now - start;

            string logTemplate =
                "Restore finish with exit code {ExitCode}." + Environment.NewLine +
                "Execution time: {Time}";

            Log.Information(logTemplate, exitCode, execTime.ToString(@"hh\:mm\:ss"));

            if (exitCode != 0)
                Log.Error("Restore process error : {Error}", stderrx);

            Log.Debug("std out : {Out}", stdout);
            Log.Debug("std err : {Error}", stderrx);

            if (exitCode != 0)
            {
                throw new Exception($"Restore fail for database {database.DatabaseName}");
            }
        }

        public abstract bool CheckConnection(Database database);
        public abstract Task<(int ExitCode, string StdOut, string StdErrrx)> RunBackupProcess(Database database, string backupPath);
        public abstract Task<(int ExitCode, string StdOut, string StdErrrx)> RunRestoreProcess(string backupPath, Database database);
        public abstract Task<bool> CheckIfDatabaseExistsAsync(Database database);
        public abstract Task CreateNewDatabaseAsync(Database database);
    }
}
