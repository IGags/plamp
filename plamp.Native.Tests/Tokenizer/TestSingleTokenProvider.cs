using System;
using System.Collections;
using System.Collections.Generic;
using plamp.Native.Tokenization;
using plamp.Native.Tokenization.Enumerations;
using plamp.Native.Tokenization.Token;

namespace plamp.Native.Tests.Tokenizer;

public class TestSingleTokenProvider : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator()
    {
        yield return [" ", typeof(WhiteSpace), new Predicate<TokenBase>(t => ((WhiteSpace)t).Kind == WhiteSpaceKind.WhiteSpace)];
        yield return ["\r", typeof(WhiteSpace), new Predicate<TokenBase>(t => ((WhiteSpace)t).Kind == WhiteSpaceKind.WhiteSpace)];
        yield return ["    ", typeof(WhiteSpace), new Predicate<TokenBase>(t => ((WhiteSpace)t).Kind == WhiteSpaceKind.Scope)];
        yield return ["\t", typeof(WhiteSpace), new Predicate<TokenBase>(t => ((WhiteSpace)t).Kind == WhiteSpaceKind.Scope)];
        yield return ["abc", typeof(Word)];
        yield return ["a1", typeof(Word)];
        yield return ["A", typeof(Word)];
        yield return ["\"\"", typeof(StringLiteral), new Predicate<TokenBase>(t => ((StringLiteral)t).GetStringRepresentation() == "")];
        yield return ["\"abc\"", typeof(StringLiteral), new Predicate<TokenBase>(t => ((StringLiteral)t).GetStringRepresentation() == "abc")];
        yield return ["\"123\"", typeof(StringLiteral), new Predicate<TokenBase>(t => ((StringLiteral)t).GetStringRepresentation() == "123")];
        yield return ["\"a1\"", typeof(StringLiteral), new Predicate<TokenBase>(t => ((StringLiteral)t).GetStringRepresentation() == "a1")];
        yield return ["\"1a\"", typeof(StringLiteral), new Predicate<TokenBase>(t => ((StringLiteral)t).GetStringRepresentation() == "1a")];
        yield return ["\"!@#№;$%:^?&*)(_-+={}[]/,.'~<>\"", typeof(StringLiteral), new Predicate<TokenBase>(t => ((StringLiteral)t).GetStringRepresentation() == "!@#№;$%:^?&*)(_-+={}[]/,.'~<>")];
        yield return ["\"\\n\"", typeof(StringLiteral), new Predicate<TokenBase>(t => ((StringLiteral)t).GetStringRepresentation() == "\n")];
        yield return ["\"\\\\\"", typeof(StringLiteral), new Predicate<TokenBase>(t => ((StringLiteral)t).GetStringRepresentation() == "\\")];
        yield return ["\"\\r\"", typeof(StringLiteral), new Predicate<TokenBase>(t => ((StringLiteral)t).GetStringRepresentation() == "\r")];
        yield return ["\"\\t\"", typeof(StringLiteral), new Predicate<TokenBase>(t => ((StringLiteral)t).GetStringRepresentation() == "\t")];
        yield return ["\"\\\"\"", typeof(StringLiteral), new Predicate<TokenBase>(t => ((StringLiteral)t).GetStringRepresentation() == "\"")];
        yield return ["[", typeof(OpenSquareBracket)];
        yield return ["]", typeof(CloseSquareBracket)];
        yield return ["(", typeof(OpenParen)];
        yield return [")", typeof(CloseParen)];
        yield return [",", typeof(Comma)];
        yield return ["\r\n", typeof(EndOfLine), new Predicate<TokenBase>(t => ((EndOfLine)t).GetStringRepresentation() == PlampNativeTokenizer.EndOfLineCrlf)];
        yield return ["->", typeof(LineBreak)];
        yield return ["+=", typeof(OperatorToken), new Predicate<TokenBase>(t => ((OperatorToken)t).Operator == OperatorEnum.PlusAndAssign)];
        yield return ["-=", typeof(OperatorToken), new Predicate<TokenBase>(t => ((OperatorToken)t).Operator == OperatorEnum.MinusAndAssign)];
        yield return ["++", typeof(OperatorToken), new Predicate<TokenBase>(t => ((OperatorToken)t).Operator == OperatorEnum.Increment)];
        yield return ["--", typeof(OperatorToken), new Predicate<TokenBase>(t => ((OperatorToken)t).Operator == OperatorEnum.Decrement)];
        yield return ["*=", typeof(OperatorToken), new Predicate<TokenBase>(t => ((OperatorToken)t).Operator == OperatorEnum.MultiplyAndAssign)];
        yield return ["/=", typeof(OperatorToken), new Predicate<TokenBase>(t => ((OperatorToken)t).Operator == OperatorEnum.DivideAndAssign)];
        yield return ["==", typeof(OperatorToken), new Predicate<TokenBase>(t => ((OperatorToken)t).Operator == OperatorEnum.Equals)];
        yield return ["!=", typeof(OperatorToken), new Predicate<TokenBase>(t => ((OperatorToken)t).Operator == OperatorEnum.NotEquals)];
        yield return ["<=", typeof(OperatorToken), new Predicate<TokenBase>(t => ((OperatorToken)t).Operator == OperatorEnum.LesserOrEquals)];
        yield return [">=", typeof(OperatorToken), new Predicate<TokenBase>(t => ((OperatorToken)t).Operator == OperatorEnum.GreaterOrEquals)];
        yield return ["&&", typeof(OperatorToken), new Predicate<TokenBase>(t => ((OperatorToken)t).Operator == OperatorEnum.And)];
        yield return ["||", typeof(OperatorToken), new Predicate<TokenBase>(t => ((OperatorToken)t).Operator == OperatorEnum.Or)];
        yield return ["%=", typeof(OperatorToken), new Predicate<TokenBase>(t => ((OperatorToken)t).Operator == OperatorEnum.ModuloAndAssign)];
        yield return ["+", typeof(OperatorToken), new Predicate<TokenBase>(t => ((OperatorToken)t).Operator == OperatorEnum.Plus)];
        yield return ["-", typeof(OperatorToken), new Predicate<TokenBase>(t => ((OperatorToken)t).Operator == OperatorEnum.Minus)];
        yield return [".", typeof(OperatorToken), new Predicate<TokenBase>(t => ((OperatorToken)t).Operator == OperatorEnum.MemberAccess)];
        yield return ["/", typeof(OperatorToken), new Predicate<TokenBase>(t => ((OperatorToken)t).Operator == OperatorEnum.Divide)];
        yield return ["*", typeof(OperatorToken), new Predicate<TokenBase>(t => ((OperatorToken)t).Operator == OperatorEnum.Multiply)];
        yield return ["<", typeof(OpenAngleBracket), new Predicate<TokenBase>(t => ((OperatorToken)t).Operator == OperatorEnum.Lesser)];
        yield return [">", typeof(CloseAngleBracket), new Predicate<TokenBase>(t => ((OperatorToken)t).Operator == OperatorEnum.Greater)];
        yield return ["!", typeof(OperatorToken), new Predicate<TokenBase>(t => ((OperatorToken)t).Operator == OperatorEnum.Not)];
        yield return ["|", typeof(OperatorToken), new Predicate<TokenBase>(t => ((OperatorToken)t).Operator == OperatorEnum.BitwiseOr)];
        yield return ["&", typeof(OperatorToken), new Predicate<TokenBase>(t => ((OperatorToken)t).Operator == OperatorEnum.BitwiseAnd)];
        yield return ["^", typeof(OperatorToken), new Predicate<TokenBase>(t => ((OperatorToken)t).Operator == OperatorEnum.Xor)];
        yield return ["^=", typeof(OperatorToken), new Predicate<TokenBase>(t => ((OperatorToken)t).Operator == OperatorEnum.XorAndAssign)];
        yield return ["|=", typeof(OperatorToken), new Predicate<TokenBase>(t => ((OperatorToken)t).Operator == OperatorEnum.OrAndAssign)];
        yield return ["&=", typeof(OperatorToken), new Predicate<TokenBase>(t => ((OperatorToken)t).Operator == OperatorEnum.AndAndAssign)];
        yield return ["use", typeof(KeywordToken), new Predicate<TokenBase>(t => ((KeywordToken)t).Keyword == Keywords.Use)];
        yield return ["def", typeof(KeywordToken), new Predicate<TokenBase>(t => ((KeywordToken)t).Keyword == Keywords.Def)];
        yield return ["new", typeof(KeywordToken), new Predicate<TokenBase>(t => ((KeywordToken)t).Keyword == Keywords.New)];
        yield return ["false", typeof(KeywordToken), new Predicate<TokenBase>(t => ((KeywordToken)t).Keyword == Keywords.False)];
        yield return ["true", typeof(KeywordToken), new Predicate<TokenBase>(t => ((KeywordToken)t).Keyword == Keywords.True)];
        yield return ["for", typeof(KeywordToken), new Predicate<TokenBase>(t => ((KeywordToken)t).Keyword == Keywords.For)];
        yield return ["while", typeof(KeywordToken), new Predicate<TokenBase>(t => ((KeywordToken)t).Keyword == Keywords.While)];
        yield return ["if", typeof(KeywordToken), new Predicate<TokenBase>(t => ((KeywordToken)t).Keyword == Keywords.If)];
        yield return ["elif", typeof(KeywordToken), new Predicate<TokenBase>(t => ((KeywordToken)t).Keyword == Keywords.Elif)];
        yield return ["else", typeof(KeywordToken), new Predicate<TokenBase>(t => ((KeywordToken)t).Keyword == Keywords.Else)];
        yield return ["in", typeof(KeywordToken), new Predicate<TokenBase>(t => ((KeywordToken)t).Keyword == Keywords.In)];
        yield return ["null", typeof(KeywordToken), new Predicate<TokenBase>(t => ((KeywordToken)t).Keyword == Keywords.Null)];
        yield return ["return", typeof(KeywordToken), new Predicate<TokenBase>(t => ((KeywordToken)t).Keyword == Keywords.Return)];
        yield return ["break", typeof(KeywordToken), new Predicate<TokenBase>(t => ((KeywordToken)t).Keyword == Keywords.Break)];
        yield return ["continue", typeof(KeywordToken), new Predicate<TokenBase>(t => ((KeywordToken)t).Keyword == Keywords.Continue)];
        yield return ["model", typeof(KeywordToken), new Predicate<TokenBase>(t => ((KeywordToken)t).Keyword == Keywords.Model)];
        yield return ["var", typeof(KeywordToken), new Predicate<TokenBase>(t => ((KeywordToken)t).Keyword == Keywords.Var)];
        yield return ["await", typeof(KeywordToken), new Predicate<TokenBase>(t => ((KeywordToken)t).Keyword == Keywords.Await)];
        yield return ["async", typeof(KeywordToken), new Predicate<TokenBase>(t => ((KeywordToken)t).Keyword == Keywords.Async)];
        yield return ["1", typeof(NumberLiteral), new Predicate<TokenBase>(t => ((NumberLiteral)t).ActualType == typeof(int) && (int)((NumberLiteral)t).ActualValue == 1)];
        yield return ["0", typeof(NumberLiteral), new Predicate<TokenBase>(t => ((NumberLiteral)t).ActualType == typeof(int) && (int)((NumberLiteral)t).ActualValue == 0)];
        yield return ["1i", typeof(NumberLiteral), new Predicate<TokenBase>(t => ((NumberLiteral)t).ActualType == typeof(int) && (int)((NumberLiteral)t).ActualValue == 1)];
        yield return ["1ui", typeof(NumberLiteral), new Predicate<TokenBase>(t => ((NumberLiteral)t).ActualType == typeof(uint) && (uint)((NumberLiteral)t).ActualValue == 1)];
        yield return ["5000000000", typeof(NumberLiteral), new Predicate<TokenBase>(t => ((NumberLiteral)t).ActualType == typeof(long) && (long)((NumberLiteral)t).ActualValue == 5000000000)];
        yield return ["1l", typeof(NumberLiteral), new Predicate<TokenBase>(t => ((NumberLiteral)t).ActualType == typeof(long) && (long)((NumberLiteral)t).ActualValue == 1)];
        yield return ["1ul", typeof(NumberLiteral), new Predicate<TokenBase>(t => ((NumberLiteral)t).ActualType == typeof(ulong) && (ulong)((NumberLiteral)t).ActualValue == 1)];
        yield return ["1.0", typeof(NumberLiteral), new Predicate<TokenBase>(t => ((NumberLiteral)t).ActualType == typeof(double) && Math.Abs((double)((NumberLiteral)t).ActualValue - 1) < 1e-9)];
        yield return ["1d", typeof(NumberLiteral), new Predicate<TokenBase>(t => ((NumberLiteral)t).ActualType == typeof(double) && Math.Abs((double)((NumberLiteral)t).ActualValue - 1) < 1e-9)];
        yield return ["1f", typeof(NumberLiteral), new Predicate<TokenBase>(t => ((NumberLiteral)t).ActualType == typeof(float) && Math.Abs((float)((NumberLiteral)t).ActualValue - 1) < 1e-5)];
        yield return ["1b", typeof(NumberLiteral), new Predicate<TokenBase>(t => ((NumberLiteral)t).ActualType == typeof(byte) && (byte)((NumberLiteral)t).ActualValue == 1)];
        yield return ["1sb", typeof(NumberLiteral), new Predicate<TokenBase>(t => ((NumberLiteral)t).ActualType == typeof(sbyte) && (sbyte)((NumberLiteral)t).ActualValue == 1)];
        yield return ["1s", typeof(NumberLiteral), new Predicate<TokenBase>(t => ((NumberLiteral)t).ActualType == typeof(short) && (short)((NumberLiteral)t).ActualValue == 1)];
        yield return ["1us", typeof(NumberLiteral), new Predicate<TokenBase>(t => ((NumberLiteral)t).ActualType == typeof(ushort) && (ushort)((NumberLiteral)t).ActualValue == 1)];
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}