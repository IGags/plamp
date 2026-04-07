using System.Reflection;
using System.Reflection.Emit;
using plamp.Abstractions.Symbols;
using plamp.Abstractions.Symbols.SymTableBuilding;
using plamp.ILCodeEmitters.EmissionDebug;

namespace plamp.ILCodeEmitters;

public static class SymTableEmitter
{
    public static void EmitModule(ISymTableBuilder builder, ModuleBuilder moduleBuilder)
    {
        var types = TypeDependencyHelper.OrderTypes(builder.ListTypes());
        foreach (var typ in types)
        {
            EmitType(moduleBuilder, typ);
        }
        
        foreach (var typ in types)
        {
            EmitFields(typ);
        }

        foreach (var typ in types)
        {
            var typeBuilder = typ.Type;
            if (typeBuilder == null) throw new Exception();
            typeBuilder.CreateType();
        }
        
        var functions = builder.ListFuncs();
        foreach (var func in functions)
        {
            EmitFunction(moduleBuilder, builder, func);
        }
    }

    public static void EmitType(ModuleBuilder module, ITypeBuilderInfo type)
    {
        var typeBuilder = module.DefineType(
            type.Name, 
            TypeAttributes.Public | TypeAttributes.AutoLayout | TypeAttributes.Sealed, 
            typeof(ValueType));
        
        var genericParams = type.GenericParameterBuilders;
        
        if (genericParams.Count != 0)
        {
           SetGenericsForType(typeBuilder, genericParams);
        }
        
        type.Type = typeBuilder;
    }

    /// <summary>
    /// Создать поля для структуры определённого типа
    /// </summary>
    /// <param name="typeInfo">Описание типа</param>
    /// <returns>Типы из текущей сборки, от которых зависит данный тип</returns>
    /// <exception cref="Exception">TypeBuilder для данного типа не находится внутри <paramref name="typeInfo"/></exception>
    private static void EmitFields(ITypeBuilderInfo typeInfo)
    {
        var fields = typeInfo.FieldBuilders;
        var typeBuilder = typeInfo.Type;
        if (typeBuilder == null) throw new Exception();

        var fieldAttributeCtor = typeof(PlampVisibleAttribute).GetConstructor(BindingFlags.Public | BindingFlags.Instance, []);
        var attribute = new CustomAttributeBuilder(fieldAttributeCtor!, []);
        foreach (var fld in fields)
        {
            var fldInfo = typeBuilder.DefineField(fld.Name, fld.FieldType.AsType(), FieldAttributes.Public);
            fldInfo.SetCustomAttribute(attribute);
            fld.Field = fldInfo;
        }
    }

    private static void SetGenericsForType(TypeBuilder typeBuilder, IReadOnlyList<IGenericParameterBuilder> genericParams)
    {
        var genericNames = genericParams.Select(x => x.Name).ToArray();
        var parameters = typeBuilder.DefineGenericParameters(genericNames);
        if (parameters.Length != genericNames.Length) throw new Exception();
        var parameterDict = parameters.ToDictionary(x => x.Name, x => x);

        foreach (var parameter in genericParams)
        {
            if (!parameterDict.TryGetValue(parameter.Name, out var builder)) throw new Exception();
            parameter.TypeBuilder = builder;
        }
    }

    public static void EmitFunction(ModuleBuilder module, ISymTableBuilder symTableBuilder, IFnBuilderInfo func)
    {
        var parameters = func.Arguments.Select(x => x.AsInfo()).ToArray();
        var parameterTypes = parameters.Select(x => x.ParameterType).ToArray(); 
        
        var retType = func.ReturnType.AsType();
        if (retType == null)
        {
            throw new InvalidOperationException("Возвращаемый тип не может быть null на этой стадии, исходный код написан неверно");
        }
        
        var methodBuilder = module.DefineGlobalMethod(
            func.Name,
            MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.Final,
            CallingConventions.Standard,
            retType, 
            parameterTypes);
        var dbg = new DebugMethodBuilder(methodBuilder);

        if (!symTableBuilder.TryGetDefinition(func, out var node))
        {
            throw new InvalidOperationException("Не найдено объявление функции в исходном ast, ошибка в коде компилятора.");
        }
        
        IlCodeEmitter.EmitMethodBody(node.Body, dbg, parameters);
        Console.WriteLine(dbg.GetIlRepresentation());
    }
}