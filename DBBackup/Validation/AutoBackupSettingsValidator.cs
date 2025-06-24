using DBBackup.Configuration;
using FluentValidation;

namespace DBBackup.Validation
{
    public class AutoBackupSettingsValidator: AbstractValidator<AutoBackupSettings>
    {
        public AutoBackupSettingsValidator()
        {
            RuleFor(s => s.Database).NotEmpty();
            RuleFor(s => s.Path).NotEmpty();
            RuleFor(s => s.Triggers).NotEmpty()
                .ForEach(t => t.NotNull().SetValidator(new TriggerSettingsValidator()));
            RuleFor(s => s.DeleteAfter).NotEqual(new TimeSpan(0));
            RuleFor(s => s.Email).SetValidator(new AutoBackupEmailSettingsValidator());
            RuleFor(s => s.Cloud).SetValidator(new CloudSettingsValidator());
        }
    }
}
