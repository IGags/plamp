using System;
using plamp.Abstractions.Ast;

namespace plamp.Alternative;

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

    public static PlampExceptionRecord AliasExpected() =>
        new()
        {
            Code = "PRS1117",
            Level = ExceptionLevel.Error,
            Message = "After as keyword expected alias"
        };

    public static PlampExceptionRecord ExpectedClosingCurlyBracket() =>
        new()
        {
            Code = "PRS1118",
            Level = ExceptionLevel.Error,
            Message = "Expected closing curly bracket"
        };

    public static PlampExceptionRecord ExpectedEndOfStatement() =>
        new()
        {
            Code = "PRS1119",
            Level = ExceptionLevel.Error,
            Message = "Expected end of statement"
        };

    public static PlampExceptionRecord ExpectedFuncName()
    {
        return new()
        {
            Code = "PRS1120",
            Level = ExceptionLevel.Error,
            Message = "Expected function name"
        };
    }

    public static PlampExceptionRecord ExpectedOpenParen()
    {
        return new()
        {
            Code = "PRS1121",
            Level = ExceptionLevel.Error,
            Message = "Expected open paren"
        };
    }

    public static PlampExceptionRecord ExpectedCloseParen()
    {
        return new()
        {
            Code = "PRS1122",
            Level = ExceptionLevel.Error,
            Message = "Expected close paren"
        };
    }

    public static PlampExceptionRecord ExpectedTypeName()
    {
        return new()
        {
            Code = "PRS1123",
            Level = ExceptionLevel.Error,
            Message = "Expected type name"
        };
    }

    public static PlampExceptionRecord ExpectedArgName()
    {
        return new()
        {
            Code = "PRS1124",
            Level = ExceptionLevel.Error,
            Message = "Expected argument name"
        };
    }

    public static PlampExceptionRecord ExpectedAssignment()
    {
        return new()
        {
            Code = "PRS1125",
            Level = ExceptionLevel.Error,
            Message = "Expected assignment"
        };
    }

    public static PlampExceptionRecord ExpectedAssignmentSource()
    {
        return new()
        {
            Code = "PRS1126",
            Level = ExceptionLevel.Error,
            Message = "Expected assignment source"
        };
    }

    public static PlampExceptionRecord ExpectedVarName()
    {
        return new()
        {
            Code = "PRS1127",
            Level = ExceptionLevel.Error,
            Message = "Expected var name"
        };
    }

    public static PlampExceptionRecord ExpectedSubmoduleName()
    {
        return new()
        {
            Code = "PRS1128",
            Level = ExceptionLevel.Error,
            Message = "Expected submodule name"
        };
    }

    public static PlampExceptionRecord TypesIsNotSupported()
    {
        return new()
        {
            Code = "PRS1129",
            Level = ExceptionLevel.Error,
            Message = "Types is not supported in current version of language",
        };
    }
    
    #endregion

    #region Semantics

    public static PlampExceptionRecord DuplicateVariableDefinition()
    {
        return new()
        {
            Code = "SEM1300",
            Level = ExceptionLevel.Error,
            Message = "Variable already defined"
        };
    }

    public static PlampExceptionRecord CannotAssign()
    {
        return new()
        {
            Code = "SEM1301",
            Level = ExceptionLevel.Error,
            Message = "Assignment target and source types are differs"
        };
    }

    public static PlampExceptionRecord CannotFindMember()
    {
        return new()
        {
            Code = "SEM1302",
            Level = ExceptionLevel.Error,
            Message = "Unknown variable or arg"
        };
    }

    public static PlampExceptionRecord UnknownFunction()
    {
        return new PlampExceptionRecord()
        {
            Code = "SEM1303",
            Level = ExceptionLevel.Error,
            Message = "Cannot find function within module with this signature"
        };
    }

    public static PlampExceptionRecord PredicateMustBeBooleanType()
    {
        return new PlampExceptionRecord()
        {
            Code = "SEM1304",
            Level = ExceptionLevel.Error,
            Message = "Predicate must be a boolean type"
        };
    }

    public static PlampExceptionRecord CannotApplyOperator()
    {
        return new PlampExceptionRecord()
        {
            Code = "SEM1305",
            Level = ExceptionLevel.Error,
            Message = "Cannot apply operator, types differ"
        };
    }

    public static PlampExceptionRecord ArgumentAlreadyDefined()
    {
        return new PlampExceptionRecord()
        {
            Code = "SEM1306",
            Level = ExceptionLevel.Error,
            Message = "Argument with same name already defined"
        };
    }
    
    #endregion
}