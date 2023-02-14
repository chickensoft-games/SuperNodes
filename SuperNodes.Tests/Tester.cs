namespace SuperNodes.Tests;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

public readonly record struct GeneratorOutput(
  IDictionary<string, string> Outputs,
  IList<Diagnostic> Diagnostics
);

public static class Tester {
  public static SemanticModel GetSemanticModel(string code) {
    var tree = CSharpSyntaxTree.ParseText(code);
    var root = tree.GetRoot();

    return CSharpCompilation
      .Create("AssemblyName")
      .AddSyntaxTrees(tree)
      .GetSemanticModel(tree);
  }

  public static GeneratorOutput Generate(string sourceCode)
    => Generate(new string[] { sourceCode });

  public static GeneratorOutput Generate(IEnumerable<string> sources) {
    var syntaxTrees = sources.Select(
      source => CSharpSyntaxTree.ParseText(source)
    );

    var references = AppDomain.CurrentDomain.GetAssemblies()
      .Where(assembly => !assembly.IsDynamic)
      .Select(assembly => MetadataReference.CreateFromFile(assembly.Location))
      .Cast<MetadataReference>();

    var compilation = CSharpCompilation.Create(
      assemblyName: "SourceGeneratorTests",
      syntaxTrees: syntaxTrees,
      references: references,
      options: new CSharpCompilationOptions(
        OutputKind.DynamicallyLinkedLibrary
      )
    );

    var generator = new SuperNodesGenerator();

    CSharpGeneratorDriver.Create(generator)
      .RunGeneratorsAndUpdateCompilation(
        compilation,
        out var outputCompilation,
        out var diagnostics
      );

    var outputs = new Dictionary<string, string>();
    foreach (var output in outputCompilation.SyntaxTrees) {
      var text = output.ToString();
      if (text is not null && !sources.Contains(text)) {
        outputs.Add(output.FilePath, text);
      }
    }

    return new GeneratorOutput(
      Outputs: outputs.ToImmutableDictionary(), Diagnostics: diagnostics
    );
  }

  /// <summary>
  /// Parses the given code and returns the first node of the given type within
  /// the syntax tree.
  /// </summary>
  /// <param name="code">Source code string.</param>
  /// <typeparam name="T">Type of the node to find in the tree.</typeparam>
  /// <returns>First matching node within the tree of type
  /// <typeparamref name="T" />.</returns>
  public static T Parse<T>(string code) where T : SyntaxNode
    => (T)CSharpSyntaxTree
      .ParseText(code)
      .GetRoot()
      .DescendantNodes()
      .First(node => node is T);

  /// <summary>
  /// Parses the given code and returns the first node of the given type within
  /// the syntax tree and the semantic model for the code.
  /// </summary>
  /// <param name="code">Source code string.</param>
  /// <param name="symbol">Symbol for the node found in the tree.</param>
  /// <typeparam name="TNode">Type of the node to find in the tree.</typeparam>
  /// <typeparam name="TSymbol">Type of symbol to find.</typeparam>
  /// <returns>First matching node within the tree of type
  /// <typeparamref name="TNode" />.</returns>
  public static TNode Parse<TNode, TSymbol>(string code, out TSymbol symbol)
    where TNode : SyntaxNode
    where TSymbol : ISymbol {
    var tree = CSharpSyntaxTree.ParseText(code);
    var node = (TNode)tree
      .GetRoot()
      .DescendantNodes()
      .First(node => node is TNode);

    symbol = (TSymbol)CSharpCompilation
      .Create("AssemblyName")
      .AddReferences(
        MetadataReference.CreateFromFile(
          typeof(object).Assembly.Location
        )
      )
      .AddSyntaxTrees(tree)
      .GetSemanticModel(tree)
      .GetDeclaredSymbol(node)!;

    return node;
  }
}
