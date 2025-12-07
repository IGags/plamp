using System.Collections.Generic;
using plamp.Abstractions;
using plamp.Intrinsics;

namespace plamp.Alternative.Tests;

public static class SymbolTableInitHelper
{
    public static SymbolTable CreateEmptyTable() => new("mod", [RuntimeSymbols.SymbolTable]);

    public static List<ISymbolTable> CreateDefaultTables() => [CreateEmptyTable(), RuntimeSymbols.SymbolTable];
}