using System.Collections.Generic;
using System.Linq;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Assign;
using plamp.Abstractions.Ast.Node.Definitions;
using plamp.Abstractions.Ast.Node.Definitions.Variable;
using plamp.Abstractions.Symbols.SymTable;
using plamp.Alternative.SymbolsBuildingImpl;
using plamp.Alternative.Visitors.ModulePreCreation;
using plamp.Alternative.Visitors.ModulePreCreation.TypeInference;
using plamp.Alternative.Visitors.SymbolTableBuilding.FuncDefInference;
using plamp.Alternative.Visitors.SymbolTableBuilding.ModuleName;
using plamp.Alternative.Visitors.SymbolTableBuilding.TypedefInference;
using Shouldly;
using Xunit;

namespace plamp.Alternative.Tests.Visitors.ModulePreCreation.TypeInference;

public class FuncCallTypeInferenceTests
{
    /// <summary>
    /// Вызов void-функции без аргументов корректен
    /// </summary>
    [Fact]
    public void CallVoid_ReturnsCorrect()
    {
        const string code = """
                            module test;
                            fn a() {}
                            fn main() {
                                a();
                            }
                            """;

        var (ast, context) = Setup(code);
        context = new TypeInferenceWeaver().WeaveDiffs(ast, context);

        context.Exceptions.ShouldBeEmpty();
        var root = ast.ShouldBeOfType<RootNode>();
        var main = root.Functions.Single(x => x.FuncName.Value == "main");
        var call = main.Body.ExpressionList.ShouldHaveSingleItem().ShouldBeOfType<CallNode>();
        call.FnInfo.ShouldNotBeNull().ReturnType.ShouldBe(Builtins.Void);
    }

    /// <summary>
    /// Вызов функции возвращает её тип результата
    /// </summary>
    [Fact]
    public void CallRetType_ReturnsCorrect()
    {
        const string code = """
                            module test;
                            fn a() int {}
                            fn main() {
                                result := a();
                            }
                            """;

        var (ast, context) = Setup(code);
        context = new TypeInferenceWeaver().WeaveDiffs(ast, context);

        context.Exceptions.ShouldBeEmpty();
        var root = ast.ShouldBeOfType<RootNode>();
        var main = root.Functions.Single(x => x.FuncName.Value == "main");
        var assign = main.Body.ExpressionList.ShouldHaveSingleItem().ShouldBeOfType<AssignNode>();
        assign.Targets.ShouldHaveSingleItem().ShouldBeOfType<VariableDefinitionNode>()
            .Type.ShouldNotBeNull().TypeInfo.ShouldBe(Builtins.Int);
        var call = assign.Sources.ShouldHaveSingleItem().ShouldBeOfType<CallNode>();
        call.FnInfo.ShouldNotBeNull().ReturnType.ShouldBe(Builtins.Int);
    }

    /// <summary>
    /// Вызов функции с аргументами корректен при совпадении типов
    /// </summary>
    [Fact]
    public void CallWithArgs_ReturnsCorrect()
    {
        const string code = """
                            module test;
                            fn a(f: int, s: string) {}
                            fn main() {
                                a(1, "hi");
                            }
                            """;

        var (ast, context) = Setup(code);
        context = new TypeInferenceWeaver().WeaveDiffs(ast, context);

        context.Exceptions.ShouldBeEmpty();
        var root = ast.ShouldBeOfType<RootNode>();
        var main = root.Functions.Single(x => x.FuncName.Value == "main");
        var call = main.Body.ExpressionList.ShouldHaveSingleItem().ShouldBeOfType<CallNode>();
        call.FnInfo.ShouldNotBeNull().ReturnType.ShouldBe(Builtins.Void);
    }

    /// <summary>
    /// Результат void-функции нельзя присваивать
    /// </summary>
    [Fact]
    public void AssignCallVoid_ReturnsException()
    {
        const string code = """
                            module test;
                            fn a() {}
                            fn main() {
                                b := a();
                            }
                            """;

        var (ast, context) = Setup(code);
        context = new TypeInferenceWeaver().WeaveDiffs(ast, context);

        context.Exceptions.ShouldHaveSingleItem().Code.ShouldBe(PlampExceptionInfo.CannotAssignNone().Code);
    }

    /// <summary>
    /// Неизвестный аргумент прерывает проверку сигнатуры вызова
    /// </summary>
    [Fact]
    public void CallNotFullArgs_ReturnExceptionFuncFunc()
    {
        const string code = """
                            fn a(first: int, second: string){}
                            
                            fn main(){
                                a(c, "hi");
                            }
                            """;

        var (ast, ctx) = Setup(code);

        var visitor = new TypeInferenceWeaver();
        var result = visitor.WeaveDiffs(ast, ctx);
        var ex = result.Exceptions.ShouldHaveSingleItem();
        ex.Code.ShouldBe(PlampExceptionInfo.CannotFindMember().Code);
    }

    /// <summary>
    /// Аргумент типа any принимает любое не-void значение
    /// </summary>
    [Fact]
    public void CallWithFunctionWithAnyTypeArgument_Correct()
    {
        const string code = """
                            module test;
                            fn mock(first: any) {}
                            fn main() {
                                mock(1);
                            }
                            """;

        var (ast, context) = Setup(code);
        context = new TypeInferenceWeaver().WeaveDiffs(ast, context);

        context.Exceptions.ShouldBeEmpty();
        var root = ast.ShouldBeOfType<RootNode>();
        var main = root.Functions.Single(x => x.FuncName.Value == "main");
        main.Body.ExpressionList.ShouldHaveSingleItem().ShouldBeOfType<CallNode>()
            .FnInfo.ShouldNotBeNull();
    }

    /// <summary>
    /// Числовой аргумент может быть расширен до ожидаемого типа
    /// </summary>
    [Fact]
    public void CallWithExpandableType_Correct()
    {
        const string code = """
                            module test;
                            fn mock(first: long) {}
                            fn main() {
                                mock(1i);
                            }
                            """;

        var (ast, context) = Setup(code);
        context = new TypeInferenceWeaver().WeaveDiffs(ast, context);

        context.Exceptions.ShouldBeEmpty();
        var root = ast.ShouldBeOfType<RootNode>();
        var main = root.Functions.Single(x => x.FuncName.Value == "main");
        main.Body.ExpressionList.ShouldHaveSingleItem().ShouldBeOfType<CallNode>()
            .FnInfo.ShouldNotBeNull();
    }

    /// <summary>
    /// Несовместимый аргумент функции даёт ошибку
    /// </summary>
    [Fact]
    public void CallWithIncompatibleType_ReturnsException()
    {
        const string code = """
                            module test;
                            fn Ex(str: string) string {
                                return str;
                            }

                            fn main() {
                                Ex(1);
                            }
                            """;


        var (ast, context) = Setup(code);

        var visitor = new TypeInferenceWeaver();
        context = visitor.WeaveDiffs(ast, context);

        var ex = context.Exceptions.ShouldHaveSingleItem();
        ex.Code.ShouldBe(PlampExceptionInfo.CannotApplyArgument().Code);
    }

    /// <summary>
    /// Число аргументов вызова должно совпадать с сигнатурой функции
    /// </summary>
    [Fact]
    public void CallWithDifferentArgumentCount_ReturnsException()
    {
        const string code = """
                            module test;
                            fn target(first: int, second: int) {}
                            fn main() {
                                target(1);
                            }
                            """;

        var (ast, context) = Setup(code);
        context = new TypeInferenceWeaver().WeaveDiffs(ast, context);

        context.Exceptions.ShouldHaveSingleItem().Code.ShouldBe(PlampExceptionInfo.FunctionHasDifferentArgCount(2, 1).Code);
    }

    /// <summary>
    /// Явных generic-аргументов должно быть столько же, сколько параметров у функции
    /// </summary>
    [Fact]
    public void ExplicitGenericCallWithDifferentGenericArgumentCount_ReturnsException()
    {
        const string code = """
                            module test;
                            fn id[T](value: T) T {}
                            fn main() {
                                id[int, string](1);
                            }
                            """;

        var (ast, context) = Setup(code);
        context = new TypeInferenceWeaver().WeaveDiffs(ast, context);

        context.Exceptions.ShouldHaveSingleItem().Code.ShouldBe(PlampExceptionInfo.GenericFuncDefinitionHasDifferentParameterCount(1, 2).Code);
    }

    /// <summary>
    /// Ненайденный явный generic-аргумент не запускает ошибки вывода функции
    /// </summary>
    [Fact]
    public void ExplicitGenericCallWithUnresolvedGenericArgument_HasNoFuncInferenceErrors()
    {
        const string code = """
                            module test;
                            fn id[T](value: T) T {}
                            fn main() {
                                id[Unknown](1);
                            }
                            """;

        var (ast, context) = Setup(code);
        context = new TypeInferenceWeaver().WeaveDiffs(ast, context);

        var codeShould = context.Exceptions.ShouldHaveSingleItem().Code;
        codeShould.ShouldBe(PlampExceptionInfo.TypeIsNotFound("Unknown").Code);
    }

    /// <summary>
    /// Явная generic-реализация должна принимать аргументы подходящих типов
    /// </summary>
    [Fact]
    public void ExplicitGenericCallWithIncompatibleArgument_ReturnsCannotApplyArgument()
    {
        const string code = """
                            module test;
                            fn id[T](value: T) T {}
                            fn main() {
                                id[string](1);
                            }
                            """;

        var (ast, context) = Setup(code);
        context = new TypeInferenceWeaver().WeaveDiffs(ast, context);

        context.Exceptions.ShouldHaveSingleItem().Code.ShouldBe(PlampExceptionInfo.CannotApplyArgument().Code);
    }

    /// <summary>
    /// Явная generic-реализация корректно выводит функцию и тип результата
    /// </summary>
    [Fact]
    public void ExplicitGenericCallCorrectScenario_ReturnsImplementedFunc()
    {
        const string code = """
                            module test;
                            fn id[T](value: T) T {}
                            fn main() {
                                result := id[int](1);
                            }
                            """;

        var (ast, context) = Setup(code);
        context = new TypeInferenceWeaver().WeaveDiffs(ast, context);

        context.Exceptions.ShouldBeEmpty();
        var root = ast.ShouldBeOfType<RootNode>();
        var main = root.Functions.Single(x => x.FuncName.Value == "main");
        var assign = main.Body.ExpressionList.ShouldHaveSingleItem().ShouldBeOfType<AssignNode>();
        var variable = assign.Targets.ShouldHaveSingleItem().ShouldBeOfType<VariableDefinitionNode>();
        variable.Type.ShouldNotBeNull().TypeInfo.ShouldBe(Builtins.Int);
        var call = assign.Sources.ShouldHaveSingleItem().ShouldBeOfType<CallNode>();
        call.FnInfo.ShouldNotBeNull().ReturnType.ShouldBe(Builtins.Int);
    }

    /// <summary>
    /// Неявный generic-вызов проверяет применимость аргумента к параметру
    /// </summary>
    [Fact]
    public void ImplicitGenericCallWithInapplicableArgument_ReturnsCannotApplyArgument()
    {
        const string code = """
                            module test;
                            fn firstArrayItem[T](items: []T) T {}
                            fn main() {
                                firstArrayItem(1);
                            }
                            """;

        var (ast, context) = Setup(code);
        context = new TypeInferenceWeaver().WeaveDiffs(ast, context);

        context.Exceptions.Select(x => x.Code).ShouldContain(PlampExceptionInfo.CannotApplyArgument().Code);
    }

    /// <summary>
    /// Один аргумент не может давать две реализации одного generic-параметра
    /// </summary>
    [Fact]
    public void ImplicitGenericCallWithManyImplementationsInOneArgument_ReturnsException()
    {
        const string code = """
                            module test;
                            type Pair[TKey, TValue];
                            fn samePair[T](value: Pair[T, T]) {}
                            fn main() {
                                samePair(Pair[int, string]{});
                            }
                            """;

        var (ast, context) = Setup(code);
        context = new TypeInferenceWeaver().WeaveDiffs(ast, context);

        context.Exceptions.Select(x => x.Code)
            .ShouldContain(PlampExceptionInfo.GenericFunctionParameterCannotHasManyImplementations("T", [Builtins.Int.Name, Builtins.String.Name]).Code);
    }

    /// <summary>
    /// Несколько аргументов не могут давать разные реализации одного generic-параметра
    /// </summary>
    [Fact]
    public void ImplicitGenericCallWithManyImplementationsAcrossArguments_ReturnsException()
    {
        const string code = """
                            module test;
                            fn same[T](first: T, second: T) {}
                            fn main() {
                                same(1, "text");
                            }
                            """;

        var (ast, context) = Setup(code);
        context = new TypeInferenceWeaver().WeaveDiffs(ast, context);
        
        var codes = context.Exceptions.Select(x => x.Code).ToList();
        codes.Count().ShouldBe(2);
        codes.ShouldContain(PlampExceptionInfo.GenericFunctionParameterCannotHasManyImplementations("T", [Builtins.Int.Name, Builtins.String.Name]).Code);
        codes.ShouldContain(PlampExceptionInfo.GenericParameterHasNoImplementationType("T").Code);
    }

    /// <summary>
    /// Generic-параметр должен иметь тип реализации
    /// </summary>
    [Fact]
    public void ImplicitGenericCallWithoutImplementationType_ReturnsException()
    {
        const string code = """
                            module test;
                            fn make[T]() T {}
                            fn main() {
                                make();
                            }
                            """;

        var (ast, context) = Setup(code);
        context = new TypeInferenceWeaver().WeaveDiffs(ast, context);

        context.Exceptions.ShouldHaveSingleItem().Code.ShouldBe(PlampExceptionInfo.GenericParameterHasNoImplementationType("T").Code);
    }

    /// <summary>
    /// Неявный generic-вызов корректно выводит generic-аргументы
    /// </summary>
    [Fact]
    public void ImplicitGenericCallCorrectScenario_ReturnsImplementedFunc()
    {
        const string code = """
                            module test;
                            fn id[T](value: T) T {}
                            fn main() {
                                result := id(1);
                            }
                            """;

        var (ast, context) = Setup(code);
        context = new TypeInferenceWeaver().WeaveDiffs(ast, context);

        context.Exceptions.ShouldBeEmpty();
        var root = ast.ShouldBeOfType<RootNode>();
        var main = root.Functions.Single(x => x.FuncName.Value == "main");
        var assign = main.Body.ExpressionList.ShouldHaveSingleItem().ShouldBeOfType<AssignNode>();
        var variable = assign.Targets.ShouldHaveSingleItem().ShouldBeOfType<VariableDefinitionNode>();
        variable.Type.ShouldNotBeNull().TypeInfo.ShouldBe(Builtins.Int);
        var call = assign.Sources.ShouldHaveSingleItem().ShouldBeOfType<CallNode>();
        call.FnInfo.ShouldNotBeNull().GetGenericArguments().ShouldHaveSingleItem().ShouldBe(Builtins.Int);
    }

    /// <summary>
    /// Не generic-функция проверяет соответствие типов аргументов
    /// </summary>
    [Fact]
    public void NonGenericCallWithIncompatibleArgument_ReturnsException()
    {
        const string code = """
                            module test;
                            fn expectsString(value: string) {}
                            fn main() {
                                expectsString(1);
                            }
                            """;

        var (ast, context) = Setup(code);
        context = new TypeInferenceWeaver().WeaveDiffs(ast, context);

        context.Exceptions.ShouldHaveSingleItem().Code.ShouldBe(PlampExceptionInfo.CannotApplyArgument().Code);
    }

    /// <summary>
    /// Ошибка явных generic-аргументов не мешает вывести возвращаемый тип
    /// </summary>
    [Fact]
    public void ExplicitGenericCallError_AllowsFurtherTypeInference()
    {
        const string code = """
                            module test;
                            fn id[T](value: T) T {}
                            fn main() {
                                result := id[string](1);
                            }
                            """;

        var (ast, context) = Setup(code);
        context = new TypeInferenceWeaver().WeaveDiffs(ast, context);

        context.Exceptions.ShouldHaveSingleItem().Code.ShouldBe(PlampExceptionInfo.CannotApplyArgument().Code);
        var root = ast.ShouldBeOfType<RootNode>();
        var main = root.Functions.Single(x => x.FuncName.Value == "main");
        var assign = main.Body.ExpressionList.ShouldHaveSingleItem().ShouldBeOfType<AssignNode>();
        assign.Targets.ShouldHaveSingleItem().ShouldBeOfType<VariableDefinitionNode>()
            .Type.ShouldNotBeNull().TypeInfo.ShouldBe(Builtins.String);
    }

    /// <summary>
    /// Ошибка в не generic-функции не мешает вывести возвращаемый тип
    /// </summary>
    [Fact]
    public void NonGenericCallError_AllowsFurtherTypeInference()
    {
        const string code = """
                            module test;
                            fn expectsString(value: string) int {}
                            fn main() {
                                result := expectsString(1);
                            }
                            """;

        var (ast, context) = Setup(code);
        context = new TypeInferenceWeaver().WeaveDiffs(ast, context);

        context.Exceptions.ShouldHaveSingleItem().Code.ShouldBe(PlampExceptionInfo.CannotApplyArgument().Code);
        var root = ast.ShouldBeOfType<RootNode>();
        var main = root.Functions.Single(x => x.FuncName.Value == "main");
        var assign = main.Body.ExpressionList.ShouldHaveSingleItem().ShouldBeOfType<AssignNode>();
        assign.Targets.ShouldHaveSingleItem().ShouldBeOfType<VariableDefinitionNode>()
            .Type.ShouldNotBeNull().TypeInfo.ShouldBe(Builtins.Int);
    }

    /// <summary>
    /// Ошибка в неявной generic-реализации не мешает вывести возвращаемый тип, если он построен
    /// </summary>
    [Fact]
    public void ImplicitGenericCallErrorWithKnownReturnType_AllowsFurtherTypeInference()
    {
        const string code = """
                            module test;
                            fn choose[T](value: T, text: string) T {}
                            fn main() {
                                result := choose(1, 2);
                            }
                            """;

        var (ast, context) = Setup(code);
        context = new TypeInferenceWeaver().WeaveDiffs(ast, context);

        context.Exceptions.ShouldHaveSingleItem().Code.ShouldBe(PlampExceptionInfo.CannotApplyArgument().Code);
        var root = ast.ShouldBeOfType<RootNode>();
        var main = root.Functions.Single(x => x.FuncName.Value == "main");
        var assign = main.Body.ExpressionList.ShouldHaveSingleItem().ShouldBeOfType<AssignNode>();
        assign.Targets.ShouldHaveSingleItem().ShouldBeOfType<VariableDefinitionNode>()
            .Type.ShouldNotBeNull().TypeInfo.ShouldBe(Builtins.Int);
    }

    private (NodeBase ast, PreCreationContext context) Setup(string code)
    {
        var (ctx, ast) = CompilationPipelineBuilder.RunSymTableVisitors(code,
        [
            (ast, ctx) => new ModuleNameValidator().Validate(ast, ctx),
            (ast, ctx) => new TypedefInferenceWeaver().WeaveDiffs(ast, ctx),
            (ast, ctx) => new FuncDefInferenceWeaver().WeaveDiffs(ast, ctx)
        ]);

        var deps = new List<ISymTable>(ctx.Dependencies) { (SymTableBuilder)ctx.SymTableBuilder };
        var context = new PreCreationContext(ctx.TranslationTable, deps);
        return (ast, context);
    }
    
}
