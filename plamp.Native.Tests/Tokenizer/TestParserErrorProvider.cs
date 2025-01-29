using System.Collections;
using System.Collections.Generic;
using plamp.Ast;
using plamp.Native.Tokenization;

namespace plamp.Native.Tests.Tokenizer;

public class TestParserErrorProvider : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator()
    {
        yield return ["\"", new List<PlampException>{new(PlampNativeExceptionInfo.StringIsNotClosed(), new FilePosition(0, 0), new FilePosition(0, 0))}];
        yield return ["\"\n", new List<PlampException>{new(PlampNativeExceptionInfo.StringIsNotClosed(), new FilePosition(0, 0), new FilePosition(0, 0))}];
        yield return ["\"\r\n", new List<PlampException>{new(PlampNativeExceptionInfo.StringIsNotClosed(), new FilePosition(0, 0), new FilePosition(0, 0))}];
        yield return ["\"\\x\"", new List<PlampException>{new(PlampNativeExceptionInfo.InvalidEscapeSequence("\\x"), new FilePosition(0, 1), new FilePosition(0, 2))}];
        yield return ["@", new List<PlampException>{new(PlampNativeExceptionInfo.UnexpectedToken("@"), new FilePosition(0, 0), new FilePosition(0, 0))}];
        yield return ["1.0i", new List<PlampException>{new (PlampNativeExceptionInfo.UnknownNumberFormat, new FilePosition(0, 0), new FilePosition(0, 3))}];
        yield return ["1fic", new List<PlampException>{new (PlampNativeExceptionInfo.UnknownNumberFormat, new FilePosition(0, 0), new FilePosition(0, 3))}];
        yield return ["\"\\x", new List<PlampException>
        {
            new (PlampNativeExceptionInfo.InvalidEscapeSequence("\\x"), new FilePosition(0, 1), new FilePosition(0, 2)),
            new (PlampNativeExceptionInfo.StringIsNotClosed(), new FilePosition(0, 0), new FilePosition(0, 2))
        }];
        yield return ["@\"", new List<PlampException>
        {
            new (PlampNativeExceptionInfo.UnexpectedToken("@"), new FilePosition(0, 0), new FilePosition(0, 0)),
            new (PlampNativeExceptionInfo.StringIsNotClosed(), new FilePosition(0, 1), new FilePosition(0, 1))
        }];
        yield return ["@\"\\x", new List<PlampException>
        {
            new (PlampNativeExceptionInfo.UnexpectedToken("@"), new FilePosition(0, 0), new FilePosition(0, 0)),
            new (PlampNativeExceptionInfo.InvalidEscapeSequence("\\x"), new FilePosition(0, 2), new FilePosition(0, 3)),
            new (PlampNativeExceptionInfo.StringIsNotClosed(), new FilePosition(0, 1), new FilePosition(0, 3))
        }];
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}