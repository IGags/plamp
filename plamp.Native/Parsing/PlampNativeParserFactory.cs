using plamp.Abstractions.Parsing;

namespace plamp.Native.Parsing;

public class PlampNativeParserFactory : IParserFactory
{
    public IParser CreateParser() => new PlampNativeParser();
    
    public bool CanReuseCreated => true;
    
    public bool CanParallelCreated => false;
}