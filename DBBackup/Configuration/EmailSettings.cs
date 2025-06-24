namespace DBBackup.Configuration
{
    public class EmailSettings
    {
        public string SmtpServer {  get; set; }
        public int? Port {  get; set; }
        public bool? UseSSL {  get; set; }
        public string Login { get; set; }
        public string Password { get; set; }
        public string SenderName { get; set; }
    }
}
