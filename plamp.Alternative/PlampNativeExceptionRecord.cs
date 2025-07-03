using plamp.Abstractions.Ast;

namespace plamp.Alternative;

/// <summary>
/// Exception template
/// </summary>
public record PlampNativeExceptionRecord(string Message, string Code, ExceptionLevel Level)
{
    public PlampExceptionRecord Format(params string[] inlines) => new()
    {
        Message = string.Format(Message, inlines),
        Code = Code,
        Level = Level
    };
}