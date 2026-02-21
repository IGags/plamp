using System.Reflection;
using System.Reflection.Emit;
using plamp.Abstractions.Symbols.SymTableBuilding;

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

        var functions = builder.ListFuncs();
        foreach (var func in functions)
        {
            EmitFunction(moduleBuilder, func);
        }
    }

    public static void EmitType(ModuleBuilder module, ITypeBuilderInfo type)
    {
        var typeBuilder = module.DefineType(
            type.Name, 
            TypeAttributes.Public | TypeAttributes.AutoLayout | TypeAttributes.Sealed, 
            typeof(object));
        type.Type = typeBuilder;

        var fields = type.FieldBuilders;
        foreach (var fld in fields)
        {
            typeBuilder.DefineField(fld.Name, fld.FieldType.AsType(), FieldAttributes.Public);
        }
        
        CreateDefaultCtor(typeBuilder, type);
    }

    public static void EmitFunction(ModuleBuilder module, IFnBuilderInfo func)
    {
        var parameters = func.Arguments.Select(x => x.AsInfo()).ToArray();
        var parameterTypes = parameters.Select(x => x.ParameterType).ToArray(); 
        
        var retType = func.Function.ReturnType.TypeInfo?.AsType();
        if(retType == null) throw new InvalidOperationException("Возвращаемый тип не может быть null на этой стадии, исходный код написан неверно");
        
        var methodBuilder = module.DefineGlobalMethod(
            func.Name,
            MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.Final,
            CallingConventions.Standard,
            retType, 
            parameterTypes);

        IlCodeEmitter.EmitMethodBody(func.Function.Body, methodBuilder, parameters);
    }
    
    private static void CreateDefaultCtor(TypeBuilder typeBuilder, ITypeBuilderInfo type)
    {
        if(type.Constructor == null) throw new Exception("Каждый пользовательский тип обязан иметь конструктор без параметров");

        var ctorFunc = type.Constructor.Function;
        if (!type.Equals(ctorFunc.ReturnType.TypeInfo)
            || ctorFunc.ParameterList.Count != 1
            || !type.Equals(ctorFunc.ParameterList[0].Type.TypeInfo))
        {
            throw new Exception("Функция конструктора обязана иметь один аргумент типа создаваемого значения и такой же возвращаемый тип.");
        }

        var parameter = ctorFunc.ParameterList[0].ParamInfo?.AsInfo();

        if (parameter == null)
        {
            throw new Exception("Аргумент создающегося типа - null");
        }

        var ctorBuilder = typeBuilder.DefineConstructor(
            MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.HideBySig,
            CallingConventions.Standard,
            []);
        
        IlCodeEmitter.EmitCtorBody(ctorFunc.Body, ctorBuilder, [parameter]);
    }
}