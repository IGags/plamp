using plamp.Abstractions.Ast;

namespace plamp.Native;

public static class PlampNativeExceptionInfo
{
    #region Tokenization(1000-1099)

    private static readonly PlampNativeExceptionRecord UnexpectedTokenRecord =
        new("Unexpected token \"{0}\"", "TOK1000", ExceptionLevel.Error);

    public static PlampExceptionRecord UnexpectedToken(string stringToken) =>
        UnexpectedTokenRecord.Format(stringToken);
    
    private static readonly PlampNativeExceptionRecord StringIsNotClosedRecord =
        new("String is not closed", "TOK1030", ExceptionLevel.Error);
    
    public static PlampExceptionRecord StringIsNotClosed() => StringIsNotClosedRecord.Format();

    private static readonly PlampNativeExceptionRecord InvalidEscapeSequenceRecord =
        new("Invalid escape sequence \"{0}\"", "TOK1031", ExceptionLevel.Error);

    public static PlampExceptionRecord InvalidEscapeSequence(string sequence) =>
        InvalidEscapeSequenceRecord.Format(sequence);

    private static readonly PlampNativeExceptionRecord UnknownNumberFormatRecord =
        new("Unknown number format", "TOK1032", ExceptionLevel.Error);

    public static PlampExceptionRecord UnknownNumberFormat => UnknownNumberFormatRecord.Format();

    #endregion

    #region PRSing(1100-1299)

    private static readonly PlampNativeExceptionRecord ExpectedIdentifierRecord =
        new("Expected identifier", "PRS1100", ExceptionLevel.Error);

    public static PlampExceptionRecord ExpectedIdentifier() => ExpectedIdentifierRecord.Format();

    private static readonly PlampNativeExceptionRecord ExpectedRecord =
        new("Expected {0}", "PRS1101", ExceptionLevel.Error);

    public static PlampExceptionRecord Expected(string value) => ExpectedRecord.Format(value);

    private static readonly PlampNativeExceptionRecord InvalidCastOperatorRecord =
        new("Invalid cast operator", "PRS1102", ExceptionLevel.Error);

    public static PlampExceptionRecord InvalidCastOperator() => InvalidCastOperatorRecord.Format();

    private static readonly PlampNativeExceptionRecord ExpectedExpressionRecord =
        new("Expected expression", "PRS1103", ExceptionLevel.Error);

    public static PlampExceptionRecord ExpectedExpression() => ExpectedExpressionRecord.Format();

    private static readonly PlampNativeExceptionRecord ExpectedMemberNameRecord =
        new("Expected member name", "PRS1104", ExceptionLevel.Error);

    public static PlampExceptionRecord ExpectedMemberName() => ExpectedMemberNameRecord.Format();

    private static readonly PlampNativeExceptionRecord InvalidForHeaderRecord =
        new("Invalid for header", "PRS1105", ExceptionLevel.Error);

    public static PlampExceptionRecord InvalidForHeader() => InvalidForHeaderRecord.Format();

    private static readonly PlampNativeExceptionRecord InvalidTypeNameDefinition =
        new("Invalid type name definition", "PRS1106", ExceptionLevel.Error);
    
    public static PlampExceptionRecord InvalidTypeName() => InvalidTypeNameDefinition.Format();

    private static readonly PlampNativeExceptionRecord InvalidGenericDefinitionRecord =
        new ("Invalid generic definition", "PRS1107", ExceptionLevel.Error);
    
    public static PlampExceptionRecord InvalidGenericDefinition() => InvalidGenericDefinitionRecord.Format();
    
    private static readonly PlampNativeExceptionRecord InvalidParameterDefinitionRecord =
        new ("Invalid parameter definition", "PRS1108", ExceptionLevel.Error);
    
    public static PlampExceptionRecord InvalidParameterDefinition() 
        => InvalidParameterDefinitionRecord.Format();

    private static readonly PlampNativeExceptionRecord ExpectedArgDefinitionRecord =
        new("Expected arg definition", "PRS1109", ExceptionLevel.Error);

    public static PlampExceptionRecord ExpectedArgDefinition() => ExpectedArgDefinitionRecord.Format();

    private static readonly PlampNativeExceptionRecord InvalidUsingNameRecord =
        new("Invalid using name", "PRS1110", ExceptionLevel.Error);
    
    public static PlampExceptionRecord InvalidUsingName() => InvalidUsingNameRecord.Format();
    
    private static readonly PlampNativeExceptionRecord ParenExpressionIsNotClosedRecord = 
        new("Paren expression is not closed", "PRS1111", ExceptionLevel.Error);

    public static PlampExceptionRecord ParenExpressionIsNotClosed() 
        => ParenExpressionIsNotClosedRecord.Format();
    
    private static readonly PlampNativeExceptionRecord EmptyConditionPredicateRecord =
        new ("Empty condition predicate", "PRS1112", ExceptionLevel.Error);

    public static PlampExceptionRecord EmptyConditionPredicate()
        => EmptyConditionPredicateRecord.Format();
    
    private static readonly PlampNativeExceptionRecord MissingConditionPredicateRecord = 
        new ("Missing condition predicate", "PRS1113", ExceptionLevel.Error);
    
    public static PlampExceptionRecord MissingConditionPredicate() 
        => MissingConditionPredicateRecord.Format();
    
    private static readonly PlampNativeExceptionRecord InvalidBodyRecord =
        new ("Invalid body", "PRS1114", ExceptionLevel.Error);
    
    public static PlampExceptionRecord InvalidBody()
        => InvalidBodyRecord.Format();

    private static readonly PlampNativeExceptionRecord InvalidDefMissingReturnTypeRecord =
        new("Invalid def missing return type", "PRS1115", ExceptionLevel.Error);
    
    public static PlampExceptionRecord InvalidDefMissingReturnType()
        => InvalidDefMissingReturnTypeRecord.Format();
    
    private static readonly PlampNativeExceptionRecord InvalidDefMissingNameRecord =
        new("Invalid def missing name", "PRS1116", ExceptionLevel.Error);
    
    public static PlampExceptionRecord InvalidDefMissingName()
        => InvalidDefMissingNameRecord.Format();
    
    #endregion
}