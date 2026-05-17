using System.Collections.Generic;
using plamp.Abstractions.Ast;

namespace plamp.Alternative;

public static class PlampExceptionInfo
{
    #region Tokenization(1000-1099)

    public static PlampExceptionRecord UnexpectedToken(string stringToken) =>
        new()
        {
            Message = $"Unexpected token \"{stringToken}\"",
            Code = "TOK1000",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord StringIsNotClosed() => 
        new()
        {
            Message = "String is not closed",
            Code = "TOK1030",
            Level = ExceptionLevel.Error
        };


    public static PlampExceptionRecord InvalidEscapeSequence(string sequence) =>
        new()
        {
            Message = $"Invalid escape sequence \"{sequence}\"",
            Code = "TOK1031",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord UnknownNumberFormat() =>
        new()
        {
            Message = "Unknown number format",
            Code = "TOK1032",
            Level = ExceptionLevel.Error
        };

    #endregion

    #region PRSing(1100-1299)

    public static PlampExceptionRecord ExpectedExpression() =>
        new()
        {
            Message = "Expected expression",
            Code = "PRS1103",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord ExpectedArgDefinition() =>
        new()
        {
            Message = "Expected arg definition",
            Code = "PRS1109",
            Level = ExceptionLevel.Error
        };

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

    public static PlampExceptionRecord EmptyGenericDefinition()
    {
        return new()
        {
            Code = "PRS1124",
            Level = ExceptionLevel.Error,
            Message = "Generic definition is empty"
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

    public static PlampExceptionRecord GenericDefinitionIsNotClosed()
    {
        return new()
        {
            Code = "PRS1127",
            Level = ExceptionLevel.Error,
            Message = "Generic definition is not closed"
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

    public static PlampExceptionRecord EmptyGenericArgs()
    {
        return new()
        {
            Code = "PRS1129",
            Level = ExceptionLevel.Error,
            Message = "Generic args for type is empty"
        };
    }

    public static PlampExceptionRecord ExpectedModuleName()
    {
        return new()
        {
            Code = "PRS1130",
            Level = ExceptionLevel.Error,
            Message = "Expected module name"
        };
    }

    public static PlampExceptionRecord ArrayDefinitionIsNotClosed()
    {
        return new()
        {
            Code = "PRS1131",
            Level = ExceptionLevel.Error,
            Message = "Array definition is not closed"
        };
    }

    public static PlampExceptionRecord ArrayInitializationMustHasLength()
    {
        return new()
        {
            Code = "PRS1132",
            Level = ExceptionLevel.Error,
            Message = "Array initialization must has length"
        };
    }

    public static PlampExceptionRecord IndexerIsNotClosed()
    {
        return new()
        {
            Code = "PRS1133",
            Level = ExceptionLevel.Error,
            Message = "Indexer is not closed"
        };
    }

    public static PlampExceptionRecord ExpectedBodyInCurlyBrackets()
    {
        return new ()
        {
            Code = "PRS1134",
            Level = ExceptionLevel.Error,
            Message = "The body is expected in curly brackets."
        };
    }

    public static PlampExceptionRecord ExpectedAssignmentTarget()
    {
        return new()
        {
            Code = "PRS1135",
            Level = ExceptionLevel.Error,
            Message = "Expected assignment target"
        };
    }

    public static PlampExceptionRecord ExpectedFieldName()
    {
        return new()
        {
            Code = "PRS1136",
            Level = ExceptionLevel.Error,
            Message = "Expected field name"
        };
    }

    public static PlampExceptionRecord ExpectedColon()
    {
        return new()
        {
            Code = "PRS1137",
            Level = ExceptionLevel.Error,
            Message = "Expected colon"
        };
    }
    
    public static PlampExceptionRecord ExpectedFieldValue()
    {
        return new()
        {
            Code = "PRS1137",
            Level = ExceptionLevel.Error,
            Message = "Expected field value"
        };
    }

    public static PlampExceptionRecord GenericArgsIsNotClosed()
    {
        return new()
        {
            Code = "PRS1138",
            Level = ExceptionLevel.Error,
            Message = "Generic args is not closed"
        };
    }

    public static PlampExceptionRecord TopLevelExpressionExpected()
    {
        return new()
        {
            Code = "PRS1139",
            Level = ExceptionLevel.Error,
            Message = "Expected fn, type, module, etc..."
        };
    }

    public static PlampExceptionRecord ExpectedFieldTypeQualifier()
    {
        return new()
        {
            Code = "PRS1140",
            Level = ExceptionLevel.Error,
            Message = "Expected type qualifier - \":\" after field name"
        };
    }

    public static PlampExceptionRecord ExpectedGenericArg()
    {
        return new()
        {
            Code = "PRS1141",
            Level = ExceptionLevel.Error,
            Message = "Expected type that will be generic argument."
        };
    }

    public static PlampExceptionRecord ExpectedGenericTypeArgumentAlias()
    {
        return new()
        {
            Code = "PRS1142",
            Level = ExceptionLevel.Error,
            Message = "Expected type name that will be name for generic argument."
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

    public static PlampExceptionRecord CannotApplyArgument()
    {
        return new PlampExceptionRecord()
        {
            Code = "SEM1303",
            Level = ExceptionLevel.Error,
            Message = "Cannot apply argument to call this function."
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

    public static PlampExceptionRecord ReturnTypeMismatch()
    {
        return new PlampExceptionRecord()
        {
            Code = "SEM1307",
            Level = ExceptionLevel.Error,
            Message = "Return expression type should match with return type of func"
        };
    }

    public static PlampExceptionRecord DuplicateMemberNameInModule()
    {
        return new PlampExceptionRecord()
        {
            Code = "SEM1308",
            Level = ExceptionLevel.Error,
            Message = "Member with same name already declared in this module"
        };
    }

    public static PlampExceptionRecord DuplicateModuleDefinition()
    {
        return new PlampExceptionRecord()
        {
            Code = "SEM1309",
            Level = ExceptionLevel.Error,
            Message = "Module name already declared in this module"
        };
    }

    public static PlampExceptionRecord MemberCannotHaveSameNameAsDeclaringModule()
    {
        return new PlampExceptionRecord()
        {
            Code = "SEM1310",
            Level = ExceptionLevel.Error,
            Message = "Member cannot have same name as declaring module"
        };
    }

    public static PlampExceptionRecord ModuleMustHaveName()
    {
        return new PlampExceptionRecord()
        {
            Code = "SEM1311",
            Level = ExceptionLevel.Error,
            Message = "The code file must have a module name that belongs to. Add \"module MODULE_NAME;\""
        };
    }
    
    public static PlampExceptionRecord FuncMustReturnValue() =>
        new()
        {
            Message = "Function must return a value",
            Code = "SEM1312",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord ReturnValueIsMissing() =>
        new()
        {
            Message = "Return value is missing",
            Code = "SEM1313",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord CannotReturnValue() =>
        new()
        {
            Message = "Cannot return value from a void function",
            Code = "SEM1314",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord CannotAssignNone() =>
        new()
        {
            Message = "Cannot assign type of none",
            Code = "SEM1315",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord DuplicateParameterName() =>
        new()
        {
            Message = "Duplicate parameter name",
            Code = "SEM1316",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord ArrayLengthMustBeInteger() =>
        new()
        {
            Message = "The array length definition must has integer type",
            Code = "SEM1317",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord EmptyAssign() =>
        new()
        {
            Message = "Assign node is empty",
            Code = "SEM1318",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord IndexerIsNotApplicable() =>
        new()
        {
            Message = "The indexer is not applicable",
            Code = "SEM1319",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord IndexerValueMustBeInteger() =>
        new()
        {
            Message = "The indexer value must be of integer type",
            Code = "SEM1320",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord AssignSourceAndTargetCountMismatch() =>
        new()
        {
            Message = "Assign target count does not match with the source count",
            Code = "SEM1321",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord CannotDefineCoreType() =>
        new()
        {
            Message = "Cannot define type with same name as builtin member",
            Code = "SEM1322",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord CannotDefineCoreFunction() =>
        new()
        {
            Message = "Cannot define function with same name as builtin member",
            Code = "SEM1323",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord DuplicateFieldDefinition(string fieldName) =>
        new()
        {
            Message = $"Field {fieldName} already declared within a type",
            Code = "SEM1324",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord TypeIsNotFound(string typeName) =>
        new()
        {
            Message = $"Type {typeName} is not found.",
            Code = "SEM1325",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord AmbiguousTypeName(string typeName, IEnumerable<string> modules) =>
        new()
        {
            Message = $"The type {typeName} is defined in {string.Join(", ", modules)} modules",
            Code = "SEM1326",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord FunctionIsNotFound(
        string functionName) =>
        new()
        {
            Message = $"Function {functionName} not found.",
            Code = "SEM1327",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord AmbiguousFunctionReference(
        string functionName,
        IEnumerable<string> modules) =>
        new()
        {
            Message = $"Function {functionName} defined in {string.Join(", ", modules)} modules",
            Code = "SEM1327",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord FieldCannotHasSameNameAsEnclosingType() =>
        new()
        {
            Message = "The field cannot has same name as its enclosing type",
            Code = "SEM1328",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord FieldIsNotFound() =>
        new()
        {
            Message = "Field is not found in declaring type",
            Code = "SEM1329",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord FieldProduceCircularDependency() =>
        new()
        {
            Message = "Field refers to type that produce circular dependency with current type",
            Code = "SEM1330",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord IllegalBodyLevelInstruction() =>
        new()
        {
            Message = "Only call, assignment, unary increment, flow control and return are allowed.",
            Code = "SEM1331",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord CannotUseControlFlowNotInLoop() =>
        new()
        {
            Message = "This instruction allowed in loop only",
            Code = "SEM1332",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord DuplicateGenericParameterName() =>
        new()
        {
            Message = "This name is used by another generic parameter already",
            Code = "SEM1333",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord GenericParameterNameSameAsDefiningType() =>
        new()
        {
            Message = "Generic parameter has the same name as defining type",
            Code = "SEM1334",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord GenericParameterHasSameNameAsBuiltinMember() =>
        new()
        {
            Message = "Generic parameter has the same name as builtin member",
            Code = "SEM1335",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord FieldHasSameNameAsBuiltinMember() =>
        new()
        {
            Message = "Field has the same name as builtin member",
            Code = "SEM1336",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord FunctionHasDifferentArgCount(int expectedArgCt, int actualArgCt) =>
        new()
        {
            Message = $"Function has {expectedArgCt} but called with {actualArgCt} arguments",
            Code = "SEM1337", //1337 - YOOOOOO
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord GenericFunctionParameterCannotHasManyImplementations(
        string genericParamTypeName,
        IEnumerable<string> implementationTypeNames) =>
        new()
        {
            Message = $"Generic parameter {genericParamTypeName} cannot be {string.Join(", ", implementationTypeNames)} simultaneously",
            Code = "SEM1338",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord GenericParameterHasNoImplementationType(
        string genericParamTypeName) =>
        new()
        {
            Message = $"Generic parameter {genericParamTypeName} has no implementation, please define function explicitly",
            Code = "SEM1339",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord GenericParamSameNameAsDefiningFunction() =>
        new()
        {
            Message = "Genric parameter has same name as difining function",
            Code = "SEM1340",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord GenericTypeDefinitionHasDifferentParameterCount(int expected, int actual) =>
        new()
        {
            Message = $"Generic type definition has {expected} parameter count, but created with {actual} arguments",
            Code = "SEM1141",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord GenericFuncDefinitionHasDifferentParameterCount(int expected, int actual) =>
        new()
        {
            Message = $"Generic func definition has {expected} generic parameter count, but created with {actual} arguments",
            Code = "SEM1142",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord CannotInitBuiltinType() =>
        new()
        {
            Message = "This type is builtin and cannot be initialized directly, use type literal instead.",
            Code = "SEM1143",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord CannotCreateNonEmptyArrayOfGenericParameter() =>
        new()
        {
            Message =
                "Array of generic type parameter with non zero length is not allowed. If you want to create an array of generic type parameter, use builtin function makeArray[T](item: T, length: int)",
            Code = "SEM1144",
            Level = ExceptionLevel.Error
        };
    
    public static PlampExceptionRecord CannotCreateGenericParameterType() =>
        new()
        {
            Message = "Cannot create generic parameter type directly.",
            Code = "SEM1145",
            Level = ExceptionLevel.Error
        };

    #endregion
}