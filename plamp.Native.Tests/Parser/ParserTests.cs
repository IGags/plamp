using System;
using System.Formats.Asn1;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using plamp.Ast.Node;
using plamp.Ast.Node.Assign;
using plamp.Ast.Node.Binary;
using plamp.Ast.Node.Body;
using plamp.Ast.Node.ControlFlow;
using plamp.Ast.Node.Unary;
using plamp.Native.Parsing;
using plamp.Native.Tokenization.Token;
using Xunit;

namespace plamp.Native.Tests.Parser;
#pragma warning disable CS0618
public class ParserTests
{
    //[Theory]
    //[InlineData("", 0, false, new[]{typeof(MemberNode)}, new[]{"2"}, -1)]
    //[InlineData("+1", 0, true, new[]{typeof(PlusNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"2", "1"}, 1)]
    //[InlineData("*1", 0, true, new[]{typeof(MultiplyNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"2", "1"}, 1)]
    //[InlineData("-1", 0, true, new[]{typeof(MinusNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"2", "1"}, 1)]
    //[InlineData("/1", 0, true, new[]{typeof(DivideNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"2", "1"}, 1)]
    //[InlineData("<1", 0, true, new[]{typeof(LessNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"2", "1"}, 1)]
    //[InlineData(">1", 0, true, new[]{typeof(GreaterNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"2", "1"}, 1)]
    //[InlineData("<=1", 0, true, new[]{typeof(LessOrEqualNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"2", "1"}, 1)]
    //[InlineData(">=1", 0, true, new[]{typeof(GreaterOrEqualsNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"2", "1"}, 1)]
    //[InlineData("==1", 0, true, new[]{typeof(EqualNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"2", "1"}, 1)]
    //[InlineData("!=1", 0, true, new[]{typeof(NotEqualNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"2", "1"}, 1)]
    //[InlineData("&&1", 0, true, new[]{typeof(AndNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"2", "1"}, 1)]
    //[InlineData("||1", 0, true, new[]{typeof(OrNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"2", "1"}, 1)]
    //[InlineData("%1", 0, true, new[]{typeof(ModuloNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"2", "1"}, 1)]
    //[InlineData("=1", 0, true, new[]{typeof(AssignNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"2", "1"}, 1)]
    //[InlineData("+=1", 0, true, new[]{typeof(AddAndAssignNode), typeof(MemberNode),typeof(MemberNode)}, new[]{"2", "1"}, 1)]
    //[InlineData("-=1", 0, true, new[]{typeof(SubAndAssignNode), typeof(MemberNode),typeof(MemberNode)}, new[]{"2", "1"}, 1)]
    //[InlineData("*=1", 0, true, new[]{typeof(MulAndAssignNode), typeof(MemberNode),typeof(MemberNode)}, new[]{"2", "1"}, 1)]
    //[InlineData("/=1", 0, true, new[]{typeof(DivAndAssignNode), typeof(MemberNode),typeof(MemberNode)}, new[]{"2", "1"}, 1)]
    //[InlineData("%=1", 0, true, new[]{typeof(ModuloAndAssignNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"2", "1"}, 1)]
    //[InlineData("&=1", 0, true, new[]{typeof(AndAndAssignNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"2", "1"}, 1)]
    //[InlineData("|=1", 0, true, new[]{typeof(OrAndAssignNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"2", "1"}, 1)]
    //[InlineData("^=1", 0, true, new[]{typeof(XorAndAssignNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"2", "1"}, 1)]
    //[InlineData("&1", 0, true, new[]{typeof(BitwiseAndNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"2", "1"}, 1)]
    //[InlineData("|1", 0, true, new[]{typeof(BitwiseOrNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"2", "1"}, 1)]
    //[InlineData("^1", 0, true, new[]{typeof(XorNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"2", "1"}, 1)]
    //[InlineData("^1", int.MaxValue, false, new[]{typeof(MemberNode)}, new[]{"2"}, -1)]
    //[InlineData("!1", 0, false, new[]{typeof(MemberNode)}, new[]{"2"}, -1)]
    //public void TestTryParseLedCorrect(string code, int rbp, bool isParsedExpected, Type[] treeTypeIterator, string[] memberIterator, int? tokenSequencePos = null)
    //{
    //    var startNode = new MemberNode("2");
    //    var parser = new PlampNativeParser(code);
    //    var isParsedActual = parser.TryParseLed(rbp, startNode, out var res);
    //    Assert.Equal(isParsedExpected, isParsedActual);
    //    var visitor = new TypeTreeVisitor(treeTypeIterator, memberIterator.ToList());
    //    visitor.Visit(res);
    //    visitor.Validate();
    //    if (tokenSequencePos != null)
    //    {
    //        Assert.Equal(tokenSequencePos.Value, parser.TokenSequence.Position);
    //    }
    //}
//
    //[Theory]
    //[InlineData("", new[]{typeof(MemberNode)}, new[]{"1"}, -1)]
    //[InlineData("[]", new[]{typeof(IndexerNode), typeof(MemberNode)}, new[]{"1"}, 1, 1, 
    //    new[]{ParserErrorConstants.EmptyIndexerDefinition}, new[]{0}, new[]{1})]
    //[InlineData("[2]", new[]{typeof(IndexerNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"1", "2"}, 2)]
    //[InlineData("[2,3]", new[]{typeof(IndexerNode), typeof(MemberNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"1", "2", "3"}, 4)]
    //[InlineData("[", new[]{typeof(MemberNode)}, new []{"1"}, 1, 2, new[]{ParserErrorConstants.InvalidExpression, ParserErrorConstants.ExpectedCloseParen}, new[]{1,1}, new[]{1,1})]
    //[InlineData("[1", new[]{typeof(MemberNode)}, new []{"1"}, 2, 1, new[]{ParserErrorConstants.ExpectedCloseParen}, new[]{2}, new[]{2})]
    //[InlineData("]", new[]{typeof(MemberNode)}, new []{"1"}, -1)]
    //[InlineData("[2,]", new[]{typeof(IndexerNode),typeof(MemberNode),typeof(MemberNode)}, new []{"1", "2"}, 3, 1, new[]{ParserErrorConstants.InvalidExpression}, new []{3},new []{3})]
    //[InlineData("[+]", new[]{typeof(MemberNode)}, new []{"1"}, 2, 2, new[]{ParserErrorConstants.InvalidExpression,ParserErrorConstants.ExpectedCloseParen}, new[]{1,1}, new[]{1,1})]
    //[InlineData("[,3]", new[]{typeof(IndexerNode),typeof(MemberNode),typeof(MemberNode)}, new []{"1", "3"}, 3, 1, new[]{ParserErrorConstants.InvalidExpression}, new []{1},new []{1})]
    //[InlineData("[2,,3]", new[]{typeof(IndexerNode),typeof(MemberNode),typeof(MemberNode),typeof(MemberNode)}, new []{"1", "2", "3"}, 5, 1, new[]{ParserErrorConstants.InvalidExpression}, new []{3},new []{3})]
    //public void TestParseIndexerOrDefault(string code, Type[] treeTypeIterator, string[] memberIterator, 
    //    int? tokenSequencePos = null, int errorCount = 0, 
    //    string[] errorTextList = null, int[] errorStartPosList = null, int[] errorEndPosList = null)
    //{
    //    var startNode = new MemberNode("1");
    //    var parser = new PlampNativeParser(code);
    //    parser.TryParseIndexer(startNode, out var res);
    //    Assert.Equal(errorCount, parser.Exceptions.Count);
    //    if (errorCount != 0)
    //    {
    //        for (int i = 0; i < parser.Exceptions.Count; i++)
    //        {
    //            Assert.Equal(errorTextList[i], parser.Exceptions[i].Message);
    //            Assert.Equal(errorStartPosList[i], parser.Exceptions[i].StartPosition);
    //            Assert.Equal(errorEndPosList[i], parser.Exceptions[i].EndPosition);
    //        }
    //    }
//
    //    if (tokenSequencePos != null)
    //    {
    //        Assert.Equal(tokenSequencePos, parser.TokenSequence.Position);
    //    }
//
    //    var visitor = new TypeTreeVisitor(treeTypeIterator, memberIterator.ToList());
    //    visitor.Visit(res);
    //    visitor.Validate();
    //}
//
    ////TODO: копирование
    //[Theory]
    //[InlineData("", new[]{typeof(MemberNode)}, new[]{"1"}, -1)]
    //[InlineData(".d", new[]{typeof(MemberAccessNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"1","d"}, 1)]
    //[InlineData(".d()", new[]{typeof(CallNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"1","d"}, 3)]
    //[InlineData(".d(a)", new[]{typeof(CallNode), typeof(MemberNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"1","d", "a"}, 4)]
    //[InlineData(".d(a,b)", new[]{typeof(CallNode), typeof(MemberNode), typeof(MemberNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"1","d", "a", "b"}, 6)]
    //[InlineData(".", new[]{typeof(MemberNode)}, new[]{"1"}, 0, 1, new[]{ParserErrorConstants.UnexpectedTokenPrefix + " " + nameof(Word)}, new []{1},new []{1})]
    //[InlineData(".+", new[]{typeof(MemberNode)}, new[]{"1"}, 0, 1, new[]{ParserErrorConstants.UnexpectedTokenPrefix + " " + nameof(Word)}, new []{1},new []{1})]
    //[InlineData(".var", new[]{typeof(MemberNode)}, new[]{"1"}, 0, 1, new[]{ParserErrorConstants.CannotUseKeyword}, new []{1},new []{3})]
    //[InlineData(".d(a,)", new[]{typeof(CallNode), typeof(MemberNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"1","d", "a"}, 5, 1, new[]{ParserErrorConstants.InvalidExpression}, new []{5},new []{5})]
    //[InlineData(".d(,a)", new[]{typeof(CallNode), typeof(MemberNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"1","d", "a"}, 5, 1, new[]{ParserErrorConstants.InvalidExpression}, new []{3},new []{3})]
    //[InlineData(".d(,)", new[]{typeof(CallNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"1","d"}, 4, 2, new[]{ParserErrorConstants.InvalidExpression,ParserErrorConstants.InvalidExpression}, new []{3,4},new []{3,4})]
    //[InlineData(".d(a", new[]{typeof(CallNode), typeof(MemberNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"1","d","a"}, 4, 1, new[]{ParserErrorConstants.ExpectedCloseParen}, new []{4},new []{4})]
    //[InlineData(".d(", new[]{typeof(CallNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"1","d"}, 3, 2, new[]{ParserErrorConstants.InvalidExpression, ParserErrorConstants.ExpectedCloseParen}, new[]{3,3}, new[]{3,3})]
    //public void TestTryParseCall(string code, Type[] treeTypeIterator, string[] memberIterator, 
    //    int? tokenSequencePos = null, int errorCount = 0, 
    //    string[] errorTextList = null, int[] errorStartPosList = null, int[] errorEndPosList = null)
    //{
    //    var startNode = new MemberNode("1");
    //    var parser = new PlampNativeParser(code);
    //    parser.TryParseMemberAccess(startNode, out var res);
    //    Assert.Equal(errorCount, parser.Exceptions.Count);
    //    if (errorCount != 0)
    //    {
    //        for (int i = 0; i < parser.Exceptions.Count; i++)
    //        {
    //            Assert.Equal(errorTextList[i], parser.Exceptions[i].Message);
    //            Assert.Equal(errorStartPosList[i], parser.Exceptions[i].StartPosition);
    //            Assert.Equal(errorEndPosList[i], parser.Exceptions[i].EndPosition);
    //        }
    //    }
//
    //    if (tokenSequencePos != null)
    //    {
    //        Assert.Equal(tokenSequencePos, parser.TokenSequence.Position);
    //    }
//
    //    var visitor = new TypeTreeVisitor(treeTypeIterator, memberIterator.ToList());
    //    visitor.Visit(res);
    //    visitor.Validate();
    //}
//
    //[Theory]
    //[InlineData("", new[]{typeof(MemberNode)}, new[]{"1"}, -1)]
    //[InlineData(".c().d()", new[]{typeof(CallNode), typeof(CallNode), typeof(MemberNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"1", "c", "d"}, 7)]
    //[InlineData(".c.d()", new[]{typeof(CallNode), typeof(MemberAccessNode), typeof(MemberNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"1", "c", "d"}, 5)]
    //[InlineData(".c().d", new[]{typeof(MemberAccessNode), typeof(CallNode), typeof(MemberNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"1", "c", "d"}, 5)]
    //[InlineData(".c()[2]", new[]{typeof(IndexerNode), typeof(CallNode), typeof(MemberNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"1", "c", "2"}, 6)]
    //[InlineData("[3][2]", new[]{typeof(IndexerNode), typeof(IndexerNode), typeof(MemberNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"1", "3", "2"}, 5)]
    //[InlineData(".d[2]", new[]{typeof(IndexerNode), typeof(MemberAccessNode), typeof(MemberNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"1", "d", "2"}, 4)]
    //[InlineData("[2].d", new[]{typeof(MemberAccessNode), typeof(IndexerNode), typeof(MemberNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"1", "2", "d"}, 4)]
    //[InlineData("++", new[]{typeof(PostfixIncrementNode), typeof(MemberNode)}, new[]{"1"}, 0)]
    //[InlineData("--", new[]{typeof(PostfixDecrementNode), typeof(MemberNode)}, new[]{"1"}, 0)]
    //[InlineData("++ddd", new[]{typeof(PostfixIncrementNode), typeof(MemberNode)}, new[]{"1"}, 0)]
    //[InlineData("--ddd", new[]{typeof(PostfixDecrementNode), typeof(MemberNode)}, new[]{"1"}, 0)]
    //[InlineData("+++", new[]{typeof(PostfixIncrementNode), typeof(MemberNode)}, new[]{"1"}, 0)]
    //[InlineData("---", new[]{typeof(PostfixDecrementNode), typeof(MemberNode)}, new[]{"1"}, 0)]
    //[InlineData("-", new[]{typeof(MemberNode)}, new[]{"1"}, -1)]
    //[InlineData("[2]++", new[]{typeof(PostfixIncrementNode), typeof(IndexerNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"1", "2"}, 3)]
    //[InlineData("++[2]", new[]{typeof(IndexerNode), typeof(PostfixIncrementNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"1", "2"}, 3)]
    //[InlineData(".d++", new[]{typeof(PostfixIncrementNode), typeof(MemberAccessNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"1", "d"}, 2)]
    //[InlineData("++.d", new[]{typeof(MemberAccessNode), typeof(PostfixIncrementNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"1", "d"}, 2)]
    //[InlineData(".d()++", new[]{typeof(PostfixIncrementNode), typeof(CallNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"1", "d"}, 4)]
    //[InlineData("++.d()", new[]{typeof(CallNode), typeof(PostfixIncrementNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"1", "d"}, 4)]
    //[InlineData(".d(a).d(a,c)", new[]{typeof(CallNode), typeof(CallNode), typeof(MemberNode), typeof(MemberNode), typeof(MemberNode), typeof(MemberNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"1", "d", "a", "d", "a", "c"}, 11)]
    //[InlineData(".d(a,b).x", new[]{typeof(MemberAccessNode), typeof(CallNode), typeof(MemberNode), typeof(MemberNode), typeof(MemberNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"1", "d", "a", "b", "x"}, 8)]
    //[InlineData(".x.d(c)", new[]{typeof(CallNode), typeof(MemberAccessNode), typeof(MemberNode), typeof(MemberNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"1", "x", "d", "c"}, 6)]
    //[InlineData(".d(a,b,c)", new[]{typeof(CallNode), typeof(MemberNode), typeof(MemberNode), typeof(MemberNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"1", "d", "a", "b", "c"}, 8)]
    //[InlineData(".d(a)++", new[]{typeof(PostfixIncrementNode), typeof(CallNode), typeof(MemberNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"1", "d", "a"}, 5)]
    //[InlineData("++.d(a)", new[]{typeof(CallNode), typeof(PostfixIncrementNode), typeof(MemberNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"1", "d", "a"}, 5)]
    //[InlineData("[2].d(a)", new[]{typeof(CallNode), typeof(IndexerNode), typeof(MemberNode), typeof(MemberNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"1", "2", "d", "a"}, 7)]
    //[InlineData(".d(a)[2]", new[]{typeof(IndexerNode), typeof(CallNode), typeof(MemberNode), typeof(MemberNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"1", "d", "a", "2"}, 7)]
    //[InlineData("[2.c()", new[]{typeof(MemberNode)}, new[]{"1"}, 6, 1, new[]{ParserErrorConstants.ExpectedCloseParen}, new[]{6}, new[]{6})]
    //[InlineData("[2\"r\"", new[]{typeof(MemberNode)}, new[]{"1"}, 3, 1, new[]{ParserErrorConstants.ExpectedCloseParen}, new[]{2}, new[]{5})]
    //[InlineData("+[2]", new[]{typeof(MemberNode)}, new[]{"1"}, -1)]
    //[InlineData(".c(2\"r\"", new[]{typeof(CallNode), typeof(MemberNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"1","c","2"}, 5, 1, new[]{ParserErrorConstants.ExpectedCloseParen}, new[]{4}, new[]{7})]
    //public void TestParsePostfixIfExist(string code, Type[] treeTypeIterator, string[] memberIterator,
    //    int? tokenSequencePos = null, int errorCount = 0,
    //    string[] errorTextList = null, int[] errorStartPosList = null, int[] errorEndPosList = null)
    //{
    //    var startNode = new MemberNode("1");
    //    var parser = new PlampNativeParser(code);
    //    var res = parser.ParsePostfixIfExist(startNode);
    //    Assert.Equal(errorCount, parser.Exceptions.Count);
    //    if (errorCount != 0)
    //    {
    //        for (int i = 0; i < parser.Exceptions.Count; i++)
    //        {
    //            Assert.Equal(errorTextList[i], parser.Exceptions[i].Message);
    //            Assert.Equal(errorStartPosList[i], parser.Exceptions[i].StartPosition);
    //            Assert.Equal(errorEndPosList[i], parser.Exceptions[i].EndPosition);
    //        }
    //    }
//
    //    if (tokenSequencePos != null)
    //    {
    //        Assert.Equal(tokenSequencePos, parser.TokenSequence.Position);
    //    }
//
    //    var visitor = new TypeTreeVisitor(treeTypeIterator, memberIterator.ToList());
    //    visitor.Visit(res);
    //    visitor.Validate();
    //}
    //
    //[Theory]
    //[InlineData("", new[]{typeof(MemberNode)}, new[]{"1"}, -1)]
    //[InlineData("1", new[]{typeof(MemberNode)}, new[]{"1"}, -1)]
    //[InlineData("++", new[]{typeof(PostfixIncrementNode), typeof(MemberNode)}, new[]{"1"}, 0)]
    //[InlineData("--", new[]{typeof(PostfixDecrementNode), typeof(MemberNode)}, new[]{"1"}, 0)]
    //[InlineData("++++", new[]{typeof(PostfixIncrementNode), typeof(MemberNode)}, new[]{"1"}, 0)]
    //[InlineData("----", new[]{typeof(PostfixDecrementNode), typeof(MemberNode)}, new[]{"1"}, 0)]
    //[InlineData("++--", new[]{typeof(PostfixIncrementNode), typeof(MemberNode)}, new[]{"1"}, 0)]
    //[InlineData("--++", new[]{typeof(PostfixDecrementNode), typeof(MemberNode)}, new[]{"1"}, 0)]
    //[InlineData("++-", new[]{typeof(PostfixIncrementNode), typeof(MemberNode)}, new[]{"1"}, 0)]
    //[InlineData("--+", new[]{typeof(PostfixDecrementNode), typeof(MemberNode)}, new[]{"1"}, 0)]
    //[InlineData("++2", new[]{typeof(PostfixIncrementNode), typeof(MemberNode)}, new[]{"1"}, 0)]
    //[InlineData("--2", new[]{typeof(PostfixDecrementNode), typeof(MemberNode)}, new[]{"1"}, 0)]
    //public void TestParsePostfixOperator(string code, Type[] treeTypeIterator, string[] memberIterator,
    //    int? tokenSequencePos = null, int errorCount = 0,
    //    string[] errorTextList = null, int[] errorStartPosList = null, int[] errorEndPosList = null)
    //{
    //    var startNode = new MemberNode("1");
    //    var parser = new PlampNativeParser(code);
    //    var res = parser.TryParsePostfixOperator(startNode);
    //    Assert.Equal(errorCount, parser.Exceptions.Count);
    //    if (errorCount != 0)
    //    {
    //        for (int i = 0; i < parser.Exceptions.Count; i++)
    //        {
    //            Assert.Equal(errorTextList[i], parser.Exceptions[i].Message);
    //            Assert.Equal(errorStartPosList[i], parser.Exceptions[i].StartPosition);
    //            Assert.Equal(errorEndPosList[i], parser.Exceptions[i].EndPosition);
    //        }
    //    }
//
    //    if (tokenSequencePos != null)
    //    {
    //        Assert.Equal(tokenSequencePos, parser.TokenSequence.Position);
    //    }
//
    //    var visitor = new TypeTreeVisitor(treeTypeIterator, memberIterator.ToList());
    //    visitor.Visit(res);
    //    visitor.Validate();
    //}
    //
    //[Theory]
    //[InlineData("", new Type[]{}, new string[]{}, -1)]
    //[InlineData("\n", new Type[]{}, new string[]{}, -1)]
    ////TD
    //[InlineData("\"123\"", new[]{typeof(ConstNode)}, new string[]{}, 0)]
    //[InlineData("aaa", new[]{typeof(MemberNode)}, new[]{"aaa"}, 0)]
    //[InlineData("321", new[]{typeof(MemberNode)}, new[]{"321"}, 0)]
    //[InlineData("-aaa", new[]{typeof(UnaryMinusNode), typeof(MemberNode)}, new[]{"aaa"}, 1)]
    //[InlineData("-321", new[]{typeof(UnaryMinusNode), typeof(MemberNode)}, new[]{"321"}, 1)]
    //[InlineData("!aaa", new[]{typeof(NotNode), typeof(MemberNode)}, new[]{"aaa"}, 1)]
    //[InlineData("!321", new[]{typeof(NotNode), typeof(MemberNode)}, new[]{"321"}, 1)]
    //[InlineData("++aaa", new[]{typeof(PrefixIncrementNode), typeof(MemberNode)}, new[]{"aaa"}, 1)]
    //[InlineData("++321", new[]{typeof(PrefixIncrementNode), typeof(MemberNode)}, new[]{"321"}, 1)]
    //[InlineData("--aaa", new[]{typeof(PrefixDecrementNode), typeof(MemberNode)}, new[]{"aaa"}, 1)]
    //[InlineData("--321", new[]{typeof(PrefixDecrementNode), typeof(MemberNode)}, new[]{"321"}, 1)]
    //[InlineData("=321", new Type[]{}, new string[]{}, -1)]
    //[InlineData("!!!321", new[]{typeof(NotNode), typeof(NotNode), typeof(NotNode), typeof(MemberNode)}, new[]{"321"}, 3)]
    //[InlineData("(int)x", new[]{typeof(CastNode), typeof(TypeNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"int", "x"}, 3)]
    //[InlineData("(int)(int)x", new[]{typeof(CastNode), typeof(TypeNode), typeof(MemberNode), typeof(CastNode), typeof(TypeNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"int", "int", "x"}, 6)]
    //[InlineData("!(int)!(int)x", new[]{typeof(NotNode), typeof(CastNode), typeof(TypeNode), typeof(MemberNode), typeof(NotNode), typeof(CastNode), typeof(TypeNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"int", "int", "x"}, 8)]
    //[InlineData("(int)", new Type[0], new string[0], -1)]
    //[InlineData("--", new Type[0], new string[0], -1)]
    //[InlineData("(int)(1 + 1)", new []{typeof(CastNode), typeof(TypeNode), typeof(MemberNode), typeof(PlusNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"int", "1", "1"})]
    //[InlineData("new", new Type[0], new string[0], -1)]
    //[InlineData("new int", new Type[0], new string[0], -1)]
    //[InlineData("new int()", new[]{typeof(ConstructorNode), typeof(TypeNode), typeof(MemberNode)}, new []{"int"}, 4)]
    //[InlineData("new int(a, b)", new[]{typeof(ConstructorNode), typeof(TypeNode), typeof(MemberNode), typeof(MemberNode), typeof(MemberNode)}, new []{"int", "a", "b"}, 8)]
    //[InlineData("\"a\"++", new[]{typeof(PostfixIncrementNode), typeof(ConstNode)}, new string[0], 1)]
    //[InlineData("var", new Type[0], new string[0], -1)]
    //[InlineData("var x", new[]{typeof(VariableDefinitionNode), typeof(MemberNode)}, new[]{"x"}, 2)]
    //[InlineData("var var", new Type[0], new string[0], -1, 1, new []{ParserErrorConstants.CannotUseKeyword}, new []{4}, new []{6})]
    //[InlineData("int d", new[]{typeof(VariableDefinitionNode), typeof(TypeNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"int", "d"}, 2)]
    //[InlineData("int", new[]{typeof(MemberNode)}, new[]{"int"}, 0)]
    //[InlineData("int x = 1 + 1", new[]{typeof(AssignNode), typeof(VariableDefinitionNode), typeof(TypeNode), typeof(MemberNode), typeof(MemberNode), typeof(PlusNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"int", "x", "1", "1"}, 10)]
    //[InlineData("var x = 1 + 1", new[]{typeof(AssignNode), typeof(VariableDefinitionNode), typeof(MemberNode), typeof(PlusNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"x", "1", "1"}, 10)]
    //[InlineData("var x=1+", new []{typeof(AssignNode), typeof(VariableDefinitionNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"x", "1"}, 4)]
    //[InlineData("true", new []{typeof(ConstNode)}, new string[0], 0)]
    //[InlineData("false", new []{typeof(ConstNode)}, new string[0], 0)]
    //public void TestTryParseNud(string code, Type[] treeTypeIterator, string[] memberIterator,
    //    int? tokenSequencePos = null, int errorCount = 0,
    //    string[] errorTextList = null, int[] errorStartPosList = null, int[] errorEndPosList = null)
    //{
    //    var parser = new PlampNativeParser(code);
    //    parser.TryParseNud(out var nud);
    //    Assert.Equal(errorCount, parser.Exceptions.Count);
    //    if (errorCount != 0)
    //    {
    //        for (int i = 0; i < parser.Exceptions.Count; i++)
    //        {
    //            Assert.Equal(errorTextList[i], parser.Exceptions[i].Message);
    //            Assert.Equal(errorStartPosList[i], parser.Exceptions[i].StartPosition);
    //            Assert.Equal(errorEndPosList[i], parser.Exceptions[i].EndPosition);
    //        }
    //    }
//
    //    if (tokenSequencePos != null)
    //    {
    //        Assert.Equal(tokenSequencePos, parser.TokenSequence.Position);
    //    }
//
    //    if (nud == null)
    //    {
    //        Assert.Empty(treeTypeIterator);
    //    }
    //    else
    //    {
    //        var visitor = new TypeTreeVisitor(treeTypeIterator, memberIterator.ToList());
    //        visitor.Visit(nud);
    //        visitor.Validate();
    //    }
    //}
    //
    //[Theory]
    //[InlineData("", new Type[0], new string[0], false, -1)]
    //[InlineData("1", new []{typeof(MemberNode)}, new []{"1"}, true, 0)]
    //[InlineData("1+1", new []{typeof(PlusNode), typeof(MemberNode), typeof(MemberNode)}, new []{"1", "1"}, true, 2)]
    //[InlineData("1+1+", new []{typeof(PlusNode), typeof(MemberNode), typeof(MemberNode)}, new []{"1", "1"}, true, 2)]
    //[InlineData("+", new Type[0], new string[0], false, -1)]
    //public void TestTryParseWithPrecedence(
    //    string code, Type[] treeTypeIterator, string[] memberIterator, bool expectedResult,
    //    int? tokenSequencePos = null, int errorCount = 0, int startPrecedence = 0,
    //    string[] errorTextList = null, int[] errorStartPosList = null, int[] errorEndPosList = null)
    //{
    //    var parser = new PlampNativeParser(code);
    //    var actualResult = parser.TryParseWithPrecedence(out var nud, startPrecedence);
    //    Assert.Equal(expectedResult, actualResult);
    //    Assert.Equal(errorCount, parser.Exceptions.Count);
    //    if (errorCount != 0)
    //    {
    //        for (int i = 0; i < parser.Exceptions.Count; i++)
    //        {
    //            Assert.Equal(errorTextList[i], parser.Exceptions[i].Message);
    //            Assert.Equal(errorStartPosList[i], parser.Exceptions[i].StartPosition);
    //            Assert.Equal(errorEndPosList[i], parser.Exceptions[i].EndPosition);
    //        }
    //    }
//
    //    if (tokenSequencePos != null)
    //    {
    //        Assert.Equal(tokenSequencePos, parser.TokenSequence.Position);
    //    }
//
    //    if (nud == null)
    //    {
    //        Assert.Empty(treeTypeIterator);
    //    }
    //    else
    //    {
    //        var visitor = new TypeTreeVisitor(treeTypeIterator, memberIterator.ToList());
    //        visitor.Visit(nud);
    //        visitor.Validate();
    //    }
    //}
//
    //[Theory]
    //[InlineData("", new Type[0], new string[0], false, -1)]
    //[InlineData("while", new []{typeof(WhileNode), typeof(BodyNode)}, new string[0], true, 1, 1, new []{$"{ParserErrorConstants.UnexpectedTokenPrefix} {nameof(OpenParen)}"}, new []{5}, new []{5})]
    //[InlineData("while()", new []{typeof(WhileNode), typeof(BodyNode)}, new string[0], true, 3, 1, new[]{ParserErrorConstants.ExpectedConditionExpression}, new[]{5}, new[]{6})]
    //[InlineData("while(a==1)", new []{typeof(WhileNode), typeof(EqualNode), typeof(MemberNode), typeof(MemberNode), typeof(BodyNode)}, new []{"a", "1"}, true, 6)]
    //[InlineData("while(a==1)\n", new []{typeof(WhileNode), typeof(EqualNode), typeof(MemberNode), typeof(MemberNode), typeof(BodyNode)}, new []{"a", "1"}, true, 6)]
    //[InlineData("while(a==1)\n    var x=0", new []{typeof(WhileNode), typeof(EqualNode), typeof(MemberNode), typeof(MemberNode), typeof(BodyNode), typeof(AssignNode), typeof(VariableDefinitionNode), typeof(MemberNode), typeof(MemberNode)}, new []{"a", "1", "x", "0"}, true, 13)]
    //[InlineData("while(a==1,5+11)\n    var x=0", new []{typeof(WhileNode), typeof(EqualNode), typeof(MemberNode), typeof(MemberNode), typeof(BodyNode), typeof(AssignNode), typeof(VariableDefinitionNode), typeof(MemberNode), typeof(MemberNode)}, new []{"a", "1", "x", "0"},true, 17, 1, new[]{ParserErrorConstants.ExpectedCloseParen}, new[]{10}, new[]{14})]
    //[InlineData("while()\n    var x=0", new []{typeof(WhileNode), typeof(BodyNode), typeof(AssignNode), typeof(VariableDefinitionNode), typeof(MemberNode), typeof(MemberNode)}, new []{"x", "0"}, true, 10, 1, new[]{ParserErrorConstants.ExpectedConditionExpression}, new[]{5}, new[]{6})]
    //[InlineData("while\n    var x=0", new []{typeof(WhileNode), typeof(BodyNode), typeof(AssignNode), typeof(VariableDefinitionNode), typeof(MemberNode), typeof(MemberNode)}, new []{"x", "0"}, true, 8, 1, new []{$"{ParserErrorConstants.UnexpectedTokenPrefix} {nameof(OpenParen)}"}, new []{5}, new []{5})]
    //[InlineData("while(a==1)555\n    var x=0", new []{typeof(WhileNode), typeof(EqualNode), typeof(MemberNode), typeof(MemberNode), typeof(BodyNode), typeof(AssignNode), typeof(VariableDefinitionNode), typeof(MemberNode), typeof(MemberNode)}, new []{"a", "1", "x", "0"}, true, 14, 1, new[]{ParserErrorConstants.ExpectedEndOfLine}, new[]{11}, new[]{14})]
    //public void TestTryParseWhile(
    //    string code, Type[] treeTypeIterator, string[] memberIterator, bool expectedResult,
    //    int? tokenSequencePos = null, int errorCount = 0,
    //    string[] errorTextList = null, int[] errorStartPosList = null, int[] errorEndPosList = null)
    //{
    //    var parser = new PlampNativeParser(code);
    //    var actualResult = parser.TryParseWhileLoop(out var whileNode);
    //    Assert.Equal(expectedResult, actualResult);
    //    Assert.Equal(errorCount, parser.Exceptions.Count);
    //    if (errorCount != 0)
    //    {
    //        for (int i = 0; i < parser.Exceptions.Count; i++)
    //        {
    //            Assert.Equal(errorTextList[i], parser.Exceptions[i].Message);
    //            Assert.Equal(errorStartPosList[i], parser.Exceptions[i].StartPosition);
    //            Assert.Equal(errorEndPosList[i], parser.Exceptions[i].EndPosition);
    //        }
    //    }
//
    //    if (tokenSequencePos != null)
    //    {
    //        Assert.Equal(tokenSequencePos, parser.TokenSequence.Position);
    //    }
//
    //    if (whileNode == null)
    //    {
    //        Assert.Empty(treeTypeIterator);
    //    }
    //    else
    //    {
    //        var visitor = new TypeTreeVisitor(treeTypeIterator, memberIterator.ToList());
    //        visitor.Visit(whileNode);
    //        visitor.Validate();
    //    }
    //}
//
    //[Theory]
    //[InlineData("", true, true, -1, 3, new []{ParserErrorConstants.InvalidExpression, ParserErrorConstants.ExpectedInKeyword, ParserErrorConstants.InvalidExpression}, new[]{-1, -1, -1}, new []{-1,0,-1})]
    //[InlineData("var t", true, false, 2, 2, new []{ParserErrorConstants.ExpectedInKeyword, ParserErrorConstants.InvalidExpression}, new[]{5, 5}, new []{5, 5})]
    //[InlineData("var t in", true, false, 4, 1, new[]{ParserErrorConstants.InvalidExpression}, new []{8}, new []{8})]
    //[InlineData("var t in d", false, false, 6)]
    //[InlineData("in d", false, true, 2, 1, new []{ParserErrorConstants.InvalidExpression}, new[]{-1}, new []{-1})]
    //[InlineData("in", true, true, 0, 2, new []{ParserErrorConstants.InvalidExpression, ParserErrorConstants.InvalidExpression}, new[]{-1, 2}, new []{-1, 2})]
    //[InlineData("var t d", false, false, 4, 1, new []{ParserErrorConstants.ExpectedInKeyword}, new[]{6}, new []{6})]
    //public void TestTryParseForHeader(string code, bool isIterableNull, bool isIteratorNull, int resultPosition, int errorCount = 0,
    //    string[] errorTextList = null, int[] errorStartPosList = null, int[] errorEndPosList = null)
    //{
    //    var parser = new PlampNativeParser(code);
    //    parser.TryParseForHeader(out var holder);
    //    if (isIterableNull)
    //    {
    //        Assert.Null(holder.Iterable);
    //    }
    //    else
    //    {
    //        Assert.NotNull(holder.Iterable);
    //    }
    //    
    //    if (isIteratorNull)
    //    {
    //        Assert.Null(holder.IteratorVar);
    //    }
    //    else
    //    {
    //        Assert.NotNull(holder.IteratorVar);
    //    }
    //    
    //    Assert.Equal(resultPosition, parser.TokenSequence.Position);
    //    Assert.Equal(errorCount, parser.Exceptions.Count);
    //    if (errorCount != 0)
    //    {
    //        for (int i = 0; i < parser.Exceptions.Count; i++)
    //        {
    //            Assert.Equal(errorTextList[i], parser.Exceptions[i].Message);
    //            Assert.Equal(errorStartPosList[i], parser.Exceptions[i].StartPosition);
    //            Assert.Equal(errorEndPosList[i], parser.Exceptions[i].EndPosition);
    //        }
    //    }
    //}
//
    //[Theory]
    //[InlineData("", new Type[0], new string[0], false, -1)]
    //[InlineData("for", new[]{typeof(ForNode), typeof(BodyNode)}, new string[0], true, 1, 1, 
    //    new []{ParserErrorConstants.UnexpectedTokenPrefix + " " + nameof(OpenParen)}, new[]{3}, new[]{3})]
    //[InlineData("for(var i in t)", new[]{typeof(ForNode), typeof(VariableDefinitionNode), typeof(MemberNode), typeof(MemberNode), typeof(BodyNode)}, 
    //    new[]{"i", "t"}, true, 10)]
    //[InlineData("for(var i in t)555", new[]{typeof(ForNode), typeof(VariableDefinitionNode), typeof(MemberNode), typeof(MemberNode), typeof(BodyNode)}, 
    //    new[]{"i", "t"}, true, 11, 1, new[]{ParserErrorConstants.ExpectedEndOfLine}, new[]{15}, new[]{18})]
    //[InlineData("for(var i in t)\n    var x=0", 
    //    new[]{typeof(ForNode), typeof(VariableDefinitionNode), 
    //        typeof(MemberNode), typeof(MemberNode), typeof(BodyNode), 
    //        typeof(AssignNode), typeof(VariableDefinitionNode), typeof(MemberNode), typeof(MemberNode)},
    //    new[]{"i", "t", "x", "0"}, true, 17)]
    //[InlineData("for\n    var x=0", new[]{typeof(ForNode), typeof(BodyNode), typeof(AssignNode), typeof(VariableDefinitionNode), typeof(MemberNode), typeof(MemberNode)},
    //    new []{"x", "0"}, true, 8, 1, new[]{ParserErrorConstants.UnexpectedTokenPrefix + " " + nameof(OpenParen)}, new[]{3}, new[]{3})]
    //public void TryParseForCycle(string code, Type[] treeTypeIterator, string[] memberIterator, bool expectedResult,
    //    int? tokenSequencePos = null, int errorCount = 0,
    //    string[] errorTextList = null, int[] errorStartPosList = null, int[] errorEndPosList = null)
    //{
    //    var parser = new PlampNativeParser(code);
    //    var actualResult = parser.TryParseForLoop(out var forNode);
    //    Assert.Equal(expectedResult, actualResult);
    //    Assert.Equal(errorCount, parser.Exceptions.Count);
    //    if (errorCount != 0)
    //    {
    //        for (int i = 0; i < parser.Exceptions.Count; i++)
    //        {
    //            Assert.Equal(errorTextList[i], parser.Exceptions[i].Message);
    //            Assert.Equal(errorStartPosList[i], parser.Exceptions[i].StartPosition);
    //            Assert.Equal(errorEndPosList[i], parser.Exceptions[i].EndPosition);
    //        }
    //    }
//
    //    if (tokenSequencePos != null)
    //    {
    //        Assert.Equal(tokenSequencePos, parser.TokenSequence.Position);
    //    }
    //    
    //    if (forNode == null)
    //    {
    //        Assert.Empty(treeTypeIterator);
    //    }
    //    else
    //    {
    //        var visitor = new TypeTreeVisitor(treeTypeIterator, memberIterator.ToList());
    //        visitor.Visit(forNode);
    //        visitor.Validate();
    //    }
    //}
    //
    //[Theory]
    //[InlineData("", new []{typeof(ClauseNode), typeof(BodyNode)}, new string[0], true, 0, 1, 
    //    new[]{ParserErrorConstants.UnexpectedTokenPrefix + " " + nameof(OpenParen)}, new []{-1}, new []{0})]
    //[InlineData("()", new []{typeof(ClauseNode), typeof(BodyNode)}, new string[0], true, 2, 1,
    //    new []{ParserErrorConstants.ExpectedConditionExpression}, new []{0}, new[]{1})]
    //[InlineData("(true)", new []{typeof(ClauseNode), typeof(ConstNode), typeof(BodyNode)}, new string[0], true, 3)]
    //[InlineData("(true)555", new []{typeof(ClauseNode), typeof(ConstNode), typeof(BodyNode)}, new string[0], true, 4, 1,
    //    new []{ParserErrorConstants.ExpectedEndOfLine}, new []{6}, new[]{9})]
    //[InlineData("(true)\n    var x=1", new []{typeof(ClauseNode), typeof(ConstNode), typeof(BodyNode), typeof(AssignNode), typeof(VariableDefinitionNode), typeof(MemberNode), typeof(MemberNode)}, new []{"x", "1"}, true, 10)]
    //[InlineData("(true)555\n    var x=1", new []{typeof(ClauseNode), typeof(ConstNode), typeof(BodyNode), typeof(AssignNode), typeof(VariableDefinitionNode), typeof(MemberNode), typeof(MemberNode)}, new []{"x", "1"}, true, 11,
    //    1, new[]{ParserErrorConstants.ExpectedEndOfLine}, new[]{6}, new[]{9})]
    //[InlineData("\n    var x=1", new []{typeof(ClauseNode), typeof(BodyNode), typeof(AssignNode), typeof(VariableDefinitionNode), typeof(MemberNode), typeof(MemberNode)}, new []{"x", "1"}, true, 7,
    //    1, new[]{ParserErrorConstants.UnexpectedTokenPrefix + " " + nameof(OpenParen)}, new[]{0}, new[]{0})]
    //public void TestTryParseClause(string code, Type[] treeTypeIterator, string[] memberIterator, bool expectedResult,
    //    int? tokenSequencePos = null, int errorCount = 0,
    //    string[] errorTextList = null, int[] errorStartPosList = null, int[] errorEndPosList = null)
    //{
    //    var parser = new PlampNativeParser(code);
    //    var actualResult = parser.TryParseConditionClause(out var clauseNode);
    //    Assert.Equal(expectedResult, actualResult);
    //    Assert.Equal(errorCount, parser.Exceptions.Count);
    //    if (errorCount != 0)
    //    {
    //        for (int i = 0; i < parser.Exceptions.Count; i++)
    //        {
    //            Assert.Equal(errorTextList[i], parser.Exceptions[i].Message);
    //            Assert.Equal(errorStartPosList[i], parser.Exceptions[i].StartPosition);
    //            Assert.Equal(errorEndPosList[i], parser.Exceptions[i].EndPosition);
    //        }
    //    }
//
    //    if (tokenSequencePos != null)
    //    {
    //        Assert.Equal(tokenSequencePos, parser.TokenSequence.Position);
    //    }
    //    
    //    if (clauseNode == null)
    //    {
    //        Assert.Empty(treeTypeIterator);
    //    }
    //    else
    //    {
    //        var visitor = new TypeTreeVisitor(treeTypeIterator, memberIterator.ToList());
    //        visitor.Visit(clauseNode);
    //        visitor.Validate();
    //    }
    //}
    //
    //[Theory]
    //[InlineData("", new Type[0], new string[0], false, -1)]
    //[InlineData("if(true)\n    var x=0", new []{typeof(ConditionNode),
    //    typeof(ClauseNode), typeof(ConstNode), typeof(BodyNode), typeof(AssignNode), typeof(VariableDefinitionNode), typeof(MemberNode), typeof(MemberNode)
    //}, new[]{"x", "0"}, true, 11)]
    //[InlineData("if(true)\n    var x=0\nelif(false)\n    var x=1", new []{typeof(ConditionNode),
    //    typeof(ClauseNode), typeof(ConstNode), typeof(BodyNode), typeof(AssignNode), typeof(VariableDefinitionNode), typeof(MemberNode), typeof(MemberNode),
    //    typeof(ClauseNode), typeof(ConstNode), typeof(BodyNode), typeof(AssignNode), typeof(VariableDefinitionNode), typeof(MemberNode), typeof(MemberNode)
    //}, new[]{"x", "0", "x", "1"}, true, 23)]
    //[InlineData("if(true)\n    var x=0\nelse\n    var x=1", new []{typeof(ConditionNode),
    //    typeof(ClauseNode), typeof(ConstNode), typeof(BodyNode), typeof(AssignNode), typeof(VariableDefinitionNode), typeof(MemberNode), typeof(MemberNode),
    //    typeof(BodyNode), typeof(AssignNode), typeof(VariableDefinitionNode), typeof(MemberNode), typeof(MemberNode)
    //}, new[]{"x", "0", "x", "1"}, true, 20)]
    //[InlineData("if(true)\n    var x=0\nelif(false)\n    var x=1\nelse\n    var x=2", new []{typeof(ConditionNode),
    //    typeof(ClauseNode), typeof(ConstNode), typeof(BodyNode), typeof(AssignNode), typeof(VariableDefinitionNode), typeof(MemberNode), typeof(MemberNode),
    //    typeof(ClauseNode), typeof(ConstNode), typeof(BodyNode), typeof(AssignNode), typeof(VariableDefinitionNode), typeof(MemberNode), typeof(MemberNode),
    //    typeof(BodyNode), typeof(AssignNode), typeof(VariableDefinitionNode), typeof(MemberNode), typeof(MemberNode)
    //}, new[]{"x", "0", "x", "1", "x", "2"}, true, 32)]
    //[InlineData("if(true)\n    var x=0\nelif(false)\n    var x=1\nelif(false)\n    var x=3\nelse\n    var x=2", new []{typeof(ConditionNode),
    //    typeof(ClauseNode), typeof(ConstNode), typeof(BodyNode), typeof(AssignNode), typeof(VariableDefinitionNode), typeof(MemberNode), typeof(MemberNode),
    //    typeof(ClauseNode), typeof(ConstNode), typeof(BodyNode), typeof(AssignNode), typeof(VariableDefinitionNode), typeof(MemberNode), typeof(MemberNode),
    //    typeof(ClauseNode), typeof(ConstNode), typeof(BodyNode), typeof(AssignNode), typeof(VariableDefinitionNode), typeof(MemberNode), typeof(MemberNode),
    //    typeof(BodyNode), typeof(AssignNode), typeof(VariableDefinitionNode), typeof(MemberNode), typeof(MemberNode)
    //}, new[]{"x", "0", "x", "1", "x", "3", "x", "2"}, true, 44)]
    //[InlineData("if(true)\n    var x=0\nelse\n    var x=2\nelif(false)\n    var x=1", new []{typeof(ConditionNode),
    //    typeof(ClauseNode), typeof(ConstNode), typeof(BodyNode), typeof(AssignNode), typeof(VariableDefinitionNode), typeof(MemberNode), typeof(MemberNode),
    //    typeof(BodyNode), typeof(AssignNode), typeof(VariableDefinitionNode), typeof(MemberNode), typeof(MemberNode)
    //}, new[]{"x", "0", "x", "2"}, true, 20)]
    //[InlineData("elif(false)\n    var x=1", new Type[0], new string[0], false, -1)]
    //[InlineData("else\n    var x=2", new Type[0], new string[0], false, -1)]
    //public void TestTryParseConditionalExpression(string code, Type[] treeTypeIterator, string[] memberIterator, bool expectedResult,
    //    int? tokenSequencePos = null, int errorCount = 0,
    //    string[] errorTextList = null, int[] errorStartPosList = null, int[] errorEndPosList = null)
    //{
    //    var parser = new PlampNativeParser(code);
    //    var actualResult = parser.TryParseConditionalExpression(out var conditionNode);
    //    Assert.Equal(expectedResult, actualResult);
    //    Assert.Equal(errorCount, parser.Exceptions.Count);
    //    if (errorCount != 0)
    //    {
    //        for (int i = 0; i < parser.Exceptions.Count; i++)
    //        {
    //            Assert.Equal(errorTextList[i], parser.Exceptions[i].Message);
    //            Assert.Equal(errorStartPosList[i], parser.Exceptions[i].StartPosition);
    //            Assert.Equal(errorEndPosList[i], parser.Exceptions[i].EndPosition);
    //        }
    //    }
//
    //    if (tokenSequencePos != null)
    //    {
    //        Assert.Equal(tokenSequencePos, parser.TokenSequence.Position);
    //    }
    //    
    //    if (conditionNode == null)
    //    {
    //        Assert.Empty(treeTypeIterator);
    //    }
    //    else
    //    {
    //        var visitor = new TypeTreeVisitor(treeTypeIterator, memberIterator.ToList());
    //        visitor.Visit(conditionNode);
    //        visitor.Validate();
    //    }
    //}
    //
    //[Theory]
    //[InlineData("", new Type[0], new string[0], false, -1)]
    //[InlineData("var x=0", new Type[0], new string[0], false, -1)]
    //[InlineData("break", new[]{typeof(BreakNode)}, new string[0], true, 1)]
    //[InlineData("break 555", new[]{typeof(BreakNode)}, new string[0], true, 3, 
    //    1, new []{ParserErrorConstants.ExpectedEndOfLine}, new []{5}, new []{9})]
    //[InlineData("continue", new[]{typeof(ContinueNode)}, new string[0], true, 1)]
    //[InlineData("continue 555", new[]{typeof(ContinueNode)}, new string[0], true, 3, 
    //    1, new []{ParserErrorConstants.ExpectedEndOfLine}, new []{8}, new []{12})]
    //[InlineData("return", new[]{typeof(ReturnNode)}, new string[0], true, 1)]
    //[InlineData("return x", new[]{typeof(ReturnNode), typeof(MemberNode)}, new []{"x"}, true, 3)]
    //[InlineData("return x+", new[]{typeof(ReturnNode), typeof(MemberNode)}, new []{"x"}, true, 4, 
    //    1, new []{ParserErrorConstants.ExpectedEndOfLine}, new []{8}, new []{9})]
    //[InlineData("if(true)\n    var x=0", new[]{typeof(ConditionNode), typeof(ClauseNode), typeof(ConstNode), typeof(BodyNode), 
    //    typeof(AssignNode), typeof(VariableDefinitionNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"x", "0"}, true, 11)]
    //[InlineData("for(var d in x)\n    var t=11", new[]{typeof(ForNode), typeof(VariableDefinitionNode), typeof(MemberNode), typeof(MemberNode),
    //    typeof(BodyNode), typeof(AssignNode), typeof(VariableDefinitionNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"d", "x", "t", "11"}, true, 17)]
    //[InlineData("while(true)\n    var x=0", new[]{typeof(WhileNode), typeof(ConstNode), 
    //    typeof(BodyNode), typeof(AssignNode), typeof(VariableDefinitionNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"x", "0"}, true, 11)]
    //public void TestTryParseKeywordExpression(string code, Type[] treeTypeIterator, string[] memberIterator, bool expectedResult,
    //    int? tokenSequencePos = null, int errorCount = 0,
    //    string[] errorTextList = null, int[] errorStartPosList = null, int[] errorEndPosList = null)
    //{
    //    var parser = new PlampNativeParser(code);
    //    var actualResult = parser.TryParseKeywordExpression(out var node);
    //    Assert.Equal(expectedResult, actualResult);
    //    Assert.Equal(errorCount, parser.Exceptions.Count);
    //    if (errorCount != 0)
    //    {
    //        for (int i = 0; i < parser.Exceptions.Count; i++)
    //        {
    //            Assert.Equal(errorTextList[i], parser.Exceptions[i].Message);
    //            Assert.Equal(errorStartPosList[i], parser.Exceptions[i].StartPosition);
    //            Assert.Equal(errorEndPosList[i], parser.Exceptions[i].EndPosition);
    //        }
    //    }
//
    //    if (tokenSequencePos != null)
    //    {
    //        Assert.Equal(tokenSequencePos, parser.TokenSequence.Position);
    //    }
    //    
    //    if (node == null)
    //    {
    //        Assert.Empty(treeTypeIterator);
    //    }
    //    else
    //    {
    //        var visitor = new TypeTreeVisitor(treeTypeIterator, memberIterator.ToList());
    //        visitor.Visit(node);
    //        visitor.Validate();
    //    }
    //}
//
    //[Theory]
    //[InlineData("", new Type[0], new string[0], false, -1)]
    //[InlineData("\n", new[]{typeof(EmptyNode)}, new string[0], true, 0)]
    //[InlineData("break", new[]{typeof(BreakNode)}, new string[0], true, 1)]
    //[InlineData("1+1", new []{typeof(PlusNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"1", "1"}, true, 3)]
    //[InlineData("def int x()", new Type[0], new string[0], false, 7,
    //    1, new[]{ParserErrorConstants.ExpectedBodyLevelExpression}, new []{0}, new[]{11})]
    //public void TestTryParseSingleBodyLevelExpression(string code, Type[] treeTypeIterator, string[] memberIterator, bool expectedResult,
    //    int? tokenSequencePos = null, int errorCount = 0,
    //    string[] errorTextList = null, int[] errorStartPosList = null, int[] errorEndPosList = null)
    //{
    //    var parser = new PlampNativeParser(code);
    //    var actualResult = parser.TryParseBodyLevelExpression(out var node);
    //    Assert.Equal(expectedResult, actualResult);
    //    Assert.Equal(errorCount, parser.Exceptions.Count);
    //    if (errorCount != 0)
    //    {
    //        for (int i = 0; i < parser.Exceptions.Count; i++)
    //        {
    //            Assert.Equal(errorTextList[i], parser.Exceptions[i].Message);
    //            Assert.Equal(errorStartPosList[i], parser.Exceptions[i].StartPosition);
    //            Assert.Equal(errorEndPosList[i], parser.Exceptions[i].EndPosition);
    //        }
    //    }
//
    //    if (tokenSequencePos != null)
    //    {
    //        Assert.Equal(tokenSequencePos, parser.TokenSequence.Position);
    //    }
    //    
    //    if (node == null)
    //    {
    //        Assert.Empty(treeTypeIterator);
    //    }
    //    else
    //    {
    //        var visitor = new TypeTreeVisitor(treeTypeIterator, memberIterator.ToList());
    //        visitor.Visit(node);
    //        visitor.Validate();
    //    }
    //}
//
    //[Theory]
    //[InlineData("", true, 1, true, false, -1)]
    //[InlineData("", true, 1, false, false, -1)]
    //[InlineData("    ", true, 1, true, true, 0)]
    //[InlineData("    ", true, 1, false, false, -1)]
    //[InlineData("        ", true, 1, true, true, 1)]
    //[InlineData("        ", true, 1, false, false, -1)]
    //[InlineData("", false, 1, true, false, -1)]
    //[InlineData("", false, 1, false, false, -1)]
    //[InlineData("    ", false, 1, true, true, 0)]
    //[InlineData("    ", false, 1, false, true, 0)]
    //[InlineData("        ", false, 1, true, true, 1)]
    //[InlineData("        ", false, 1, false, true, 1)]
    //public void TestTryParseScopedWithDepth(string code, bool isStrict, int depth,  bool delegateResult, bool expectedResult,
    //    int? tokenSequencePos = null)
    //{
    //    var parser = new PlampNativeParser(code);
    //    var actualResult = parser.TryParseScopedWithDepth<object>(out _, depth, isStrict);
    //    Assert.Equal(expectedResult, actualResult);
    //    Assert.Empty(parser.Exceptions);
//
    //    if (tokenSequencePos != null)
    //    {
    //        Assert.Equal(tokenSequencePos, parser.TokenSequence.Position);
    //    }
//
    //    bool DelegateFunc(out object res)
    //    {
    //        res = null;
    //        return delegateResult;
    //    }
    //}
//
    //[Theory]
    //[InlineData("", new Type[0], new string[0], false, -1)]
    //[InlineData("int", new []{typeof(TypeNode), typeof(MemberNode)}, new[]{"int"}, true, 0)]
    //[InlineData("<int>", new Type[0], new string[0], false, -1)]
    //[InlineData("List<>", new[]{typeof(TypeNode), typeof(MemberNode)}, new[]{"List"}, true, 2, 
    //    1, new []{ParserErrorConstants.ExpectedInnerGenerics}, new[]{4}, new[]{5})]
    //[InlineData("List<int>", new[]{typeof(TypeNode), typeof(MemberNode), typeof(TypeNode), typeof(MemberNode)}, new[]{"List", "int"}, true, 3)]
    //[InlineData("Dictionary<string, int>", new[]{typeof(TypeNode), typeof(MemberNode), typeof(TypeNode), typeof(MemberNode), typeof(TypeNode), typeof(MemberNode)}, new[]{"Dictionary", "string", "int"}, true, 6)]
    //[InlineData("System.int", new[]{typeof(TypeNode), typeof(MemberNode)}, new[]{"System.int"}, true, 2)]
    //[InlineData("Generic.List<int>", new[]{typeof(TypeNode), typeof(MemberNode), typeof(TypeNode), typeof(MemberNode)}, new[]{"Generic.List", "int"}, true, 5)]
    //[InlineData("List<System.int>", new[]{typeof(TypeNode), typeof(MemberNode), typeof(TypeNode), typeof(MemberNode)}, new[]{"List", "System.int"}, true, 5)]
    //[InlineData("List<System.>", new[]{typeof(TypeNode), typeof(MemberNode), typeof(TypeNode), typeof(MemberNode)}, new[]{"List", "System"}, true, 4,
    //    1, new[]{ParserErrorConstants.InvalidTypeName}, new[]{5}, new[]{11})]
    //[InlineData("List<int,>", new[]{typeof(TypeNode), typeof(MemberNode), typeof(TypeNode), typeof(MemberNode)}, new[]{"List", "int"}, true, 4, 
    //    1, new[]{ParserErrorConstants.ExpectedType}, new[]{8}, new[]{8})]
    //[InlineData("Generic.<int>", new[]{typeof(TypeNode), typeof(MemberNode), typeof(TypeNode), typeof(MemberNode)}, new[]{"Generic", "int"}, true, 4,
    //    1, new []{ParserErrorConstants.InvalidTypeName}, new []{0}, new[]{7})]
    //public void TestTryParseTypeStrict(string code, Type[] treeTypeIterator, string[] memberIterator, bool expectedResult,
    //    int? tokenSequencePos = null, int errorCount = 0,
    //    string[] errorTextList = null, int[] errorStartPosList = null, int[] errorEndPosList = null)
    //{
    //    var parser = new PlampNativeParser(code);
    //    var actualResult = parser.TryParseType(out var node, true);
    //    Assert.Equal(expectedResult, actualResult);
    //    Assert.Equal(errorCount, parser.Exceptions.Count);
    //    if (errorCount != 0)
    //    {
    //        for (int i = 0; i < parser.Exceptions.Count; i++)
    //        {
    //            Assert.Equal(errorTextList[i], parser.Exceptions[i].Message);
    //            Assert.Equal(errorStartPosList[i], parser.Exceptions[i].StartPosition);
    //            Assert.Equal(errorEndPosList[i], parser.Exceptions[i].EndPosition);
    //        }
    //    }
//
    //    if (tokenSequencePos != null)
    //    {
    //        Assert.Equal(tokenSequencePos, parser.TokenSequence.Position);
    //    }
    //    
    //    if (node == null)
    //    {
    //        Assert.Empty(treeTypeIterator);
    //    }
    //    else
    //    {
    //        var visitor = new TypeTreeVisitor(treeTypeIterator, memberIterator.ToList());
    //        visitor.Visit(node);
    //        visitor.Validate();
    //    }
    //}
//
    //[Theory]
    //[InlineData("", new Type[0], new string[0], false, -1)]
    //[InlineData("int", new[]{typeof(ParameterNode), typeof(TypeNode), typeof(MemberNode)}, new[]{"int"}, true, 0,
    //    1, new[]{ParserErrorConstants.ExpectedParameterName}, new[]{3}, new[]{3})]
    //[InlineData(".", new Type[0], new string[0], false, -1)]
    //[InlineData("int param", new []{typeof(ParameterNode), typeof(TypeNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"int", "param"}, true, 2)]
    //[InlineData("int var", new []{typeof(ParameterNode), typeof(TypeNode), typeof(MemberNode)}, new[]{"int"}, true, 2,
    //    1, new[]{ParserErrorConstants.CannotUseKeyword}, new[]{4}, new[]{6})]
    //public void TestTryParseParameter(string code, Type[] treeTypeIterator, string[] memberIterator, bool expectedResult,
    //    int? tokenSequencePos = null, int errorCount = 0,
    //    string[] errorTextList = null, int[] errorStartPosList = null, int[] errorEndPosList = null)
    //{
    //    var parser = new PlampNativeParser(code);
    //    var actualResult = parser.TryParseParameter(out var node);
    //    Assert.Equal(expectedResult, actualResult);
    //    Assert.Equal(errorCount, parser.Exceptions.Count);
    //    if (errorCount != 0)
    //    {
    //        for (int i = 0; i < parser.Exceptions.Count; i++)
    //        {
    //            Assert.Equal(errorTextList[i], parser.Exceptions[i].Message);
    //            Assert.Equal(errorStartPosList[i], parser.Exceptions[i].StartPosition);
    //            Assert.Equal(errorEndPosList[i], parser.Exceptions[i].EndPosition);
    //        }
    //    }
//
    //    if (tokenSequencePos != null)
    //    {
    //        Assert.Equal(tokenSequencePos, parser.TokenSequence.Position);
    //    }
    //    
    //    if (node == null)
    //    {
    //        Assert.Empty(treeTypeIterator);
    //    }
    //    else
    //    {
    //        var visitor = new TypeTreeVisitor(treeTypeIterator, memberIterator.ToList());
    //        visitor.Visit(node);
    //        visitor.Validate();
    //    }
    //}
//
    //[Theory]
    //[InlineData("", new []{typeof(BodyNode)}, new string[0], -1)]
    //[InlineData("\n", new []{typeof(BodyNode)}, new string[0], -1)]
    //[InlineData("    \n", new []{typeof(BodyNode), typeof(EmptyNode)}, new string[0], 2)]
    //[InlineData("    \n12", new []{typeof(BodyNode), typeof(EmptyNode)}, new string[0], 1)]
    //[InlineData("    \n    \n", new []{typeof(BodyNode), typeof(EmptyNode), typeof(EmptyNode)}, new string[0], 4)]
    //[InlineData("        \n", new []{typeof(BodyNode), typeof(EmptyNode)}, new string[0], 3)]
    //[InlineData("        \n    \n", new []{typeof(BodyNode), typeof(EmptyNode), typeof(EmptyNode)}, new string[0], 5)]
    //public void TestTryParseBody(string code, Type[] treeTypeIterator, string[] memberIterator, int? tokenSequencePos = null)
    //{
    //    var parser = new PlampNativeParser(code);
    //    var actualResult = parser.TryParseBody(out var node);
    //    Assert.True(actualResult);
    //    Assert.Empty(parser.Exceptions);
//
    //    if (tokenSequencePos != null)
    //    {
    //        Assert.Equal(tokenSequencePos, parser.TokenSequence.Position);
    //    }
    //    
    //    if (node == null)
    //    {
    //        Assert.Empty(treeTypeIterator);
    //    }
    //    else
    //    {
    //        var visitor = new TypeTreeVisitor(treeTypeIterator, memberIterator.ToList());
    //        visitor.Visit(node);
    //        visitor.Validate();
    //    }
    //}
//
    //[Theory]
    //[InlineData("", new Type[0], new string[0], false, -1)]
    //[InlineData("var", new Type[0], new string[0], false, -1)]
    //[InlineData("def", new []{typeof(DefNode), typeof(BodyNode)}, new string[0], true, 1, 
    //    2, new[]{ParserErrorConstants.ExpectedFunctionName, ParserErrorConstants.UnexpectedTokenPrefix + " " + nameof(OpenParen)}, new[]{3, 3}, new[]{3, 3})]
    //[InlineData("def int", new[]{typeof(DefNode), typeof(TypeNode), typeof(MemberNode), typeof(BodyNode)}, new[]{"int"}, true, 3,
    //    2, new[]{ParserErrorConstants.ExpectedFunctionName, ParserErrorConstants.UnexpectedTokenPrefix + " " + nameof(OpenParen)}, new[]{7, 7}, new[]{7, 7})]
    //[InlineData("def()", new[]{typeof(DefNode), typeof(BodyNode)}, new string[0], true, 3,
    //    1, new[]{ParserErrorConstants.ExpectedFunctionName}, new[]{3}, new[]{3})]
    //[InlineData("def int()", new[]{typeof(DefNode), typeof(TypeNode), typeof(MemberNode), typeof(BodyNode)}, new[]{"int"}, true, 5,
    //    1, new[]{ParserErrorConstants.ExpectedFunctionName}, new[]{7}, new[]{7})]
    //[InlineData("def int call", new[]{typeof(DefNode), typeof(TypeNode), typeof(MemberNode), typeof(MemberNode), typeof(BodyNode)}, new[]{"int", "call"}, true, 5,
    //    1, new[]{ParserErrorConstants.UnexpectedTokenPrefix + " " + nameof(OpenParen)}, new[]{12}, new[]{12})]
    //[InlineData("def int call()", new[]{typeof(DefNode), typeof(TypeNode), typeof(MemberNode), typeof(MemberNode), typeof(BodyNode)}, new[]{"int", "call"}, true, 7)]
    //[InlineData("def int call()555", new[]{typeof(DefNode), typeof(TypeNode), typeof(MemberNode), typeof(MemberNode), typeof(BodyNode)}, new[]{"int", "call"}, true, 8,
    //    1, new[]{ParserErrorConstants.ExpectedEndOfLine}, new []{14}, new[]{17})]
    //public void TestTryParseFunction(string code, Type[] treeTypeIterator, string[] memberIterator, bool expectedResult,
    //    int? tokenSequencePos = null, int errorCount = 0,
    //    string[] errorTextList = null, int[] errorStartPosList = null, int[] errorEndPosList = null)
    //{
    //    var parser = new PlampNativeParser(code);
    //    var actualResult = parser.TryParseFunction(out var node);
    //    Assert.Equal(expectedResult, actualResult);
    //    Assert.Equal(errorCount, parser.Exceptions.Count);
    //    if (errorCount != 0)
    //    {
    //        for (int i = 0; i < parser.Exceptions.Count; i++)
    //        {
    //            Assert.Equal(errorTextList[i], parser.Exceptions[i].Message);
    //            Assert.Equal(errorStartPosList[i], parser.Exceptions[i].StartPosition);
    //            Assert.Equal(errorEndPosList[i], parser.Exceptions[i].EndPosition);
    //        }
    //    }
//
    //    if (tokenSequencePos != null)
    //    {
    //        Assert.Equal(tokenSequencePos, parser.TokenSequence.Position);
    //    }
    //    
    //    if (node == null)
    //    {
    //        Assert.Empty(treeTypeIterator);
    //    }
    //    else
    //    {
    //        var visitor = new TypeTreeVisitor(treeTypeIterator, memberIterator.ToList());
    //        visitor.Visit(node);
    //        visitor.Validate();
    //    }
    //}
//
    //[Theory]
    //[InlineData("", new Type[0], new string[0], false, -1)]
    //[InlineData("var", new Type[0], new string[0], false, -1)]
    //[InlineData("use", new Type[0], new string[0], false, 1,
    //     1, new[]{ParserErrorConstants.InvalidAssemblyName}, new[]{3}, new[]{3})]
    //[InlineData("use System", new []{typeof(UseNode), typeof(MemberNode)}, new []{"System"}, true, 3)]
    //[InlineData("use var", new Type[0], new string[0], false, 3,
    //    2, new[]{ParserErrorConstants.InvalidAssemblyName, ParserErrorConstants.ExpectedEndOfLine}, new[]{3, 3}, new[]{6, 7})]
    //[InlineData("use System.Collections", new []{typeof(UseNode), typeof(MemberNode)}, new []{"System.Collections"}, true, 5)]
    //[InlineData("use System.", new []{typeof(UseNode), typeof(MemberNode)}, new []{"System"}, true, 4,
    //    1, new[]{ParserErrorConstants.InvalidAssemblyName}, new[]{3}, new[]{10})]
    //[InlineData("use System 555", new []{typeof(UseNode), typeof(MemberNode)}, new []{"System"}, true, 5,
    //    1, new[]{ParserErrorConstants.ExpectedEndOfLine}, new[]{10}, new[]{14})]
    //public void TestTryParseUsing(string code, Type[] treeTypeIterator, string[] memberIterator, bool expectedResult,
    //    int? tokenSequencePos = null, int errorCount = 0,
    //    string[] errorTextList = null, int[] errorStartPosList = null, int[] errorEndPosList = null)
    //{
    //    var parser = new PlampNativeParser(code);
    //    var actualResult = parser.TryParseUsing(out var node);
    //    Assert.Equal(expectedResult, actualResult);
    //    Assert.Equal(errorCount, parser.Exceptions.Count);
    //    if (errorCount != 0)
    //    {
    //        for (int i = 0; i < parser.Exceptions.Count; i++)
    //        {
    //            Assert.Equal(errorTextList[i], parser.Exceptions[i].Message);
    //            Assert.Equal(errorStartPosList[i], parser.Exceptions[i].StartPosition);
    //            Assert.Equal(errorEndPosList[i], parser.Exceptions[i].EndPosition);
    //        }
    //    }
//
    //    if (tokenSequencePos != null)
    //    {
    //        Assert.Equal(tokenSequencePos, parser.TokenSequence.Position);
    //    }
    //    
    //    if (node == null)
    //    {
    //        Assert.Empty(treeTypeIterator);
    //    }
    //    else
    //    {
    //        var visitor = new TypeTreeVisitor(treeTypeIterator, memberIterator.ToList());
    //        visitor.Visit(node);
    //        visitor.Validate();
    //    }
    //}
    //
    //[Theory]
    //[InlineData("", false, true, -1)]
    //[InlineData("x", false, true, -1)]
    //[InlineData("\n", true, false, 0)]
    //[InlineData("\nx", true, false, 0)]
    //public void TestTryParseEmpty(string code, bool result, bool isNull, int resultPosition)
    //{
    //    var parser = new PlampNativeParser(code);
    //    var actualResult = parser.TryParseEmpty(out var node);
    //    Assert.Equal(result, actualResult);
    //    if (isNull)
    //    {
    //        Assert.Null(node);
    //    }
    //    else
    //    {
    //        Assert.Equal(typeof(EmptyNode), node.GetType());
    //    }
    //    Assert.Empty(parser.Exceptions);
    //    Assert.Equal(resultPosition, parser.TokenSequence.Position);
    //}
//
    //[Theory]
    //[InlineData("", new Type[0], new string[0], false, -1)]
    //[InlineData("\n", new []{typeof(EmptyNode)}, new string[0], true, 0)]
    //[InlineData("    \n", new []{typeof(EmptyNode)}, new string[0], true, 1)]
    //[InlineData("var x=3", new Type[0], new string[0], false, 5,
    //    1, new []{ParserErrorConstants.ExpectedTopLevel}, new[]{-1}, new[]{7})]
    //[InlineData("use System", new []{typeof(UseNode), typeof(MemberNode)}, new[]{"System"}, true, 3)]
    //[InlineData("    use System", new []{typeof(UseNode), typeof(MemberNode)}, new[]{"System"}, true, 4)]
    //[InlineData("    def int call()\n    return 0", new[]{typeof(DefNode), typeof(TypeNode), typeof(MemberNode), typeof(MemberNode), typeof(BodyNode)}, new []{"int", "call"}, true, 8)]
    //[InlineData("def int call()\n    return 0", new[]{typeof(DefNode), typeof(TypeNode), typeof(MemberNode), typeof(MemberNode), typeof(BodyNode), typeof(ReturnNode), typeof(MemberNode)},
    //    new []{"int", "call", "0"}, true, 12)]
    //public void TestTryParseTopLevel(string code, Type[] treeTypeIterator, string[] memberIterator, bool expectedResult,
    //    int? tokenSequencePos = null, int errorCount = 0,
    //    string[] errorTextList = null, int[] errorStartPosList = null, int[] errorEndPosList = null)
    //{
    //    var parser = new PlampNativeParser(code);
    //    var actualResult = parser.TryParseTopLevel(out var node);
    //    Assert.Equal(expectedResult, actualResult);
    //    Assert.Equal(errorCount, parser.Exceptions.Count);
    //    if (errorCount != 0)
    //    {
    //        for (int i = 0; i < parser.Exceptions.Count; i++)
    //        {
    //            Assert.Equal(errorTextList[i], parser.Exceptions[i].Message);
    //            Assert.Equal(errorStartPosList[i], parser.Exceptions[i].StartPosition);
    //            Assert.Equal(errorEndPosList[i], parser.Exceptions[i].EndPosition);
    //        }
    //    }
//
    //    if (tokenSequencePos != null)
    //    {
    //        Assert.Equal(tokenSequencePos, parser.TokenSequence.Position);
    //    }
    //    
    //    if (node == null)
    //    {
    //        Assert.Empty(treeTypeIterator);
    //    }
    //    else
    //    {
    //        var visitor = new TypeTreeVisitor(treeTypeIterator, memberIterator.ToList());
    //        visitor.Visit(node);
    //        visitor.Validate();
    //    }
    //}
}