using System.Linq;
using plamp.Alternative;
using plamp.EndToEnd.Tests.Infrastructure;
using Shouldly;

namespace plamp.EndToEnd.Tests.Tests;

/// <summary>
/// Тесты диагностики невалидного синтаксиса
/// </summary>
public class SyntaxDiagnosticsTests
{
    private const string InvalidSyntaxFolder = "InvalidSyntaxFiles";

    /// <summary>
    /// Набор невалидных файлов с их ошибками
    /// </summary>
    public static TheoryData<string, string> InvalidPrograms => new()
    {
        { InvalidFile("InvalidCall.plp"), PlampExceptionInfo.CannotApplyArgument().Code },
        { InvalidFile("InvalidMissingTypeName.plp"), PlampExceptionInfo.ExpectedTypeName().Code },
        { InvalidFile("InvalidStructureField.plp"), PlampExceptionInfo.ExpectedFieldTypeQualifier().Code },
        { InvalidFile("InvalidGenericTypeDeclaration.plp"), PlampExceptionInfo.EmptyGenericDefinition().Code },
        { InvalidFile("InvalidGenericFunctionDeclaration.plp"), PlampExceptionInfo.EmptyGenericDefinition().Code },
        { InvalidFile("InvalidChaoticTopLevel.plp"), PlampExceptionInfo.TopLevelExpressionExpected().Code },
        { InvalidFile("InvalidMalformedNumber.plp"), PlampExceptionInfo.UnknownNumberFormat().Code },
        { InvalidFile("InvalidFloatArgumentForInt.plp"), PlampExceptionInfo.CannotApplyArgument().Code },
        { InvalidFile("InvalidDoubleArgumentForInt.plp"), PlampExceptionInfo.CannotApplyArgument().Code },
        { InvalidFile("InvalidBreakOutsideLoop.plp"), PlampExceptionInfo.CannotUseControlFlowNotInLoop().Code },
        { InvalidFile("InvalidContinueOutsideLoop.plp"), PlampExceptionInfo.CannotUseControlFlowNotInLoop().Code },
        { InvalidFile("InvalidMissingReturn.plp"), PlampExceptionInfo.FuncMustReturnValue().Code },
        { InvalidFile("InvalidReturnValueFromVoid.plp"), PlampExceptionInfo.CannotReturnValue().Code },
        { InvalidFile("InvalidReturnWithoutValue.plp"), PlampExceptionInfo.ReturnValueIsMissing().Code },
        { InvalidFile("InvalidReturnTypeMismatch.plp"), PlampExceptionInfo.ReturnTypeMismatch().Code },
        { InvalidFile("InvalidDuplicateVariable.plp"), PlampExceptionInfo.DuplicateVariableDefinition().Code },
        { InvalidFile("InvalidBuiltinTypeInit.plp"), PlampExceptionInfo.CannotInitBuiltinType().Code },
        { InvalidFile("InvalidGenericManyImplementations.plp"), PlampExceptionInfo.GenericFunctionParameterCannotHasManyImplementations("T", [Builtins.Int.Name, Builtins.String.Name]).Code },
        { InvalidFile("InvalidGenericMissingImplementation.plp"), PlampExceptionInfo.GenericParameterHasNoImplementationType("T").Code },
        { InvalidFile("InvalidGenericArgumentCount.plp"), PlampExceptionInfo.GenericFuncDefinitionHasDifferentParameterCount(1, 2).Code },
        { InvalidFile("InvalidTypeInitializerMissingFieldValue.plp"), PlampExceptionInfo.ExpectedFieldValue().Code },
        { InvalidFile("InvalidTypeInitializerUnknownField.plp"), PlampExceptionInfo.FieldIsNotFound().Code },
        { InvalidFile("InvalidTypeInitializerFieldType.plp"), PlampExceptionInfo.CannotAssign().Code },
        { InvalidFile("InvalidArrayLongLiteralIndex.plp"), PlampExceptionInfo.IndexerValueMustBeInteger().Code },
        { InvalidFile("InvalidDuplicateParameter.plp"), PlampExceptionInfo.DuplicateParameterName().Code },
        { InvalidFile("InvalidDuplicateField.plp"), PlampExceptionInfo.DuplicateFieldDefinition("value").Code },
        { InvalidFile("InvalidFieldBuiltinName.plp"), PlampExceptionInfo.FieldHasSameNameAsBuiltinMember().Code },
        { InvalidFile("InvalidCoreTypeDefinition.plp"), PlampExceptionInfo.CannotDefineCoreType().Code },
        { InvalidFile("InvalidCoreFunctionDefinition.plp"), PlampExceptionInfo.CannotDefineCoreFunction().Code },
        { InvalidFile("InvalidDuplicateGenericTypeParameter.plp"), PlampExceptionInfo.DuplicateGenericParameterName().Code },
        { InvalidFile("InvalidGenericTypeParameterSameAsType.plp"), PlampExceptionInfo.GenericParameterNameSameAsDefiningType().Code },
        { InvalidFile("InvalidGenericFunctionParameterSameAsFunction.plp"), PlampExceptionInfo.GenericParamSameNameAsDefiningFunction().Code },
        { InvalidFile("InvalidGenericParameterBuiltinName.plp"), PlampExceptionInfo.GenericParameterHasSameNameAsBuiltinMember().Code },
        { InvalidFile("InvalidIfPredicateInt.plp"), PlampExceptionInfo.PredicateMustBeBooleanType().Code },
        { InvalidFile("InvalidWhilePredicateInt.plp"), PlampExceptionInfo.PredicateMustBeBooleanType().Code },
        { InvalidFile("InvalidArrayLengthDouble.plp"), PlampExceptionInfo.ArrayLengthMustBeInteger().Code },
        { InvalidFile("InvalidIndexerOnInt.plp"), PlampExceptionInfo.IndexerIsNotApplicable().Code },
        { InvalidFile("InvalidAssignCountMismatch.plp"), PlampExceptionInfo.AssignSourceAndTargetCountMismatch().Code },
        { InvalidFile("InvalidCallArgumentCount.plp"), PlampExceptionInfo.FunctionHasDifferentArgCount(2, 1).Code },
        { InvalidFile("InvalidUnclosedMultiLineComment.plp"), PlampExceptionInfo.CommentIsNotClosed().Code },
        { InvalidFile("InvalidUnclosedChar.plp"), PlampExceptionInfo.ExpectedEndOfStatement().Code },
        { InvalidFile("InvalidEmptyChar.plp"), PlampExceptionInfo.InvalidCharLiteral().Code },
        { InvalidFile("InvalidLongChar.plp"), PlampExceptionInfo.InvalidCharLiteral().Code },
        { InvalidFile("InvalidCharEscape.plp"), PlampExceptionInfo.InvalidEscapeSequence("\\x").Code },
        { InvalidFile("InvalidCommentThenStringTail.plp"), PlampExceptionInfo.ExpectedEndOfStatement().Code },
        { InvalidFile("InvalidStringIntConcat.plp"), PlampExceptionInfo.CannotApplyOperator().Code },
        { InvalidFile("InvalidCharIntConcat.plp"), PlampExceptionInfo.CannotApplyOperator().Code },
        { InvalidFile("InvalidStringBoolConcat.plp"), PlampExceptionInfo.CannotApplyOperator().Code },
        { InvalidFile("InvalidCharCharAddition.plp"), PlampExceptionInfo.CannotApplyOperator().Code },
        { InvalidFile("InvalidStringAnyConcat.plp"), PlampExceptionInfo.CannotApplyOperator().Code },
        { InvalidFile("InvalidAnyStringConcat.plp"), PlampExceptionInfo.CannotApplyOperator().Code },
        { InvalidFile("InvalidAssignAnyToInt.plp"), PlampExceptionInfo.CannotAssign().Code },
        { InvalidFile("InvalidCharStringComparison.plp"), PlampExceptionInfo.CannotApplyOperator().Code },
        { InvalidFile("InvalidDirectTypeSelfReference.plp"), PlampExceptionInfo.FieldProduceCircularDependency().Code },
        { InvalidFile("InvalidMutualTypeReference.plp"), PlampExceptionInfo.FieldProduceCircularDependency().Code },
        { InvalidFile("InvalidGenericTypeSelfReference.plp"), PlampExceptionInfo.FieldProduceCircularDependency().Code },
        { InvalidFile("InvalidArrayTypeSelfReference.plp"), PlampExceptionInfo.FieldProduceCircularDependency().Code },
        { InvalidFile("NoReturnAfterIf.plp"), PlampExceptionInfo.FuncMustReturnValue().Code },
        { InvalidFile("NoReturnAfterWhile.plp"), PlampExceptionInfo.FuncMustReturnValue().Code },
        { InvalidFile("ReturnInOnlyOneBranch.plp"), PlampExceptionInfo.FuncMustReturnValue().Code }
    };

    /// <summary>
    /// Проверяет, что некорректный вызов функции с аргументом неподходящего типа возвращает ошибку
    /// </summary>
    [Fact]
    public async Task ReturnsTypeInferenceErrors()
    {
        var diagnostics = await PlampE2ERunner.GetDiagnosticsAsync(InvalidFile("InvalidCall.plp"));

        diagnostics.ShouldNotBeEmpty();
        diagnostics.Select(x => x.Code)
            .ShouldContain(PlampExceptionInfo.CannotApplyArgument().Code);
    }

    /// <summary>
    /// Проверяет, что некорректный синтаксис выкидывает ожидаемую ошибку
    /// </summary>
    [Theory]
    [MemberData(nameof(InvalidPrograms))]
    public async Task ReturnsPlampDiagnosticsInsteadOfClrException(
        string sourceFile,
        string expectedCode)
    {
        var diagnostics = await PlampE2ERunner.GetDiagnosticsAsync(sourceFile);

        diagnostics.ShouldNotBeEmpty();
        diagnostics.Select(x => x.Code).ShouldContain(expectedCode);
    }

    private static string InvalidFile(string fileName) => Path.Combine(InvalidSyntaxFolder, fileName);
}
