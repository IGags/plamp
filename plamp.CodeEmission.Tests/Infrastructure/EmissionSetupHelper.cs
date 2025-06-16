using System.Reflection;
using System.Reflection.Emit;
using plamp.Abstractions.Ast.Node;

namespace plamp.CodeEmission.Tests.Infrastructure;

public class EmissionSetupHelper
{
    public static (
        AssemblyBuilder asmBuilder,
        TypeBuilder typeBuilder,
        DebugMethodBuilder methodBuilder,
        ModuleBuilder moduleBuilder) CreateMethodBuilder(
            string methodName,
            Type returnType,
            Type[] argumentTypes,
            MethodAttributes attributes = MethodAttributes.Public | MethodAttributes.Final)
    {
        var name = $"{Guid.NewGuid()}_{DateTime.Now.Ticks}";
        var assemblyName = new AssemblyName(name);
        var assembly = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndCollect);
        var module = assembly.DefineDynamicModule(name);
        var type = module.DefineType(name, TypeAttributes.Public | TypeAttributes.Class);
        var method = type.DefineMethod(methodName, attributes, returnType,
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

        return (assembly, type, dbgMeth, module);
    }

    public static (object? instance, MethodInfo? methodInfo) CreateObject(Type builtType, string methodName)
    {
        var createdMethod = builtType.GetMethod(methodName);
        var instance = Activator.CreateInstance(builtType);
        return (instance, createdMethod);
    }

    public static TypeNode CreateTypeNode(Type type) => new ConcreteType(new MemberNode(type.Name), [], type);

    public static MemberNode CreateMemberNode(MemberInfo memberInfo) => new ConcreteMember(memberInfo.Name, memberInfo);

    public static CallNode CreateCallNode(NodeBase? from, MethodInfo info, List<NodeBase> args) 
        => new ConcreteCall(from, new MemberNode(info.Name), args, info);

    public static CastNode CreateCastNode(Type from, Type to, NodeBase inner)
    {
        var fromTyp = CreateTypeNode(from);
        var toTyp = CreateTypeNode(to);
        return new ConcreteCastNode(toTyp, inner, fromTyp);
    }

    public static ConstructorCallNode CreateConstructorNode(TypeNode type, List<NodeBase> args, ConstructorInfo ctor) 
        => new ConcreteConstructorNode(type, args, ctor);

    private class ConcreteConstructorNode(TypeNode type, List<NodeBase> args, ConstructorInfo ctor) 
        : ConstructorCallNode(type, args)
    {
        public override ConstructorInfo Symbol { get; } = ctor;
    }
    
    private class ConcreteCastNode(NodeBase toType, NodeBase inner, NodeBase fromType) : CastNode(toType, inner)
    {
        public override NodeBase FromType { get; } = fromType;
    }
    
    private class ConcreteType(MemberNode name, List<NodeBase> generics, Type symbol) 
        : TypeNode(name, generics)
    {
        public override Type Symbol { get; } = symbol;
    }
    
    private class ConcreteMember(string name, MemberInfo symbol) : MemberNode(name)
    {
        public override MemberInfo Symbol { get; } = symbol;
    }
    
    private class ConcreteCall(NodeBase? from, NodeBase name, List<NodeBase> args, MethodInfo symbol) 
        : CallNode(from, name, args)
    {
        public override MethodInfo Symbol { get; } = symbol;
    }
}