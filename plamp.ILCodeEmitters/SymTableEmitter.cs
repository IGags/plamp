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
        var builderParis = new List<TypeInfoBuilderPair>();
        foreach (var typ in types)
        {
            var typeBuilder = EmitType(moduleBuilder, typ);
            typ.Builder = typeBuilder;
            builderParis.Add(new(typ, typeBuilder));
        }
        
        foreach (var pair in builderParis)
        {
            EmitFields(pair);
        }

        foreach (var pair in builderParis)
        {
            SetTypeMembers(pair);
        }
        
        var functions = builder.ListFuncs();
        foreach (var func in functions)
        {
            EmitFunction(moduleBuilder, builder, func);
        }
    }

    public static TypeBuilder EmitType(ModuleBuilder module, ITypeBuilderInfo type)
    {
        var typeBuilder = module.DefineType(
            type.DefinitionName, 
            TypeAttributes.Public | TypeAttributes.AutoLayout | TypeAttributes.Sealed, 
            typeof(ValueType));
        
        var genericParams = type.GenericParameterBuilders;
        
        if (genericParams.Count != 0)
        {
           SetGenericsForType(typeBuilder, genericParams);
        }
        
        return typeBuilder;
    }

    /// <summary>
    /// Устанавливает <see cref="ITypeBuilderInfo"/> его поля, дженерики и в конце концов сам тип.
    /// Завершает создание типа.
    /// </summary>
    /// <param name="pair">Пара информация о типе и его билдер.</param>
    private static void SetTypeMembers(TypeInfoBuilderPair pair)
    {
        var (info, builder) = pair;

        var type = info.Type = builder.CreateType();

        var fields = type.GetFields()
            .Where(x => x.GetCustomAttribute<PlampVisibleAttribute>() != null)
            .ToDictionary(x => x.Name, x => x);

        foreach (var field in info.FieldBuilders)
        {
            if (!fields.TryGetValue(field.Name, out var value))
                throw new Exception("Не удалось найти поле после создания типа внутри сборки");

            field.Field = value;
        }

        var generics = type.GetGenericArguments().ToDictionary(x => x.Name, x => x);

        foreach (var generic in info.GenericParameterBuilders)
        {
            if (!generics.TryGetValue(generic.Name, out var value))
                throw new Exception("Не удалось найти объявление дженерик параметра после создания типа внутри сборки");

            generic.GenericParameterType = value;
        }
    }

    /// <summary>
    /// Создать поля для структуры определённого типа
    /// </summary>
    /// <param name="pair">Пара - информация о собираемом типе, объект - билдер Reflection.Emit</param>
    /// <returns>Типы из текущей сборки, от которых зависит данный тип</returns>
    private static void EmitFields(TypeInfoBuilderPair pair)
    {
        var fields = pair.Info.FieldBuilders;
        var typeBuilder = pair.Builder;

        var fieldAttributeCtor = typeof(PlampVisibleAttribute).GetConstructor(BindingFlags.Public | BindingFlags.Instance, []);
        var attribute = new CustomAttributeBuilder(fieldAttributeCtor!, []);
        foreach (var fld in fields)
        {
            var fldInfo = typeBuilder.DefineField(fld.Name, fld.FieldType.AsType(), FieldAttributes.Public);
            fldInfo.SetCustomAttribute(attribute);
            fld.Builder = fldInfo;
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
            parameter.ParameterBuilder = builder;
        }
    }

    public static void EmitFunction(ModuleBuilder module, ISymTableBuilder symTableBuilder, IFnBuilderInfo func)
    {
        var methodBuilder = module.DefineGlobalMethod(
            func.DefinitionName,
            MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.Final,
            CallingConventions.Standard,
            null,
            null);
        
        var genericParams = func.GetGenericParameterBuilders();
        if (genericParams.Count != 0)
        {
            SetGenericsForFunc(methodBuilder, genericParams);
        }
        
        var parameters = func.Arguments.Select(x => x.AsInfo()).ToArray();
        var parameterTypes = parameters.Select(x => x.ParameterType).ToArray(); 
        
        var retType = func.ReturnType.AsType();
        if (retType == null)
        {
            throw new InvalidOperationException("Возвращаемый тип не может быть null на этой стадии, исходный код написан неверно");
        }
        
        methodBuilder.SetParameters(parameterTypes);
        methodBuilder.SetReturnType(retType);
        func.MethodBuilder = methodBuilder;
        
        var dbg = new DebugMethodBuilder(methodBuilder);

        if (!symTableBuilder.TryGetDefinition(func, out var node))
        {
            throw new InvalidOperationException("Не найдено объявление функции в исходном ast, ошибка в коде компилятора.");
        }
        
        IlCodeEmitter.EmitMethodBody(node.Body, dbg, parameters);
        Console.WriteLine(dbg.GetIlRepresentation());
    }

    private static void SetGenericsForFunc(MethodBuilder methodBuilder, IReadOnlyList<IGenericParameterBuilder> genericParams)
    {
        var genericNames = genericParams.Select(x => x.Name).ToArray();
        var parameters = methodBuilder.DefineGenericParameters(genericNames);
        
        if (parameters.Length != genericNames.Length) throw new Exception();
        
        var parameterDict = parameters.ToDictionary(x => x.Name, x => x);

        foreach (var parameter in genericParams)
        {
            if (!parameterDict.TryGetValue(parameter.Name, out var builder)) throw new Exception();
            parameter.GenericParameterType = builder;
        }
    }

    private record struct TypeInfoBuilderPair(ITypeBuilderInfo Info, TypeBuilder Builder);
}