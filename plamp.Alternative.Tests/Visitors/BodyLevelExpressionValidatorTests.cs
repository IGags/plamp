using Moq;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.Ast.Node.Unary;
using plamp.Alternative.Visitors.ModulePreCreation;
using plamp.Alternative.Visitors.ModulePreCreation.BodyLevelExpression;
using Shouldly;
using Xunit;

namespace plamp.Alternative.Tests.Visitors;

/// <summary>
/// Тесты валидации выражений внутри тела функции
/// </summary>
public class BodyLevelExpressionValidatorTests
{
    /// <summary>
    /// Проверяет, что постфиксный инкремент разрешён
    /// </summary>
    [Fact]
    public void ValidatePostfixIncrementInstruction_ReturnsNoExceptions()
    {
        var translationTable = new Mock<ITranslationTable>();
        translationTable
            .Setup(x => x.SetExceptionToNode(It.IsAny<NodeBase>(), It.IsAny<PlampExceptionRecord>()))
            .Returns<NodeBase, PlampExceptionRecord>((_, record) => new PlampException(record, default));
        
        var body = new BodyNode([new PostfixIncrementNode(new MemberNode("a"))]);
        var context = new PreCreationContext(translationTable.Object, SymbolTableInitHelper.CreateDefaultTables());
        var validator = new BodyLevelExpressionValidator();
        
        var result = validator.Validate(body, context);
        
        result.Exceptions.ShouldBeEmpty();
    }
}
