using System.Collections.Generic;
using plamp.Abstractions.Symbols;
using plamp.Alternative.SymbolsImpl;

namespace plamp.Alternative.Tests;

public static class SymbolTableInitHelper
{
    public static ISymTable CreateEmptyTable() => new SymTable("mod", [], []);

    public static List<ISymTable> CreateDefaultTables() => [CreateEmptyTable(), Builtins.SymTable];
}