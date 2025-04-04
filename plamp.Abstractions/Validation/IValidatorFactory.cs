namespace plamp.Abstractions.Validation;

public interface IValidatorFactory : ICompilerEntity
{
    public IValidator CreateValidator();
}