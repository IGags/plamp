using System.Reflection;
using System.Reflection.Emit;
using plamp.Abstractions.Ast.Node.ComplexTypes;
using plamp.Abstractions.Ast.Node.Definitions.Type.Definition;
using plamp.Alternative.SymbolsBuildingImpl;
using plamp.ILCodeEmitters;
using Shouldly;

namespace plamp.CodeEmission.Tests;

public class SymTableEmitterTests
{
    [Fact]
    public void CreateEmptyModuleFromTable_Correct()
    {
        var (asm, module) = BootstrapAssembly();
        var builder = new SymTableBuilder() { ModuleName = "test" };
        SymTableEmitter.EmitModule(builder, module);

        module.CreateGlobalFunctions();

        var assembly = (Assembly)asm;
        var mod = assembly.Modules.ShouldHaveSingleItem();
        mod.GetFields().ShouldBeEmpty();
        mod.GetMethods().ShouldBeEmpty();
        mod.GetTypes().ShouldBeEmpty();
    }

    [Fact]
    public void EmitType_InsertsEmptyBuilder()
    {
        var (_, module) = BootstrapAssembly();
        var builder = new SymTableBuilder() { ModuleName = "test" };
        var typeNode = new TypedefNode(new TypedefNameNode("Typee"), [], []);
        var typeInfo = builder.DefineType(typeNode);
        
        SymTableEmitter.EmitType(module, typeInfo);
        
        var typeBuilder = typeInfo.Type.ShouldNotBeNull();
        typeBuilder.Module.ShouldBe(module);
        var type = typeBuilder.CreateType();
        type.BaseType.ShouldBe(typeof(ValueType));
        type.Name.ShouldBe("Typee");
        type.GetFields().ShouldBeEmpty();
    }

    [Fact]
    public void EmitType_Generic_Correct()
    {
        var (_, module) = BootstrapAssembly();
        var builder = new SymTableBuilder() { ModuleName = "tteees" };
        var generics = new List<GenericDefinitionNode>
        {
            new(new GenericParameterNameNode("T1")),
            new(new GenericParameterNameNode("T2"))
        };

        var genericBuilders = generics.Select(x => builder.CreateGenericParameter(x));
        
        var typeNode = new TypedefNode(new TypedefNameNode("1A"), [], generics);
        var typeInfo = builder.DefineType(typeNode, genericBuilders.ToArray());
        
        SymTableEmitter.EmitType(module, typeInfo);

        var typeBuilder = typeInfo.Type.ShouldNotBeNull();
        typeBuilder.Module.ShouldBe(module);
        var type = typeBuilder.CreateType();
        type.BaseType.ShouldBe(typeof(ValueType));
        type.Name.ShouldBe("1A`2");
        var genericArgs = type.GetGenericArguments();
        genericArgs.Length.ShouldBe(2);
        genericArgs[0].Name.ShouldBe("T1");
        genericArgs[1].Name.ShouldBe("T2");
    }

    private (AssemblyBuilder, ModuleBuilder) BootstrapAssembly()
    {
        var asmName = Guid.NewGuid().ToString("N");
        var asm = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(asmName), AssemblyBuilderAccess.RunAndCollect);
        var module = asm.DefineDynamicModule(asmName);
        return (asm, module);
    }
}