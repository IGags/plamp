using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Definitions;
using plamp.Abstractions.AstManipulation.Validation;
using plamp.Abstractions.Symbols.SymTable;
using plamp.Abstractions.Symbols.SymTableBuilding;
using plamp.Alternative.EmissionDebug;
using plamp.Alternative.SymbolsImpl;

namespace plamp.Alternative.Visitors.ModuleCreation;

public class ModuleSymbolsCreationValidator : BaseValidator<CreationContext, CreationContext>
{
    protected override CreationContext CreateInnerContext(CreationContext context) => context;

    protected override CreationContext MapInnerToOuter(CreationContext outerContext, CreationContext innerContext) =>
        innerContext;

    protected override VisitResult PreVisitRoot(RootNode node, CreationContext context, NodeBase? parent)
    {
        var typeBuildingInfos = context.CurrentModuleBuilder.ListTypes();
        var contexts = new List<TypeBuildingContext>();
        foreach (var info in typeBuildingInfos)
        {
            var typeContext = DefineType(context.ModuleBuilder, info);
            contexts.Add(typeContext);
        }

        var currentModuleCtors = contexts.ToDictionary(ITypeInfo (x) => x.TypeBuilderInfo, x => x.ConstructorBuilder); 

        foreach (var (typeInfo, typeBuilder, ctorBuilder) in contexts)
        {
            var generator = new DebugILGenerator(ctorBuilder.GetILGenerator());
            
            foreach (var fieldInfo in typeInfo.FieldBuilders)
            {
                var fieldBuilder = DefineField(fieldInfo, typeBuilder);
                if(TryInitAsEmptyArray(fieldBuilder, generator))                                       continue;
                if(TryInitAsPrimitive(fieldBuilder))                                                   continue;
                if(TryInitAsEmptyString(fieldBuilder, fieldInfo, generator))                           continue;
                if(TryInitAsCurrentModuleCtor(fieldBuilder, generator, fieldInfo, currentModuleCtors)) continue;
                if(TryInitAsNonCurrentModuleCtor(fieldBuilder, generator))                             continue;
                throw new InvalidOperationException("Cannot init field with default value. Compilation fault.");
            }

            FinishDefaultCtor(generator);
            Console.WriteLine(generator.ToString());
            
            //OverrideToString(typeBuilder, fields);
            //OverrideEquals(typeBuilder);
        }

        return VisitResult.SkipChildren;
    }

    private TypeBuildingContext DefineType(ModuleBuilder builder, ITypeBuilderInfo typeInfo)
    {
        var typeBuilder = builder.DefineType(
            typeInfo.Name,
            TypeAttributes.AutoLayout | TypeAttributes.Public | TypeAttributes.Sealed, 
            typeof(object));
        typeInfo.Definition.Type = typeBuilder;
        var ctorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public | MethodAttributes.Final, CallingConventions.HasThis, []);
        return new TypeBuildingContext(typeInfo, typeBuilder, ctorBuilder);
    }

    private FieldBuilder DefineField(IFieldBuilderInfo fieldInfo, TypeBuilder typeBuilder)
    {
        var fieldType = fieldInfo.FieldType.AsType();
        var fieldBuilder = typeBuilder.DefineField(fieldInfo.Name, fieldType, FieldAttributes.Public);
        var attributeBuilder = new CustomAttributeBuilder(typeof(PlampFieldGeneratedAttribute).GetConstructor([])!, []);
        fieldBuilder.SetCustomAttribute(attributeBuilder);
        fieldInfo.Definition.Field = fieldBuilder;
        return fieldBuilder;
    }

    private bool TryInitAsCurrentModuleCtor(
        FieldBuilder builder,
        ILGenerator generator,
        IFieldBuilderInfo fieldInfo,
        Dictionary<ITypeInfo, ConstructorBuilder> currentModuleCtors)
    {
        if (!currentModuleCtors.TryGetValue(fieldInfo.FieldType, out var ctor)) return false;
        generator.Emit(OpCodes.Ldarg_0);
        generator.Emit(OpCodes.Newobj, ctor);
        generator.Emit(OpCodes.Stfld, builder);
        return true;
    }

    private bool TryInitAsEmptyArray(
        FieldBuilder builder,
        ILGenerator generator)
    {
        if (!builder.FieldType.IsArray) return false;
        var elemType = builder.FieldType.GetElementType()!;
        var meth = typeof(Array)
            .GetMethod(nameof(Array.Empty), BindingFlags.Public | BindingFlags.Static)!
            .MakeGenericMethod(elemType);
        
        generator.Emit(OpCodes.Ldarg_0);
        generator.EmitCall(OpCodes.Call, meth, []);
        generator.Emit(OpCodes.Stfld, builder);
        return true;
    }

    private bool TryInitAsPrimitive(FieldBuilder builder) => builder.FieldType.IsPrimitive;

    private bool TryInitAsNonCurrentModuleCtor(FieldBuilder builder, ILGenerator generator)
    {
        var ctor = builder.FieldType.GetConstructor(BindingFlags.Public | BindingFlags.Instance, []);
        if (ctor == null) return false;
        generator.Emit(OpCodes.Ldarg_0);
        generator.Emit(OpCodes.Newobj, ctor);
        generator.Emit(OpCodes.Stfld, builder);
        return true;
    }

    private bool TryInitAsEmptyString(FieldBuilder builder, IFieldBuilderInfo fieldInfo, ILGenerator generator)
    {
        if (!SymbolSearchUtility.IsString(fieldInfo.FieldType)) return false;
        var emptyField = typeof(string).GetField(nameof(string.Empty), BindingFlags.Static | BindingFlags.Public)!;
        generator.Emit(OpCodes.Ldarg_0);
        generator.Emit(OpCodes.Ldsfld, emptyField);
        generator.Emit(OpCodes.Stfld, builder);
        return true;
    }

    private void FinishDefaultCtor(ILGenerator generator)
    {
        var objectConstructor = typeof(object).GetConstructor(BindingFlags.Public | BindingFlags.Instance, [])!;
        generator.Emit(OpCodes.Ldarg_0);
        generator.Emit(OpCodes.Call, objectConstructor);
        generator.Emit(OpCodes.Ret);
    }

    private record struct TypeBuildingContext(
        ITypeBuilderInfo TypeBuilderInfo, 
        TypeBuilder TypeBuilder, 
        ConstructorBuilder ConstructorBuilder);
}