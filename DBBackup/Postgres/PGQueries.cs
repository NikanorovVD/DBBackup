namespace DBBackup.Postgres
{
    public static class PGQueries
    {
        public const string CheckDbExists = @"SELECT EXISTS(SELECT datname FROM pg_database WHERE datname=@dbname)";
        public const string CreatDb = "CREATE DATABASE \"{0}\" WITH OWNER=\"{1}\";";
    }
}
