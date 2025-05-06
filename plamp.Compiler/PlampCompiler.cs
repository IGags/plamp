using System.Collections.Generic;
using System.Linq;
using plamp.Abstractions.Assemblies;
using plamp.Abstractions.Compilation;
using plamp.Abstractions.CompilerEmission;
using plamp.Abstractions.FileLoading;
using plamp.Abstractions.Parsing;
using plamp.Abstractions.Validation;
using plamp.Compiler.Util;

namespace plamp.Compiler;

internal class PlampCompiler<TLoaderFactory, TLoader> 
    : BaseCompiler<TLoaderFactory, TLoader>
    where TLoaderFactory : IFileLoaderFactory<TLoader>
    where TLoader : FileLoaderBase
{
    private readonly IReadOnlyList<ResourceScheduler<IValidator>> _validators;

    private readonly ResourceScheduler<IParser> _parserScheduler;
    
    private readonly ResourceScheduler<ICompiledEmitter> _emitterScheduler;
    
    private readonly ICompiledAssemblyContainer _compiledAssemblyContainer;
    
    public PlampCompiler(
        TLoaderFactory loaderFactory,
        List<IValidatorFactory> validators,
        ICompiledEmitterFactory compiledEmitterFactory,
        IParserFactory parserFactory,
        ICompiledAssemblyContainer compiledAssemblyContainer) : base(loaderFactory)
    {
        _validators = validators
            .Select(x => ResourceScheduler<IValidator>.CreateScheduler(x.CreateValidator, x))
            .ToList();
        _parserScheduler = ResourceScheduler<IParser>.CreateScheduler(parserFactory.CreateParser, parserFactory);
        _emitterScheduler =
            ResourceScheduler<ICompiledEmitter>.CreateScheduler(compiledEmitterFactory.CreateCompiledEmitter,
                compiledEmitterFactory);
        _compiledAssemblyContainer = compiledAssemblyContainer;
    }

    public override TLoader CreateCompilation()
    {
        return LoaderFactory.CreateFileLoader(
            new PlampCompilation(
                _parserScheduler,
                _validators,
                _emitterScheduler,
                _compiledAssemblyContainer));
    }
}