using System;
using System.Collections.Generic;
using System.Linq;
using plamp.Abstractions.Assemblies;
using plamp.Abstractions.Compilation;
using plamp.Abstractions.CompilerEmission;
using plamp.Abstractions.FileLoading;
using plamp.Abstractions.Parsing;
using plamp.Abstractions.Validation;

namespace plamp.Compiler.Building;

public class CompilerBuilder
{
    private Func<IParserFactory> _parserFactoryCreator;

    private ICompiledAssemblyContainer _assemblyContainer;

    private Func<ICompiledEmitterFactory> _compiledEmitterFactoryCreator;
    
    private readonly List<Func<IValidatorFactory>> _validatorFactories = [];

    public CompilerBuilder WithParserFactory(Func<IParserFactory> factoryCreator)
    {
        _parserFactoryCreator = factoryCreator;
        return this;
    }

    public CompilerBuilder WithStaticAssemblyContainerFactory<TContainerFactory, TBuilder>(
        Func<TContainerFactory> factoryCreator,
        Action<TBuilder> builderAction)
        where TContainerFactory : IStaticAssemblyContainerFactory<TBuilder>
        where TBuilder : IStaticAssemblyContainerBuilder
    {
        var factory = factoryCreator();
        var builder = factory.CreateBuilder();
        builderAction(builder);
        _assemblyContainer = builder.Build();
        return this;
    }

    public CompilerBuilder WithCompiledEmitterFactory(Func<ICompiledEmitterFactory> factoryCreator)
    {
        _compiledEmitterFactoryCreator = factoryCreator;
        return this;
    }

    public CompilerBuilder WithValidatorFactory(Func<IValidatorFactory> factoryCreator)
    {
        _validatorFactories.Add(factoryCreator);
        return this;
    }

    public CompilerBuilder WithWeaverFactory(Func<object> factoryCreator)
    {
        throw new NotImplementedException();
    }

    public BaseCompiler<TLoaderFactory, TLoader> BuildWithLoaderFactory<TLoader, TLoaderFactory>(
            Func<TLoaderFactory> factoryCreator)
        where TLoaderFactory : IFileLoaderFactory<TLoader>
        where TLoader : FileLoaderBase
    {
        var loaderFactory = factoryCreator();
        var validatorFactories = _validatorFactories.Select(x => x()).ToList();
        var compiler = new PlampCompiler<TLoaderFactory, TLoader>(
            loaderFactory,
            validatorFactories,
            _compiledEmitterFactoryCreator(),
            _parserFactoryCreator(),
            _assemblyContainer);
        return compiler;
    } 
}