using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace plamp.CodeEmission.Tests.Infrastructure;

public class DebugMethodBuilder : MethodBuilder
{
    private readonly MethodBuilder _inner;

    private ILGenerator? _dbGen;

    #region Reflection

    /*
     *  Тест раннер решарпера не запустится без имплементации MethodInfo
     */
    
    public override string Name => _inner.Name;

    public override Type? DeclaringType => _inner.DeclaringType;

    public override Type? ReflectedType => _inner.ReflectedType;

    public override MethodAttributes Attributes => _inner.Attributes;

    public override RuntimeMethodHandle MethodHandle => _inner.MethodHandle;

    public override ICustomAttributeProvider ReturnTypeCustomAttributes => _inner.ReturnTypeCustomAttributes;

    public override MethodInfo GetBaseDefinition() => _inner.GetBaseDefinition();

    public override object Invoke(object? obj, BindingFlags invokeAttr, Binder? binder, object?[]? parameters, CultureInfo? culture) 
        => _inner.Invoke(obj, invokeAttr, binder, parameters, culture);

    public override MethodImplAttributes GetMethodImplementationFlags() => _inner.GetMethodImplementationFlags();

    public override bool IsDefined(Type attributeType, bool inherit) => _inner.IsDefined(attributeType, inherit);

    public override object[] GetCustomAttributes(bool inherit) => _inner.GetCustomAttributes(inherit);

    public override object[] GetCustomAttributes(Type attributeType, bool inherit) => _inner.GetCustomAttributes(attributeType, inherit);

    public override ParameterInfo[] GetParameters() => _inner.GetParameters();

    public override int MetadataToken => _inner.MetadataToken;

    public override Module Module => _inner.Module;

    public override Type ReturnType => _inner.ReturnType;

    #endregion
    
    public DebugMethodBuilder(MethodBuilder inner)
    {
        _inner = inner;
    }
    
    protected override GenericTypeParameterBuilder[] DefineGenericParametersCore(params string[] names)
    {
        return _inner.DefineGenericParameters(names);
    }

    protected override ParameterBuilder DefineParameterCore(int position, ParameterAttributes attributes, string? strParamName)
    {
        return _inner.DefineParameter(position, attributes, strParamName);
    }

    protected override ILGenerator GetILGeneratorCore(int size)
    {
        _dbGen = new DebugILGenerator(_inner.GetILGenerator(size));
        return _dbGen;
    }

    protected override void SetCustomAttributeCore(ConstructorInfo con, ReadOnlySpan<byte> binaryAttribute)
    {
        _inner.SetCustomAttribute(con, binaryAttribute.ToArray());
    }

    protected override void SetImplementationFlagsCore(MethodImplAttributes attributes)
    {
        _inner.SetImplementationFlags(attributes);
    }

    protected override void SetSignatureCore(Type? returnType, Type[]? returnTypeRequiredCustomModifiers,
        Type[]? returnTypeOptionalCustomModifiers, Type[]? parameterTypes, Type[][]? parameterTypeRequiredCustomModifiers,
        Type[][]? parameterTypeOptionalCustomModifiers)
    {
        _inner.SetSignature(
            returnType, 
            returnTypeRequiredCustomModifiers, 
            returnTypeOptionalCustomModifiers,
            parameterTypes, 
            parameterTypeRequiredCustomModifiers, 
            parameterTypeOptionalCustomModifiers);
    }

    protected override bool InitLocalsCore
    {
        get => _inner.InitLocals;
        set => _inner.InitLocals = value;
    }

    public string? GetIlRepresentation()
    {
        return _dbGen?.ToString();
    }

    public MethodBuilder GetInner() => _inner;
}