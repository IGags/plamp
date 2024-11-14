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
        new("Invalid cast operator", 1002, ExceptionLevel.Error);

    public static PlampNativeExceptionFinalRecord InvalidCastOperator() => InvalidCastOperatorRecord.Format();

    private static readonly PlampNativeExceptionRecord ExpectedExpressionRecord =
        new("Expected expression", 1003, ExceptionLevel.Error);

    public static PlampNativeExceptionFinalRecord ExpectedExpression() => ExpectedExpressionRecord.Format();

    #endregion
}