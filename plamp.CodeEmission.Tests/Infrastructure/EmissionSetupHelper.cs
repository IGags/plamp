using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using plamp.Abstractions;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.Ast.Node.Definitions;
using plamp.Abstractions.Ast.Node.Definitions.Func;
using plamp.Abstractions.Ast.Node.Definitions.Type;
using plamp.ILCodeEmitters;

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

    public static ICompileTimeFunction MakeFuncRef(MethodInfo info) => new MockFuncRef(info);
    public static ICompileTimeFunction MakeFuncRef(MethodInfo info, IEnumerable<Type> argTypes, Type retType) => new MockFuncRef(info, argTypes, retType);

    public static ICompileTimeType MakeTypeRef(Type type) => new MockTypeRef(type);

    public static (object? instance, MethodInfo? methodInfo) CreateObject(Type builtType, string methodName)
    {
        var createdMethod = builtType.GetMethod(methodName);
        var instance = Activator.CreateInstance(builtType);
        return (instance, createdMethod);
    }

    public static TypeNode CreateTypeNode(ICompileTimeType type) => new ConcreteType(new TypeNameNode(type.TypeName), type);

    public static MemberNode CreateMemberNode(MemberInfo memberInfo) => new ConcreteMember(memberInfo.Name, memberInfo);

    public static CallNode CreateCallNode(NodeBase? from, ICompileTimeFunction info, List<NodeBase> args) 
        => new ConcreteCall(from, new FuncCallNameNode(info.Name), args, info);

    public static CastNode CreateCastNode(ICompileTimeType from, ICompileTimeType to, NodeBase inner)
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
        public ConcreteCastNode(NodeBase toType, NodeBase inner, ICompileTimeType fromType) : base(toType, inner)
        {
            FromType = fromType;
        }
    }
    
    private sealed class ConcreteType : TypeNode
    {
        public ConcreteType(TypeNameNode name, ICompileTimeType typedefRef) : base(name)
        {
            TypedefRef = typedefRef;
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
        public ConcreteCall(NodeBase? from, FuncCallNameNode name, List<NodeBase> args, ICompileTimeFunction symbol) : base(from, name, args)
        {
            Symbol = symbol;
        }
    }

    private class MockTypeRef(Type definition) : ICompileTimeType
    {
        public bool Equals(ICompileTimeType? other) => ReferenceEquals(this, other);

        public string TypeName { get; } = definition.Name;

        public ISymbolTable DeclaringTable => throw new Exception();

        public TypeDefinitionInfo GetDefinitionInfo()
        {
            var arrayUnderlyingType = definition.IsArray ? new MockTypeRef(definition.GetElementType()!) : null;
            var info = new TypeDefinitionInfo()
            {
                ArrayUnderlyingType = arrayUnderlyingType,
                DefinitionPosition = default,
                Fields = [],
                TypeName = TypeName
            };
            info.SetClrType(definition);
            return info;
        }

        public ICompileTimeType MakeArrayType() => new MockTypeRef(definition.MakeArrayType());

        public ICompileTimeField DefineField(string name, ICompileTimeType type) => throw new NotSupportedException();
    }
    
    private class MockFuncRef : ICompileTimeFunction
    {
        private readonly MethodInfo _definition;

        private readonly List<ICompileTimeType> _args;

        private readonly ICompileTimeType _returnType;
        
        public bool Equals(ICompileTimeFunction? other) => ReferenceEquals(this, other);

        public ISymbolTable DeclaringTable => throw new Exception();
        public string Name { get; }
        public IReadOnlyList<ICompileTimeType> ArgumentTypes => _args;

        public MockFuncRef(MethodInfo info, IEnumerable<Type> argTypes, Type retType)
        {
            _definition = info;
            Name = info.Name;
            _args = new List<ICompileTimeType>();
            foreach (var argType in argTypes)
            {
                _args.Add(new MockTypeRef(argType));
            }

            _returnType = new MockTypeRef(retType);
        }
        
        public MockFuncRef(MethodInfo info)
        {
            _definition = info;
            Name = info.Name;
            _args = new List<ICompileTimeType>();
            foreach (var type in info.GetParameters().Select(x => x.ParameterType))
            {
                _args.Add(new MockTypeRef(type));
            }

            _returnType = new MockTypeRef(info.ReturnType);
        }
        
        public FunctionDefinitionInfo GetDefinitionInfo()
        {
            var info = new FunctionDefinitionInfo()
            {
                ArgumentList = _args,
                DefinitionPosition = default,
                Name = Name,
                ReturnType = _returnType
            };
            info.SetClrMethod(_definition);
            return info;
        }
    }
}