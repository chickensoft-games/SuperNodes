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

public static class TestUtils {
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
}
