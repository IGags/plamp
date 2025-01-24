using plamp.Ast.Node;
using plamp.Ast.Node.Assign;
using plamp.Ast.Node.Binary;
using plamp.Ast.Node.Body;
using plamp.Ast.Node.ControlFlow;
using plamp.Ast.Node.Unary;
using plamp.Native.Parsing;
using Xunit;

namespace plamp.Native.Tests.Parser;

public class ParserGeneralTests
{
    [Fact]
    public void ParseFindMaximum()
    {
        const string code = """
                            def int max(List<int> list)
                                var max = int.Min()
                                for(var item in list)
                                    if(max < item)
                                        max = item
                                return max
                            """;
        var parser = new PlampNativeParser();
        var result = parser.Parse(code);
        Assert.Empty(result.Exceptions);
        Assert.Single(result.NodeList);
        var expressionShould
            = new DefNode(
                new TypeNode(
                    new MemberNode("int"),
                    null),
                new MemberNode("max"),
                [
                    new ParameterNode(
                        new TypeNode(
                            new MemberNode("List"),
                            [
                                new TypeNode(
                                    new MemberNode("int"),
                                    null)
                            ]),
                        new MemberNode("list"))
                ],
                new BodyNode(
                [
                    new AssignNode(
                        new VariableDefinitionNode(
                            null,
                            new MemberNode("max")),
                        new CallNode(
                            new MemberAccessNode(
                                new MemberNode("int"),
                                new MemberNode("Min")),
                            [])),
                    new ForeachNode(
                        new VariableDefinitionNode(
                            null,
                            new MemberNode("item")),
                        new MemberNode("list"),
                        new BodyNode(
                        [
                            new ConditionNode(
                                new ClauseNode(
                                    new LessNode(
                                        new MemberNode("max"),
                                        new MemberNode("item")),
                                    new BodyNode(
                                    [
                                        new AssignNode(
                                            new MemberNode("max"),
                                            new MemberNode("item"))
                                    ])),
                                [],
                                null)
                        ])),
                    new ReturnNode(
                        new MemberNode("max"))
                ]));
        Assert.Equal(expressionShould, result.NodeList[0]);
    }

    [Fact]
    public void ParseFindSubstring()
    {
        const string code = """
                            def int substring(string str, string substr)
                                if(str == null || substr == null || str.Length < substr.Length) return -1
                                for(int index = 0, index < str.Length - substr.Length - 1, index++)
                                    bool isComplete = true
                                    for(int i = 0, i < substr.Length, i++)
                                        var current = index + i
                                        if(str[current] != substr[current])
                                            isComplete = false
                                            break
                                    if(isComplete) return index
                                return -1
                            """;
        var parser = new PlampNativeParser();
        var result = parser.Parse(code);
        Assert.Empty(result.Exceptions);
        Assert.Single(result.NodeList);
        var expressionShould
            = new DefNode(
                new TypeNode(
                    new MemberNode("int"),
                    null),
                new MemberNode("substring"),
                [
                    new ParameterNode(
                        new TypeNode(
                            new MemberNode("string"),
                            null),
                        new MemberNode("str")),
                    new ParameterNode(
                        new TypeNode(
                            new MemberNode("string"),
                            null),
                        new MemberNode("substr"))
                ],
                new BodyNode(
                [
                    new ConditionNode(
                        new ClauseNode(
                            new OrNode(
                                new OrNode(
                                    new EqualNode(
                                        new MemberNode("str"),
                                        new ConstNode(null, null)),
                                    new EqualNode(
                                        new MemberNode("substr"),
                                        new ConstNode(null, null))),
                                new LessNode(
                                    new MemberAccessNode(
                                        new MemberNode("str"),
                                        new MemberNode("Length")),
                                    new MemberAccessNode(
                                        new MemberNode("substr"),
                                        new MemberNode("Length")))),
                            new BodyNode(
                            [
                                new ReturnNode(
                                    new UnaryMinusNode(
                                        new ConstNode(1, typeof(int))))
                            ])),
                        [],
                        null),
                    new ForNode(
                        new AssignNode(
                            new VariableDefinitionNode(
                                new TypeNode(
                                    new MemberNode("int"),
                                    null),
                                new MemberNode("index")),
                            new ConstNode(0, typeof(int))),
                        new LessNode(
                            new MemberNode("index"),
                            new MinusNode(
                                new MinusNode(
                                    new MemberAccessNode(
                                        new MemberNode("str"),
                                        new MemberNode("Length")),
                                    new MemberAccessNode(
                                        new MemberNode("substr"),
                                        new MemberNode("Length"))),
                                new ConstNode(1, typeof(int)))),
                        new PostfixIncrementNode(
                            new MemberNode("index")),
                        new BodyNode(
                        [
                            new AssignNode(
                                new VariableDefinitionNode(
                                    new TypeNode(
                                        new MemberNode("bool"),
                                        null),
                                    new MemberNode("isComplete")),
                                new ConstNode(true, typeof(bool))),
                            new ForNode(
                                new AssignNode(
                                    new VariableDefinitionNode(
                                        new TypeNode(
                                            new MemberNode("int"),
                                            null),
                                        new MemberNode("i")),
                                    new ConstNode(0, typeof(int))),
                                new LessNode(
                                    new MemberNode("i"),
                                    new MemberAccessNode(
                                        new MemberNode("substr"),
                                        new MemberNode("Length"))),
                                new PostfixIncrementNode(
                                    new MemberNode("i")),
                                new BodyNode(
                                [
                                    new AssignNode(
                                        new VariableDefinitionNode(
                                            null,
                                            new MemberNode("current")),
                                        new PlusNode(
                                            new MemberNode("index"),
                                            new MemberNode("i"))),
                                    new ConditionNode(
                                        new ClauseNode(
                                            new NotEqualNode(
                                                new IndexerNode(
                                                    new MemberNode("str"),
                                                    [
                                                        new MemberNode("current")
                                                    ]),
                                                new IndexerNode(
                                                    new MemberNode("substr"),
                                                    [
                                                        new MemberNode("current")
                                                    ])),
                                            new BodyNode(
                                            [
                                                new AssignNode(
                                                    new MemberNode("isComplete"),
                                                    new ConstNode(false, typeof(bool))),
                                                new BreakNode()
                                            ])),
                                        [],
                                        null)
                                ])),
                            new ConditionNode(
                                new ClauseNode(
                                    new MemberNode("isComplete"),
                                    new BodyNode(
                                    [
                                        new ReturnNode(
                                            new MemberNode("index"))
                                    ])),
                                [],
                                null)
                        ])),
                    new ReturnNode(
                        new UnaryMinusNode(
                            new ConstNode(1, typeof(int))))
                ]));
        Assert.Equal(expressionShould, result.NodeList[0]);
    }

    [Fact]
    public void MergeSort()
    {
        const string code = """
                            def List<int> mergeSort(List<int> list)
                                if(list == null || list.Count == 0) return list
                                if(list.Count > 2)
                                    var splitBasis = list.Count / 2
                                    var first = mergeSort(list.Span(0, splitBasis))
                                    var second = mergeSort(list.Span(splitBasis, list.Count - splitBasis))
                                    var firstPtr = 0
                                    var secondPtr = 0
                                    var result = new List<int>()
                                    
                                    while(firstPtr < first.Count && secondPtr < second.Count)
                                        if(first[firstPtr] <= second[secondPtr])
                                            result.Add(first[firstPtr])
                                            firstPtr++
                                        else
                                            result.Add(second[secondPtr])
                                            secondPtr++
                                    return result
                                
                                elif(list.Count == 2)
                                    if(list[1] < list[0])
                                        list.Reverse()
                                    return list
                                
                                else return list
                            """;
        var parser = new PlampNativeParser();
        var result = parser.Parse(code);
        Assert.Single(result.NodeList);
        Assert.Empty(result.Exceptions);
        var expressionShould
            = new DefNode(
                new TypeNode(
                    new MemberNode("List"),
                    [
                        new TypeNode(
                            new MemberNode("int"),
                            null)
                    ]),
                new MemberNode("mergeSort"),
                [
                    new ParameterNode(
                        new TypeNode(
                            new MemberNode("List"),
                            [
                                new TypeNode(
                                    new MemberNode("int"),
                                    null)
                            ]),
                        new MemberNode("list")
                    )
                ],
                new BodyNode(
                [
                    new ConditionNode(
                        new ClauseNode(
                            new OrNode(
                                new EqualNode(
                                    new MemberNode("list"),
                                    new ConstNode(null, null)),
                                new EqualNode(
                                    new MemberAccessNode(
                                        new MemberNode("list"),
                                        new MemberNode("Count")),
                                    new ConstNode(0, typeof(int)))),
                            new BodyNode(
                            [
                                new ReturnNode(
                                    new MemberNode("list"))
                            ])),
                        [],
                        null
                    ),
                    new ConditionNode(
                        new ClauseNode(
                            new GreaterNode(
                                new MemberAccessNode(
                                    new MemberNode("list"),
                                    new MemberNode("Count")),
                                new ConstNode(2, typeof(int))),
                            new BodyNode(
                            [   
                                new AssignNode(
                                    new VariableDefinitionNode(
                                        null,
                                        new MemberNode("splitBasis")),
                                    new DivideNode(
                                        new MemberAccessNode(
                                            new MemberNode("list"),
                                            new MemberNode("Count")),
                                        new ConstNode(2, typeof(int)))),
                                new AssignNode(
                                    new VariableDefinitionNode(
                                        null,
                                        new MemberNode("first")),
                                    new CallNode(
                                        new MemberNode("mergeSort"),
                                        [
                                            new CallNode(
                                                new MemberAccessNode(
                                                    new MemberNode("list"),
                                                    new MemberNode("Span")),
                                                [
                                                    new ConstNode(0, typeof(int)),
                                                    new MemberNode("splitBasis")
                                                ])
                                        ])),
                                new AssignNode(
                                    new VariableDefinitionNode(
                                        null,
                                        new MemberNode("second")),
                                    new CallNode(
                                        new MemberNode("mergeSort"),
                                        [
                                            new CallNode(
                                                new MemberAccessNode(
                                                    new MemberNode("list"),
                                                    new MemberNode("Span")),
                                                [
                                                    new MemberNode("splitBasis"),
                                                    new MinusNode(
                                                        new MemberAccessNode(
                                                            new MemberNode("list"),
                                                            new MemberNode("Count")),
                                                        new MemberNode("splitBasis"))
                                                ])
                                        ])),
                                new AssignNode(
                                    new VariableDefinitionNode(
                                        null,
                                        new MemberNode("firstPtr")),
                                    new ConstNode(0, typeof(int))),
                                new AssignNode(
                                    new VariableDefinitionNode(
                                        null,
                                        new MemberNode("secondPtr")),
                                    new ConstNode(0, typeof(int))),
                                new AssignNode(
                                    new VariableDefinitionNode(
                                        null,
                                        new MemberNode("result")),
                                    new ConstructorNode(
                                        new TypeNode(
                                            new MemberNode("List"),
                                            [
                                                new TypeNode(
                                                    new MemberNode("int"),
                                                    null)
                                            ]),
                                        [])),
                                new EmptyNode(),
                                new WhileNode(
                                    new AndNode(
                                        new LessNode(
                                            new MemberNode("firstPtr"),
                                            new MemberAccessNode(
                                                new MemberNode("first"),
                                                new MemberNode("Count"))),
                                        new LessNode(
                                            new MemberNode("secondPtr"),
                                            new MemberAccessNode(
                                                new MemberNode("second"),
                                                new MemberNode("Count")))),
                                    new BodyNode(
                                    [
                                        new ConditionNode(
                                            new ClauseNode(
                                                new LessOrEqualNode(
                                                    new IndexerNode(
                                                        new MemberNode("first"),
                                                        [
                                                            new MemberNode("firstPtr")
                                                        ]),
                                                    new IndexerNode(
                                                        new MemberNode("second"),
                                                        [
                                                            new MemberNode("secondPtr")
                                                        ])),
                                                new BodyNode(
                                                [
                                                    new CallNode(
                                                        new MemberAccessNode(
                                                            new MemberNode("result"),
                                                            new MemberNode("Add")),
                                                        [
                                                            new IndexerNode(
                                                                new MemberNode("first"),
                                                                [
                                                                    new MemberNode("firstPtr")
                                                                ])
                                                        ]),
                                                    new PostfixIncrementNode(
                                                        new MemberNode("firstPtr"))
                                                ])),
                                            [],
                                            new BodyNode(
                                            [
                                                new CallNode(
                                                    new MemberAccessNode(
                                                        new MemberNode("result"),
                                                        new MemberNode("Add")),
                                                    [
                                                        new IndexerNode(
                                                            new MemberNode("second"),
                                                            [
                                                                new MemberNode("secondPtr")
                                                            ])
                                                    ]),
                                                new PostfixIncrementNode(
                                                    new MemberNode("secondPtr"))
                                            ]))
                                    ])),
                                new ReturnNode(
                                    new MemberNode("result"))
                            ])),
                        [
                            new ClauseNode(
                                new EqualNode(
                                    new MemberAccessNode(
                                        new MemberNode("list"),
                                        new MemberNode("Count")),
                                    new ConstNode(2, typeof(int))),
                                new BodyNode(
                                [
                                    new ConditionNode(
                                        new ClauseNode(
                                            new LessNode(
                                                new IndexerNode(
                                                    new MemberNode("list"),
                                                    [
                                                        new ConstNode(1, typeof(int))
                                                    ]),
                                                new IndexerNode(
                                                    new MemberNode("list"),
                                                    [
                                                        new ConstNode(0, typeof(int))
                                                    ])),
                                            new BodyNode(
                                            [
                                                new CallNode(
                                                    new MemberAccessNode(
                                                        new MemberNode("list"),
                                                        new MemberNode("Reverse")),
                                                    [])
                                            ])),
                                        [],
                                        null),
                                    new ReturnNode(
                                        new MemberNode("list"))
                                ]))
                        ],
                        new BodyNode(
                        [
                            new ReturnNode(
                                new MemberNode("list"))
                        ]))
                ])
            );
        Assert.Equal(expressionShould, result.NodeList[0]);
    }

    [Fact]
    public void ParseSomethingWeird()
    {
        const string code = """
                                        use RememberMe
                            
                            def void main()
                                    var im = new Image()
                                    im = fillImage(im)
                                    print(im.generateStory())
                            
                            def Image fillImage(Image image)
                                image.Add("Joy")
                                image.Add("Temptation")
                                image.Add("Honey")
                                image.Add("Bees")
                                image.Add("Pain")
                                image.Add("Lesson")
                                image.Build()
                                return image
                            """;
        var parser = new PlampNativeParser();
        var result = parser.Parse(code);
        Assert.Empty(result.Exceptions);
        Assert.Equal(5, result.NodeList.Count);
        var expressionShould1
            = new UseNode(new MemberNode("RememberMe"));
        Assert.Equal(expressionShould1, result.NodeList[0]);
        Assert.Equal(new EmptyNode(), result.NodeList[1]);
        var expressionShould2
            = new DefNode(
                new TypeNode(
                    new MemberNode("void"),
                    null),
                new MemberNode("main"),
                [],
                new BodyNode(
                [
                    new AssignNode(
                        new VariableDefinitionNode(
                            null,
                            new MemberNode("im")),
                        new ConstructorNode(
                            new TypeNode(
                                new MemberNode("Image"),
                                null),
                            [])),
                    new AssignNode(
                        new MemberNode("im"),
                        new CallNode(
                            new MemberNode("fillImage"),
                            [
                                new MemberNode("im")
                            ])),
                    new CallNode(
                        new MemberNode("print"),
                        [
                            new CallNode(
                                new MemberAccessNode(
                                    new MemberNode("im"),
                                    new MemberNode("generateStory")),
                                [])
                        ])
                ]));
        Assert.Equal(expressionShould2, result.NodeList[2]);
        Assert.Equal(new EmptyNode(), result.NodeList[3]);
        var expressionShould3
            = new DefNode(
                new TypeNode(
                    new MemberNode("Image"),
                    null),
                new MemberNode("fillImage"),
                [
                    new ParameterNode(
                        new TypeNode(
                            new MemberNode("Image"),
                            null),
                        new MemberNode("image"))
                ],
                new BodyNode(
                [
                    new CallNode(
                        new MemberAccessNode(
                            new MemberNode("image"),
                            new MemberNode("Add")),
                        [
                            new ConstNode("Joy", typeof(string))
                        ]),
                    new CallNode(
                        new MemberAccessNode(
                            new MemberNode("image"),
                            new MemberNode("Add")),
                        [
                            new ConstNode("Temptation", typeof(string))
                        ]),
                    new CallNode(
                        new MemberAccessNode(
                            new MemberNode("image"),
                            new MemberNode("Add")),
                        [
                            new ConstNode("Honey", typeof(string))
                        ]),
                    new CallNode(
                        new MemberAccessNode(
                            new MemberNode("image"),
                            new MemberNode("Add")),
                        [
                            new ConstNode("Bees", typeof(string))
                        ]),
                    new CallNode(
                        new MemberAccessNode(
                            new MemberNode("image"),
                            new MemberNode("Add")),
                        [
                            new ConstNode("Pain", typeof(string))
                        ]),
                    new CallNode(
                        new MemberAccessNode(
                            new MemberNode("image"),
                            new MemberNode("Add")),
                        [
                            new ConstNode("Lesson", typeof(string))
                        ]),
                    new CallNode(
                        new MemberAccessNode(
                            new MemberNode("image"),
                            new MemberNode("Build")),
                        []),
                    new ReturnNode(
                        new MemberNode("image"))
                ]));
        Assert.Equal(expressionShould3, result.NodeList[4]);
    }
}