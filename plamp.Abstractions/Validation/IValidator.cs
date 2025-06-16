using System.Threading;
using plamp.Abstractions.Validation.Models;

namespace plamp.Abstractions.Validation;

public interface IValidator
{
    public ValidationResult Validate(ValidationContext context, CancellationToken cancellationToken);
}