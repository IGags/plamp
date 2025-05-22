using plamp.Abstractions.Compilation;
using plamp.Abstractions.Parsing;

namespace plamp.Native.Parsing;

public class PlampNativeParserFactory : IParserFactory
{
    public IParser CreateParser() => new PlampNativeParser();
    
    public ResourceType Type => ResourceType.Parallel;
}