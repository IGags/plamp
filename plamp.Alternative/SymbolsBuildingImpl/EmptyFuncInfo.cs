using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using plamp.Abstractions.Symbols.SymTable;
using plamp.Abstractions.Symbols.SymTableBuilding;

namespace plamp.Alternative.SymbolsBuildingImpl;

public class EmptyFuncInfo(string name, IReadOnlyList<IArgInfo> args, ITypeInfo returnType, ISymTableBuilder definingBuilder) : IFnBuilderInfo
{
    public string Name => name;
    
    public IReadOnlyList<IArgInfo> Arguments { get; } = args;
    
    public ITypeInfo ReturnType => returnType;
    
    public MethodInfo AsFunc()
    {
        MethodInfo funcInfo = MethodBuilder ?? throw new NullReferenceException();
        return funcInfo;
    }

    public bool Equals(IFnInfo? other)
    {
        if (other is not EmptyFuncInfo fnInfo) return false;
        if (!definingBuilder.TryGetDefinition(fnInfo, out var otherDef)) return false;
        if (!definingBuilder.TryGetDefinition(this, out var thisDef))
        {
            throw new InvalidOperationException("По какой-то причине функция находится не в том модуле, который она считает объявляющим.");
        }

        return thisDef == otherDef;
    }

    public MethodBuilder? MethodBuilder { get; set; }
}