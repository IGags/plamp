using System.Collections.Generic;
using plamp.Abstractions.Symbols.SymTable;
using plamp.Alternative.SymbolsImpl;

namespace plamp.Alternative.Tests;

public static class SymbolTableInitHelper
{
    public static List<ISymTable> CreateDefaultTables() => [new SymTable("mod", [], []), Builtins.SymTable];
}