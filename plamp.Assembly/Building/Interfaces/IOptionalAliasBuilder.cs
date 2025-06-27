namespace plamp.Assembly.Building.Interfaces;

public interface IOptionalAliasBuilder<T> : IMemberBuilder<T>
{
    public IMemberBuilder<T> As(string alias);
}

public interface IOptionalAliasBuilder : IMemberBuilder
{
    public IMemberBuilder As(string alias);
}