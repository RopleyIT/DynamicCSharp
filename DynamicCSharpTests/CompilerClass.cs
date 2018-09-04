using System;
using Xunit;
using DynamicCSharp;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using System.IO;

namespace DynamicCSharpTests
{
    public class CompilerClass
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
        public void Constructs()
        {
            ICompiler c = Compiler.Create();
            c.AssemblyName = "Assem1";
            Assert.IsType<Compiler>(c);
        }

        [Fact]
        public void AddsAReferenceToAType()
        {
            ICompiler c = Compiler.Create();
            c.AssemblyName = "Assem2";
            c.AddReference(typeof(object));
            Assert.IsType<Compiler>(c);
        }

        [Fact]
        public void AddsAReferenceByTypeName()
        {
            ICompiler c = Compiler.Create();
            c.AssemblyName = "Assem3";
            c.AddReference("System.Int32");
            Assert.IsType<Compiler>(c);
        }

        [Fact]
        public void AddsAListOfReferencesByTypeName()
        {
            ICompiler c = Compiler.Create();
            c.AssemblyName = "Assem4";
            c.AddReferences(new string[] { "System.Int32", "System.Double", "System.IO.Path" });
            Assert.IsType<Compiler>(c);
        }

        [Fact]
        public void ThrowsExceptionOnAddingBadReference()
        {
            ICompiler c = Compiler.Create();
            c.AssemblyName = "Assem5";
            Assert.Throws<TypeLoadException>(() => c.AddReference("xyzzy"));
        }

        [Fact]
        public void GeneratesSyntaxTreeFromSource()
        {
            ICompiler c = Compiler.Create();
            c.AssemblyName = "Assem6";
            c.Source = source;
            c.Compile();
            Assert.NotNull(c.SyntaxTree);
            var root = c.SyntaxTree.GetCompilationUnitRoot();
            Assert.NotNull(root);
            Assert.Equal(SyntaxKind.CompilationUnit, root.Kind());
            Assert.Single(root.Members);
            Assert.Single(root.Usings);
            Assert.Equal("System", root.Usings.First().Name.ToString());
        }

        [Fact]
        public void GeneratesCompilation()
        {
            ICompiler c = Compiler.Create();
            c.AssemblyName = "Assem7";
            c.Source = source;
            c.Compile();
            var cp = c.Compilation;
            Assert.NotNull(cp);
            Assert.Equal("Assem7", cp.AssemblyName);
        }

        [Fact]
        public void GeneratesCompilationWithReferences()
        {
            ICompiler c = Compiler.Create();
            c.AssemblyName = "Assem8";
            c.AddReferences(new string[] { "System.Int32", "System.Double", "System.IO.Path" });
            c.Source = source;
            c.Compile();
            var cp = c.Compilation;
            Assert.NotNull(cp);
            Assert.Equal("Assem8", cp.AssemblyName);
            Assert.Equal(3, cp.References.Count());
        }

        [Fact]
        public void EmitsAssembly()
        {
            ICompiler c = Compiler.Create();
            c.AssemblyName = "Assem9";
            c.AddReferences(new string[] { "System.Int32", "System.Double", "System.IO.Path" });
            c.Source = source;
            c.Compile();
            Assert.NotNull(c.Assembly);
            Assert.Equal("Joe", c.Assembly.DefinedTypes.First().Name);
            Assert.Equal("Fred", c.Assembly.ExportedTypes.First().Namespace);
        }

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

        [Fact]
        public void CanUseAssemblyFromSourceFile()
        {
            ICompiler c = Compiler.Create();
            c.AssemblyName = "Assem12";
            c.AddReferences(new string[] { "System.Int32", "System.Double", "System.IO.Path" });
            string tmpFile = Path.Combine(Path.GetTempPath(), "DynamicCSharpTest.cs");
            using (var outFile = new StreamWriter(tmpFile, false))
                outFile.Write(source);
            c.Source = null;
            using (c.SourceStream = new FileStream(tmpFile, FileMode.Open, FileAccess.Read))
                c.Compile();
            File.Delete(tmpFile);
            Type type = c.Assembly.ExportedTypes.Where(t => t.Name == "Joe").FirstOrDefault();
            var joe = Activator.CreateInstance(type);
            Assert.IsType(type, joe);
            var getNextInt = (Func<int>)Delegate
                .CreateDelegate(typeof(Func<int>), joe, type.GetMethod("GetNextInt"));
            Assert.Equal(1, getNextInt());
            Assert.Equal(2, getNextInt());
        }

        private string badSource = @"
                using System;
                namespace Fred
                {
                    public class Joe
                    {
                        private int i = 0;
                        public int GetNextInt( // Syntax Error
                        {
                            return ++i;
                        }
                    }
                }";

        [Fact]
        public void CatchesErrors()
        {
            ICompiler c = Compiler.Create();
            c.AssemblyName = "Assem11";
            c.AddReferences(new string[] { "System.Int32", "System.Double", "System.IO.Path" });
            c.Source = badSource;
            c.Compile();
            Assert.True(c.HasErrors);
            Assert.Null(c.Assembly);
            Assert.Equal(2, c.Diagnostics.Count());
        }
    }
}
