using System;
using System.Collections.Generic;
using plamp.Ast;
using plamp.Ast.Node;
using plamp.Ast.Node.Assign;
using plamp.Ast.Node.Binary;
using plamp.Ast.Node.Body;
using plamp.Ast.Node.ControlFlow;
using plamp.Ast.Node.Unary;
using plamp.Native.Parsing.Symbols;
using plamp.Native.Parsing.Transactions;
using plamp.Native.Tokenization;
using plamp.Native.Tokenization.Enumerations;
using plamp.Native.Tokenization.Token;

namespace plamp.Native.Parsing;

//!!!NOT THREAD SAFE!!!
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
        _transactionSource = new ParsingTransactionSource(
            tokenRes.Sequence, tokenRes.Exceptions, new());
    }

    [Obsolete("For test purposes only")]
    internal PlampNativeParser(TokenSequence sequence)
    {
        _depth = 0;
        _tokenSequence = sequence;
        _transactionSource = new ParsingTransactionSource(
            _tokenSequence, new(), new());
    }

    public PlampNativeParser(){}
    
    public ParserResult Parse(string code)
    {
        var tokenRes = code.Tokenize();

        _depth = 0;
        _transactionSource = new ParsingTransactionSource(
            tokenRes.Sequence, tokenRes.Exceptions, new());
        _tokenSequence = tokenRes.Sequence;

        var expressionList = new List<NodeBase>();

        while (_tokenSequence.PeekNext() != null 
               || (_tokenSequence.PeekNext() != null && _tokenSequence.Current() == null))
        {
            TryParseTopLevel(out var node);
            if (node != null)
            {
                expressionList.Add(node);
            }
        }

        var symbolTable = new PlampNativeSymbolTable(_transactionSource.SymbolDictionary);
        return new ParserResult(expressionList, tokenRes.Exceptions, symbolTable);
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

    private ExpressionParsingResult TryParseEmpty(out EmptyNode node)
    {
        var transaction = _transactionSource.BeginTransaction();
        if (TryConsumeNextNonWhiteSpace<EndOfLine>(_ => true, _ => {}, out var eol))
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

    private ExpressionParsingResult TryParseUsing(out UseNode node)
    {
        node = null;
        if (!TryConsumeNextNonWhiteSpace<KeywordToken>(
                x => x.Keyword == Keywords.Use, _ => { }, 
                out var useKeyword))
        {
            throw new Exception("Internal parser bug");
        }
        
        var transaction = _transactionSource.BeginTransaction();
        var res = ParseMemberAccessSequence(transaction, out var useMember);
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
        
        AdvanceToRequestedTokenWithException<EndOfLine>(transaction);
        node = new UseNode(useMember);
        transaction.AddSymbol(node, [useMember], [useKeyword]);
        transaction.Commit();
        
        return ExpressionParsingResult.Success;
    }

    private ExpressionParsingResult TryParseFunction(out DefNode node)
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
        transaction.AddSymbol(nameNode, [], [name]);
        
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
                PlampNativeExceptionInfo.ExpectedArgDefinition().GetPlampException(def));
            AdvanceToRequestedTokenWithException<EndOfLine>(transaction);
        }

        var body = ParseOptionalBody(transaction);
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

        ExpressionParsingResult ParameterWrapper(out ParameterNode node)
        {
            return TryParseParameter(transaction, out node);
        }
    }

    private void TryParseBody(out BodyNode body)
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
        var outerTransaction = _transactionSource.BeginTransaction();
        outerTransaction.AddSymbol(body, expressions.ToArray(), []);
        outerTransaction.Commit();
    }

    private ExpressionParsingResult TryParseParameter(IParsingTransaction transaction, out ParameterNode parameterNode)
    {
        parameterNode = null;
        var typePeek = _tokenSequence.PeekNextNonWhiteSpace();
        if (typePeek == null || typePeek.GetType() != typeof(Word))
        {
            return ExpressionParsingResult.FailedNeedPass;
        }
        
        TryParseType(transaction, out var type);
        var argPeek = _tokenSequence.PeekNextNonWhiteSpace();
        if (argPeek is null || argPeek.GetType() != typeof(Word))
        {
            AddExceptionToTheTokenRange(typePeek, argPeek, PlampNativeExceptionInfo.InvalidParameterDefinition(), transaction);
            return ExpressionParsingResult.FailedNeedCommit;
        }

        TryConsumeNextNonWhiteSpace<Word>(_ => true, _ => { }, out _);
        var name = new MemberNode(argPeek.GetStringRepresentation());
        transaction.AddSymbol(name, [], [argPeek]);

        parameterNode = new ParameterNode(type, name);
        transaction.AddSymbol(parameterNode, [type, name], []);
        return ExpressionParsingResult.Success;
    }
    
    internal ExpressionParsingResult TryParseType(IParsingTransaction transaction, out NodeBase typeNode, bool strict = true)
    {
        typeNode = null;
        var res = ParseMemberAccessSequence(transaction, out var typeMember);
        
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
        typeNode = new TypeNode(typeMember, types);
        var children = new List<NodeBase>{typeMember};
        if(types != null) children.AddRange(types);
        transaction.AddSymbol(typeNode, children.ToArray(), []);
        
        return ExpressionParsingResult.Success;
    }
    
    /// <summary>
    /// Parsing member access, return node member_access(member_access(member_access(...),member),member)
    /// or member if one
    /// </summary>
    private ExpressionParsingResult ParseMemberAccessSequence(IParsingTransaction transaction, out NodeBase node)
    {
        node = null;
        var peek = _tokenSequence.PeekNextNonWhiteSpace();
        if (peek == null || peek.GetType() != typeof(Word))
        {
            return ExpressionParsingResult.FailedNeedRollback;
        }

        _tokenSequence.GetNextNonWhiteSpace();
        node = new MemberNode(peek.GetStringRepresentation());
        transaction.AddSymbol(node, [], [peek]);
        
        while (true)
        {
            if (!TryConsumeNextNonWhiteSpace<OperatorToken>(x => x.Operator == OperatorEnum.MemberAccess, 
                    _ => { },
                    out var op))
            {
                break;
            }
            
            if (TryConsumeNextNonWhiteSpace<Word>(_ => true, _ =>
                {
                    AddExceptionToTheTokenRange(peek, op, PlampNativeExceptionInfo.InvalidTypeName(),
                        transaction);
                }, out var word))
            {
                var accessTo = new MemberNode(word.GetStringRepresentation());
                transaction.AddSymbol(accessTo, [], [word]);
                var fromOld = node;
                node = new MemberAccessNode(node, accessTo);
                transaction.AddSymbol(node, [fromOld, accessTo], [op]);
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

    private ExpressionParsingResult TryParseScopedWithDepth<TReturn>(
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
                transaction.AddSymbol(expression, [], [keyword]);
                AdvanceToRequestedTokenWithException<EndOfLine>(transaction);
                return ExpressionParsingResult.Success;
            case Keywords.Continue:
                expression = new ContinueNode();
                transaction.AddSymbol(expression, [], [keyword]);
                AdvanceToRequestedTokenWithException<EndOfLine>(transaction);
                return ExpressionParsingResult.Success;
            case Keywords.Return:
                TryParseWithPrecedence(out var precedence);
                expression = new ReturnNode(precedence);
                transaction.AddSymbol(expression, precedence == null ? [] : [precedence], [keyword]);
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

        while (true)
        {
            if(TryParseEmpty(out _) == ExpressionParsingResult.Success) continue;
            var condTrans = _transactionSource.BeginTransaction();
            
            if (TryParseScopedWithDepth(TryParseElifKeyword, out KeywordToken keyword) != ExpressionParsingResult.Success)
            {
                condTrans.Rollback();
                break;
            }
            
            if (keyword != null)
            {
                //TODO: Skip body with match depth
                TryParseConditionClause(keyword, condTrans, out var elifClause);
                if(elifClause != null) elifClauses.Add(elifClause);
            }
            condTrans.Commit();
        }
        
        var elseBody = default(BodyNode);
        if (TryConsumeNextNonWhiteSpace<KeywordToken>(x => x.Keyword == Keywords.Else, _ => {}, out _))
        {
            elseBody = ParseOptionalBody(transaction);
        }
            
        conditionNode = new ConditionNode(baseClause, elifClauses, elseBody);
        var children = new List<NodeBase>(elifClauses.Count + 2)
            {baseClause};
        children.AddRange(elifClauses);
        if(elseBody != null) children.Add(elseBody);
        transaction.AddSymbol(conditionNode, children.ToArray(), []);
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
        transaction.AddSymbol(conditionNode, [condition, body], [clauseDefinition]);
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
        //TODO: may add to symbol table
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
                    PlampNativeExceptionInfo.Expected(nameof(Comma))
                        .GetPlampException(_tokenSequence.PeekNext()));
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
                transaction.AddSymbol(whileNode, [expression, body], [whileToken]);
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

            List<NodeBase> expressions = expression == null ? [] : [expression];
            var bodyNode = new BodyNode(expressions);
            transaction.AddSymbol(bodyNode, expressions.ToArray(), []);
            return bodyNode;
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

    private ExpressionParsingResult TryParseWithPrecedence(out NodeBase node, int rbp)
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

    private ExpressionParsingResult TryParseNud(out NodeBase node)
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
            transaction = _transactionSource.BeginTransaction();
            var member = new MemberNode(word.GetStringRepresentation());
            transaction.AddSymbol(member, [], [word]);
            transaction.Commit();
            node = ParsePostfixIfExist(member);
            return ExpressionParsingResult.Success;
        }

        node = null;
        transaction.Rollback();
        
        
        return ExpressionParsingResult.FailedNeedCommit;
    }

    private ExpressionParsingResult TryParseLed(int rbp, NodeBase left, out NodeBase output)
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
            transaction.AddSymbol(output, [left, right], [token]);
            transaction.Commit();
            return res;
        }
        
        transaction.Rollback();
        output = left;
        return ExpressionParsingResult.FailedNeedCommit;
    }

    private ExpressionParsingResult TryParseVariableDeclaration(
        IParsingTransaction transaction, 
        out NodeBase variableDeclaration)
    {
        var typ = default(NodeBase);
        if (!TryConsumeNextNonWhiteSpace<KeywordToken>(
                x => x.Keyword == Keywords.Var, _ => { }, out var varWord)
            && TryParseType(transaction, out typ) != ExpressionParsingResult.Success)
        {
            variableDeclaration = null;
            return ExpressionParsingResult.FailedNeedRollback;
        }
        
        //Null denotation starts with variable declaration
        if (TryConsumeNextNonWhiteSpaceWithoutRollback<Word>(
                _ => true,
                token => transaction.AddException(
                    PlampNativeExceptionInfo.ExpectedIdentifier().GetPlampException(token)), 
                out var name))
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

    private ExpressionParsingResult TryParseCastOperator(
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

    private ExpressionParsingResult TryParseSubExpression(
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

    private ExpressionParsingResult TryParseLiteral(out NodeBase node)
    {
        node = null;
        IParsingTransaction transaction;
        if (TryConsumeNextNonWhiteSpace<StringLiteral>(_ => true, _ => { }, out var literal))
        {
            transaction = _transactionSource.BeginTransaction();
            var stringLiteral = new ConstNode(literal.GetStringRepresentation(), typeof(string));
            transaction.AddSymbol(stringLiteral, [], [literal]);
            transaction.Commit();
            node = ParsePostfixIfExist(stringLiteral);
            return ExpressionParsingResult.Success;
        }

        if (TryConsumeNextNonWhiteSpace<NumberLiteral>(_ => true, _ => { }, out var numberLiteral))
        {
            transaction = _transactionSource.BeginTransaction();
            var number = new ConstNode(numberLiteral.ActualValue, numberLiteral.ActualType);
            transaction.AddSymbol(number, [], [numberLiteral]);
            transaction.Commit();
            node = ParsePostfixIfExist(number);
            return ExpressionParsingResult.Success;
        }

        if (TryConsumeNextNonWhiteSpace(
                t => t.Keyword is Keywords.True or Keywords.False, _ => { },
                out KeywordToken boolLiteral))
        {
            transaction = _transactionSource.BeginTransaction();
            var value = bool.Parse(boolLiteral.GetStringRepresentation());
            var boolNode = new ConstNode(value, typeof(bool));
            transaction.AddSymbol(boolNode, [], [boolLiteral]);
            transaction.Commit();
            node = ParsePostfixIfExist(boolNode);
            return ExpressionParsingResult.Success;
        }

        if (!TryConsumeNextNonWhiteSpace(
                t => t.Keyword is Keywords.Null, _ => { }, out KeywordToken nullToken))
            return ExpressionParsingResult.FailedNeedRollback;
        
        var nullNode = new ConstNode(null, null);
        transaction = _transactionSource.BeginTransaction();
        transaction.AddSymbol(nullNode, [], [nullToken]);
        transaction.Commit();
        node = ParsePostfixIfExist(nullNode);
        return ExpressionParsingResult.Success;
    }

    private ExpressionParsingResult TryParseConstructor(
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
                var children = new List<NodeBase> { type };
                children.AddRange(parameters);
                transaction.AddSymbol(ctor, children.ToArray(), [keywordToken]);
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

    private ExpressionParsingResult TryParsePrefixOperator(out NodeBase node)
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
        node = ParsePostfixIfExist(node);
        return ExpressionParsingResult.Success;

    }

    private NodeBase ParsePostfixIfExist(NodeBase inner)
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

    private bool TryParsePostfixOperator(NodeBase nodeBase, out NodeBase node)
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
        var transaction = _transactionSource.BeginTransaction();
        transaction.AddSymbol(node, [nodeBase], [@operator]);
        transaction.Commit();
        return true;

    }

    private bool TryParseIndexer(NodeBase inner, out NodeBase node)
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

    private bool TryParseMemberAccess(NodeBase input, out NodeBase res)
    {
        res = null;
        var transaction = _transactionSource.BeginTransaction();
        if (!TryConsumeNextNonWhiteSpace<OperatorToken>(
                x => x.Operator == OperatorEnum.MemberAccess, _ => { },
                out var access))
        {
            transaction.Rollback();
            return false;
        }

        if (TryConsumeNextNonWhiteSpace<Word>(_ => true, _ => { }, out var word))
        {
            var member = new MemberNode(word.GetStringRepresentation());
            res = new MemberAccessNode(input, member);
            transaction.AddSymbol(member, [], [word]);
            transaction.AddSymbol(res, [input, member], [access]);
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

    private bool TryParseCall(NodeBase input, out NodeBase res)
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
        PlampExceptionRecord exceptionRecord, 
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