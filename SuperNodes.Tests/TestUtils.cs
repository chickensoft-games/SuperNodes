namespace SuperNodes.Tests;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

public readonly record struct GeneratorOutput(
  IList<string> Outputs,
  IList<Diagnostic> Diagnostics
);

public static class TestUtils {
  public static SemanticModel GetSemanticModel(string code) {
    var tree = CSharpSyntaxTree.ParseText(code);
    var root = tree.GetRoot();

    var model = CSharpCompilation
      .Create("AssemblyName")
      .AddSyntaxTrees(tree)
      .GetSemanticModel(tree);

    return model;
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

    var outputs = outputCompilation
      .SyntaxTrees
      .Select(tree => tree.ToString())
      .Where(tree => tree is not null)
      .Cast<string>()
      .ToImmutableArray();

    return new GeneratorOutput(Outputs: outputs, Diagnostics: diagnostics);
  }
}
