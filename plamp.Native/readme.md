## Native plamp dialect

This is first and last dialect of plamp.

### Any valid plamp dialect should

* Tokenize program text
* Parse token sequence to abstract syntax tree(from **plamp.Ast** package)
* Validate tokenization errors
* Validate syntax errors

> If you want to figure out in functionality of this module, you can check **plamp.Native.Tests** package.

### Quick package structure overview

* **PlampNativeTokenizer** - simple tokenizer, that sequentially tokenize source code.
* **PlampNativeParser** - main class of the package. Parses prepared token sequence to abstract syntax tree.
* **DepthCounter** - simple class that counts current scope depth for body parsing and nested scope validation.
* **DepthHandle** - product of depth counter *disposable*. Decrement depth counter after dispose.
* **ParsingTransactionSource** - generator of parsing transaction. 
Transaction can be commited(advance position and add exceptions), 
rolled back(reset position and exceptions) or passed(advance position and ignore exceptions). 
You cannot commit transaction if nested transaction exists and not closed.

* **TokenSequence** - helper class that used in parser, provide token enumeration methods.

### Syntax reference

#### General

**Statements**

Statements should be split by new line separator. If a statement is too long you can move part of a statement to another line and add 
'->' symbol to end of the previous line.<br>

If you want to break line inside a statement just do 
```csharp
(IsDog || IsCat || IsPig || IsCat) && ->
!(IsBig || IsScary)
```

Is statement has a body (*ex. Condition statement or cycle*) 
body should be at the same line if it single-line or should be on the next line and beyond
(in this case every line should has tab \t or 4 white space prefix multiplied on scope depth)

```csharp
while(true) ping()
```

Or
```csharp
while(true)
    ping()
    pong()
```

Nested statements
```csharp
if(i < 2)
    print("hi")
elif(i >= 2 && < 10)
    for(,i < 10, i++)
        print(i)
else
    print("bye")
```

In the root of code file should be top level statements(such as **def** or **use**)

**Use statement**

This statement import external c# library

```csharp
use [assembly name]
```

**Def statement**

This statement defines a function

Function can be single line

```csharp
def [output type] [func name]([arg, ...]?) [body]?
```

Or multiline

```csharp
def [output type] [func name]([arg, ...]?)
    [[body]\n
    ...]?
```

Function should have return type or void if function returns nothing,
name, list of arguments - list of pairs(type and name) enclosed in parens
and function body.

#### Top level statements

These statements must not be in other statements body