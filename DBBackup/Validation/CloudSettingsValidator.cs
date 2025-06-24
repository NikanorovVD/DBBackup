using DBBackup.Configuration;
using FluentValidation;

namespace DBBackup.Validation
{
    public class CloudSettingsValidator: AbstractValidator<CloudSettings>
    {
        public CloudSettingsValidator()
        {
            RuleFor(s => s.Path).NotEmpty();
            RuleFor(s => s.Type).NotNull();
            RuleFor(s => s.OAuthToken).NotEmpty();
        }
    }
}
