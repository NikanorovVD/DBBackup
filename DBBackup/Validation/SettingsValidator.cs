using DBBackup.Configuration;
using FluentValidation;

namespace DBBackup.Validation
{
    public class SettingsValidator : AbstractValidator<Settings>
    {
        public SettingsValidator()
        {
            RuleFor(s => s.Connection).SetValidator(new ConnectionValidator());
            RuleFor(s => s.AutoBackups).NotEmpty()
                .ForEach(b => b.NotNull().SetValidator(new AutoBackupSettingsValidator()));
            RuleFor(s => s.OldFilesDeletion).SetValidator(new TriggerSettingsValidator());
            RuleFor(s => s.Email).SetValidator(new EmailSettingsValidator());
        }
    }
}
