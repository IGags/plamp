using System;
using System.Collections.Generic;
using plamp.Ast.Node;
using plamp.Ast.Node.Assign;
using plamp.Ast.Node.Binary;
using plamp.Ast.Node.Body;
using plamp.Ast.Node.ControlFlow;
using plamp.Ast.Node.Unary;
using plamp.Native.Parsing.Transactions;
using plamp.Native.Tokenization;
using plamp.Native.Tokenization.Enumerations;
using plamp.Native.Tokenization.Token;

namespace plamp.Native.Parsing;

public sealed class PlampNativeParser
{
    internal delegate ExpressionParsingResult TryParseInternal<T>(out T result);
    
    private TokenSequence _tokenSequence;
    private DepthCounter _depth;
    private ParsingTransactionSource _transactionSource;

    [Obsolete("For test purposes only")]
    internal ParsingTransactionSource TransactionSource => _transactionSource;
    
    [Obsolete("For test purposes only")]
    internal TokenSequence TokenSequence => _tokenSequence;

    [Obsolete("For test purposes only")]
    internal PlampNativeParser(string code)
    {
        var tokenRes = code.Tokenize();
        _depth = 0;
        _tokenSequence = tokenRes.Sequence;
        _transactionSource = new ParsingTransactionSource(tokenRes.Sequence, tokenRes.Exceptions);
    }

    public PlampNativeParser(){}
    
    public ParserResult Parse(string code)
    {
        var tokenRes = code.Tokenize();

        _depth = 0;
        _transactionSource = new ParsingTransactionSource(tokenRes.Sequence, tokenRes.Exceptions);
        _tokenSequence = tokenRes.Sequence;

        var expressionList = new List<NodeBase>();

        while (_tokenSequence.PeekNext() != null || (_tokenSequence.PeekNext() != null && _tokenSequence.Current() == null))
        {
            TryParseTopLevel(out var node);
            if (node != null)
            {
                expressionList.Add(node);
            }
        }

        return new ParserResult(expressionList, tokenRes.Exceptions);
    }
    
    internal ExpressionParsingResult TryParseTopLevel(out NodeBase resultNode)
    {
        resultNode = null;
        if (_tokenSequence.PeekNext() == null)
        {
            return ExpressionParsingResult.FailedNeedRollback;
        }
        
        var transaction = _transactionSource.BeginTransaction();
        if (TryParseScopedWithDepth<EmptyNode>(TryParseEmpty, out var empty) == ExpressionParsingResult.Success)
        {
            resultNode = empty;
            transaction.Commit();
            return ExpressionParsingResult.Success;
        }
        transaction.Rollback();
        
        var handleList = new List<DepthHandle>();
        while (TryConsumeNext<WhiteSpace>(_ => true, _ => {}, out var space))
        {
            if (space.Kind == WhiteSpaceKind.Scope)
            {
                handleList.Add(_depth.EnterNewScope());
            }
        }

        handleList.Reverse();
        
        var token = _tokenSequence.PeekNextNonWhiteSpace();
        if (token is KeywordToken keyword)
        {
            switch (keyword.Keyword)
            {
                case Keywords.Def:
                    var defRes = TryParseFunction(out var defNode);
                    resultNode = defNode;
                    DisposeHandles();
                    return defRes;
                case Keywords.Use:
                    var useRes = TryParseUsing(out var useNode);
                    resultNode = useNode;
                    DisposeHandles();
                    return useRes;
            }
        }
        
        transaction =_transactionSource.BeginTransaction();
        AdvanceToRequestedTokenWithException<EndOfLine>(transaction);
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

    internal ExpressionParsingResult TryParseEmpty(out EmptyNode node)
    {
        if (TryConsumeNextNonWhiteSpace<EndOfLine>(_ => true, _ => {}, out _))
        {
            node = new EmptyNode();
            return ExpressionParsingResult.Success;
        }

        node = null;
        return ExpressionParsingResult.FailedNeedRollback;
    }

    internal ExpressionParsingResult TryParseUsing(out UseNode node)
    {
        node = null;
        if (!TryConsumeNextNonWhiteSpace<KeywordToken>(
                x => x.Keyword == Keywords.Use, _ => { }, 
                out _))
        {
            throw new Exception("Internal parser bug");
        }
        
        var transaction = _transactionSource.BeginTransaction();
        var res = ParseMemberAccessSequence(transaction, out var list);
        if (res == ExpressionParsingResult.FailedNeedRollback)
        {
            transaction.Rollback();
            var next = _tokenSequence.PeekNextNonWhiteSpace();
            AdvanceToEndOfLineOrRequested<EndOfLine>();
            var current = _tokenSequence.Current();
            transaction = _transactionSource.BeginTransaction();
            AddExceptionToTheTokenRange(next, current,
                PlampNativeExceptionInfo.InvalidUsingName(),
                transaction);
            transaction.Commit();
            return ExpressionParsingResult.FailedNeedCommit;
        }
        list.Reverse();
        
        NodeBase memberNode = new MemberNode(list[0].GetStringRepresentation());
        foreach (var member in list[1..])
        {
            memberNode = new MemberAccessNode(new MemberNode(member.GetStringRepresentation()), memberNode);
        }
        
        AdvanceToRequestedTokenWithException<EndOfLine>(transaction);
        transaction.Commit();
        node = new UseNode(memberNode);
        
        return ExpressionParsingResult.Success;
    }

    internal ExpressionParsingResult TryParseFunction(out DefNode node)
    {
        node = null;
        if (_tokenSequence.PeekNextNonWhiteSpace() == null 
            || !TryConsumeNextNonWhiteSpace<KeywordToken>(
                x => x.Keyword == Keywords.Def, _ => { }, out var def))
        {
            return ExpressionParsingResult.FailedNeedRollback;
        }

        var transaction = _transactionSource.BeginTransaction();
        var res = TryParseType(transaction, out var typeNode, false);
        if (res == ExpressionParsingResult.FailedNeedRollback)
        {
            AddExceptionToTheTokenRange(def, def,
                PlampNativeExceptionInfo.InvalidDefMissingReturnType(), transaction);
            AdvanceToRequestedTokenWithException<EndOfLine>(transaction);
            AddBodyException(transaction);
            transaction.Commit();
            return ExpressionParsingResult.FailedNeedCommit;
        }

        if (!TryConsumeNextNonWhiteSpace<Word>(_ => true, _ => { }, out var name))
        {
            AddExceptionToTheTokenRange(def, def,
                PlampNativeExceptionInfo.InvalidDefMissingName(), transaction);
            AdvanceToRequestedTokenWithException<EndOfLine>(transaction);
            AddBodyException(transaction);
            transaction.Commit();
            return ExpressionParsingResult.FailedNeedCommit;
        }

        var nameNode = new MemberNode(name.GetStringRepresentation());
        
        //TOO HARD
        res = TryParseInParen<List<ParameterNode>, OpenParen, CloseParen>(
            transaction,
            WrapParseCommaSeparated<ParameterNode>(
                ParameterWrapper, ExpressionParsingResult.FailedNeedCommit),
            (_, _) => [], out var parameterNodes, 
            ExpressionParsingResult.FailedNeedPass, ExpressionParsingResult.Success);
        if (res == ExpressionParsingResult.FailedNeedPass)
        {
            transaction.AddException(
                new PlampException(PlampNativeExceptionInfo.ExpectedArgDefinition(), def));
            AdvanceToRequestedTokenWithException<EndOfLine>(transaction);
        }

        var body = ParseOptionalBody(transaction);
        node = new DefNode(typeNode, nameNode, parameterNodes ?? [], body);
        transaction.Commit();
        return ExpressionParsingResult.Success;

        ExpressionParsingResult ParameterWrapper(out ParameterNode node)
        {
            return TryParseParameter(transaction, out node);
        }
    }

    internal ExpressionParsingResult TryParseBody(out BodyNode body)
    {
        using var handle = _depth.EnterNewScope();
        var expressions = new List<NodeBase>();
        while (true)
        {
            var transaction = _transactionSource.BeginTransaction();
            var res = TryParseScopedWithDepth<NodeBase>(
                TryParseBodyLevelExpression, out var expression);
            if (res == ExpressionParsingResult.Success)
            {
                AdvanceToRequestedTokenWithException<EndOfLine>(transaction);
                expressions.Add(expression);
                transaction.Commit();
                continue;
            }
            
            transaction.Rollback();
            break;
        }
        
        body = new BodyNode(expressions);
        return ExpressionParsingResult.Success;
    }
    
    internal ExpressionParsingResult TryParseParameter(IParsingTransaction transaction, out ParameterNode parameterNode)
    {
        parameterNode = null;
        var typePeek = _tokenSequence.PeekNextNonWhiteSpace();
        if (typePeek == null || typePeek.GetType() != typeof(Word))
        {
            return ExpressionParsingResult.FailedNeedPass;
        }
        
        TryParseType(transaction, out var type);
        var argPeek = _tokenSequence.PeekNextNonWhiteSpace();
        MemberNode name;
        if (argPeek is null || argPeek.GetType() != typeof(Word))
        {
            AddExceptionToTheTokenRange(typePeek, argPeek, PlampNativeExceptionInfo.InvalidParameterDefinition(), transaction);
            name = null;
        }
        else
        {
            TryConsumeNextNonWhiteSpace<Word>(_ => true, _ => { }, out _);
            name = new MemberNode(argPeek.GetStringRepresentation());
        }
        
        parameterNode = new ParameterNode(type, name);
        return ExpressionParsingResult.Success;
    }
    
    internal ExpressionParsingResult TryParseType(IParsingTransaction transaction, out NodeBase typeNode, bool strict = true)
    {
        typeNode = null;
        var res = ParseMemberAccessSequence(transaction, out var list);
        
        if (res == ExpressionParsingResult.FailedNeedRollback) return ExpressionParsingResult.FailedNeedRollback;

        var inner = _transactionSource.BeginTransaction();
        TryParseInParen<List<NodeBase>, OpenAngleBracket, CloseAngleBracket>(transaction,
            WrapParseCommaSeparated(TryParseTypeWrapper(transaction), ExpressionParsingResult.FailedNeedCommit),
            (start, end)
                =>
            {
                AddExceptionToTheTokenRange(start, end, PlampNativeExceptionInfo.InvalidGenericDefinition(),
                    transaction);
                return null;
            },
            out var types, ExpressionParsingResult.FailedNeedPass, ExpressionParsingResult.Success);
        
        if (_tokenSequence.Current()?.GetType() != typeof(CloseAngleBracket) 
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
        NodeBase node = null;
        for (var i = 0; i < list.Count; i++)
        {
            if (i != list.Count - 1)
            {
                if (node == null)
                {
                    node = new MemberNode(list[i].GetStringRepresentation());
                }
                else
                {
                    node = new MemberAccessNode(node, new MemberNode(list[i].GetStringRepresentation()));
                }
                continue;
            }
            
            var type = new TypeNode(new MemberNode(list[i].GetStringRepresentation()), types);
            if (node == null)
            {
                node = type;
            }
            else
            {
                node = new MemberAccessNode(node, type);
            }
        }
        
        typeNode = node;
        return ExpressionParsingResult.Success;
    }

    private ExpressionParsingResult ParseMemberAccessSequence(IParsingTransaction transaction, out List<Word> members)
    {
        members = null;
        var peek = _tokenSequence.PeekNextNonWhiteSpace();
        if (peek == null || peek.GetType() != typeof(Word))
        {
            return ExpressionParsingResult.FailedNeedRollback;
        }

        _tokenSequence.GetNextNonWhiteSpace();
        members = [(Word)peek];
        
        while (true)
        {
            if (!TryConsumeNextNonWhiteSpace<OperatorToken>(x => x.Operator == OperatorEnum.MemberAccess, 
                    _ => { },
                    out var op))
            {
                break;
            }

            var first = members[0];
            if (TryConsumeNextNonWhiteSpace<Word>(_ => true, _ =>
                {
                    AddExceptionToTheTokenRange(first, op, PlampNativeExceptionInfo.InvalidTypeName(),
                        transaction);
                }, out var word))
            {
                members.Add(word);
            }
            else
            {
                return ExpressionParsingResult.FailedNeedCommit;
            }
        }

        return ExpressionParsingResult.Success;
    }
    
    private TryParseInternal<NodeBase> TryParseTypeWrapper(IParsingTransaction transaction)
    {
        return FuncWrapper;
            
        ExpressionParsingResult FuncWrapper(out NodeBase result)
        {
            return TryParseType(transaction, out result);
        }
    }
    
    internal ExpressionParsingResult TryParseScopedWithDepth<TReturn>(
        TryParseInternal<TReturn> @internal, out TReturn result, int depth = -1)
    {
        if (depth < 0)
        {
            depth = (int)_depth;
        }

        var currentDepth = 0;
        while (TryConsumeNext<WhiteSpace>(t =>
               {
                   if (t.Kind == WhiteSpaceKind.Scope)
                   {
                       currentDepth++;
                   }

                   return true;
               }, _ => { }, out _)) { }

        if (currentDepth < depth)
        {
            result = default;
            return ExpressionParsingResult.FailedNeedRollback;
        }

        var res = @internal(out result);
        
        return res;
    }

    internal ExpressionParsingResult TryParseBodyLevelExpression(out NodeBase expression)
    {
        expression = null;
        if (_tokenSequence.PeekNextNonWhiteSpace() is null)
        {
            return ExpressionParsingResult.FailedNeedCommit;
        }
        
        if (TryParseEmpty(out var emptyNode) == ExpressionParsingResult.Success)
        {
            expression = emptyNode;
            return ExpressionParsingResult.Success;
        }

        var transaction = _transactionSource.BeginTransaction();
        var res = TryParseKeywordExpression(transaction, out var keywordExpression);
        if (res == ExpressionParsingResult.Success 
            || res == ExpressionParsingResult.FailedNeedCommit)
        {
            expression = keywordExpression;
            transaction.Commit();
            return ExpressionParsingResult.Success;
        }
        transaction.Rollback();

        return TryParseWithPrecedence(out expression);
    }

    internal ExpressionParsingResult TryParseKeywordExpression(IParsingTransaction transaction, out NodeBase expression)
    {
        expression = null;
        if (!TryConsumeNextNonWhiteSpace<KeywordToken>(_ => true, _ => { }, out var keyword))
            return ExpressionParsingResult.FailedNeedPass;
        
        switch (keyword.Keyword)
        {
            case Keywords.Break:
                expression = new BreakNode();
                AdvanceToRequestedTokenWithException<EndOfLine>(transaction);
                return ExpressionParsingResult.Success;
            case Keywords.Continue:
                expression = new ContinueNode();
                AdvanceToRequestedTokenWithException<EndOfLine>(transaction);
                return ExpressionParsingResult.Success;
            case Keywords.Return:
                TryParseWithPrecedence(out var precedence);
                expression = new ReturnNode(precedence);
                AdvanceToRequestedTokenWithException<EndOfLine>(transaction);
                return ExpressionParsingResult.Success;
            case Keywords.If:
                var res 
                    = TryParseConditionalExpression(keyword, transaction, out var node);
                AdvanceToRequestedTokenWithException<EndOfLine>(transaction);
                expression = node;
                return res;
            case Keywords.For:
                res = TryParseForLoop(keyword, transaction, out var forNode);
                AdvanceToRequestedTokenWithException<EndOfLine>(transaction);
                expression = forNode;
                return res;
            case Keywords.While:
                res = TryParseWhileLoop(transaction, keyword, out var whileNode);
                expression = whileNode;
                return res;
            default:
                return ExpressionParsingResult.FailedNeedPass;
        }
    }

    private ExpressionParsingResult TryParseConditionalExpression(KeywordToken ifClauseKeyword, IParsingTransaction transaction, out ConditionNode conditionNode)
    {
        conditionNode = null;
        
        var ifClauseRes = TryParseConditionClause(ifClauseKeyword, transaction, out var baseClause);
        if(ifClauseRes == ExpressionParsingResult.FailedNeedCommit)
        {
            return ifClauseRes;
        }
        
        var elifClauses = new List<ClauseNode>();

        KeywordToken keyword = null;
        var elifTransaction = _transactionSource.BeginTransaction();
        while (TryParseEmpty(out _) == ExpressionParsingResult.Success
               || TryParseScopedWithDepth(TryParseElifKeyword, out keyword) 
               == ExpressionParsingResult.Success)
        {
            //Strange but need
            elifTransaction.Commit();
            elifTransaction = _transactionSource.BeginTransaction();
            var inner = _transactionSource.BeginTransaction();
            if (keyword != null)
            {
                //TODO: Skip body with match depth
                TryParseConditionClause(keyword, inner, out var elifClause);
                if(elifClause != null) elifClauses.Add(elifClause);
                keyword = null;
            }
            inner.Commit();
        }
        elifTransaction?.Rollback();
        
        var elseBody = default(BodyNode);
        if (TryConsumeNextNonWhiteSpace<KeywordToken>(x => x.Keyword == Keywords.Else, _ => {}, out _))
        {
            elseBody = ParseOptionalBody(transaction);
        }
            
        conditionNode = new ConditionNode(baseClause, elifClauses, elseBody);
        return ExpressionParsingResult.Success;

        ExpressionParsingResult TryParseElifKeyword(out KeywordToken res)
        {
             return TryConsumeNextNonWhiteSpace(x => x.Keyword == Keywords.Elif, _ => { }, out res) 
                 ? ExpressionParsingResult.Success : ExpressionParsingResult.FailedNeedRollback;
        }
    }

    private ExpressionParsingResult TryParseConditionClause(
        KeywordToken clauseDefinition, 
        IParsingTransaction transaction, 
        out ClauseNode conditionNode)
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
                    transaction);
                return null;
            },
            out var condition,
            ExpressionParsingResult.FailedNeedCommit, ExpressionParsingResult.Success);

        if (res == ExpressionParsingResult.FailedNeedCommit)
        {
            AddExceptionToTheTokenRange(clauseDefinition, clauseDefinition,
                PlampNativeExceptionInfo.MissingConditionPredicate(), transaction);
            AdvanceToRequestedTokenWithException<EndOfLine>(transaction);
            AddBodyException(transaction);
            return ExpressionParsingResult.FailedNeedCommit;
        }
        var body = ParseOptionalBody(transaction);
        conditionNode = new ClauseNode(condition, body);
        return res;
    }

    private ExpressionParsingResult TryParseForLoop(
        KeywordToken keyword, 
        IParsingTransaction transaction, 
        out NodeBase counterLoopHolder)
    {
        counterLoopHolder = null;

        var res = TryParseInParen<CounterLoopHolder, OpenParen, CloseParen>(
            transaction, ForHeaderWrapper, (_, _) => default, out var holder,
            ExpressionParsingResult.FailedNeedCommit, ExpressionParsingResult.FailedNeedPass);
        
        if (res == ExpressionParsingResult.FailedNeedCommit)
        {
            AddExceptionToTheTokenRange(keyword, keyword, 
                PlampNativeExceptionInfo.InvalidForHeader(), transaction);
            AdvanceToRequestedTokenWithException<EndOfLine>(transaction);
            AddBodyException(transaction);
            return ExpressionParsingResult.FailedNeedCommit;
        }

        var body = ParseOptionalBody(transaction);

        counterLoopHolder = holder.ForeachHeaderHolder == default
            ? new ForNode(
                holder.ForHeaderHolder.IteratorVar,
                holder.ForHeaderHolder.TilCondition,
                holder.ForHeaderHolder.Counter,
                body) :
            new ForeachNode(
                holder.ForeachHeaderHolder.IteratorVar,
                holder.ForeachHeaderHolder.Iterable,
                body);
        
        return ExpressionParsingResult.Success;

        ExpressionParsingResult ForHeaderWrapper(out CounterLoopHolder header) =>
            TryParseForHeader(transaction, out header);
    }

    private readonly record struct ForeachHeaderHolder(
        NodeBase IteratorVar, NodeBase Iterable);
    
    private readonly record struct ForHeaderHolder(
        NodeBase IteratorVar, NodeBase TilCondition, NodeBase Counter);

    private readonly record struct CounterLoopHolder(
        ForeachHeaderHolder ForeachHeaderHolder,
        ForHeaderHolder ForHeaderHolder);
    
    private ExpressionParsingResult TryParseForHeader(
        IParsingTransaction transaction, 
        out CounterLoopHolder loop)
    {
        var innerTransaction = _transactionSource.BeginTransaction();
        TryParseWithPrecedence(out var iteratorVar);
        if (TryConsumeNextNonWhiteSpace<KeywordToken>(
                x => x.Keyword == Keywords.In,
                _ => { },
                out _))
        {
            innerTransaction.Commit();
            TryParseWithPrecedence(out var iterable);
            loop = new CounterLoopHolder(
                new ForeachHeaderHolder(iteratorVar, iterable),
                default);
        }
        else if (TryConsumeNextNonWhiteSpace<Comma>(
                     _ => true,
                     _ => { },
                     out _))
        {
            innerTransaction.Commit();
            TryParseWithPrecedence(out var tilCondition);
            var res = TryConsumeNextNonWhiteSpace<Comma>(
                _ => true,
                _ => { },
                out _);
            if (!res)
            {
                transaction.AddException(
                    new PlampException(
                        PlampNativeExceptionInfo.Expected(nameof(Comma)), 
                        _tokenSequence.PeekNext()));
            }

            TryParseWithPrecedence(out var counter);
            loop = new CounterLoopHolder(
                default,
                new ForHeaderHolder(iteratorVar, tilCondition, counter));
        }
        else
        {
            innerTransaction.Rollback();
            loop = default;
            AdvanceToEndOfLineOrRequested<CloseParen>();
            //dirty hack
            _tokenSequence.Position--;
            return ExpressionParsingResult.FailedNeedCommit;
        }
        
        return ExpressionParsingResult.Success;
    }
    
    private ExpressionParsingResult TryParseWhileLoop(
        IParsingTransaction transaction, 
        KeywordToken whileToken,
        out WhileNode whileNode)
    {
        var res = TryParseInParen<NodeBase, OpenParen, CloseParen>(
            transaction, TryParseWithPrecedence, (from, to) =>
            {
                AddExceptionToTheTokenRange(from, to,
                    PlampNativeExceptionInfo.EmptyConditionPredicate(),
                    transaction);
                return null;
            }, out var expression, 
            ExpressionParsingResult.FailedNeedCommit, ExpressionParsingResult.Success);
        
        switch (res)
        {
            case ExpressionParsingResult.Success:
                var body = ParseOptionalBody(transaction);
                whileNode = new WhileNode(expression, body);
                return ExpressionParsingResult.Success;
            case ExpressionParsingResult.FailedNeedCommit:
                AddExceptionToTheTokenRange(whileToken, whileToken, 
                    PlampNativeExceptionInfo.MissingConditionPredicate(), transaction);
                AdvanceToRequestedTokenWithException<EndOfLine>(transaction);
                AddBodyException(transaction);
                whileNode = null;
                return ExpressionParsingResult.FailedNeedCommit;
        }
        
        //Never invoked in common cases
        throw new Exception("Parser exception");
    }

    /// <summary>
    /// Add exception to matching body calls from end of line
    /// </summary>
    private void AddBodyException(IParsingTransaction transaction)
    {
        using var handle = _depth.EnterNewScope();
        while (true)
        {
            var res
                = TryParseScopedWithDepth<NodeBase>(AddExceptionToBodyLevelWrapper, out _);
            if (res != ExpressionParsingResult.Success)
            {
                return;
            }
        }

        ExpressionParsingResult AddExceptionToBodyLevelWrapper(out NodeBase res)
        {
            return AddExceptionToBodyLevel(transaction, out res);
        }
    }

    private ExpressionParsingResult AddExceptionToBodyLevel(
        IParsingTransaction transaction, out NodeBase result)
    {
        var next = _tokenSequence.PeekNext();
        AdvanceToEndOfLineOrRequested<EndOfLine>();
        var end = _tokenSequence.Current();
        AddExceptionToTheTokenRange(
            next, end, PlampNativeExceptionInfo.InvalidBody(), transaction);
        result = null;
        return ExpressionParsingResult.Success;
    }
    
    private BodyNode ParseOptionalBody(IParsingTransaction transaction)
    {
        if (_tokenSequence.PeekNext()?.GetType() != typeof(EndOfLine))
        {
            TryParseBodyLevelExpression(out var expression);
            if (_tokenSequence.Current() is not EndOfLine)
            {
                AdvanceToRequestedTokenWithException<EndOfLine>(transaction);
            }
            return new BodyNode(expression == null ? [] : [expression]);
        }

        if (!TryConsumeNextNonWhiteSpace<EndOfLine>(_ => true, _ => { }, out _))
        {
            AdvanceToRequestedTokenWithException<EndOfLine>(transaction);
        }

        TryParseBody(out var body);
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

    internal ExpressionParsingResult TryParseWithPrecedence(out NodeBase node) 
        => TryParseWithPrecedence(out node, 0);
    
    internal ExpressionParsingResult TryParseWithPrecedence(out NodeBase node, int rbp)
    {
        var nudParsingResult = TryParseNud(out node);
        if (nudParsingResult != ExpressionParsingResult.Success)
        {
            return nudParsingResult;
        }

        while (TryParseLed(rbp, node, out node) == ExpressionParsingResult.Success)
        {
        }

        return ExpressionParsingResult.Success;
    }
    
    internal ExpressionParsingResult TryParseNud(out NodeBase node)
    {
        var transaction = _transactionSource.BeginTransaction();
        var result = TryParseVariableDeclaration(transaction, out node);
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

        transaction = _transactionSource.BeginTransaction();
        switch (TryParseCastOperator(transaction, out var typeCasting))
        {
            case ExpressionParsingResult.Success:
                if (TryParseNud(out var cast) == ExpressionParsingResult.Success)
                {
                    node = new CastNode(typeCasting, cast);
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

        transaction = _transactionSource.BeginTransaction();
        switch (TryParseSubExpression(transaction, out var sub))
        {
            case ExpressionParsingResult.Success:
                node = sub;
                node = ParsePostfixIfExist(sub);
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

        transaction = _transactionSource.BeginTransaction();
        var ctorParseRes = TryParseConstructor(transaction, out var ctor);
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

        if (TryParsePrefixOperator(out node) == ExpressionParsingResult.Success)
        {
            return ExpressionParsingResult.Success;
        }

        if (TryParseLiteral(out node) == ExpressionParsingResult.Success)
        {
            return ExpressionParsingResult.Success;
        }

        if (TryConsumeNextNonWhiteSpace<Word>(_ => true, _ => { }, out var word))
        {
            var member = new MemberNode(word.GetStringRepresentation());
            node = ParsePostfixIfExist(member);
            return ExpressionParsingResult.Success;
        }

        node = null;
        transaction.Rollback();
        
        
        return ExpressionParsingResult.FailedNeedCommit;
    }
    
    internal ExpressionParsingResult TryParseLed(int rbp, NodeBase left, out NodeBase output)
    {
        var transaction = _transactionSource.BeginTransaction();
        SkipLineBreak();
        
        if (TryConsumeNextNonWhiteSpace<OperatorToken>(_ => true, _ => { }, out var token))
        {
            var precedence = token.GetPrecedence(false);
            if (precedence <= rbp)
            {
                output = left;
                transaction.Rollback();
                return ExpressionParsingResult.FailedNeedCommit;
            }

            var res = TryParseWithPrecedence(out var right, precedence);
            
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
                    output = new GreaterOrEqualsNode(left, right);
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
            transaction.Commit();
            return res;
        }
        
        transaction.Rollback();
        output = left;
        return ExpressionParsingResult.FailedNeedCommit;
    }
    
    internal ExpressionParsingResult TryParseVariableDeclaration(
        IParsingTransaction transaction, 
        out NodeBase variableDeclaration)
    {
        var typ = default(NodeBase);
        if (!TryConsumeNextNonWhiteSpace<KeywordToken>(
                x => x.Keyword == Keywords.Var, _ => { }, out _)
            && TryParseType(transaction, out typ) != ExpressionParsingResult.Success)
        {
            variableDeclaration = null;
            return ExpressionParsingResult.FailedNeedRollback;
        }
        
        //Null denotation starts with variable declaration
        if (TryConsumeNextNonWhiteSpaceWithoutRollback<Word>(
                _ => true,
                token => transaction.AddException(
                    new PlampException(PlampNativeExceptionInfo.ExpectedIdentifier(), token)), 
                out var name))
        {
            variableDeclaration = new VariableDefinitionNode(
                typ, new MemberNode(name.GetStringRepresentation()));
            return ExpressionParsingResult.Success;
        }
        variableDeclaration = null;
        return ExpressionParsingResult.FailedNeedRollback;
    }

    internal ExpressionParsingResult TryParseCastOperator(
        IParsingTransaction transaction, 
        out NodeBase cast)
    {
        cast = null;
        return TryParseInParen<NodeBase, OpenParen, CloseParen>(
            transaction, TryParseTypeWrapper(transaction),
            (_, _) => null, out cast,
            ExpressionParsingResult.FailedNeedRollback, 
            ExpressionParsingResult.FailedNeedRollback);
    }

    internal ExpressionParsingResult TryParseSubExpression(
        IParsingTransaction transaction, 
        out NodeBase sub)
    {
        return TryParseInParen<NodeBase, OpenParen, CloseParen>(
            transaction, TryParseWithPrecedence, (open, close) =>
            {
                AddExceptionToTheTokenRange(open, close, PlampNativeExceptionInfo.ExpectedExpression(),
                    transaction);
                return null;
            },
            out sub, 
            ExpressionParsingResult.FailedNeedRollback, ExpressionParsingResult.FailedNeedCommit);
    }

    internal ExpressionParsingResult TryParseLiteral(out NodeBase node)
    {
        node = null;
        if (TryConsumeNextNonWhiteSpace<StringLiteral>(_ => true, _ => { }, out var literal))
        {
            var stringLiteral = new ConstNode(literal.GetStringRepresentation(), typeof(string));
            node = ParsePostfixIfExist(stringLiteral);
            return ExpressionParsingResult.Success;
        }

        if (TryConsumeNextNonWhiteSpace<NumberLiteral>(_ => true, _ => { }, out var numberLiteral))
        {
            var number = new ConstNode(numberLiteral.ActualValue, numberLiteral.ActualType);
            node = ParsePostfixIfExist(number);
            return ExpressionParsingResult.Success;
        }

        if (TryConsumeNextNonWhiteSpace(
                t => t.Keyword is Keywords.True or Keywords.False, _ => { },
                out KeywordToken boolLiteral))
        {
            var value = bool.Parse(boolLiteral.GetStringRepresentation());
            var boolNode = new ConstNode(value, typeof(bool));
            node = ParsePostfixIfExist(boolNode);
            return ExpressionParsingResult.Success;
        }

        if (!TryConsumeNextNonWhiteSpace(
                t => t.Keyword is Keywords.Null, _ => { }, out KeywordToken _))
            return ExpressionParsingResult.FailedNeedRollback;
        
        var nullNode = new ConstNode(null, null);
        node = ParsePostfixIfExist(nullNode);
        return ExpressionParsingResult.Success;
    }

    internal ExpressionParsingResult TryParseConstructor(
        IParsingTransaction transaction, 
        out NodeBase ctor)
    {
        ctor = null;
        if (!TryConsumeNextNonWhiteSpace<KeywordToken>(x => x.Keyword == Keywords.New, _ => { }, out var keywordToken))
        {
            return ExpressionParsingResult.FailedNeedRollback;
        }
        
        if (TryParseType(transaction, out var type) != ExpressionParsingResult.Success)
        {
            return ExpressionParsingResult.FailedNeedRollback;
        }
        
        var typeEnd = _tokenSequence.Current();
        var parenRes = TryParseInParen<List<NodeBase>, OpenParen, CloseParen>(
            transaction,
            WrapParseCommaSeparated<NodeBase>(TryParseWithPrecedence, ExpressionParsingResult.FailedNeedCommit),
            (_, _) => [], out var parameters, ExpressionParsingResult.FailedNeedRollback,
            ExpressionParsingResult.Success);
        
        switch (parenRes)
        {
            case ExpressionParsingResult.Success:
                ctor = new ConstructorNode(type, parameters);
                return ExpressionParsingResult.Success;
            case ExpressionParsingResult.FailedNeedRollback:
                return ExpressionParsingResult.FailedNeedRollback;
            //Dead branch current focus is not in exception precision
            case ExpressionParsingResult.FailedNeedCommit:
                AddExceptionToTheTokenRange(keywordToken, typeEnd,
                    PlampNativeExceptionInfo.Expected("arguments in ()"), transaction);
                return ExpressionParsingResult.FailedNeedCommit;
        }

        return ExpressionParsingResult.FailedNeedCommit;
    }

    internal ExpressionParsingResult TryParsePrefixOperator(out NodeBase node)
    {
        node = null;
        var transaction = _transactionSource.BeginTransaction();
        if (!TryConsumeNextNonWhiteSpace<OperatorToken>(
                x => x.Operator 
                    is OperatorEnum.Minus 
                    or OperatorEnum.Not 
                    or OperatorEnum.Increment
                    or OperatorEnum.Decrement,
                _ => { }, out var operatorToken))
        {
            transaction.Rollback();
            return ExpressionParsingResult.FailedNeedRollback;
        }
        
        var res 
            = TryParseWithPrecedence(out var inner, operatorToken.GetPrecedence(true));

        if (res != ExpressionParsingResult.Success)
        {
            transaction.Rollback();
            return ExpressionParsingResult.FailedNeedRollback;
        }
        
        transaction.Commit();
        node = operatorToken.Operator switch
        {
            OperatorEnum.Minus => new UnaryMinusNode(inner),
            OperatorEnum.Not => new NotNode(inner),
            OperatorEnum.Increment => new PrefixIncrementNode(inner),
            OperatorEnum.Decrement => new PrefixDecrementNode(inner),
            _ => throw new ArgumentOutOfRangeException()
        };

        node = ParsePostfixIfExist(node);
        return ExpressionParsingResult.Success;

    }
    
    internal NodeBase ParsePostfixIfExist(NodeBase inner)
    {
        while (true)
        {
            if (TryParsePostfixOperator(inner, out var node))
            {
                inner = node;
                continue;
            }

            if (TryParseIndexer(inner, out node))
            {
                inner = node;
                continue;
            }

            if (TryParseCall(inner, out node))
            {
                inner = node;
                continue;
            }
            
            if (!TryParseMemberAccess(inner, out node)) return inner;
            inner = node;
        }
    }
    
    internal bool TryParsePostfixOperator(NodeBase nodeBase, out NodeBase node)
    {
        node = null;
        if (!TryConsumeNextNonWhiteSpace<OperatorToken>(
                x => x is { Operator: OperatorEnum.Increment or OperatorEnum.Decrement },
                _ => { }, out var @operator))
        {
            return false;
        }
        
        node =  @operator.Operator switch
        {
            OperatorEnum.Increment => new PostfixIncrementNode(nodeBase),
            OperatorEnum.Decrement => new PostfixDecrementNode(nodeBase),
            _ => throw new Exception("Parser exception")
        };
        return true;

    }
    
    internal bool TryParseIndexer(NodeBase inner, out NodeBase node)
    {
        var next = _tokenSequence.PeekNextNonWhiteSpace();
        if (next is not OpenSquareBracket)
        {
            node = null;
            return false;
        }

        var transaction = _transactionSource.BeginTransaction();
        var isParsed 
            = TryParseInParen<List<NodeBase>, OpenSquareBracket, CloseSquareBracket>(
            transaction, 
            WrapParseCommaSeparated<NodeBase>(
                TryParseWithPrecedence, ExpressionParsingResult.FailedNeedCommit), (_, _) => [], 
            out var index, 
            ExpressionParsingResult.FailedNeedRollback, ExpressionParsingResult.Success);
        
        if (isParsed == ExpressionParsingResult.Success)
        {
            node = new IndexerNode(inner, index);
            transaction.Commit();
            return true;
        }
        transaction.Rollback();
        node = null;
        return false;
    }

    internal bool TryParseMemberAccess(NodeBase input, out NodeBase res)
    {
        res = null;
        var transaction = _transactionSource.BeginTransaction();
        if (!TryConsumeNextNonWhiteSpace<OperatorToken>(
                x => x.Operator == OperatorEnum.MemberAccess, _ => { },
                out _))
        {
            transaction.Rollback();
            return false;
        }

        if (TryConsumeNextNonWhiteSpace<Word>(_ => true, _ => { }, out var word))
        {
            res = new MemberAccessNode(input, new MemberNode(word.GetStringRepresentation()));
            transaction.Commit();
            return true;
        }

        //var transaction = _transactionSource.BeginTransaction();
        //TODO: specify exception later
        //transaction.AddException(
        //    new PlampException(PlampNativeExceptionInfo.ExpectedMemberName(), call));
        //transaction.Commit();
        
        transaction.Rollback();
        return false;
    }

    internal bool TryParseCall(NodeBase input, out NodeBase res)
    {
        res = null;
        var transaction = _transactionSource.BeginTransaction();
        var parenRes = TryParseInParen<List<NodeBase>, OpenParen, CloseParen>(
            transaction,
            WrapParseCommaSeparated<NodeBase>(TryParseWithPrecedence, ExpressionParsingResult.FailedNeedPass),
            (_, _) => [], out var args, 
            ExpressionParsingResult.FailedNeedRollback, ExpressionParsingResult.Success);
        switch (parenRes)
        {
            case ExpressionParsingResult.Success:
                res = new CallNode(input, args);
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

    internal ExpressionParsingResult TryParseCommaSeparated<TReturn>(
        TryParseInternal<TReturn> parserFunc, 
        out List<TReturn> result, 
        ExpressionParsingResult resultIfFail)
    {
        result = [];
        var accumulate = ExpressionParsingResult.Success;
        while (true)
        {
            if (parserFunc(out var res) != ExpressionParsingResult.Success)
            {
                accumulate = resultIfFail;
            }
            
            result.Add(res);
            
            if (!TryConsumeNextNonWhiteSpace<Comma>(_ => true, _ => {}, out _))
            {
                return accumulate;
            }
        }
    }
    
    //TODO: No need to test because 1 usage(until it change)
    private bool TryConsumeNextNonWhiteSpaceWithoutRollback<TToken>(
        Func<TToken, bool> predicate, 
        Action<TokenBase> ifPredicateFalse, 
        out TToken token) where TToken : TokenBase
    {
        token = null;
        if (typeof(TToken) == typeof(WhiteSpace))
        {
            return false;
        }
        
        var next = _tokenSequence.GetNextNonWhiteSpace();
        if (next is TToken target && predicate(target))
        {
            token = target;
            return true;
        }

        ifPredicateFalse(next);
        return false;
    }

    internal ExpressionParsingResult TryParseInParen<TResult, TOpen, TClose>(
        IParsingTransaction transaction,
        TryParseInternal<TResult> parserFunc, 
        Func<TokenBase, TokenBase, TResult> emptyCase, 
        out TResult result, 
        ExpressionParsingResult missingOpenParen,
        ExpressionParsingResult emptyCaseResult)
        where TOpen : TokenBase where TClose : TokenBase
    {
        result = default;
        
        if (!TryConsumeNextNonWhiteSpace<TOpen>(_ => true, _ => { }, out var open))
        {
            return missingOpenParen;
        }
        if (TryConsumeNextNonWhiteSpace<TClose>(_ => true, _ => { }, out var close))
        {
            result = emptyCase(open, close);
            return emptyCaseResult;
        }

        var res = parserFunc(out result);
        var isClosed = TryConsumeNextNonWhiteSpace<TClose>(_ => true,
            _ => {}, out _);

        if (isClosed) return res;
        AdvanceToEndOfLineOrRequested<TClose>();
        AddExceptionToTheTokenRange(open, _tokenSequence.Current(),
            _tokenSequence.Current() is null or EndOfLine
                ? PlampNativeExceptionInfo.ParenExpressionIsNotClosed()
                : PlampNativeExceptionInfo.Expected(typeof(TClose).Name), transaction);
        
        return res;
    }

    internal bool TryConsumeNext<TToken>(
        Func<TToken, bool> predicate, 
        Action<TokenBase> ifPredicateFalse,
        out TToken token) where TToken : TokenBase
    {
        token = null;
        var next = _tokenSequence.PeekNext();
        if (next is TToken target && predicate(target))
        {
            token = target;
            _tokenSequence.GetNextToken();
            return true;
        }
        
        ifPredicateFalse(next);
        return false;
    }
    
    internal bool TryConsumeNextNonWhiteSpace<TToken>(
        Func<TToken, bool> predicate, 
        Action<TokenBase> ifPredicateFalse, 
        out TToken token)
        where TToken : TokenBase
    {
        token = null;
        if (typeof(TToken) == typeof(WhiteSpace))
        {
            throw new Exception();
        }
        
        var next = _tokenSequence.PeekNextNonWhiteSpace();
        if (next is TToken target && predicate(target))
        { 
            token = target;
            _tokenSequence.GetNextNonWhiteSpace();
            return true;
        }

        ifPredicateFalse(next);
        return false;
    }

    internal void AdvanceToEndOfLineOrRequested<TToken>()
    {
        if (typeof(TToken) == typeof(WhiteSpace))
        {
            throw new Exception("Cannot use with white space");
        }
        
        var current = _tokenSequence.Current();
        while (current == null 
               || (current.GetType() != typeof(EndOfLine) 
                   && current.GetType() != typeof(TToken)))
        {
            SkipLineBreak();
            current = _tokenSequence.GetNextNonWhiteSpace();
        } 
    }

    internal void SkipLineBreak()
    {
        if (TryConsumeNextNonWhiteSpace<LineBreak>(_ => true, _ => { }, out _))
        {
            TryConsumeNextNonWhiteSpace<EndOfLine>(_ => true, _ => { }, out _);
        }
    }
    
    private TryParseInternal<List<TReturn>> WrapParseCommaSeparated<TReturn>(
        TryParseInternal<TReturn> parserFunc, 
        ExpressionParsingResult errorResult)
    {
        return FuncWrapper;
        ExpressionParsingResult FuncWrapper(out List<TReturn> resultList)
        {
            return TryParseCommaSeparated(parserFunc, out resultList, errorResult);
        }
    }
    
    #endregion

    #region ExceptionGeneration

    internal void AddExceptionToTheTokenRange(
        TokenBase start, 
        TokenBase end,
        PlampNativeExceptionFinalRecord exceptionRecord, 
        IParsingTransaction transaction)
    {
        transaction.AddException(new PlampException(exceptionRecord, start.Start, end.End));
    }

    internal void AdvanceToRequestedTokenWithException<TRequested>(
        IParsingTransaction transaction)
    {
        if (_tokenSequence.Current() is EndOfLine)
        {
            return;
        }

        var next = _tokenSequence.PeekNext();
        if (next is EndOfLine)
        {
            _tokenSequence.GetNextToken();
            return;
        }
        
        AdvanceToEndOfLineOrRequested<TRequested>();
        var end = _tokenSequence.Current();
        AddExceptionToTheTokenRange(next, end, PlampNativeExceptionInfo.Expected(typeof(TRequested).Name), transaction);
    }

    #endregion
}