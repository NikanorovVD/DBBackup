using DBBackup.Configuration;
using FluentValidation;

namespace DBBackup.Validation
{
    public class TriggerSettingsValidator: AbstractValidator<TriggerSettings>
    {
        public TriggerSettingsValidator()
        {
            RuleFor(s => s.Start).NotNull();
            RuleFor(s => s.Period).NotNull().NotEqual(new TimeSpan(0));
        }
    }
}
