using DBBackup.Configuration;

namespace DBBackup
{
    public class Connection
    {
        public string User {  get; set; }
        public string Password { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }
        public Connection() { }
        public Connection(ConnectionSettings settings)
        {
            User = settings.User;
            Password = settings.Password;
            Host = settings.Host;
            Port = settings.Port.Value;
        }
    }
}
