using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.Ast.Node.Definitions;
using plamp.Abstractions.Ast.Node.Definitions.Func;
using plamp.Abstractions.Ast.Node.Definitions.Type;
using plamp.Abstractions.Symbols;
using plamp.Alternative.SymbolsImpl;
using plamp.ILCodeEmitters;
using TypeInfo = plamp.Alternative.SymbolsImpl.TypeInfo;

namespace plamp.CodeEmission.Tests.Infrastructure;

public class EmissionSetupHelper
{
    public static (
        TypeBuilder typeBuilder, 
        DebugMethodBuilder methodBuilder, 
        ModuleBuilder moduleBuilder)
        CreateMethodBuilder(string methodName,
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

        return (type, dbgMeth, module);
    }

    public static IFnInfo MakeFuncRef(MethodInfo info) => new FuncInfo(info);

    public static ITypeInfo MakeTypeRef(Type type) => new TypeInfo(type);

    public static (object? instance, MethodInfo? methodInfo) CreateObject(Type builtType, string methodName)
    {
        var createdMethod = builtType.GetMethod(methodName);
        var instance = Activator.CreateInstance(builtType);
        return (instance, createdMethod);
    }

    public static TypeNode CreateTypeNode(ITypeInfo type) => new ConcreteType(new TypeNameNode(type.Name));

    public static MemberNode CreateMemberNode(MemberInfo memberInfo) => new ConcreteMember(memberInfo.Name, memberInfo);

    public static CallNode CreateCallNode(NodeBase? from, IFnInfo info, List<NodeBase> args) 
        => new ConcreteCall(from, new FuncCallNameNode(info.Name), args, info);

    public static CastNode CreateCastNode(ITypeInfo from, ITypeInfo to, NodeBase inner)
    {
        var toTyp = CreateTypeNode(to);
        return new ConcreteCastNode(toTyp, inner, from);
    }

    public static (object? instance, MethodInfo? methodInfo) CreateInstanceWithMethod(
        ParameterInfo[] args,
        BodyNode body,
        Type returnType)
    {
        var methodName = $"{Guid.NewGuid()} {DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)}";
        var argTypes = args.Select(x => x.ParameterType).ToArray();
        var (typeBuilder, methodBuilder, _) = CreateMethodBuilder(methodName, returnType, argTypes);
        var context = new CompilerEmissionContext(body, methodBuilder, args, null);
        IlCodeEmitter.EmitMethodBody(context);
        var type = typeBuilder.CreateType();
        var (instance, methodInfo) = CreateObject(type, methodName);
        return (instance, methodInfo);
    }
    
    private sealed class ConcreteCastNode : CastNode
    {
        public ConcreteCastNode(NodeBase toType, NodeBase inner, ITypeInfo fromType) : base(toType, inner)
        {
            FromType = fromType;
        }
    }
    
    private sealed class ConcreteType(TypeNameNode name) : TypeNode(name);
    
    private sealed class ConcreteMember : MemberNode
    {
        public ConcreteMember(string name, MemberInfo symbol) : base(name)
        {
            Symbol = symbol;
        }
    }
    
    private sealed class ConcreteCall : CallNode
    {
        public ConcreteCall(NodeBase? from, FuncCallNameNode name, List<NodeBase> args, IFnInfo symbol) : base(from, name, args)
        {
            FnInfo = symbol;
        }
    }
}