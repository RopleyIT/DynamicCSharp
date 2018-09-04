﻿# DynamicCSharp
## Overview
DynamicCSharp is a small library designed for use 
on .NET Core 2.1 or later. With the Windows implementation
of the .NET framework, we could use CodeDom classes to
parse C# source code and compile it to a dynamically-loaded
assembly. With .NET core, these classes are replaced by the
Roslyn compiler API, a complex suite of classes that support
syntax tree generation and semantic analysis in addition to
emitting output assemblies.

DynamicCSharp provides a simple interface and Facade onto these
Roslyn classes, so that users who just want to compile C#
source to a dynamically loaded assembly at run-time have a simple
library that does this.

## Using the library
### Referencing the library
Add the NuGet package reference for this library to any .NET core 2.1 project
that needs to use this class first.

Once you have the package reference included, you will need to
add suitable using statements at the top of your source code:

``` c
using DynamicCSharp;
```

If you intend to use the Roslyn compiler's syntax or semantic analysis
products, which are captured as properties by this library, you
may also need using statements for each of those namespaces that you use:

``` c
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
```

### Creating the compiler facade
The library exposes its functionality via an interface with the name `ICompiler`. 
This interface is exposed by invoking the factory method of the
`Compiler` class:

``` c
ICompiler compiler = Compiler.Create();
```
### Setting the source code
The source code to be compiled must be either a text string, or
text retrieved from a `Stream`, where that stream is a source
of text possibly using text encoding.

If the source code is to be provided direct from a character string,
the `Source` property of the `ICompiler` interface must be assigned the
source text. In this case, the `SourceStream` property must be set
to `null`.

If the source code is to be read from an input stream, the
`SourceStream` property should be assigned an input stream with
at least read mode permissions enabled, containing the source
code in one of the standard text encodings as can be
discovered and read by a `StreamReader`.

Note that the source code must be sufficient to describe the
source for a complete assembly. At present there is no
mechanism for creating a dynamic assembly from a collection
of input sources or files.

### Referencing types outside the new assembly
It is probable that the code in the dynamic assembly needs to
reference .NET types outside the dynamic assembly. At the very
least this will include `System.Object` and probably a number
of other types.

These references should be set up prior to compilation using
one of three methods of the `ICompiler` interface:

|Method|Description|
|------|-----------|
|`AddReference (Type t)`|Adds a reference to a specific data type|
|`AddReference (string typeName)`|Adds a reference to a data type by type name|
|`AdddReferences (IEnumerable<string> typeNames)`|Adds a set of references to each of the named types in the enumerable|


### Generating and loading the output assembly

Once the source has been connected to the compiler as described
above, the compilation is invoked by setting a output assembly name
of your choice, followed by calling the `Compile` method:

``` c
compiler.AssemblyName = "MyUniqueAssemblyName";
compiler.Compile();
```

This method has a single boolean argument that is set to true if
you wish to build a debug assembly, or can be false or absent
to build a release assembly.

The results of the compilation are scattered across a number of
properties of the `ICompiler` interface as follows:

| Property | Roslyn or other class | Description |
|----------|-----------------------|-------------|
| `Assembly` | `Assembly` | The output assembly object, already dynamically loaded into the application |
| `SyntaxTree` | `SyntaxTree` | The Roslyn syntax tree object for the compilation |
| `SemanticModel` | `SemanticModel` | The Roslyn semantic analysis object |
| `Diagnostics` | `IEnumerable<Diagnostic>` | The list of warnings and errors generated by the compiler |
| `Compilation` | `Compilation` | The Roslyn compilation object |
| `HasErrors` | `bool` | Indicates whether the compilation was successful and generated an output assembly |

### Instantiating the new type and invoking methods
Since the newly created assembly has automatically been loaded into the
default loader context by the compilation process, we use standard
reflection techniques to create instances of types from the assembly
and to invoke the methods on those new instances. For example:

``` c
var newInst = Activator.CreateInstance(newType);
var getNextInt = (Func<int>)Delegate
     .CreateDelegate(typeof(Func<int>), newInst, newType.GetMethod("MyMethodName"));
```

You might want to create expression trees and compile them so that
construction and method invocation do not need to use reflection
every time you need to create or invoke. Reflection pays a high performance
price, so these are serious suggestions to consider.

### Example
The following is an example from one of the unit tests for this package:

``` c
public class TestClass
{
    private string source = @"
            using System;
            namespace Fred
            {
                public class Joe
                {
                    private int i = 0;
                    public int GetNextInt()
                    {
                        return ++i;
                    }
                }
            }";

    [Fact]
    public void CanInvokeEmittedMethods()
    {
        ICompiler c = Compiler.Create();
        c.AssemblyName = "Assem10";
        c.AddReferences(new string[] { "System.Int32", "System.Double", "System.IO.Path" });
        c.Source = source;
        c.Compile();
        Type type = c.Assembly.ExportedTypes.Where(t => t.Name == "Joe").FirstOrDefault();
        var joe = Activator.CreateInstance(type);
        Assert.IsType(type, joe);
        var getNextInt = (Func<int>)Delegate
            .CreateDelegate(typeof(Func<int>), joe, type.GetMethod("GetNextInt"));
        Assert.Equal(1, getNextInt());
        Assert.Equal(2, getNextInt());
    }
}
```
### Licensing

This product is published under the standard MIT License as 
described at https://opensource.org/licenses/MIT. The specific
wording for this license is as follows:

Copyright 2018 Ropley Information Technology Ltd.

Permission is hereby granted, free of charge, to any person 
obtaining a copy of this software and associated documentation 
files (the "Software"), to deal in the Software without 
restriction, including without limitation the rights 
to use, copy, modify, merge, publish, distribute, sublicense, 
and/or sell copies of the Software, and to permit persons 
to whom the Software is furnished to do so, subject to the 
following conditions:

The above copyright notice and this permission notice shall 
be included in all copies or substantial portions of the 
Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, 
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES 
OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND 
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT 
HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, 
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR 
OTHER DEALINGS IN THE SOFTWARE.
