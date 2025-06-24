namespace plamp.Assembly.Building.Interfaces;

public interface IAfterTypeInfoNameBuilder<T>
{
    public IMemberBuilder<T> As(string alias);

    public IMemberBuilder<T> WithMembers();

    public IModuleBuilderSyntax CompleteType();
}