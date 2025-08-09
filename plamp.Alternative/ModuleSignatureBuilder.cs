using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node.Definitions;
using RootNode = plamp.Abstractions.Ast.Node.Definitions.RootNode;

namespace plamp.Alternative;

public static class ModuleSignatureBuilder
{
    public static ModuleBuildingResult BuildSignature(RootNode node, SignatureBuildingContext context)
    {
        if (string.IsNullOrWhiteSpace(node.ModuleName?.ModuleName))
        {
            throw new ArgumentException("Parser must return valid module name");
        }

        var assemblyName = new AssemblyName(node.ModuleName.ModuleName);
        var assembly = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndCollect);
        var module = assembly.DefineDynamicModule(node.ModuleName.ModuleName);
        var funcs = new List<MethodBuilder>();
        foreach (var func in node.Functions)
        {
            var signature = CreateMethodSignature(func, context, module);
            if(signature != null) funcs.Add(signature);
        }

        var methodSignature = new ModuleSignature()
            { Assembly = assembly, ModuleBuilder = module, MethodList = funcs.ToDictionary(x => x.Name, x => x) };
        return new ModuleBuildingResult(methodSignature, context.Exceptions);
    }

    private static MethodBuilder? CreateMethodSignature(FuncNode func, SignatureBuildingContext context, ModuleBuilder moduleBuilder)
    {
        var exCount = context.Exceptions.Count;

        Type? returnType;
        if (func.ReturnType == null)
        {
            returnType = typeof(void);
        }
        else if ((returnType = TypeResolveHelper.ResolveType(func.ReturnType, context.Exceptions, context.Symbols, context.FileName)) == null)
        {
            throw new ArgumentException("Parser must return type explicitly");
        }

        var argTypes = new List<Type>();
        foreach (var arg in func.ParameterList)
        {
            var type = TypeResolveHelper.ResolveType(arg.Type, context.Exceptions, context.Symbols, context.FileName);
            if(type == null) continue;
            argTypes.Add(type);
        }

        if (context.Exceptions.Count != exCount) return null;
        return moduleBuilder.DefineGlobalMethod(
            func.Name.MemberName,
            MethodAttributes.Static | MethodAttributes.Final | MethodAttributes.Public,
            CallingConventions.Standard, returnType, argTypes.ToArray());
    }
}

public record SignatureBuildingContext(List<PlampException> Exceptions, SymbolTable Symbols, string FileName);

public record ModuleBuildingResult(ModuleSignature? Signature, List<PlampException> Exceptions);

public record ModuleSignature
{
    public required AssemblyBuilder Assembly { get; init; }
    public required ModuleBuilder ModuleBuilder { get; init; }
    public required Dictionary<string, MethodBuilder> MethodList { get; init; }
}