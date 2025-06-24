using MimeKit;
using MailKit.Net.Smtp;
using Serilog;
using DBBackup.Configuration;

namespace DBBackup.Email
{
    public class EmailService
    {
        private readonly string _smtpServer;
        private readonly int _potr;
        private readonly bool _useSsl;
        private readonly string _login;
        private readonly string _password;
        private readonly string _senderName;

        public EmailService(string smtpServer, int potr, bool useSsl, string login, string password, string senderName)
        {
            _smtpServer = smtpServer;
            _potr = potr;
            _useSsl = useSsl;
            _login = login;
            _password = password;
            _senderName = senderName;
        }

        public EmailService(EmailSettings settings)
            : this(settings.SmtpServer, settings.Port.Value, settings.UseSSL.Value, settings.Login, settings.Password, settings.SenderName) { }

        public async Task<bool> CheckConnectionAsync()
        {
            using var client = new SmtpClient();
            try
            {
                await client.ConnectAsync(_smtpServer, _potr, _useSsl);
                await client.AuthenticateAsync(_login, _password);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error("Error connecting SMTP server: {Error}", ex.ToString());
                return false;
            }
            finally
            {
                await client.DisconnectAsync(true);
            }
        }

        public async Task SendEmailAsync(string email, string subject, string message)
        {
            using var emailMessage = new MimeMessage();

            emailMessage.From.Add(new MailboxAddress(_senderName, _login));
            emailMessage.To.Add(new MailboxAddress("", email));
            emailMessage.Subject = subject;
            emailMessage.Body = new TextPart(MimeKit.Text.TextFormat.Html)
            {
                Text = message
            };

            using var client = new SmtpClient();
            await client.ConnectAsync(_smtpServer, _potr, _useSsl);
            await client.AuthenticateAsync(_login, _password);
            await client.SendAsync(emailMessage);

            await client.DisconnectAsync(true);
        }

        public async Task SendEmailAboutBackupSuccess(string address, string database, DateTime dateTime)
        {
            await SendEmailAsync(
                address,
                "Successful backup",
                $"Successful backup for database {database} at {dateTime:G}");
        }

        public async Task SendEmailAboutBackupFail(string address, string database, DateTime dateTime)
        {
            await SendEmailAsync(
                address,
                "Backup fail",
                $"Backup fail for database {database} at {DateTime.Now:G}");
        }

        public async Task SendEmailAboutCloudError(string address, string database, DateTime dateTime)
        {
            await SendEmailAsync(
                address,
                "Backup save to cloud fail",
                $"Error while saving backup in cloud for database {database} at {dateTime:G}");
        }
    }
}
