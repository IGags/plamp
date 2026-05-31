using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text;

namespace plamp.ILCodeEmitters.EmissionDebug;

/// <summary>
/// Декоратор <see cref="ModuleBuilder"/>, который собирает IL дамп для глобальных функций модуля
/// </summary>
public sealed class IlDumpModuleBuilder(ModuleBuilder inner) : ModuleBuilder
{
    private readonly List<DebugMethodBuilder> _methods = [];

    /// <summary>
    /// Возвращает IL дамп всех функций, сгенерированных через этот декоратор
    /// </summary>
    public string GetIlDump()
    {
        var result = new StringBuilder();
        foreach (var method in _methods)
        {
            result.AppendLine(method.GetIlRepresentation());
        }

        return result.ToString();
    }

    /// <inheritdoc />
    protected override void CreateGlobalFunctionsCore()
    {
        inner.CreateGlobalFunctions();
    }

    /// <inheritdoc />
    protected override EnumBuilder DefineEnumCore(string name, TypeAttributes visibility, Type underlyingType)
    {
        return inner.DefineEnum(name, visibility, underlyingType);
    }

    /// <inheritdoc />
    protected override MethodBuilder DefineGlobalMethodCore(
        string name,
        MethodAttributes attributes,
        CallingConventions callingConvention,
        Type? returnType,
        Type[]? requiredReturnTypeCustomModifiers,
        Type[]? optionalReturnTypeCustomModifiers,
        Type[]? parameterTypes,
        Type[][]? requiredParameterTypeCustomModifiers,
        Type[][]? optionalParameterTypeCustomModifiers)
    {
        var methodBuilder = inner.DefineGlobalMethod(
            name,
            attributes,
            callingConvention,
            returnType,
            requiredReturnTypeCustomModifiers,
            optionalReturnTypeCustomModifiers,
            parameterTypes,
            requiredParameterTypeCustomModifiers,
            optionalParameterTypeCustomModifiers);

        var debugMethodBuilder = new DebugMethodBuilder(methodBuilder);
        _methods.Add(debugMethodBuilder);

        return debugMethodBuilder;
    }

    /// <inheritdoc />
    protected override FieldBuilder DefineInitializedDataCore(string name, byte[] data, FieldAttributes attributes)
    {
        return inner.DefineInitializedData(name, data, attributes);
    }

    /// <inheritdoc />
    protected override MethodBuilder DefinePInvokeMethodCore(
        string name,
        string dllName,
        string? entryName,
        MethodAttributes attributes,
        CallingConventions callingConvention,
        Type? returnType,
        Type[]? parameterTypes,
        CallingConvention nativeCallConv,
        CharSet nativeCharSet)
    {
        return inner.DefinePInvokeMethod(
            name,
            dllName,
            entryName ?? name,
            attributes,
            callingConvention,
            returnType,
            parameterTypes,
            nativeCallConv,
            nativeCharSet);
    }

    /// <inheritdoc />
    protected override TypeBuilder DefineTypeCore(
        string name,
        TypeAttributes attr,
        Type? parent,
        Type[]? interfaces,
        PackingSize packingSize,
        int typesize)
    {
        if (interfaces is { Length: > 0 } && (packingSize != PackingSize.Unspecified || typesize != 0))
        {
            throw new NotSupportedException(
                "IlDumpModuleBuilder не поддерживает одновременное объявление интерфейсов и явного размера типа.");
        }

        if (packingSize != PackingSize.Unspecified && typesize != 0)
        {
            return inner.DefineType(name, attr, parent, packingSize, typesize);
        }

        if (packingSize != PackingSize.Unspecified)
        {
            return inner.DefineType(name, attr, parent, packingSize);
        }

        if (typesize != 0)
        {
            return inner.DefineType(name, attr, parent, typesize);
        }

        return inner.DefineType(name, attr, parent, interfaces);
    }

    /// <inheritdoc />
    protected override FieldBuilder DefineUninitializedDataCore(string name, int size, FieldAttributes attributes)
    {
        return inner.DefineUninitializedData(name, size, attributes);
    }

    /// <inheritdoc />
    protected override MethodInfo GetArrayMethodCore(
        Type arrayClass,
        string methodName,
        CallingConventions callingConvention,
        Type? returnType,
        Type[]? parameterTypes)
    {
        return inner.GetArrayMethod(arrayClass, methodName, callingConvention, returnType, parameterTypes);
    }

    /// <inheritdoc />
    public override int GetFieldMetadataToken(FieldInfo field)
    {
        return inner.GetFieldMetadataToken(field);
    }

    /// <inheritdoc />
    public override int GetMethodMetadataToken(ConstructorInfo constructor)
    {
        return inner.GetMethodMetadataToken(constructor);
    }

    /// <inheritdoc />
    public override int GetMethodMetadataToken(MethodInfo method)
    {
        return inner.GetMethodMetadataToken(method);
    }

    /// <inheritdoc />
    public override int GetSignatureMetadataToken(SignatureHelper signature)
    {
        return inner.GetSignatureMetadataToken(signature);
    }

    /// <inheritdoc />
    public override int GetStringMetadataToken(string stringConstant)
    {
        return inner.GetStringMetadataToken(stringConstant);
    }

    /// <inheritdoc />
    public override int GetTypeMetadataToken(Type type)
    {
        return inner.GetTypeMetadataToken(type);
    }

    /// <inheritdoc />
    protected override void SetCustomAttributeCore(ConstructorInfo con, ReadOnlySpan<byte> binaryAttribute)
    {
        inner.SetCustomAttribute(con, binaryAttribute.ToArray());
    }
}
