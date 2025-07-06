using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.CompilerEmission;
using plamp.ILCodeEmitters;

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
        var toTyp = CreateTypeNode(to);
        return new ConcreteCastNode(toTyp, inner, from);
    }

    public static ConstructorCallNode CreateConstructorNode(TypeNode type, List<NodeBase> args, ConstructorInfo ctor) 
        => new ConcreteConstructorNode(type, args, ctor);

    public static async Task<(object? instance, MethodInfo? methodInfo)> CreateInstanceWithMethodAsync(
        ParameterInfo[] args,
        BodyNode body,
        Type returnType)
    {
        var methodName = $"{Guid.NewGuid()} {DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)}";
        var argTypes = args.Select(x => x.ParameterType).ToArray();
        var (_, typeBuilder, methodBuilder, _) = CreateMethodBuilder(methodName, returnType, argTypes);
        var context = new CompilerEmissionContext(body, methodBuilder, args, null, null);
        var emitter = new DefaultIlCodeEmitter();
        await emitter.EmitMethodBodyAsync(context, CancellationToken.None);
        var type = typeBuilder.CreateType();
        var (instance, methodInfo) = CreateObject(type, methodName);
        return (instance, methodInfo);
    }

    private sealed class ConcreteConstructorNode(TypeNode type, List<NodeBase> args, ConstructorInfo ctor) 
        : ConstructorCallNode(type, args)
    {
        public override ConstructorInfo Symbol { get; init; } = ctor;
    }
    
    private sealed class ConcreteCastNode : CastNode
    {
        public ConcreteCastNode(NodeBase toType, NodeBase inner, Type fromType) : base(toType, inner)
        {
            FromType = fromType;
        }
    }
    
    private sealed class ConcreteType : TypeNode
    {
        public ConcreteType(MemberNode name, List<NodeBase> generics, Type symbol) : base(name, generics)
        {
            Symbol = symbol;
        }
    }
    
    private sealed class ConcreteMember : MemberNode
    {
        public ConcreteMember(string name, MemberInfo symbol) : base(name)
        {
            Symbol = symbol;
        }
    }
    
    private sealed class ConcreteCall : CallNode
    {
        public ConcreteCall(NodeBase? from, NodeBase name, List<NodeBase> args, MethodInfo symbol) : base(from, name as MemberNode, args)
        {
            Symbol = symbol;
        }
    }
}