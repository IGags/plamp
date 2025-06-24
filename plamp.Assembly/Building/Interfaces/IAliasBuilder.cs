namespace plamp.Assembly.Building.Interfaces;

public interface IAliasBuilder<T>
{
    public IMemberBuilder<T> As(string alias);
}