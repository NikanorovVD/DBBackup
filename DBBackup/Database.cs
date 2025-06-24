namespace DBBackup
{
    public class Database
    {
        public Connection Connection { get; set; }
        public string DatabaseName { get; set; }
        public Database(Connection connection, string databaseName)
        {
            Connection = connection;
            DatabaseName = databaseName;
        }
    }
}
