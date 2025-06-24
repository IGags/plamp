namespace plamp.Assembly.Building.Interfaces;

public interface IOptionalAliasBuilder<T> : IMemberBuilder<T>
{
    public IMemberBuilder<T> As(string alias);
}