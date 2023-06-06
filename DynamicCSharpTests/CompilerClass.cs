using DynamicCSharp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SF = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace DynamicCSharpTests
{
    [TestClass]
    public class CompilerClass
    {
        private readonly string source = @"
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

        private static SyntaxTree CreateSyntaxTree()
        {
            var compilationUnit = SF
                .CompilationUnit()
                .AddUsings
                (
                    SF.UsingDirective
                        (SF.ParseName("System"))
                );

            var ns = SF
                .NamespaceDeclaration(SF.IdentifierName("SynTreeFred"));

            var cls = SF
                .ClassDeclaration("Henry")
                .AddModifiers(SF.Token(SyntaxKind.PublicKeyword));

            // How to define base class and interfaces, with/without generics
            //
            //cls = cls.AddBaseListTypes(
            //    SF.SimpleBaseType(SF.ParseTypeName("Object")),
            //    SF.SimpleBaseType(SF.ParseTypeName("IEnumerable<string>")));

            var inti = SF
                .VariableDeclaration(SF.ParseTypeName("int"))
                .AddVariables
                (
                    SF.VariableDeclarator
                    (
                        SF.Identifier("i"),
                        null,
                        SF.EqualsValueClause
                        (
                            SF.LiteralExpression
                            (
                                SyntaxKind.NumericLiteralExpression,
                                SF.Literal(12)
                            )
                        )
                    )
                );

            var field = SF.FieldDeclaration(inti)
                .AddModifiers(SF.Token(SyntaxKind.PrivateKeyword));

            var syntax = SF.ParseStatement("return ++i;");
            var methodDeclaration = SF
                .MethodDeclaration(SF.ParseTypeName("int"), "GetNextInt")
                .AddModifiers(SF.Token(SyntaxKind.PublicKeyword))
                .WithBody(SF.Block(syntax));

            cls = cls.AddMembers(field, methodDeclaration);
            ns = ns.AddMembers(cls);
            compilationUnit = compilationUnit.AddMembers(ns);
            return compilationUnit.SyntaxTree;
        }

        [TestMethod]
        public void Constructs()
        {
            ICompiler c = Compiler.Create();
            c.AssemblyName = "Assem1";
            Assert.IsInstanceOfType(c, typeof(Compiler));
        }

        [TestMethod]
        public void AddsAReferenceToAType()
        {
            ICompiler c = Compiler.Create();
            c.AssemblyName = "Assem2";
            c.AddReference(typeof(object));
            Assert.IsInstanceOfType(c, typeof(Compiler));
        }

        [TestMethod]
        public void AddsAReferenceByTypeName()
        {
            ICompiler c = Compiler.Create();
            c.AssemblyName = "Assem3";
            c.AddReference("System.IO.Path");
            Assert.IsInstanceOfType(c, typeof(Compiler));
        }

        [TestMethod]
        public void AddsAReferenceByAssemblyName()
        {
            ICompiler c = Compiler.Create();
            c.AssemblyName = "Assem3A";
            c.AddAssemblyReference("System.IO");
            Assert.IsInstanceOfType(c, typeof(Compiler));
        }

        [TestMethod]
        public void AddsALocalReferenceByAssemblyName()
        {
            ICompiler c = Compiler.Create();
            c.AssemblyName = "Assem3A";
            c.AddAssemblyReference("DynamicCSharp");
            Assert.IsInstanceOfType(c, typeof(Compiler));
        }

        [TestMethod]
        public void AddsAListOfReferencesByTypeName()
        {
            ICompiler c = Compiler.Create();
            c.AssemblyName = "Assem4";
            c.AddReferences(new string[] { "System.Int32", "System.Double", "System.IO.Path" });
            Assert.IsInstanceOfType(c, typeof(Compiler));
        }

        [TestMethod]
        public void AddsAListOfReferencesByAssemblyName()
        {
            ICompiler c = Compiler.Create();
            c.AssemblyName = "Assem4A";
            c.AddAssemblyReferences(new string[] { "System", "System.IO", "System.XML" });
            Assert.IsInstanceOfType(c, typeof(Compiler));
        }

        [TestMethod]
        public void ThrowsExceptionOnAddingBadReference()
        {
            ICompiler c = Compiler.Create();
            c.AssemblyName = "Assem5";
            Assert.ThrowsException<TypeLoadException>
                (() => c.AddReference("xyzzy"));
        }

        [TestMethod]
        public void GeneratesSyntaxTreeFromSource()
        {
            ICompiler c = Compiler.Create();
            c.AssemblyName = "Assem6";
            c.Source = source;
            c.Compile();
            Assert.IsNotNull(c.SyntaxTree);
            var root = c.SyntaxTree.GetCompilationUnitRoot();
            Assert.IsNotNull(root);
            Assert.AreEqual(SyntaxKind.CompilationUnit, root.Kind());
            Assert.AreEqual(1, root.Members.Count);
            Assert.AreEqual(1, root.Usings.Count);
            Assert.AreEqual("System", root.Usings.First().Name.ToString());
        }

        [TestMethod]
        public void GeneratesCompilation()
        {
            ICompiler c = Compiler.Create();
            c.AssemblyName = "Assem7";
            c.Source = source;
            c.Compile();
            var cp = c.Compilation;
            Assert.IsNotNull(cp);
            Assert.AreEqual("Assem7", cp.AssemblyName);
        }

        [TestMethod]
        public void GeneratesCompilationWithReferences()
        {
            ICompiler c = Compiler.Create();
            c.AssemblyName = "Assem8";
            c.AddReferences(new string[] { "System.Int32", "System.Double", "System.IO.Path" });
            c.Source = source;
            c.Compile();
            var cp = c.Compilation;
            Assert.IsNotNull(cp);
            Assert.AreEqual("Assem8", cp.AssemblyName);
            Assert.AreEqual(3, cp.References.Count());
        }

        [TestMethod]
        public void EmitsAssembly()
        {
            ICompiler c = Compiler.Create();
            c.AssemblyName = "Assem9";
            c.AddReferences(new string[] { "System.Int32", "System.Double", "System.IO.Path" });
            c.Source = source;
            c.Compile();
            Assert.IsNotNull(c.Assembly);
            Assert.IsTrue(c.Assembly.DefinedTypes
                .Select(t => t.Name).Contains("Joe"));
            Assert.IsTrue(c.Assembly.ExportedTypes
                .Select(t => t.Namespace).Contains("Fred"));
        }

        [TestMethod]
        public void CanInvokeEmittedMethods()
        {
            ICompiler c = Compiler.Create();
            c.AssemblyName = "Assem10";
            c.AddReferences(new string[] { "System.Int32", "System.Double", "System.IO.Path" });
            c.Source = source;
            c.Compile();
            Type type = c.Assembly.ExportedTypes.Where(t => t.Name == "Joe").FirstOrDefault();
            var joe = Activator.CreateInstance(type);
            Assert.IsInstanceOfType(joe, type);
            var getNextInt = (Func<int>)Delegate
                .CreateDelegate(typeof(Func<int>), joe, type.GetMethod("GetNextInt"));
            Assert.AreEqual(1, getNextInt());
            Assert.AreEqual(2, getNextInt());
        }

        [TestMethod]
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
            Assert.IsInstanceOfType(joe, type);
            var getNextInt = (Func<int>)Delegate
                .CreateDelegate(typeof(Func<int>), joe, type.GetMethod("GetNextInt"));
            Assert.AreEqual(1, getNextInt());
            Assert.AreEqual(2, getNextInt());
        }

        [TestMethod]
        public void CanUseAssemblyFromSyntaxTree()
        {
            ICompiler c = Compiler.Create();
            c.AssemblyName = "Assem13";
            c.AddReferences(new string[] { "System.Int32", "System.Double", "System.IO.Path" });
            c.SyntaxTree = CreateSyntaxTree();
            c.Compile();
            Type type = c.Assembly.ExportedTypes.Where(t => t.Name == "Henry").FirstOrDefault();
            var joe = Activator.CreateInstance(type);
            Assert.IsInstanceOfType(joe, type);
            var getNextInt = (Func<int>)Delegate
                .CreateDelegate(typeof(Func<int>), joe, type.GetMethod("GetNextInt"));
            Assert.AreEqual(13, getNextInt());
            Assert.AreEqual(14, getNextInt());
        }

        private readonly string badSource =
                @"using System;
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

        [TestMethod]
        public void CatchesErrors()
        {
            ICompiler c = Compiler.Create();
            c.AssemblyName = "Assem11";
            c.AddReferences(new string[] { "System.Int32", "System.Double", "System.IO.Path" });
            c.Source = badSource;
            c.Compile();
            Assert.IsTrue(c.HasErrors);
            Assert.IsNull(c.Assembly);
            Assert.AreEqual(2, c.Diagnostics.Count());
        }

        [TestMethod]
        public void ErrorsAreMeaningFul()
        {
            ICompiler c = Compiler.Create();
            c.AssemblyName = "Assem11";
            c.AddReferences(new string[] { "System.Int32", "System.Double", "System.IO.Path" });
            c.Source = badSource;
            c.Compile();
            Assert.IsTrue(c.HasErrors);
            Assert.IsNull(c.Assembly);
            Assert.AreEqual(1, c.Errors.Count());
            Assert.AreEqual("     7(46): ) expected", c.Errors.First());
        }
    }
}