using System.Reflection;
using System.Reflection.Emit;
using plamp.Abstractions.Symbols;
using plamp.Abstractions.Symbols.SymTable;
using plamp.Abstractions.Symbols.SymTableBuilding;
using plamp.ILCodeEmitters.EmissionDebug;

namespace plamp.ILCodeEmitters;

public static class SymTableEmitter
{
    public static void EmitModule(ISymTableBuilder builder, ModuleBuilder moduleBuilder)
    {
        var types = builder.ListTypes();
        foreach (var typ in types)
        {
            EmitType(moduleBuilder, typ);
        }

        foreach (var typ in types)
        {
            EmitFields(typ);
        }

        var functions = builder.ListFuncs();
        foreach (var func in functions)
        {
            EmitFunction(moduleBuilder, builder, func);
        }

        foreach (var type in types)
        {
            var typeBuilder = type.Type ?? throw new Exception("В этой точке тип не может быть null, проверьте код компилятора");
            typeBuilder.CreateType();
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
    /// <param name="typeInfo">Описание структуры</param>
    /// <returns>Типы из текущей сборки, от которых зависит данный тип</returns>
    /// <exception cref="Exception">TypeBuilder для данного типа не находится внутри <paramref name="typeInfo"/></exception>
    private static List<ITypeInfo> EmitFields(ITypeBuilderInfo typeInfo)
    {
        var fields = typeInfo.FieldBuilders;
        var typeBuilder = typeInfo.Type;
        if (typeBuilder == null) throw new Exception();

        var fieldAttributeCtor = typeof(PlampVisibleAttribute).GetConstructor(BindingFlags.Public | BindingFlags.Instance, []);
        var attribute = new CustomAttributeBuilder(fieldAttributeCtor!, []);
        
        foreach (var fld in fields)
        {
            var fldType = fld.FieldType;
            
            
            var fldInfo = typeBuilder.DefineField(fld.Name, fld.FieldType.AsType(), FieldAttributes.Public);
            fldInfo.SetCustomAttribute(attribute);
            fld.Field = fldInfo;
        }

        return dependencyCount;
    }

    private static List<ITypeInfo> FindDependenciesFromSameAssembly(ITypeBuilderInfo originalType, ITypeInfo fieldType)
    {
        if (fieldType.IsGenericTypeDefinition) throw new Exception();
        var elem = fieldType;
        while (elem.IsArrayType)
        {
            elem = elem.ElementType();
            if (elem == null) throw new Exception();
        }

        var deps = new List<ITypeInfo>();

        if (elem.IsGenericType)
        {
            var def = elem.GetGenericTypeDefinition();
            if (def == null) throw new Exception();
            var args = elem.GetGenericArguments();
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