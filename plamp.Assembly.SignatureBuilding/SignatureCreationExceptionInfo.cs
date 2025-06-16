using plamp.Abstractions.Ast;

namespace plamp.Assembly.SignatureBuilding;

public class SignatureCreationExceptionInfo
{
    // Error codes 3000-3499 !RESERVED!
    
    public static readonly PlampExceptionRecord DuplicateTypeNameRecord =
        new()
        {
            Code = 3000,
            Level = ExceptionLevel.Error,
            Message = "Duplicate type name"
        };

    public static readonly PlampExceptionRecord DuplicateMemberNameRecord =
        new()
        {
            Code = 3001,
            Level = ExceptionLevel.Error,
            Message = "Duplicate member name"
        };

    public static readonly PlampExceptionRecord InvalidTypeName =
        new()
        {
            Code = 3002,
            Level = ExceptionLevel.Error,
            Message = "Invalid type name"
        };
}