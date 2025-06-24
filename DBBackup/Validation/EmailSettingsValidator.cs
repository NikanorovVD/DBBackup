using DBBackup.Configuration;
using FluentValidation;

namespace DBBackup.Validation
{
    public class EmailSettingsValidator : AbstractValidator<EmailSettings>
    {
        public EmailSettingsValidator()
        {
            RuleFor(s => s.SmtpServer).NotEmpty();
            RuleFor(s => s.Port).NotNull().GreaterThanOrEqualTo(0);
            RuleFor(s => s.UseSSL).NotNull();
            RuleFor(s => s.Login).NotEmpty().EmailAddress();
            RuleFor(s => s.Password).NotNull();
            RuleFor(s => s.SenderName).NotNull();
        }
    }
}
