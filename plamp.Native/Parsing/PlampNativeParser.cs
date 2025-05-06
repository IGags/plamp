using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Assign;
using plamp.Abstractions.Ast.Node.Binary;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.Ast.Node.ControlFlow;
using plamp.Abstractions.Ast.Node.Extensions;
using plamp.Abstractions.Ast.Node.Unary;
using plamp.Abstractions.Compilation;
using plamp.Abstractions.Compilation.Models;
using plamp.Abstractions.Parsing;
using plamp.Native.Parsing.Symbols;
using plamp.Native.Parsing.Transactions;
using plamp.Native.Tokenization;
using plamp.Native.Tokenization.Enumerations;
using plamp.Native.Tokenization.Token;

namespace plamp.Native.Parsing;

//NOW THREAD SAFE :^)
//TODO: refactor this
public sealed class PlampNativeParser : IParser
{
    internal delegate ExpressionParsingResult TryParseInternal<T>(out T result, ParsingContext context);

    //TODO: fixit
    public ParserResult Parse(SourceFile sourceFile, AssemblyName assemblyName, CancellationToken cancellationToken)
    {
        var context = BuildContext(sourceFile, assemblyName);

        var expressionList = new List<NodeBase>();

        while (context.TokenSequence.PeekNext() != null 
               || (context.TokenSequence.PeekNext() != null && context.TokenSequence.Current() == null))
        {
            TryParseTopLevel(out var node, context);
            if (node != null)
            {
                expressionList.Add(node);
            }
        }

        var symbolTable = new PlampNativeSymbolTable(context.TransactionSource.SymbolDictionary);
        return new ParserResult(expressionList, context.TransactionSource.Exceptions, symbolTable);
    }

    internal static ParsingContext BuildContext(SourceFile sourceFile, AssemblyName assemblyName)
    {
        var tokenRes = sourceFile.Tokenize(assemblyName);
        var transactionSource = new ParsingTransactionSource(
            tokenRes.Sequence, 
            tokenRes.Exceptions, 
            new());
        
        return new ParsingContext(
            tokenRes.Sequence, 0, transactionSource,
            assemblyName, sourceFile.FileName);
    }
    
    internal static ExpressionParsingResult TryParseTopLevel(out NodeBase resultNode, ParsingContext context)
    {
        resultNode = null;
        if (context.TokenSequence.PeekNext() == null)
        {
            return ExpressionParsingResult.FailedNeedRollback;
        }
        
        var transaction = context.TransactionSource.BeginTransaction();
        if (TryParseScopedWithDepth<EmptyNode>(TryParseEmpty, out var empty, context) == ExpressionParsingResult.Success)
        {
            resultNode = empty;
            transaction.Commit();
            return ExpressionParsingResult.Success;
        }
        transaction.Rollback();
        
        var handleList = new List<DepthHandle>();
        while (TryConsumeNext<WhiteSpace>(_ => true, _ => {}, out var space, context))
        {
            if (space.Kind == WhiteSpaceKind.Scope)
            {
                handleList.Add(context.Depth.EnterNewScope());
            }
        }

        handleList.Reverse();
        
        var token = context.TokenSequence.PeekNextNonWhiteSpace();
        if (token is KeywordToken keyword)
        {
            switch (keyword.Keyword)
            {
                case Keywords.Def:
                    var defRes = TryParseFunction(out var defNode, context);
                    resultNode = defNode;
                    DisposeHandles();
                    return defRes;
                case Keywords.Use:
                    var useRes = TryParseUsing(out var useNode, context);
                    resultNode = useNode;
                    DisposeHandles();
                    return useRes;
            }
        }
        
        transaction =context.TransactionSource.BeginTransaction();
        AdvanceToRequestedTokenWithException<EndOfLine>(transaction, context);
        transaction.Commit();
        return ExpressionParsingResult.FailedNeedCommit;
        
        void DisposeHandles()
        {
            foreach (var handle in handleList)
            {
                handle.Dispose();
            }
        }
    }

    private static ExpressionParsingResult TryParseEmpty(out EmptyNode node, ParsingContext context)
    {
        var transaction = context.TransactionSource.BeginTransaction();
        if (TryConsumeNextNonWhiteSpace<EndOfLine>(_ => true, _ => {}, out var eol, context))
        {
            node = new EmptyNode();
            transaction.AddSymbol(node, [], [eol]);
            transaction.Commit();
            return ExpressionParsingResult.Success;
        }

        node = null;
        transaction.Rollback();
        return ExpressionParsingResult.FailedNeedRollback;
    }

    private static ExpressionParsingResult TryParseUsing(out UseNode node, ParsingContext context)
    {
        node = null;
        if (!TryConsumeNextNonWhiteSpace<KeywordToken>(
                x => x.Keyword == Keywords.Use, _ => { }, 
                out var useKeyword,
                context))
        {
            throw new Exception("Internal parser bug");
        }
        
        var transaction = context.TransactionSource.BeginTransaction();
        var res = ParseMemberAccessSequence(transaction, out var useMember, context);
        if (res == ExpressionParsingResult.FailedNeedRollback)
        {
            transaction.Rollback();
            var next = context.TokenSequence.PeekNextNonWhiteSpace();
            AdvanceToEndOfLineOrRequested<EndOfLine>(context);
            var current = context.TokenSequence.Current();
            transaction = context.TransactionSource.BeginTransaction();
            AddExceptionToTheTokenRange(next, current,
                PlampNativeExceptionInfo.InvalidUsingName(),
                transaction, context);
            transaction.Commit();
            return ExpressionParsingResult.FailedNeedCommit;
        }
        
        AdvanceToRequestedTokenWithException<EndOfLine>(transaction, context);
        node = new UseNode(useMember);
        transaction.AddSymbol(node, [useMember], [useKeyword]);
        transaction.Commit();
        
        return ExpressionParsingResult.Success;
    }

    private static ExpressionParsingResult TryParseFunction(out DefNode node, ParsingContext context)
    {
        node = null;
        if (context.TokenSequence.PeekNextNonWhiteSpace() == null 
            || !TryConsumeNextNonWhiteSpace<KeywordToken>(
                x => x.Keyword == Keywords.Def, _ => { }, out var def, context))
        {
            return ExpressionParsingResult.FailedNeedRollback;
        }

        var transaction = context.TransactionSource.BeginTransaction();
        var res = TryParseType(transaction, out var typeNode, context, false);
        if (res == ExpressionParsingResult.FailedNeedRollback)
        {
            AddExceptionToTheTokenRange(def, def,
                PlampNativeExceptionInfo.InvalidDefMissingReturnType(), 
                transaction, 
                context);
            
            AdvanceToRequestedTokenWithException<EndOfLine>(transaction, context);
            AddBodyException(transaction, context);
            transaction.Commit();
            return ExpressionParsingResult.FailedNeedCommit;
        }

        if (!TryConsumeNextNonWhiteSpace<Word>(_ => true, _ => { }, out var name, context))
        {
            AddExceptionToTheTokenRange(def, def,
                PlampNativeExceptionInfo.InvalidDefMissingName(), 
                transaction, 
                context);
            AdvanceToRequestedTokenWithException<EndOfLine>(transaction, context);
            AddBodyException(transaction, context);
            transaction.Commit();
            return ExpressionParsingResult.FailedNeedCommit;
        }

        var nameNode = new MemberNode(name.GetStringRepresentation());
        transaction.AddSymbol(nameNode, [], [name]);
        
        //TOO HARD
        res = TryParseInParen<List<NodeBase>, OpenParen, CloseParen>(
            transaction,
            WrapParseCommaSeparated<NodeBase>(
                ParameterWrapper, ExpressionParsingResult.FailedNeedCommit),
            (_, _) => [], out var parameterNodes, 
            ExpressionParsingResult.FailedNeedPass, 
            ExpressionParsingResult.Success, 
            context);
        
        if (res == ExpressionParsingResult.FailedNeedPass)
        {
            transaction.AddException(
                PlampNativeExceptionInfo.ExpectedArgDefinition().GetPlampException(def, context.FileName, context.AssemblyName));
            AdvanceToRequestedTokenWithException<EndOfLine>(transaction, context);
        }

        var body = ParseOptionalBody(transaction, context);
        node = new DefNode(typeNode, nameNode, parameterNodes ?? [], body);
        var children = new List<NodeBase>()
        {
            typeNode, nameNode
        };
        if (parameterNodes != null)
        {
            children.AddRange(parameterNodes);
        }
        children.Add(body);

        transaction.AddSymbol(node, children.ToArray(), [def]);
        transaction.Commit();
        return ExpressionParsingResult.Success;

        ExpressionParsingResult ParameterWrapper(out NodeBase node, ParsingContext _)
        {
            return TryParseParameter(transaction, out node, context);
        }
    }

    private static void TryParseBody(out BodyNode body, ParsingContext context)
    {
        using var handle = context.Depth.EnterNewScope();
        var expressions = new List<NodeBase>();
        while (true)
        {
            var transaction = context.TransactionSource.BeginTransaction();
            var res = TryParseScopedWithDepth<NodeBase>(
                TryParseBodyLevelExpression, out var expression, context);
            if (res == ExpressionParsingResult.Success)
            {
                AdvanceToRequestedTokenWithException<EndOfLine>(transaction, context);
                expressions.Add(expression);
                transaction.Commit();
                continue;
            }
            
            transaction.Rollback();
            break;
        }
        
        body = new BodyNode(expressions);
        var outerTransaction = context.TransactionSource.BeginTransaction();
        outerTransaction.AddSymbol(body, expressions.ToArray(), []);
        outerTransaction.Commit();
    }

    private static ExpressionParsingResult TryParseParameter(IParsingTransaction transaction, out NodeBase parameterNode, ParsingContext context)
    {
        parameterNode = null;
        var typePeek = context.TokenSequence.PeekNextNonWhiteSpace();
        if (typePeek == null || typePeek.GetType() != typeof(Word))
        {
            return ExpressionParsingResult.FailedNeedPass;
        }
        
        TryParseType(transaction, out var type, context);
        var argPeek = context.TokenSequence.PeekNextNonWhiteSpace();
        if (argPeek is null || argPeek.GetType() != typeof(Word))
        {
            AddExceptionToTheTokenRange(
                typePeek,
                argPeek,
                PlampNativeExceptionInfo.InvalidParameterDefinition(),
                transaction,
                context);
            return ExpressionParsingResult.FailedNeedCommit;
        }

        TryConsumeNextNonWhiteSpace<Word>(_ => true, _ => { }, out _, context);
        var name = new MemberNode(argPeek.GetStringRepresentation());
        transaction.AddSymbol(name, [], [argPeek]);

        parameterNode = new ParameterNode(type, name);
        transaction.AddSymbol(parameterNode, [type, name], []);
        return ExpressionParsingResult.Success;
    }
    
    internal static ExpressionParsingResult TryParseType(IParsingTransaction transaction, out NodeBase typeNode, ParsingContext context, bool strict = true)
    {
        typeNode = null;
        var res = ParseMemberAccessSequence(transaction, out var typeMember, context);
        
        if (res == ExpressionParsingResult.FailedNeedRollback) return ExpressionParsingResult.FailedNeedRollback;

        var inner = context.TransactionSource.BeginTransaction();
        TryParseInParen<List<NodeBase>, OpenAngleBracket, CloseAngleBracket>(transaction,
            WrapParseCommaSeparated(
                TryParseTypeWrapper(transaction, context), 
                ExpressionParsingResult.FailedNeedCommit),
            (start, end)
                =>
            {
                AddExceptionToTheTokenRange(
                    start, end, 
                    PlampNativeExceptionInfo.InvalidGenericDefinition(),
                    transaction,context);
                return null;
            },
            out var types, 
            ExpressionParsingResult.FailedNeedPass, 
            ExpressionParsingResult.Success,
            context);
        
        if (context.TokenSequence.Current()?.GetType() != typeof(CloseAngleBracket) 
            && strict)
        {
            inner.Rollback();
            types = null;
        }
        else
        {
            inner.Commit();
        }
        
        
        //Member access should put previous member chain in first arg
        typeNode = new TypeNode(typeMember, types);
        var children = new List<NodeBase>{typeMember};
        if(types != null) children.AddRange(types);
        transaction.AddSymbol(typeNode, children.ToArray(), []);
        
        return ExpressionParsingResult.Success;
    }
    
    /// <summary>
    /// Parsing member access
    /// or member if one
    /// </summary>
    private static ExpressionParsingResult ParseMemberAccessSequence(IParsingTransaction transaction, out NodeBase node, ParsingContext context)
    {
        node = null;
        var peek = context.TokenSequence.PeekNextNonWhiteSpace();
        if (peek == null || peek.GetType() != typeof(Word))
        {
            return ExpressionParsingResult.FailedNeedRollback;
        }
        context.TokenSequence.GetNextNonWhiteSpace();
        
        var members = new List<TokenBase>(){peek};
        
        while (true)
        {
            if (!TryConsumeNextNonWhiteSpace<OperatorToken>(x => x.Operator == OperatorEnum.MemberAccess, 
                    _ => { },
                    out var op, context))
            {
                break;
            }
            members.Add(op);
            
            if (TryConsumeNextNonWhiteSpace<Word>(_ => true, _ =>
                {
                    AddExceptionToTheTokenRange(peek, op, PlampNativeExceptionInfo.InvalidTypeName(),
                        transaction, context);
                }, out var word, context))
            {
                members.Add(word);
            }
            else
            {
                node = new MemberNode(string.Concat(members.Select(x => x.GetStringRepresentation())));
                transaction.AddSymbol(node, [], members.ToArray());
                return ExpressionParsingResult.FailedNeedCommit;
            }
        }
        
        node = new MemberNode(string.Join('.', members));
        return ExpressionParsingResult.Success;
    }
    
    private static TryParseInternal<NodeBase> TryParseTypeWrapper(IParsingTransaction transaction, ParsingContext _)
    {
        return FuncWrapper;
            
        ExpressionParsingResult FuncWrapper(out NodeBase result, ParsingContext context)
        {
            return TryParseType(transaction, out result, context);
        }
    }

    private static ExpressionParsingResult TryParseScopedWithDepth<TReturn>(
        TryParseInternal<TReturn> @internal,
        out TReturn result,
        ParsingContext context,
        int depth = -1)
    {
        if (depth < 0)
        {
            depth = (int)context.Depth;
        }

        var currentDepth = 0;
        while (TryConsumeNext<WhiteSpace>(t =>
               {
                   if (t.Kind == WhiteSpaceKind.Scope)
                   {
                       currentDepth++;
                   }

                   return true;
               }, _ => { }, out _,
               context)) { }

        if (currentDepth < depth)
        {
            result = default;
            return ExpressionParsingResult.FailedNeedRollback;
        }

        var res = @internal(out result, context);
        
        return res;
    }

    internal static ExpressionParsingResult TryParseBodyLevelExpression(out NodeBase expression, ParsingContext context)
    {
        expression = null;
        if (context.TokenSequence.PeekNextNonWhiteSpace() is null)
        {
            return ExpressionParsingResult.FailedNeedCommit;
        }
        
        if (TryParseEmpty(out var emptyNode, context) == ExpressionParsingResult.Success)
        {
            expression = emptyNode;
            return ExpressionParsingResult.Success;
        }

        var transaction = context.TransactionSource.BeginTransaction();
        var res = TryParseKeywordExpression(transaction, out var keywordExpression, context);
        if (res == ExpressionParsingResult.Success 
            || res == ExpressionParsingResult.FailedNeedCommit)
        {
            expression = keywordExpression;
            transaction.Commit();
            return ExpressionParsingResult.Success;
        }
        transaction.Rollback();

        return TryParseWithPrecedence(out expression, context);
    }

    internal static ExpressionParsingResult TryParseKeywordExpression(IParsingTransaction transaction, out NodeBase expression, ParsingContext context)
    {
        expression = null;
        if (!TryConsumeNextNonWhiteSpace<KeywordToken>(_ => true, _ => { }, out var keyword, context))
            return ExpressionParsingResult.FailedNeedPass;
        
        switch (keyword.Keyword)
        {
            case Keywords.Break:
                expression = new BreakNode();
                transaction.AddSymbol(expression, [], [keyword]);
                AdvanceToRequestedTokenWithException<EndOfLine>(transaction, context);
                return ExpressionParsingResult.Success;
            case Keywords.Continue:
                expression = new ContinueNode();
                transaction.AddSymbol(expression, [], [keyword]);
                AdvanceToRequestedTokenWithException<EndOfLine>(transaction, context);
                return ExpressionParsingResult.Success;
            case Keywords.Return:
                TryParseWithPrecedence(out var precedence, context);
                expression = new ReturnNode(precedence);
                transaction.AddSymbol(expression, precedence == null ? [] : [precedence], [keyword]);
                AdvanceToRequestedTokenWithException<EndOfLine>(transaction, context);
                return ExpressionParsingResult.Success;
            case Keywords.If:
                var res 
                    = TryParseConditionalExpression(keyword, transaction, out var node, context);
                AdvanceToRequestedTokenWithException<EndOfLine>(transaction, context);
                expression = node;
                return res;
            case Keywords.For:
                res = TryParseForLoop(keyword, transaction, out var forNode, context);
                AdvanceToRequestedTokenWithException<EndOfLine>(transaction, context);
                expression = forNode;
                return res;
            case Keywords.While:
                res = TryParseWhileLoop(transaction, keyword, out var whileNode, context);
                expression = whileNode;
                return res;
            default:
                return ExpressionParsingResult.FailedNeedPass;
        }
    }

    private static ExpressionParsingResult TryParseConditionalExpression(
        KeywordToken ifClauseKeyword,
        IParsingTransaction transaction,
        out ConditionNode conditionNode,
        ParsingContext context)
    {
        conditionNode = null;
        
        var ifClauseRes = TryParseConditionClause(ifClauseKeyword, transaction, out var baseClause, context);
        if(ifClauseRes == ExpressionParsingResult.FailedNeedCommit)
        {
            return ifClauseRes;
        }
        
        var elifClauses = new List<ClauseNode>();

        while (true)
        {
            if(TryParseEmpty(out _, context) == ExpressionParsingResult.Success) continue;
            var condTrans = context.TransactionSource.BeginTransaction();
            
            if (TryParseScopedWithDepth(TryParseElifKeyword, out KeywordToken keyword, context) != ExpressionParsingResult.Success)
            {
                condTrans.Rollback();
                break;
            }
            
            if (keyword != null)
            {
                //TODO: Skip body with match depth
                TryParseConditionClause(keyword, condTrans, out var elifClause, context);
                if(elifClause != null) elifClauses.Add(elifClause);
            }
            condTrans.Commit();
        }
        
        var elseBody = default(BodyNode);
        if (TryConsumeNextNonWhiteSpace<KeywordToken>(x => x.Keyword == Keywords.Else, _ => {}, out _, context))
        {
            elseBody = ParseOptionalBody(transaction, context);
        }
            
        conditionNode = new ConditionNode(baseClause, elifClauses, elseBody);
        var children = new List<NodeBase>(elifClauses.Count + 2)
            {baseClause};
        children.AddRange(elifClauses);
        if(elseBody != null) children.Add(elseBody);
        transaction.AddSymbol(conditionNode, children.ToArray(), []);
        return ExpressionParsingResult.Success;

        ExpressionParsingResult TryParseElifKeyword(out KeywordToken res, ParsingContext _)
        {
             return TryConsumeNextNonWhiteSpace(x => x.Keyword == Keywords.Elif, _ => { }, out res, context) 
                 ? ExpressionParsingResult.Success : ExpressionParsingResult.FailedNeedRollback;
        }
    }

    private static ExpressionParsingResult TryParseConditionClause(
        KeywordToken clauseDefinition, 
        IParsingTransaction transaction, 
        out ClauseNode conditionNode,
        ParsingContext context)
    {
        conditionNode = null;
        var res = TryParseInParen<NodeBase, OpenParen, CloseParen>(
            transaction, TryParseWithPrecedence, (
                start, end) 
                =>
            {
                AddExceptionToTheTokenRange(
                    start, end, 
                    PlampNativeExceptionInfo.EmptyConditionPredicate(),
                    transaction, context);
                return null;
            },
            out var condition,
            ExpressionParsingResult.FailedNeedCommit, ExpressionParsingResult.Success, context);

        if (res == ExpressionParsingResult.FailedNeedCommit)
        {
            AddExceptionToTheTokenRange(clauseDefinition, clauseDefinition,
                PlampNativeExceptionInfo.MissingConditionPredicate(), transaction, context);
            AdvanceToRequestedTokenWithException<EndOfLine>(transaction, context);
            AddBodyException(transaction, context);
            return ExpressionParsingResult.FailedNeedCommit;
        }
        var body = ParseOptionalBody(transaction, context);
        conditionNode = new ClauseNode(condition, body);
        transaction.AddSymbol(conditionNode, [condition, body], [clauseDefinition]);
        return res;
    }

    private static ExpressionParsingResult TryParseForLoop(
        KeywordToken keyword, 
        IParsingTransaction transaction, 
        out NodeBase counterLoopHolder,
        ParsingContext context)
    {
        counterLoopHolder = null;

        var res = TryParseInParen<CounterLoopHolder, OpenParen, CloseParen>(
            transaction, ForHeaderWrapper, (_, _) => default, out var holder,
            ExpressionParsingResult.FailedNeedCommit, ExpressionParsingResult.FailedNeedPass, context);
        
        if (res == ExpressionParsingResult.FailedNeedCommit)
        {
            AddExceptionToTheTokenRange(keyword, keyword, 
                PlampNativeExceptionInfo.InvalidForHeader(), transaction, context);
            AdvanceToRequestedTokenWithException<EndOfLine>(transaction, context);
            AddBodyException(transaction, context);
            return ExpressionParsingResult.FailedNeedCommit;
        }

        var body = ParseOptionalBody(transaction, context);
            
        if (holder.ForeachHeaderHolder == default)
        {
            counterLoopHolder = new ForNode(
                holder.ForHeaderHolder.IteratorVar,
                holder.ForHeaderHolder.TilCondition,
                holder.ForHeaderHolder.Counter,
                body);
            transaction.AddSymbol(
                counterLoopHolder, 
                [
                    holder.ForHeaderHolder.IteratorVar, 
                    holder.ForHeaderHolder.TilCondition, 
                    holder.ForHeaderHolder.Counter,
                    body
                ],
                [keyword]);
        }
        else
        {
            counterLoopHolder = new ForeachNode(
                holder.ForeachHeaderHolder.IteratorVar,
                holder.ForeachHeaderHolder.Iterable,
                body);
            transaction.AddSymbol(
                counterLoopHolder,
                [
                    holder.ForeachHeaderHolder.IteratorVar,
                    holder.ForeachHeaderHolder.Iterable,
                    body
                ],
                [keyword]);
        }
        
        return ExpressionParsingResult.Success;

        ExpressionParsingResult ForHeaderWrapper(out CounterLoopHolder header, ParsingContext _) =>
            TryParseForHeader(transaction, out header, context);
    }

    private readonly record struct ForeachHeaderHolder(
        NodeBase IteratorVar, NodeBase Iterable);
    
    private readonly record struct ForHeaderHolder(
        NodeBase IteratorVar, NodeBase TilCondition, NodeBase Counter);

    private readonly record struct CounterLoopHolder(
        ForeachHeaderHolder ForeachHeaderHolder,
        ForHeaderHolder ForHeaderHolder);
    
    private static ExpressionParsingResult TryParseForHeader(
        IParsingTransaction transaction, 
        out CounterLoopHolder loop,
        ParsingContext context)
    {
        var innerTransaction = context.TransactionSource.BeginTransaction();
        TryParseWithPrecedence(out var iteratorVar, context);
        //TODO: may add to symbol table
        if (TryConsumeNextNonWhiteSpace<KeywordToken>(
                x => x.Keyword == Keywords.In,
                _ => { },
                out _, context))
        {
            innerTransaction.Commit();
            TryParseWithPrecedence(out var iterable, context);
            loop = new CounterLoopHolder(
                new ForeachHeaderHolder(iteratorVar, iterable),
                default);
        }
        else if (TryConsumeNextNonWhiteSpace<Comma>(
                     _ => true,
                     _ => { },
                     out _,
                     context))
        {
            innerTransaction.Commit();
            TryParseWithPrecedence(out var tilCondition, context);
            var res = TryConsumeNextNonWhiteSpace<Comma>(
                _ => true,
                _ => { },
                out _,
                context);
            if (!res)
            {
                transaction.AddException(
                    PlampNativeExceptionInfo.Expected(nameof(Comma))
                        .GetPlampException(context.TokenSequence.PeekNext(), context.FileName, context.AssemblyName));
            }

            TryParseWithPrecedence(out var counter, context);
            loop = new CounterLoopHolder(
                default,
                new ForHeaderHolder(iteratorVar, tilCondition, counter));
        }
        else
        {
            innerTransaction.Rollback();
            loop = default;
            AdvanceToEndOfLineOrRequested<CloseParen>(context);
            //dirty hack
            context.TokenSequence.Position--;
            return ExpressionParsingResult.FailedNeedCommit;
        }
        
        return ExpressionParsingResult.Success;
    }
    
    private static ExpressionParsingResult TryParseWhileLoop(
        IParsingTransaction transaction, 
        KeywordToken whileToken,
        out WhileNode whileNode,
        ParsingContext context)
    {
        var res = TryParseInParen<NodeBase, OpenParen, CloseParen>(
            transaction, TryParseWithPrecedence, (from, to) =>
            {
                AddExceptionToTheTokenRange(from, to,
                    PlampNativeExceptionInfo.EmptyConditionPredicate(),
                    transaction, context);
                return null;
            }, out var expression, 
            ExpressionParsingResult.FailedNeedCommit, ExpressionParsingResult.Success,
            context);
        
        switch (res)
        {
            case ExpressionParsingResult.Success:
                var body = ParseOptionalBody(transaction, context);
                whileNode = new WhileNode(expression, body);
                transaction.AddSymbol(whileNode, [expression, body], [whileToken]);
                return ExpressionParsingResult.Success;
            case ExpressionParsingResult.FailedNeedCommit:
                AddExceptionToTheTokenRange(whileToken, whileToken, 
                    PlampNativeExceptionInfo.MissingConditionPredicate(), transaction, context);
                AdvanceToRequestedTokenWithException<EndOfLine>(transaction, context);
                AddBodyException(transaction, context);
                whileNode = null;
                return ExpressionParsingResult.FailedNeedCommit;
        }
        
        //Never invoked in common cases
        throw new Exception("Parser exception");
    }

    /// <summary>
    /// Add exception to matching body calls from end of line
    /// </summary>
    private static void AddBodyException(
        IParsingTransaction transaction,
        ParsingContext context)
    {
        using var handle = context.Depth.EnterNewScope();
        while (true)
        {
            var res
                = TryParseScopedWithDepth<NodeBase>(AddExceptionToBodyLevelWrapper, out _, context);
            if (res != ExpressionParsingResult.Success)
            {
                return;
            }
        }

        ExpressionParsingResult AddExceptionToBodyLevelWrapper(out NodeBase res, ParsingContext _)
        {
            return AddExceptionToBodyLevel(transaction, out res, context);
        }
    }

    private static ExpressionParsingResult AddExceptionToBodyLevel(
        IParsingTransaction transaction, 
        out NodeBase result,
        ParsingContext context)
    {
        var next = context.TokenSequence.PeekNext();
        AdvanceToEndOfLineOrRequested<EndOfLine>(context);
        var end = context.TokenSequence.Current();
        AddExceptionToTheTokenRange(
            next, end, PlampNativeExceptionInfo.InvalidBody(), transaction, context);
        result = null;
        return ExpressionParsingResult.Success;
    }
    
    private static BodyNode ParseOptionalBody(
        IParsingTransaction transaction,
        ParsingContext context)
    {
        if (context.TokenSequence.PeekNext()?.GetType() != typeof(EndOfLine))
        {
            TryParseBodyLevelExpression(out var expression, context);
            if (context.TokenSequence.Current() is not EndOfLine)
            {
                AdvanceToRequestedTokenWithException<EndOfLine>(transaction, context);
            }

            List<NodeBase> expressions = expression == null ? [] : [expression];
            var bodyNode = new BodyNode(expressions);
            transaction.AddSymbol(bodyNode, expressions.ToArray(), []);
            return bodyNode;
        }

        if (!TryConsumeNextNonWhiteSpace<EndOfLine>(_ => true, _ => { }, out _, context))
        {
            AdvanceToRequestedTokenWithException<EndOfLine>(transaction, context);
        }

        TryParseBody(out var body, context);
        return body;
    }

    #region ExpressionParsing

    internal enum ExpressionParsingResult
    {
        Success,
        FailedNeedRollback,
        FailedNeedCommit,
        FailedNeedPass,
    }

    internal static ExpressionParsingResult TryParseWithPrecedence(out NodeBase node, ParsingContext context) 
        => TryParseWithPrecedence(out node, 0, context);

    private static ExpressionParsingResult TryParseWithPrecedence(out NodeBase node, int rbp, ParsingContext context)
    {
        var nudParsingResult = TryParseNud(out node, context);
        if (nudParsingResult != ExpressionParsingResult.Success)
        {
            return nudParsingResult;
        }

        while (TryParseLed(rbp, node, out node, context) == ExpressionParsingResult.Success)
        {
        }

        return ExpressionParsingResult.Success;
    }

    private static ExpressionParsingResult TryParseNud(
        out NodeBase node,
        ParsingContext context)
    {
        var transaction = context.TransactionSource.BeginTransaction();
        var result = TryParseVariableDeclaration(transaction, out node, context);
        switch (result)
        {
            case ExpressionParsingResult.Success:
                transaction.Commit();
                return ExpressionParsingResult.Success;
            case ExpressionParsingResult.FailedNeedCommit:
                transaction.Commit();
                return ExpressionParsingResult.FailedNeedCommit;
            case ExpressionParsingResult.FailedNeedRollback:
                transaction.Rollback();
                break;
            case ExpressionParsingResult.FailedNeedPass:
            default:
                transaction.Pass();
                break;
        }

        transaction = context.TransactionSource.BeginTransaction();
        switch (TryParseCastOperator(transaction, out var typeCasting, context))
        {
            case ExpressionParsingResult.Success:
                if (TryParseNud(out var cast, context) == ExpressionParsingResult.Success)
                {
                    node = new CastNode(typeCasting, cast);
                    transaction.AddSymbol(node, [typeCasting, cast], []);
                    transaction.Commit();
                    return ExpressionParsingResult.Success;
                }
                transaction.Rollback();
                return ExpressionParsingResult.FailedNeedCommit;
            case ExpressionParsingResult.FailedNeedRollback:
                transaction.Rollback();
                break;
            case ExpressionParsingResult.FailedNeedCommit:
                transaction.Commit();
                return ExpressionParsingResult.FailedNeedCommit;
        }

        transaction = context.TransactionSource.BeginTransaction();
        switch (TryParseSubExpression(transaction, out var sub, context))
        {
            case ExpressionParsingResult.Success:
                node = sub;
                node = ParsePostfixIfExist(sub, context);
                transaction.Commit();
                return ExpressionParsingResult.Success;
            case ExpressionParsingResult.FailedNeedCommit:
                transaction.Commit();
                return ExpressionParsingResult.FailedNeedCommit;
            case ExpressionParsingResult.FailedNeedRollback:
            default:
                transaction.Rollback();
                break;
        }

        transaction = context.TransactionSource.BeginTransaction();
        var ctorParseRes = TryParseConstructor(transaction, out var ctor, context);
        switch (ctorParseRes)
        {
            case ExpressionParsingResult.Success:
                node = ctor;
                transaction.Commit();
                return ExpressionParsingResult.Success;
            case ExpressionParsingResult.FailedNeedCommit:
                transaction.Commit();
                return ExpressionParsingResult.FailedNeedCommit;
            case ExpressionParsingResult.FailedNeedRollback:
            default:
                transaction.Rollback();
                break;
        }

        if (TryParsePrefixOperator(out node, context) == ExpressionParsingResult.Success)
        {
            return ExpressionParsingResult.Success;
        }

        if (TryParseLiteral(out node, context) == ExpressionParsingResult.Success)
        {
            return ExpressionParsingResult.Success;
        }

        if (TryConsumeNextNonWhiteSpace<Word>(_ => true, _ => { }, out var word, context))
        {
            transaction = context.TransactionSource.BeginTransaction();
            var member = new MemberNode(word.GetStringRepresentation());
            transaction.AddSymbol(member, [], [word]);
            transaction.Commit();
            node = ParsePostfixIfExist(member, context);
            return ExpressionParsingResult.Success;
        }

        node = null;
        transaction.Rollback();
        
        
        return ExpressionParsingResult.FailedNeedCommit;
    }

    private static ExpressionParsingResult TryParseLed(
        int rbp, 
        NodeBase left, 
        out NodeBase output, 
        ParsingContext context)
    {
        var transaction = context.TransactionSource.BeginTransaction();
        SkipLineBreak(context);
        
        if (TryConsumeNextNonWhiteSpace<OperatorToken>(_ => true, _ => { }, out var token, context))
        {
            var precedence = token.GetPrecedence(false);
            if (precedence <= rbp)
            {
                output = left;
                transaction.Rollback();
                return ExpressionParsingResult.FailedNeedCommit;
            }

            var res = TryParseWithPrecedence(out var right, precedence, context);
            
            switch (token.Operator)
            {
                case OperatorEnum.Multiply:
                    output = new MultiplyNode(left, right);
                    break;
                case OperatorEnum.Divide:
                    output = new DivideNode(left, right);
                    break;
                case OperatorEnum.Plus:
                    output = new PlusNode(left, right);
                    break;
                case OperatorEnum.Minus:
                    output = new MinusNode(left, right);
                    break;
                case OperatorEnum.Lesser:
                    output = new LessNode(left, right);
                    break;
                case OperatorEnum.Greater:
                    output = new GreaterNode(left, right);
                    break;
                case OperatorEnum.LesserOrEquals:
                    output = new LessOrEqualNode(left, right);
                    break;
                case OperatorEnum.GreaterOrEquals:
                    output = new GreaterOrEqualNode(left, right);
                    break;
                case OperatorEnum.Equals:
                    output = new EqualNode(left, right);
                    break;
                case OperatorEnum.NotEquals:
                    output = new NotEqualNode(left, right);
                    break;
                case OperatorEnum.And:
                    output = new AndNode(left, right);
                    break;
                case OperatorEnum.Or:
                    output = new OrNode(left, right);
                    break;
                case OperatorEnum.Modulo:
                    output = new ModuloNode(left, right);
                    break;
                case OperatorEnum.Assign:
                    output = new AssignNode(left, right);
                    break;
                case OperatorEnum.PlusAndAssign:
                    output = new AddAndAssignNode(left, right);
                    break;
                case OperatorEnum.MinusAndAssign:
                    output = new SubAndAssignNode(left, right);
                    break;
                case OperatorEnum.MultiplyAndAssign:
                    output = new MulAndAssignNode(left, right);
                    break;
                case OperatorEnum.DivideAndAssign:
                    output = new DivAndAssignNode(left, right);
                    break;
                case OperatorEnum.ModuloAndAssign:
                    output = new ModuloAndAssignNode(left, right);
                    break;
                case OperatorEnum.AndAndAssign:
                    output = new AndAndAssignNode(left, right);
                    break;
                case OperatorEnum.OrAndAssign:
                    output = new OrAndAssignNode(left, right);
                    break;
                case OperatorEnum.XorAndAssign:
                    output = new XorAndAssignNode(left, right);
                    break;
                case OperatorEnum.BitwiseAnd:
                    output = new BitwiseAndNode(left, right);
                    break;
                case OperatorEnum.BitwiseOr:
                    output = new BitwiseOrNode(left, right);
                    break;
                case OperatorEnum.Xor:
                    output = new XorNode(left, right);
                    break;
                default:
                    throw new Exception();
            }
            transaction.AddSymbol(output, [left, right], [token]);
            transaction.Commit();
            return res;
        }
        
        transaction.Rollback();
        output = left;
        return ExpressionParsingResult.FailedNeedCommit;
    }

    private static ExpressionParsingResult TryParseVariableDeclaration(
        IParsingTransaction transaction, 
        out NodeBase variableDeclaration,
        ParsingContext context)
    {
        var typ = default(NodeBase);
        if (!TryConsumeNextNonWhiteSpace<KeywordToken>(
                x => x.Keyword == Keywords.Var, _ => { }, out var varWord, context)
            && TryParseType(transaction, out typ, context) != ExpressionParsingResult.Success)
        {
            variableDeclaration = null;
            return ExpressionParsingResult.FailedNeedRollback;
        }
        
        //Null denotation starts with variable declaration
        if (TryConsumeNextNonWhiteSpaceWithoutRollback<Word>(
                _ => true,
                token => transaction.AddException(
                    PlampNativeExceptionInfo.ExpectedIdentifier()
                        .GetPlampException(token,
                            context.FileName,
                            context.AssemblyName)), 
                out var name, context))
        {
            var variableName = new MemberNode(name.GetStringRepresentation());
            variableDeclaration = new VariableDefinitionNode(typ, variableName);
            transaction.AddSymbol(variableName, [], [name]);
            var children = new List<NodeBase>();
            if (typ != null)
            {
                children.Add(typ);
            }
            children.Add(variableName);

            transaction.AddSymbol(
                variableDeclaration, 
                children.ToArray(), 
                varWord != null ? [varWord] : []);
            
            return ExpressionParsingResult.Success;
        }
        variableDeclaration = null;
        return ExpressionParsingResult.FailedNeedRollback;
    }

    private static ExpressionParsingResult TryParseCastOperator(
        IParsingTransaction transaction, 
        out NodeBase cast,
        ParsingContext context)
    {
        cast = null;
        return TryParseInParen<NodeBase, OpenParen, CloseParen>(
            transaction, TryParseTypeWrapper(transaction, context),
            (_, _) => null, out cast,
            ExpressionParsingResult.FailedNeedRollback, 
            ExpressionParsingResult.FailedNeedRollback,
            context);
    }

    private static ExpressionParsingResult TryParseSubExpression(
        IParsingTransaction transaction, 
        out NodeBase sub,
        ParsingContext context)
    {
        return TryParseInParen<NodeBase, OpenParen, CloseParen>(
            transaction, TryParseWithPrecedence, (open, close) =>
            {
                AddExceptionToTheTokenRange(open, close, PlampNativeExceptionInfo.ExpectedExpression(),
                    transaction, context);
                return null;
            },
            out sub, 
            ExpressionParsingResult.FailedNeedRollback, 
            ExpressionParsingResult.FailedNeedCommit,
            context);
    }

    private static ExpressionParsingResult TryParseLiteral(out NodeBase node, ParsingContext context)
    {
        node = null;
        IParsingTransaction transaction;
        if (TryConsumeNextNonWhiteSpace<StringLiteral>(_ => true, _ => { }, out var literal, context))
        {
            transaction = context.TransactionSource.BeginTransaction();
            var stringLiteral = new LiteralNode(literal.GetStringRepresentation(), typeof(string));
            transaction.AddSymbol(stringLiteral, [], [literal]);
            transaction.Commit();
            node = ParsePostfixIfExist(stringLiteral, context);
            return ExpressionParsingResult.Success;
        }

        if (TryConsumeNextNonWhiteSpace<NumberLiteral>(_ => true, _ => { }, out var numberLiteral, context))
        {
            transaction = context.TransactionSource.BeginTransaction();
            var number = new LiteralNode(numberLiteral.ActualValue, numberLiteral.ActualType);
            transaction.AddSymbol(number, [], [numberLiteral]);
            transaction.Commit();
            node = ParsePostfixIfExist(number, context);
            return ExpressionParsingResult.Success;
        }

        if (TryConsumeNextNonWhiteSpace(
                t => t.Keyword is Keywords.True or Keywords.False, _ => { },
                out KeywordToken boolLiteral,
                context))
        {
            transaction = context.TransactionSource.BeginTransaction();
            var value = bool.Parse(boolLiteral.GetStringRepresentation());
            var boolNode = new LiteralNode(value, typeof(bool));
            transaction.AddSymbol(boolNode, [], [boolLiteral]);
            transaction.Commit();
            node = ParsePostfixIfExist(boolNode, context);
            return ExpressionParsingResult.Success;
        }

        if (!TryConsumeNextNonWhiteSpace(
                t => t.Keyword is Keywords.Null, _ => { }, out KeywordToken nullToken, context))
            return ExpressionParsingResult.FailedNeedRollback;
        
        var nullNode = new LiteralNode(null, null);
        transaction = context.TransactionSource.BeginTransaction();
        transaction.AddSymbol(nullNode, [], [nullToken]);
        transaction.Commit();
        node = ParsePostfixIfExist(nullNode, context);
        return ExpressionParsingResult.Success;
    }

    private static ExpressionParsingResult TryParseConstructor(
        IParsingTransaction transaction, 
        out NodeBase ctor,
        ParsingContext context)
    {
        ctor = null;
        if (!TryConsumeNextNonWhiteSpace<KeywordToken>(x => x.Keyword == Keywords.New, _ => { }, out var keywordToken, context))
        {
            return ExpressionParsingResult.FailedNeedRollback;
        }
        
        if (TryParseType(transaction, out var type, context) != ExpressionParsingResult.Success)
        {
            return ExpressionParsingResult.FailedNeedRollback;
        }
        
        var typeEnd = context.TokenSequence.Current();
        var parenRes = TryParseInParen<List<NodeBase>, OpenParen, CloseParen>(
            transaction,
            WrapParseCommaSeparated<NodeBase>(TryParseWithPrecedence, ExpressionParsingResult.FailedNeedCommit),
            (_, _) => [], out var parameters, ExpressionParsingResult.FailedNeedRollback,
            ExpressionParsingResult.Success,
            context);
        
        switch (parenRes)
        {
            case ExpressionParsingResult.Success:
                ctor = new ConstructorCallNode(type, parameters);
                var children = new List<NodeBase> { type };
                children.AddRange(parameters);
                transaction.AddSymbol(ctor, children.ToArray(), [keywordToken]);
                return ExpressionParsingResult.Success;
            case ExpressionParsingResult.FailedNeedRollback:
                return ExpressionParsingResult.FailedNeedRollback;
            //Dead branch current focus is not in exception precision
            case ExpressionParsingResult.FailedNeedCommit:
                AddExceptionToTheTokenRange(keywordToken, typeEnd,
                    PlampNativeExceptionInfo.Expected("arguments in ()"), transaction, context);
                break;
        }

        return ExpressionParsingResult.FailedNeedCommit;
    }

    private static ExpressionParsingResult TryParsePrefixOperator(out NodeBase node, ParsingContext context)
    {
        node = null;
        var transaction = context.TransactionSource.BeginTransaction();
        if (!TryConsumeNextNonWhiteSpace<OperatorToken>(
                x => x.Operator 
                    is OperatorEnum.Minus 
                    or OperatorEnum.Not 
                    or OperatorEnum.Increment
                    or OperatorEnum.Decrement,
                _ => { }, out var operatorToken,
                context))
        {
            transaction.Rollback();
            return ExpressionParsingResult.FailedNeedRollback;
        }
        
        var res 
            = TryParseWithPrecedence(out var inner, operatorToken.GetPrecedence(true), context);

        if (res != ExpressionParsingResult.Success)
        {
            transaction.Rollback();
            return ExpressionParsingResult.FailedNeedRollback;
        }
        
        node = operatorToken.Operator switch
        {
            OperatorEnum.Minus => new UnaryMinusNode(inner),
            OperatorEnum.Not => new NotNode(inner),
            OperatorEnum.Increment => new PrefixIncrementNode(inner),
            OperatorEnum.Decrement => new PrefixDecrementNode(inner),
            _ => throw new ArgumentOutOfRangeException()
        };
        transaction.AddSymbol(node, [inner], [operatorToken]);
        transaction.Commit();
        node = ParsePostfixIfExist(node, context);
        return ExpressionParsingResult.Success;

    }

    private static NodeBase ParsePostfixIfExist(NodeBase inner, ParsingContext context)
    {
        while (true)
        {
            if (TryParsePostfixOperator(inner, out var node, context))
            {
                inner = node;
                continue;
            }

            if (TryParseIndexer(inner, out node, context))
            {
                inner = node;
                continue;
            }

            if (TryParseCall(inner, out node, context))
            {
                inner = node;
                continue;
            }
            
            if (!TryParseMemberAccess(inner, out node, context)) return inner;
            inner = node;
        }
    }

    private static bool TryParsePostfixOperator(NodeBase nodeBase, out NodeBase node, ParsingContext context)
    {
        node = null;
        if (!TryConsumeNextNonWhiteSpace<OperatorToken>(
                x => x is { Operator: OperatorEnum.Increment or OperatorEnum.Decrement },
                _ => { }, out var @operator,
                context))
        {
            return false;
        }
        
        node =  @operator.Operator switch
        {
            OperatorEnum.Increment => new PostfixIncrementNode(nodeBase),
            OperatorEnum.Decrement => new PostfixDecrementNode(nodeBase),
            _ => throw new Exception("Parser exception")
        };
        var transaction = context.TransactionSource.BeginTransaction();
        transaction.AddSymbol(node, [nodeBase], [@operator]);
        transaction.Commit();
        return true;

    }

    private static bool TryParseIndexer(NodeBase inner, out NodeBase node, ParsingContext context)
    {
        var next = context.TokenSequence.PeekNextNonWhiteSpace();
        if (next is not OpenSquareBracket)
        {
            node = null;
            return false;
        }

        var transaction = context.TransactionSource.BeginTransaction();
        var isParsed 
            = TryParseInParen<List<NodeBase>, OpenSquareBracket, CloseSquareBracket>(
            transaction, 
            WrapParseCommaSeparated<NodeBase>(
                TryParseWithPrecedence, ExpressionParsingResult.FailedNeedCommit), (_, _) => [], 
            out var index, 
            ExpressionParsingResult.FailedNeedRollback, ExpressionParsingResult.Success,
            context);
        
        if (isParsed == ExpressionParsingResult.Success)
        {
            node = new IndexerNode(inner, index);
            var children = new List<NodeBase>() { inner };
            children.AddRange(index);
            transaction.AddSymbol(node, children.ToArray(), []);
            transaction.Commit();
            return true;
        }
        transaction.Rollback();
        node = null;
        return false;
    }

    private static bool TryParseMemberAccess(NodeBase input, out NodeBase res, ParsingContext context)
    {
        res = null;
        var transaction = context.TransactionSource.BeginTransaction();
        if (!TryConsumeNextNonWhiteSpace<OperatorToken>(
                x => x.Operator == OperatorEnum.MemberAccess, _ => { },
                out var access, context))
        {
            transaction.Rollback();
            return false;
        }

        if (TryConsumeNextNonWhiteSpace<Word>(_ => true, _ => { }, out var word, context))
        {
            var member = new MemberNode(word.GetStringRepresentation());
            res = new MemberAccessNode(input, member);
            transaction.AddSymbol(member, [], [word]);
            transaction.AddSymbol(res, [input, member], [access]);
            transaction.Commit();
            return true;
        }
        
        transaction.Rollback();
        return false;
    }

    private static bool TryParseCall(NodeBase input, out NodeBase res, ParsingContext context)
    {
        res = null;
        var transaction = context.TransactionSource.BeginTransaction();
        var parenRes = TryParseInParen<List<NodeBase>, OpenParen, CloseParen>(
            transaction,
            WrapParseCommaSeparated<NodeBase>(TryParseWithPrecedence, ExpressionParsingResult.FailedNeedPass),
            (_, _) => [], out var args, 
            ExpressionParsingResult.FailedNeedRollback, ExpressionParsingResult.Success, context);
        switch (parenRes)
        {
            case ExpressionParsingResult.Success:
                res = new CallNode(input, args);
                var children = new List<NodeBase>{ input };
                children.AddRange(args);
                transaction.AddSymbol(res, children.ToArray(), []);
                transaction.Commit();
                return true;
            case ExpressionParsingResult.FailedNeedRollback:
                transaction.Rollback();
                return false;
            case ExpressionParsingResult.FailedNeedCommit:
                transaction.Commit();
                return false;
            default:
                transaction.Pass();
                return false;
        }
    }

    #endregion
    
    #region Helper

    internal static ExpressionParsingResult TryParseCommaSeparated<TReturn>(
        TryParseInternal<TReturn> parserFunc, 
        out List<TReturn> result, 
        ExpressionParsingResult resultIfFail,
        ParsingContext context)
    {
        result = [];
        var accumulate = ExpressionParsingResult.Success;
        while (true)
        {
            if (parserFunc(out var res, context) != ExpressionParsingResult.Success)
            {
                accumulate = resultIfFail;
            }
            
            result.Add(res);
            
            if (!TryConsumeNextNonWhiteSpace<Comma>(_ => true, _ => {}, out _, context))
            {
                return accumulate;
            }
        }
    }
    
    //No need to test - 1 usage(until it change)
    private static bool TryConsumeNextNonWhiteSpaceWithoutRollback<TToken>(
        Func<TToken, bool> predicate, 
        Action<TokenBase> ifPredicateFalse, 
        out TToken token,
        ParsingContext context) where TToken : TokenBase
    {
        token = null;
        if (typeof(TToken) == typeof(WhiteSpace))
        {
            return false;
        }
        
        var next = context.TokenSequence.GetNextNonWhiteSpace();
        if (next is TToken target && predicate(target))
        {
            token = target;
            return true;
        }

        ifPredicateFalse(next);
        return false;
    }

    internal static ExpressionParsingResult TryParseInParen<TResult, TOpen, TClose>(
        IParsingTransaction transaction,
        TryParseInternal<TResult> parserFunc, 
        Func<TokenBase, TokenBase, TResult> emptyCase, 
        out TResult result, 
        ExpressionParsingResult missingOpenParen,
        ExpressionParsingResult emptyCaseResult,
        ParsingContext context)
        where TOpen : TokenBase where TClose : TokenBase
    {
        result = default;
        
        if (!TryConsumeNextNonWhiteSpace<TOpen>(_ => true, _ => { }, out var open, context))
        {
            return missingOpenParen;
        }
        if (TryConsumeNextNonWhiteSpace<TClose>(_ => true, _ => { }, out var close, context))
        {
            result = emptyCase(open, close);
            return emptyCaseResult;
        }

        var res = parserFunc(out result, context);
        var isClosed = TryConsumeNextNonWhiteSpace<TClose>(_ => true,
            _ => {}, out _, context);

        if (isClosed) return res;
        AdvanceToEndOfLineOrRequested<TClose>(context);
        AddExceptionToTheTokenRange(open, context.TokenSequence.Current(),
            context.TokenSequence.Current() is null or EndOfLine
                ? PlampNativeExceptionInfo.ParenExpressionIsNotClosed()
                : PlampNativeExceptionInfo.Expected(typeof(TClose).Name), transaction, context);
        
        return res;
    }

    internal static bool TryConsumeNext<TToken>(
        Func<TToken, bool> predicate, 
        Action<TokenBase> ifPredicateFalse,
        out TToken token,
        ParsingContext context) where TToken : TokenBase
    {
        token = null;
        var next = context.TokenSequence.PeekNext();
        if (next is TToken target && predicate(target))
        {
            token = target;
            context.TokenSequence.GetNextToken();
            return true;
        }
        
        ifPredicateFalse(next);
        return false;
    }
    
    internal static bool TryConsumeNextNonWhiteSpace<TToken>(
        Func<TToken, bool> predicate, 
        Action<TokenBase> ifPredicateFalse, 
        out TToken token,
        ParsingContext context)
        where TToken : TokenBase
    {
        token = null;
        if (typeof(TToken) == typeof(WhiteSpace))
        {
            throw new Exception();
        }
        
        var next = context.TokenSequence.PeekNextNonWhiteSpace();
        if (next is TToken target && predicate(target))
        { 
            token = target;
            context.TokenSequence.GetNextNonWhiteSpace();
            return true;
        }

        ifPredicateFalse(next);
        return false;
    }

    internal static void AdvanceToEndOfLineOrRequested<TToken>(ParsingContext context)
    {
        if (typeof(TToken) == typeof(WhiteSpace))
        {
            throw new Exception("Cannot use with white space");
        }
        
        var current = context.TokenSequence.Current();
        while (current == null 
               || (current.GetType() != typeof(EndOfLine) 
                   && current.GetType() != typeof(TToken)))
        {
            SkipLineBreak(context);
            current = context.TokenSequence.GetNextNonWhiteSpace();
        } 
    }

    internal static void SkipLineBreak(ParsingContext context)
    {
        if (TryConsumeNextNonWhiteSpace<LineBreak>(_ => true, _ => { }, out _, context))
        {
            TryConsumeNextNonWhiteSpace<EndOfLine>(_ => true, _ => { }, out _, context);
        }
    }
    
    private static TryParseInternal<List<TReturn>> WrapParseCommaSeparated<TReturn>(
        TryParseInternal<TReturn> parserFunc, 
        ExpressionParsingResult errorResult)
    {
        return FuncWrapper;
        ExpressionParsingResult FuncWrapper(out List<TReturn> resultList, ParsingContext context)
        {
            return TryParseCommaSeparated(parserFunc, out resultList, errorResult, context);
        }
    }
    
    #endregion

    #region ExceptionGeneration

    internal static void AddExceptionToTheTokenRange(
        TokenBase start, 
        TokenBase end,
        PlampExceptionRecord exceptionRecord, 
        IParsingTransaction transaction,
        ParsingContext context)
    {
        transaction.AddException(new PlampException(exceptionRecord, start.Start, end.End, context.FileName, context.AssemblyName));
    }

    internal static void AdvanceToRequestedTokenWithException<TRequested>(
        IParsingTransaction transaction,
        ParsingContext context)
    {
        if (context.TokenSequence.Current() is EndOfLine)
        {
            return;
        }

        var next = context.TokenSequence.PeekNext();
        if (next is EndOfLine)
        {
            context.TokenSequence.GetNextToken();
            return;
        }
        
        AdvanceToEndOfLineOrRequested<TRequested>(context);
        var end = context.TokenSequence.Current();
        AddExceptionToTheTokenRange(next,
            end,
            PlampNativeExceptionInfo.Expected(typeof(TRequested).Name),
            transaction,
            context);
    }

    #endregion
}