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
        type.Type = typeBuilder;

        var fields = type.FieldBuilders;

        var fieldAttributeCtor = typeof(PlampVisibleAttribute).GetConstructor(BindingFlags.Public | BindingFlags.Instance, []);
        var attribute = new CustomAttributeBuilder(fieldAttributeCtor!, []);
        foreach (var fld in fields)
        {
            var fldInfo = typeBuilder.DefineField(fld.Name, fld.FieldType.AsType(), FieldAttributes.Public);
            fldInfo.SetCustomAttribute(attribute);
            fld.Field = fldInfo;
        }
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
        var dbg = new DebugMethodBuilder(methodBuilder);
        
        IlCodeEmitter.EmitMethodBody(func.Function.Body, dbg, parameters);
        Console.WriteLine(dbg.GetIlRepresentation());
    }
}