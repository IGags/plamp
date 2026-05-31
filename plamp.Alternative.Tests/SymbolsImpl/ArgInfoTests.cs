using System;
using plamp.Alternative.SymbolsImpl;
using Shouldly;
using Xunit;

namespace plamp.Alternative.Tests.SymbolsImpl;

public class ArgInfoTests
{
    /// <summary>
    /// Не может существовать аргумента с типом void
    /// </summary>
    [Fact]
    public void ArgInfoOfVoid_ThrowsInvalidOperation()
    {
        Should.Throw<InvalidOperationException>(() => new ArgInfo("f", Builtins.Void));
    }

    /// <summary>
    /// У аргумента должно быть имя
    /// </summary>
    [Fact]
    public void ArgInfoWithEmptyName_ThrowsInvalidOperation()
    {
        Should.Throw<InvalidOperationException>(() => new ArgInfo("", Builtins.Uint));
    }

    /// <summary>
    /// Happy path
    /// </summary>
    [Fact]
    public void ArgInfo_Correct()
    {
        var type = Builtins.Any;
        var name = "Abc123$_@";
        var info = new ArgInfo(name, type);
        info.Name.ShouldBe(name);
        info.Type.ShouldBe(type);
    }
}