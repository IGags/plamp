namespace plamp.Abstractions.Parsing;

public interface IParserFactory : ICompilerEntity
{
    public IParser CreateParser();
}