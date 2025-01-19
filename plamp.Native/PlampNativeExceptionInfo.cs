namespace plamp.Native;

public static class PlampNativeExceptionInfo
{
    #region Tokenization(1000-1099)

    private static readonly PlampNativeExceptionRecord UnexpectedTokenRecord =
        new("Unexpected token \"{0}\"", 1000, ExceptionLevel.Error);

    public static PlampNativeExceptionFinalRecord UnexpectedToken(string stringToken) =>
        UnexpectedTokenRecord.Format(stringToken);
    
    private static readonly PlampNativeExceptionRecord StringIsNotClosedRecord =
        new("String is not closed", 1030, ExceptionLevel.Error);
    
    public static PlampNativeExceptionFinalRecord StringIsNotClosed() => StringIsNotClosedRecord.Format();

    private static readonly PlampNativeExceptionRecord InvalidEscapeSequenceRecord =
        new("Invalid escape sequence \"{0}\"", 1031, ExceptionLevel.Error);

    public static PlampNativeExceptionFinalRecord InvalidEscapeSequence(string sequence) =>
        InvalidEscapeSequenceRecord.Format(sequence);

    private static readonly PlampNativeExceptionRecord UnknownNumberFormatRecord =
        new("Unknown number format", 1032, ExceptionLevel.Error);

    public static PlampNativeExceptionFinalRecord UnknownNumberFormat => UnknownNumberFormatRecord.Format();

    #endregion

    #region Parsing(1100-1300)

    private static readonly PlampNativeExceptionRecord ExpectedIdentifierRecord =
        new("Expected identifier", 1100, ExceptionLevel.Error);

    public static PlampNativeExceptionFinalRecord ExpectedIdentifier() => ExpectedIdentifierRecord.Format();

    private static readonly PlampNativeExceptionRecord ExpectedRecord =
        new("Expected {0}", 1101, ExceptionLevel.Error);

    public static PlampNativeExceptionFinalRecord Expected(string value) => ExpectedRecord.Format(value);

    private static readonly PlampNativeExceptionRecord InvalidCastOperatorRecord =
        new("Invalid cast operator", 1102, ExceptionLevel.Error);

    public static PlampNativeExceptionFinalRecord InvalidCastOperator() => InvalidCastOperatorRecord.Format();

    private static readonly PlampNativeExceptionRecord ExpectedExpressionRecord =
        new("Expected expression", 1103, ExceptionLevel.Error);

    public static PlampNativeExceptionFinalRecord ExpectedExpression() => ExpectedExpressionRecord.Format();

    private static readonly PlampNativeExceptionRecord ExpectedMemberNameRecord =
        new("Expected member name", 1104, ExceptionLevel.Error);

    public static PlampNativeExceptionFinalRecord ExpectedMemberName() => ExpectedMemberNameRecord.Format();

    private static readonly PlampNativeExceptionRecord InvalidForHeaderRecord =
        new("Invalid for header", 1105, ExceptionLevel.Error);

    public static PlampNativeExceptionFinalRecord InvalidForHeader() => InvalidForHeaderRecord.Format();

    private static readonly PlampNativeExceptionRecord InvalidTypeNameDefinition =
        new("Invalid type name definition", 1106, ExceptionLevel.Error);
    
    public static PlampNativeExceptionFinalRecord InvalidTypeName() => InvalidTypeNameDefinition.Format();

    private static readonly PlampNativeExceptionRecord InvalidGenericDefinitionRecord =
        new ("Invalid generic definition", 1107, ExceptionLevel.Error);
    
    public static PlampNativeExceptionFinalRecord InvalidGenericDefinition() => InvalidGenericDefinitionRecord.Format();
    
    private static readonly PlampNativeExceptionRecord InvalidParameterDefinitionRecord =
        new ("Invalid parameter definition", 1108, ExceptionLevel.Error);
    
    public static PlampNativeExceptionFinalRecord InvalidParameterDefinition() 
        => InvalidParameterDefinitionRecord.Format();

    private static readonly PlampNativeExceptionRecord ExpectedArgDefinitionRecord =
        new("Expected arg definition", 1109, ExceptionLevel.Error);

    public static PlampNativeExceptionFinalRecord ExpectedArgDefinition() => ExpectedArgDefinitionRecord.Format();

    private static readonly PlampNativeExceptionRecord InvalidUsingNameRecord =
        new("Invalid using name", 1110, ExceptionLevel.Error);
    
    public static PlampNativeExceptionFinalRecord InvalidUsingName() => InvalidUsingNameRecord.Format();
    
    private static readonly PlampNativeExceptionRecord ParenExpressionIsNotClosedRecord = 
        new("Paren expression is not closed", 1111, ExceptionLevel.Error);

    public static PlampNativeExceptionFinalRecord ParenExpressionIsNotClosed() 
        => ParenExpressionIsNotClosedRecord.Format();
    
    private static readonly PlampNativeExceptionRecord EmptyConditionPredicateRecord =
        new ("Empty condition predicate", 1112, ExceptionLevel.Error);

    public static PlampNativeExceptionFinalRecord EmptyConditionPredicate()
        => EmptyConditionPredicateRecord.Format();
    
    private static readonly PlampNativeExceptionRecord MissingConditionPredicateRecord = 
        new ("Missing condition predicate", 1113, ExceptionLevel.Error);
    
    public static PlampNativeExceptionFinalRecord MissingConditionPredicate() 
        => MissingConditionPredicateRecord.Format();
    
    private static readonly PlampNativeExceptionRecord InvalidBodyRecord =
        new ("Invalid body", 1114, ExceptionLevel.Error);
    
    public static PlampNativeExceptionFinalRecord InvalidBody()
        => InvalidBodyRecord.Format();
    
    #endregion
}