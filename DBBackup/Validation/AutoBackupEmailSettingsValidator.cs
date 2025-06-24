using DBBackup.Configuration;
using FluentValidation;


namespace DBBackup.Validation
{
    public class AutoBackupEmailSettingsValidator : AbstractValidator<AutoBackupEmailSettings>
    {
        public AutoBackupEmailSettingsValidator()
        {
            RuleFor(s => s.Address).NotEmpty().EmailAddress();
            RuleFor(s => s.Level).NotNull();
        }
    }
}
