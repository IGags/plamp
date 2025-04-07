using System.Collections;
using System.Collections.Generic;
using plamp.Abstractions.Ast;

namespace plamp.Native.Tests.Tokenizer;

public class TestParserErrorProvider : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator()
    {
        yield return ["\"", new List<PlampException>{new(PlampNativeExceptionInfo.StringIsNotClosed(), new FilePosition(0, 0), new FilePosition(0, 0), ParserTestHelper.FileName, ParserTestHelper.AssemblyName)}];
        yield return ["\"\n", new List<PlampException>{new(PlampNativeExceptionInfo.StringIsNotClosed(), new FilePosition(0, 0), new FilePosition(0, 0), ParserTestHelper.FileName, ParserTestHelper.AssemblyName)}];
        yield return ["\"\r\n", new List<PlampException>{new(PlampNativeExceptionInfo.StringIsNotClosed(), new FilePosition(0, 0), new FilePosition(0, 0), ParserTestHelper.FileName, ParserTestHelper.AssemblyName)}];
        yield return ["\"\\x\"", new List<PlampException>{new(PlampNativeExceptionInfo.InvalidEscapeSequence("\\x"), new FilePosition(0, 1), new FilePosition(0, 2), ParserTestHelper.FileName, ParserTestHelper.AssemblyName)}];
        yield return ["@", new List<PlampException>{new(PlampNativeExceptionInfo.UnexpectedToken("@"), new FilePosition(0, 0), new FilePosition(0, 0), ParserTestHelper.FileName, ParserTestHelper.AssemblyName)}];
        yield return ["1.0i", new List<PlampException>{new (PlampNativeExceptionInfo.UnknownNumberFormat, new FilePosition(0, 0), new FilePosition(0, 3), ParserTestHelper.FileName, ParserTestHelper.AssemblyName)}];
        yield return ["1fic", new List<PlampException>{new (PlampNativeExceptionInfo.UnknownNumberFormat, new FilePosition(0, 0), new FilePosition(0, 3), ParserTestHelper.FileName, ParserTestHelper.AssemblyName)}];
        yield return ["\"\\x", new List<PlampException>
        {
            new (PlampNativeExceptionInfo.InvalidEscapeSequence("\\x"), new FilePosition(0, 1), new FilePosition(0, 2), ParserTestHelper.FileName, ParserTestHelper.AssemblyName),
            new (PlampNativeExceptionInfo.StringIsNotClosed(), new FilePosition(0, 0), new FilePosition(0, 2), ParserTestHelper.FileName, ParserTestHelper.AssemblyName)
        }];
        yield return ["@\"", new List<PlampException>
        {
            new (PlampNativeExceptionInfo.UnexpectedToken("@"), new FilePosition(0, 0), new FilePosition(0, 0), ParserTestHelper.FileName, ParserTestHelper.AssemblyName),
            new (PlampNativeExceptionInfo.StringIsNotClosed(), new FilePosition(0, 1), new FilePosition(0, 1), ParserTestHelper.FileName, ParserTestHelper.AssemblyName)
        }];
        yield return ["@\"\\x", new List<PlampException>
        {
            new (PlampNativeExceptionInfo.UnexpectedToken("@"), new FilePosition(0, 0), new FilePosition(0, 0), ParserTestHelper.FileName, ParserTestHelper.AssemblyName),
            new (PlampNativeExceptionInfo.InvalidEscapeSequence("\\x"), new FilePosition(0, 2), new FilePosition(0, 3), ParserTestHelper.FileName, ParserTestHelper.AssemblyName),
            new (PlampNativeExceptionInfo.StringIsNotClosed(), new FilePosition(0, 1), new FilePosition(0, 3), ParserTestHelper.FileName, ParserTestHelper.AssemblyName)
        }];
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}