using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Assign;
using plamp.Abstractions.Ast.Node.Binary;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.Ast.Node.ControlFlow;
using plamp.Abstractions.CompilerEmission;
using plamp.CodeEmission.Tests.Infrastructure;
using plamp.ILCodeEmitters;

namespace plamp.CodeEmission.Tests;

public class ConditionTests
{
    [Fact]
    public async Task EmitIfCondition()
    {
        var arg = new TestParameter(typeof(int), "arg");

        var tempVarName = "temp";
        var tempVarName2 = "temp2";
        var tempVarName3 = "temp3";
        var tempVarName4 = "temp4";
        const string lesser10 = "Number lesser 10";
        const string greater10 = "Great number";
        
        /*
         * bool temp
         * int temp4
         * temp4 = 10
         * temp = arg < temp4
         * if(temp)
         *     string temp2
         *     temp2 = "Number lesser 10"
         *     return temp2
         * string temp3
         * temp3 = "Great number"
         * return temp3
         */
        var body = new BodyNode(
        [
            new VariableDefinitionNode(EmissionSetupHelper.CreateTypeNode(typeof(bool)), new MemberNode(tempVarName)),
            new VariableDefinitionNode(EmissionSetupHelper.CreateTypeNode(typeof(int)), new MemberNode(tempVarName4)),
            new AssignNode(new MemberNode(tempVarName4), new LiteralNode(10, typeof(int))),
            new AssignNode(new MemberNode(tempVarName), new LessNode(new MemberNode(arg.Name), new MemberNode(tempVarName4))),
            new ConditionNode(
                new MemberNode(tempVarName),
                new BodyNode([
                    new VariableDefinitionNode(EmissionSetupHelper.CreateTypeNode(typeof(string)), new MemberNode(tempVarName2)),
                    new AssignNode(new MemberNode(tempVarName2), new LiteralNode(lesser10, typeof(string))),
                    new ReturnNode(new MemberNode(tempVarName2))
                ]), null),
            new VariableDefinitionNode(EmissionSetupHelper.CreateTypeNode(typeof(string)), new MemberNode(tempVarName3)),
            new AssignNode(new MemberNode(tempVarName3), new LiteralNode(greater10, typeof(string))),
            new ReturnNode(new MemberNode(tempVarName3))
        ]);

        var (instance, methodInfo) =
            await EmissionSetupHelper.CreateInstanceWithMethodAsync([arg], body, typeof(string));
        
        var res = methodInfo!.Invoke(instance, [5])!;
        Assert.Equal(lesser10, res);
        var res2 = methodInfo.Invoke(instance, [882])!;
        Assert.Equal(greater10, res2);
    }

    [Fact]
    public async Task EmitIfElseCondition()
    {
        var arg = new TestParameter(typeof(int), "arg");
        
        //TODO: phi-funcs need to be added in my cmp :^)
        var tempVarName = "temp";
        var tempVarName2 = "temp2";
        var tempVarName3 = "temp3";
        var tempVarName4 = "temp4";
        var tempVarName5 = "temp5";
        var tempVarName6 = "temp6";
        var tempVarName7 = "temp6";
        /*
         * int temp
         * int temp2
         * int temp3
         * bool temp4
         * string temp5
         * 
         * temp = 2
         * temp2 = arg % temp
         * temp3 = 0
         * temp4 = temp2 == temp3
         * 
         * if(temp4)
         *     string temp6
         *     temp6 = "Chet"
         *     temp5 = temp6
         * else
         *     string temp7
         *     temp7 = "Nechet"
         *     temp5 = temp7
         * return temp5
         */
        const string trueBranchVal = "Chet";
        const string falseBranchVal = "Nechet";
        var body = new BodyNode(
        [
            new VariableDefinitionNode(EmissionSetupHelper.CreateTypeNode(typeof(int)), new MemberNode(tempVarName)),
            new VariableDefinitionNode(EmissionSetupHelper.CreateTypeNode(typeof(int)), new MemberNode(tempVarName2)),
            new VariableDefinitionNode(EmissionSetupHelper.CreateTypeNode(typeof(int)), new MemberNode(tempVarName3)),
            new VariableDefinitionNode(EmissionSetupHelper.CreateTypeNode(typeof(bool)), new MemberNode(tempVarName4)),
            new VariableDefinitionNode(EmissionSetupHelper.CreateTypeNode(typeof(string)), new MemberNode(tempVarName5)),
            
            new AssignNode(new MemberNode(tempVarName), new LiteralNode(2, typeof(int))),
            new AssignNode(new MemberNode(tempVarName2), new ModuloNode(new MemberNode(arg.Name), new MemberNode(tempVarName))),
            new AssignNode(new MemberNode(tempVarName3), new LiteralNode(0, typeof(int))),
            new AssignNode(new MemberNode(tempVarName4), new EqualNode(new MemberNode(tempVarName2), new MemberNode(tempVarName3))),
            
            new ConditionNode(
                    new MemberNode(tempVarName4),
                    new BodyNode(
                        [
                            new VariableDefinitionNode(EmissionSetupHelper.CreateTypeNode(typeof(string)), new MemberNode(tempVarName6)),
                            new AssignNode(new MemberNode(tempVarName6), new LiteralNode(trueBranchVal, typeof(string))),
                            new AssignNode(new MemberNode(tempVarName5), new MemberNode(tempVarName6))
                        ]),
                new BodyNode(
                    [
                        new VariableDefinitionNode(EmissionSetupHelper.CreateTypeNode(typeof(string)), new MemberNode(tempVarName7)),
                        new AssignNode(new MemberNode(tempVarName7), new LiteralNode(falseBranchVal, typeof(string))),
                        new AssignNode(new MemberNode(tempVarName5), new MemberNode(tempVarName7))
                    ])),
            new ReturnNode(new MemberNode(tempVarName5))
        ]);

        var (instance, methodInfo) =
            await EmissionSetupHelper.CreateInstanceWithMethodAsync([arg], body, typeof(string));
        var res = methodInfo!.Invoke(instance, [0])!;
        Assert.Equal(trueBranchVal, res);
        var res2 = methodInfo.Invoke(instance, [1])!;
        Assert.Equal(falseBranchVal, res2);
    }

    [Fact]
    public async Task EmitIfElifCondition()
    {
        var arg = new TestParameter(typeof(int), "arg");
        
        var tempVarName = "temp";
        var tempVarName2 = "temp2";
        var tempVarName3 = "temp3";
        var tempVarName4 = "temp4";
        var tempVarName5 = "temp5";
        var tempVarName6 = "temp6";
        var tempVarName7 = "temp7";
        /*
         * int temp
         * temp = 18
         * bool temp2
         * temp2 = arg < temp
         *
         * int temp4
         * temp4 = 100
         * bool temp5
         * temp5 = arg < temp4
         * 
         * if(temp2)
         *     string temp3
         *     temp3 = "u're too young"
         *     retrun temp3
         *
         * else
         *     if(temp5)
         *         string temp6
         *         temp6 = "yogurt is ready"
         *         retrun temp6
         *
         * string temp7
         * temp7 = "u're wizard!"
         * return temp7
         */
        const string ifClause = "u're too young";
        const string elifClause = "yogurt is ready";
        const string rootScope = "u're wizard!";
        
        var body = new BodyNode(
        [
            new VariableDefinitionNode(EmissionSetupHelper.CreateTypeNode(typeof(int)), new MemberNode(tempVarName)),
            new AssignNode(new MemberNode(tempVarName), new LiteralNode(18, typeof(int))),
            new VariableDefinitionNode(EmissionSetupHelper.CreateTypeNode(typeof(bool)), new MemberNode(tempVarName2)),
            new AssignNode(new MemberNode(tempVarName2), new LessNode(new MemberNode(arg.Name), new MemberNode(tempVarName))),
            
            new VariableDefinitionNode(EmissionSetupHelper.CreateTypeNode(typeof(int)), new MemberNode(tempVarName4)),
            new AssignNode(new MemberNode(tempVarName4), new LiteralNode(100, typeof(int))),
            new VariableDefinitionNode(EmissionSetupHelper.CreateTypeNode(typeof(bool)), new MemberNode(tempVarName5)),
            new AssignNode(new MemberNode(tempVarName5), new LessNode(new MemberNode(arg.Name), new MemberNode(tempVarName4))),
            
            new ConditionNode(
                    new MemberNode(tempVarName2),
                    new BodyNode(
                        [
                            new VariableDefinitionNode(EmissionSetupHelper.CreateTypeNode(typeof(string)), new MemberNode(tempVarName3)),
                            new AssignNode(new MemberNode(tempVarName3), new LiteralNode(ifClause, typeof(string))),
                            new ReturnNode(new MemberNode(tempVarName3))
                        ]),
                    new BodyNode(
                        [
                            new ConditionNode(
                                new MemberNode(tempVarName5),
                                new BodyNode(
                                [
                                    new VariableDefinitionNode(EmissionSetupHelper.CreateTypeNode(typeof(string)), new MemberNode(tempVarName6)),
                                    new AssignNode(new MemberNode(tempVarName6), new LiteralNode(elifClause, typeof(string))),
                                    new ReturnNode(new MemberNode(tempVarName6))
                                ]), null)
                        ])),
            new VariableDefinitionNode(EmissionSetupHelper.CreateTypeNode(typeof(string)), new MemberNode(tempVarName7)),
            new AssignNode(new MemberNode(tempVarName7), new LiteralNode(rootScope, typeof(string))),
            new ReturnNode(new MemberNode(tempVarName7))
        ]);

        var (instance, methodInfo) =
            await EmissionSetupHelper.CreateInstanceWithMethodAsync([arg], body, typeof(string));
        
        var res = methodInfo!.Invoke(instance, [8])!;
        Assert.Equal(ifClause, res);
        var res2 = methodInfo.Invoke(instance, [27])!;
        Assert.Equal(elifClause, res2);
        var res3 = methodInfo.Invoke(instance, [102])!;
        Assert.Equal(rootScope, res3);
    }

    [Fact]
    public async Task EmitIfManyElifElseClause()
    {
        var arg = new TestParameter(typeof(int), "arg");
        
        var tempVarName = "temp";
        var tempVarName2 = "temp2";
        var tempVarName3 = "temp3";
        var tempVarName4 = "temp4";
        var tempVarName5 = "temp5";
        var tempVarName6 = "temp6";
        var tempVarName7 = "temp7";
        
        const string ifClause = "u're too young";
        const string elifClause = "yogurt is ready";
        const string rootScope = "u're wizard!";
        const string helloClause = "hello";
        
        /*
         * int temp
         * temp = 18
         * bool temp2
         * temp2 = arg < temp
         *
         * int temp3
         * temp3 = 100
         * bool temp4
         * temp4 = arg < temp3
         *
         * int temp5
         * temp5 = 1900
         * bool temp6
         * temp6 = arg == temp5
         *
         * string temp7
         * if(temp2)
         *     temp7 = "u're too young"
         * else
         *     if(temp4)
         *         temp7 = "yogurt is ready"
         *         else
         *             if(temp6)
         *                 temp7 = "hello"
         *             else
         *                 temp7 = "u're wizard!"
         * return temp7
         */
        var body = new BodyNode(
        [
            new VariableDefinitionNode(EmissionSetupHelper.CreateTypeNode(typeof(int)), new MemberNode(tempVarName)),
            new AssignNode(new MemberNode(tempVarName), new LiteralNode(18, typeof(int))),
            new VariableDefinitionNode(EmissionSetupHelper.CreateTypeNode(typeof(bool)), new MemberNode(tempVarName2)),
            new AssignNode(new MemberNode(tempVarName2), new LessNode(new MemberNode(arg.Name), new MemberNode(tempVarName))),
            
            new VariableDefinitionNode(EmissionSetupHelper.CreateTypeNode(typeof(int)), new MemberNode(tempVarName3)),
            new AssignNode(new MemberNode(tempVarName3), new LiteralNode(100, typeof(int))),
            new VariableDefinitionNode(EmissionSetupHelper.CreateTypeNode(typeof(bool)), new MemberNode(tempVarName4)),
            new AssignNode(new MemberNode(tempVarName4), new LessNode(new MemberNode(arg.Name), new MemberNode(tempVarName3))),
            
            new VariableDefinitionNode(EmissionSetupHelper.CreateTypeNode(typeof(int)), new MemberNode(tempVarName5)),
            new AssignNode(new MemberNode(tempVarName5), new LiteralNode(1900, typeof(int))),
            new VariableDefinitionNode(EmissionSetupHelper.CreateTypeNode(typeof(bool)), new MemberNode(tempVarName6)),
            new AssignNode(new MemberNode(tempVarName6), new EqualNode(new MemberNode(arg.Name), new MemberNode(tempVarName5))),
            
            new VariableDefinitionNode(EmissionSetupHelper.CreateTypeNode(typeof(string)), new MemberNode(tempVarName7)),
            new ConditionNode(
                new MemberNode(tempVarName2),
                new BodyNode(
                [
                    new AssignNode(new MemberNode(tempVarName7), new LiteralNode(ifClause, typeof(string)))
                ]),
                new BodyNode(
                    [
                        new ConditionNode(
                            new MemberNode(tempVarName4),
                            new BodyNode(
                            [
                                new AssignNode(new MemberNode(tempVarName7), new LiteralNode(elifClause, typeof(string)))
                            ]),
                            new BodyNode(
                            [
                                new ConditionNode(
                                    new MemberNode(tempVarName6),
                                    new BodyNode(
                                    [
                                        new AssignNode(new MemberNode(tempVarName7), new LiteralNode(helloClause, typeof(string)))
                                    ]),
                                    new BodyNode(
                                    [
                                        new AssignNode(new MemberNode(tempVarName7), new LiteralNode(rootScope, typeof(string)))
                                    ]))
                            ]))
                    ]
                )
            ),
            new ReturnNode(new MemberNode(tempVarName7))
        ]);

        var (instance, methodInfo) =
            await EmissionSetupHelper.CreateInstanceWithMethodAsync([arg], body, typeof(string));
        
        var res = methodInfo!.Invoke(instance, [8])!;
        Assert.Equal(ifClause, res);
        var res2 = methodInfo.Invoke(instance, [27])!;
        Assert.Equal(elifClause, res2);
        var res3 = methodInfo.Invoke(instance, [102])!;
        Assert.Equal(rootScope, res3);
        var res4 = methodInfo.Invoke(instance, [1900])!;
        Assert.Equal(helloClause, res4);
    }

    [Fact]
    public async Task EmitReturnInBothClause()
    {
        var arg = new TestParameter(typeof(int), "arg");

        const string subRes = "sub";
        const string nontRes = "nont";
        /*
         * if(arg < 0)
         *     return "sub"
         * else
         *     return "nont"
         */
        var body = new BodyNode(
        [
            new ConditionNode(
                new LessNode(new MemberNode(arg.Name), new LiteralNode(0, typeof(int))),
                new BodyNode(
                [
                    new ReturnNode(new LiteralNode(subRes, subRes.GetType()))
                ]),
                new BodyNode(
                [
                    new ReturnNode(new LiteralNode(nontRes, nontRes.GetType()))
                ]))
        ]);

        var (instance, method) = await EmissionSetupHelper.CreateInstanceWithMethodAsync([arg], body, typeof(string));
        var res1 = method!.Invoke(instance, [-1]);
        Assert.Equal(subRes, res1);
        
        var res2 = method.Invoke(instance, [0]);
        Assert.Equal(nontRes, res2);
        
        var res3 = method.Invoke(instance, [1]);
        Assert.Equal(nontRes, res3);
    }
}