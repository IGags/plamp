using plamp.Abstractions.Ast;

namespace plamp.Native;

public static class PlampNativeExceptionInfo
{
    #region Tokenization(1000-1099)

    private static readonly PlampNativeExceptionRecord UnexpectedTokenRecord =
        new("Unexpected token \"{0}\"", 1000, ExceptionLevel.Error);

    public static PlampExceptionRecord UnexpectedToken(string stringToken) =>
        UnexpectedTokenRecord.Format(stringToken);
    
    private static readonly PlampNativeExceptionRecord StringIsNotClosedRecord =
        new("String is not closed", 1030, ExceptionLevel.Error);
    
    public static PlampExceptionRecord StringIsNotClosed() => StringIsNotClosedRecord.Format();

    private static readonly PlampNativeExceptionRecord InvalidEscapeSequenceRecord =
        new("Invalid escape sequence \"{0}\"", 1031, ExceptionLevel.Error);

    public static PlampExceptionRecord InvalidEscapeSequence(string sequence) =>
        InvalidEscapeSequenceRecord.Format(sequence);

    private static readonly PlampNativeExceptionRecord UnknownNumberFormatRecord =
        new("Unknown number format", 1032, ExceptionLevel.Error);

    public static PlampExceptionRecord UnknownNumberFormat => UnknownNumberFormatRecord.Format();

    #endregion

    #region Parsing(1100-1299)

    private static readonly PlampNativeExceptionRecord ExpectedIdentifierRecord =
        new("Expected identifier", 1100, ExceptionLevel.Error);

    public static PlampExceptionRecord ExpectedIdentifier() => ExpectedIdentifierRecord.Format();

    private static readonly PlampNativeExceptionRecord ExpectedRecord =
        new("Expected {0}", 1101, ExceptionLevel.Error);

    public static PlampExceptionRecord Expected(string value) => ExpectedRecord.Format(value);

    private static readonly PlampNativeExceptionRecord InvalidCastOperatorRecord =
        new("Invalid cast operator", 1102, ExceptionLevel.Error);

    public static PlampExceptionRecord InvalidCastOperator() => InvalidCastOperatorRecord.Format();

    private static readonly PlampNativeExceptionRecord ExpectedExpressionRecord =
        new("Expected expression", 1103, ExceptionLevel.Error);

    public static PlampExceptionRecord ExpectedExpression() => ExpectedExpressionRecord.Format();

    private static readonly PlampNativeExceptionRecord ExpectedMemberNameRecord =
        new("Expected member name", 1104, ExceptionLevel.Error);

    public static PlampExceptionRecord ExpectedMemberName() => ExpectedMemberNameRecord.Format();

    private static readonly PlampNativeExceptionRecord InvalidForHeaderRecord =
        new("Invalid for header", 1105, ExceptionLevel.Error);

    public static PlampExceptionRecord InvalidForHeader() => InvalidForHeaderRecord.Format();

    private static readonly PlampNativeExceptionRecord InvalidTypeNameDefinition =
        new("Invalid type name definition", 1106, ExceptionLevel.Error);
    
    public static PlampExceptionRecord InvalidTypeName() => InvalidTypeNameDefinition.Format();

    private static readonly PlampNativeExceptionRecord InvalidGenericDefinitionRecord =
        new ("Invalid generic definition", 1107, ExceptionLevel.Error);
    
    public static PlampExceptionRecord InvalidGenericDefinition() => InvalidGenericDefinitionRecord.Format();
    
    private static readonly PlampNativeExceptionRecord InvalidParameterDefinitionRecord =
        new ("Invalid parameter definition", 1108, ExceptionLevel.Error);
    
    public static PlampExceptionRecord InvalidParameterDefinition() 
        => InvalidParameterDefinitionRecord.Format();

    private static readonly PlampNativeExceptionRecord ExpectedArgDefinitionRecord =
        new("Expected arg definition", 1109, ExceptionLevel.Error);

    public static PlampExceptionRecord ExpectedArgDefinition() => ExpectedArgDefinitionRecord.Format();

    private static readonly PlampNativeExceptionRecord InvalidUsingNameRecord =
        new("Invalid using name", 1110, ExceptionLevel.Error);
    
    public static PlampExceptionRecord InvalidUsingName() => InvalidUsingNameRecord.Format();
    
    private static readonly PlampNativeExceptionRecord ParenExpressionIsNotClosedRecord = 
        new("Paren expression is not closed", 1111, ExceptionLevel.Error);

    public static PlampExceptionRecord ParenExpressionIsNotClosed() 
        => ParenExpressionIsNotClosedRecord.Format();
    
    private static readonly PlampNativeExceptionRecord EmptyConditionPredicateRecord =
        new ("Empty condition predicate", 1112, ExceptionLevel.Error);

    public static PlampExceptionRecord EmptyConditionPredicate()
        => EmptyConditionPredicateRecord.Format();
    
    private static readonly PlampNativeExceptionRecord MissingConditionPredicateRecord = 
        new ("Missing condition predicate", 1113, ExceptionLevel.Error);
    
    public static PlampExceptionRecord MissingConditionPredicate() 
        => MissingConditionPredicateRecord.Format();
    
    private static readonly PlampNativeExceptionRecord InvalidBodyRecord =
        new ("Invalid body", 1114, ExceptionLevel.Error);
    
    public static PlampExceptionRecord InvalidBody()
        => InvalidBodyRecord.Format();

    private static readonly PlampNativeExceptionRecord InvalidDefMissingReturnTypeRecord =
        new("Invalid def missing return type", 1115, ExceptionLevel.Error);
    
    public static PlampExceptionRecord InvalidDefMissingReturnType()
        => InvalidDefMissingReturnTypeRecord.Format();
    
    private static readonly PlampNativeExceptionRecord InvalidDefMissingNameRecord =
        new("Invalid def missing name", 1116, ExceptionLevel.Error);
    
    public static PlampExceptionRecord InvalidDefMissingName()
        => InvalidDefMissingNameRecord.Format();
    
    #endregion
}