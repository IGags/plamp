using System;
using System.Collections.Generic;
using plamp.Abstractions.Assemblies;
using plamp.Abstractions.CompilerEmission;
using plamp.Abstractions.Parsing;
using plamp.Abstractions.Validation;

namespace plamp.Compiler.Building;

public class CompilerBuilder
{
    private IParserFactory _parserFactory;

    private IStaticAssemblyContainerBuilder _containerBuilder;

    private ICompiledEmitterFactory _compiledEmitterFactory;
    
    private List<IValidatorFactory> _validatorFactories = new();
    
    public CompilerBuilder WithParserFactory<TParserFactory>() where TParserFactory : IParserFactory, new()
    {
        _parserFactory = new TParserFactory();
        return this;
    }

    public CompilerBuilder WithStaticAssemblyContainerFactory<TContainerFactory, TBuilder>(
        Action<IStaticAssemblyContainerBuilder> builderAction)
        where TContainerFactory : IStaticAssemblyContainerFactory<TBuilder>, new()
        where TBuilder : IStaticAssemblyContainerBuilder
    {
        var factory = new TContainerFactory();
        _containerBuilder = factory.CreateBuilder();
        builderAction(_containerBuilder);
        return this;
    }

    public CompilerBuilder WithCompiledEmitterFactory<TEmitterFactory>() 
        where TEmitterFactory : ICompiledEmitterFactory, new()
    {
        _compiledEmitterFactory = new TEmitterFactory();
        return this;
    }

    public CompilerBuilder WithValidatorFactory<TValidatorFactory>() 
        where TValidatorFactory : IValidatorFactory, new()
    {
        _validatorFactories.Add(new TValidatorFactory());
        return this;
    }

    public CompilerBuilder WithWeaverFactory<TWeaverFactory>()
    {
        throw new NotImplementedException();
    }
}