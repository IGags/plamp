using System.Reflection;
using System.Reflection.Emit;
using plamp.Abstractions.Ast.Node;

namespace plamp.CodeEmission.Tests.Infrastructure;

public class EmissionSetupHelper
{
    public static (
        AssemblyBuilder asmBuilder,
        TypeBuilder typeBuilder,
        MethodBuilder methodBuilder, 
        string typeName) 
        CreateMethodBuilder(
            string methodName, 
            Type returnType, 
            Type[] argumentTypes)
    {
        var name = $"{Guid.NewGuid()}_{DateTime.Now.Ticks}";
        var assemblyName = new AssemblyName(name);
        var assembly = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndCollect);
        var module = assembly.DefineDynamicModule(name);
        var type = module.DefineType(name, TypeAttributes.Public | TypeAttributes.Class);
        var method = type.DefineMethod(methodName, MethodAttributes.Public | MethodAttributes.Final, returnType,
            argumentTypes);
        var dbgMeth = new DebugMethodBuilder(method);
        
        var ctor = type.DefineConstructor(
            MethodAttributes.Public | MethodAttributes.Final, 
            CallingConventions.Standard, 
            Type.EmptyTypes);
        
        var il = ctor.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Call, typeof(object).GetConstructor(Type.EmptyTypes)!);
        il.Emit(OpCodes.Ret);

        return (assembly, type, dbgMeth, name);
    }

    public static (object? instance, MethodInfo? methodInfo) CreateObject(Type builtType, string methodName)
    {
        var createdMethod = builtType.GetMethod(methodName);
        var instance = Activator.CreateInstance(builtType);
        return (instance, createdMethod);
    }

    public static TypeNode CreateTypeNode(Type type) => new ConcreteType(new MemberNode(type.Name), [], type);
    
    private class ConcreteType(MemberNode name, List<NodeBase> generics, Type symbol) 
        : TypeNode(name, generics)
    {
        public override Type Symbol { get; } = symbol;
    }
}