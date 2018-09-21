# DynamicCSharp
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

For detailed documentation on how to use the library, please consult 
the [GitHub wiki pages](https://github.com/RopleyIT/DynamicCSharp/wiki).
