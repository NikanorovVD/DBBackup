using DBBackup.Configuration;
using FluentValidation;

namespace DBBackup.Validation
{
    public class ConnectionValidator: AbstractValidator<ConnectionSettings> 
    {
        public ConnectionValidator() 
        {
            RuleFor(c => c.Host).NotEmpty();
            RuleFor(c => c.Port).NotEmpty().GreaterThanOrEqualTo(0);
            RuleFor(c => c.User).NotEmpty();
            RuleFor(c => c.Password).NotNull();
        }
    }
}
