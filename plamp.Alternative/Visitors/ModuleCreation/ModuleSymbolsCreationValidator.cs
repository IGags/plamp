using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
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

    private void OverrideToString(TypeBuilder builder, List<FieldBuilder> fields)
    {
        const string name = "<object.ToStringInternal>";
        var internalMethod = builder.DefineMethod(
            name,
            MethodAttributes.Private
            | MethodAttributes.HideBySig
            | MethodAttributes.NewSlot
            | MethodAttributes.Virtual
            | MethodAttributes.Final, 
            CallingConventions.HasThis, 
            typeof(string), []);

        var baseMethod = typeof(object).GetMethod(nameof(ToString), [])!;
        builder.DefineMethodOverride(internalMethod, baseMethod);
        var generator = new DebugILGenerator(internalMethod.GetILGenerator());
        
        var partList = generator.DeclareLocal(typeof(StringBuilder));
        var addMethod = typeof(StringBuilder)
            .GetMethod(nameof(StringBuilder.Append), BindingFlags.Public | BindingFlags.Instance, [typeof(string)])!;
        var ctor = typeof(StringBuilder).GetConstructor(BindingFlags.Public | BindingFlags.Instance, [])!;
        generator.Emit(OpCodes.Newobj, ctor);
        generator.Emit(OpCodes.Stloc, partList);
        
        ObjectToString(builder, fields, generator, partList, addMethod);

        var concatInfo = typeof(StringBuilder)
            .GetMethod(nameof(StringBuilder.ToString), BindingFlags.Instance | BindingFlags.Public, [])!;
        generator.Emit(OpCodes.Ldloc, partList);
        generator.Emit(OpCodes.Call, concatInfo);
        generator.Emit(OpCodes.Ret);
        
        Console.WriteLine(builder.Name + " ToStirng()");
        Console.WriteLine(generator.ToString());
    }

    private void ObjectToString(TypeBuilder type, List<FieldBuilder> fields, ILGenerator generator, LocalBuilder list, MethodInfo addMethod)
    {
        AddStringMacro(generator, $"{type.Name} {{", list, addMethod);
        var first = true;
        foreach (var fld in fields)
        {
            if (!first) AddStringMacro(generator, "; ", list, addMethod);
            AddStringMacro(generator, $"{fld.Name}: ", list, addMethod);

            var fldLoc = FldToLoc(generator, fld);
            
            if(fld.FieldType.IsArray) ArrayToString(fldLoc, generator, list, addMethod);
            else if (fld.FieldType.IsPrimitive)
            {
                var toStringMethod = fld.FieldType.GetMethod(nameof(ToString), BindingFlags.Public | BindingFlags.Instance, [])!;
                generator.Emit(OpCodes.Ldloc, list);
                generator.Emit(OpCodes.Ldloca, fldLoc);
                generator.Emit(OpCodes.Call, toStringMethod);
                generator.Emit(OpCodes.Call, addMethod);
                generator.Emit(OpCodes.Pop);
            }
            else CallVirtToStringMacro(generator, fldLoc, list, addMethod);

            first = false;
        }
        AddStringMacro(generator, "}", list, addMethod);
    }

    private void ArrayToString(LocalBuilder arrayRef, ILGenerator generator, LocalBuilder list, MethodInfo addMethod)
    {
        var elemType = arrayRef.LocalType.GetElementType()!;
        var itemIsArray = elemType.IsArray;
        AddStringMacro(generator, "[", list, addMethod);
        
        var i = generator.DeclareLocal(typeof(int));
        var first = generator.DeclareLocal(typeof(bool));
        
        generator.Emit(OpCodes.Ldc_I4_1);
        generator.Emit(OpCodes.Stloc, first);
        
        generator.Emit(OpCodes.Ldc_I4_0);
        generator.Emit(OpCodes.Stloc, i);
        
        var loopStart = generator.DefineLabel();
        var loopEnd = generator.DefineLabel();
        var itemWrite = generator.DefineLabel();
        generator.MarkLabel(loopStart);
        
        generator.Emit(OpCodes.Ldloc, i);
        generator.Emit(OpCodes.Ldloc, arrayRef);
        generator.Emit(OpCodes.Ldlen);
        generator.Emit(OpCodes.Clt);
        generator.Emit(OpCodes.Brfalse, loopEnd);
        
        generator.Emit(OpCodes.Ldloc, first);
        generator.Emit(OpCodes.Ldc_I4_1);
        generator.Emit(OpCodes.Beq);
        AddStringMacro(generator, ", ", list, addMethod);
        
        generator.MarkLabel(itemWrite);
        generator.Emit(OpCodes.Ldloc, arrayRef);
        generator.Emit(OpCodes.Ldloc, i);
        generator.Emit(OpCodes.Ldelem);
        
        var elemLoc = generator.DeclareLocal(elemType);
        generator.Emit(OpCodes.Stloc);

        if (itemIsArray) ArrayToString(elemLoc, generator, list, addMethod);
        else CallVirtToStringMacro(generator, elemLoc, list, addMethod);

        generator.Emit(OpCodes.Ldc_I4_0);
        generator.Emit(OpCodes.Stloc, first);
        
        generator.Emit(OpCodes.Ldloc, i);
        generator.Emit(OpCodes.Add, 1);
        generator.Emit(OpCodes.Br, loopStart);
        
        generator.MarkLabel(loopEnd);
        AddStringMacro(generator, "]", list, addMethod);
    }

    private void AddStringMacro(ILGenerator generator, string toAdd, LocalBuilder list, MethodInfo addMethod)
    {
        generator.Emit(OpCodes.Ldloc, list);
        generator.Emit(OpCodes.Ldstr, toAdd);
        generator.Emit(OpCodes.Callvirt, addMethod);
        generator.Emit(OpCodes.Pop);
    }

    private void CallVirtToStringMacro(ILGenerator generator, LocalBuilder loc, LocalBuilder list, MethodInfo addMethod)
    {
        var virtInfo = typeof(object).GetMethod(nameof(ToString), BindingFlags.Public | BindingFlags.Instance, [])!;
        generator.Emit(OpCodes.Ldloc, list);
        generator.Emit(OpCodes.Ldloc, loc);
        generator.Emit(OpCodes.Callvirt, virtInfo);
        generator.Emit(OpCodes.Callvirt, addMethod);
        generator.Emit(OpCodes.Pop);
    }

    private LocalBuilder FldToLoc(ILGenerator generator, FieldInfo fld)
    {
        generator.Emit(OpCodes.Ldarg_0);
        generator.Emit(OpCodes.Ldfld, fld);
        var loc = generator.DeclareLocal(fld.FieldType);
        generator.Emit(OpCodes.Stloc, loc);
        return loc;
    }
    
    private void OverrideEquals(TypeBuilder builder)
    {
        
    }

    private record struct TypeBuildingContext(
        ITypeBuilderInfo TypeBuilderInfo, 
        TypeBuilder TypeBuilder, 
        ConstructorBuilder ConstructorBuilder);
}