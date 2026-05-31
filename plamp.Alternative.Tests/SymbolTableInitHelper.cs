using System.Collections.Generic;
using plamp.Abstractions.Symbols.SymTable;

namespace plamp.Alternative.Tests;

public static class SymbolTableInitHelper
{
    public static List<ISymTable> CreateDefaultTables() => [Builtins.SymTable];
}