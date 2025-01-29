using plamp.Ast;

namespace plamp.Native;

/// <summary>
/// Exception template
/// </summary>
public record PlampNativeExceptionRecord(string Message, int Code, ExceptionLevel Level)
{
    public PlampExceptionRecord Format(params string[] inlines) => new()
    {
        Message = string.Format(Message, inlines),
        Code = Code,
        Level = Level
    };
}