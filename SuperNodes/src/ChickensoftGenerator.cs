namespace SuperNodes;

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

public abstract class ChickensoftGenerator {
  /// <summary>Spaces per tab. Adjust to your generator's liking.</summary>
  public static int SPACES_PER_TAB = 2;

  /// <summary>
  /// Produces whitespace for the required number of tabs.
  /// </summary>
  /// <param name="numTabs">Indentation level.</param>
  /// <returns><paramref name="numTabs" /> * <see cref="SPACES_PER_TAB"/>
  /// spaces in a string.</returns>
  public static string Tab(int numTabs)
  => new(' ', numTabs * SPACES_PER_TAB);

  /// <summary>Indents the given text by the given number of tabs.</summary>
  /// <param name="numTabs">Indentation level.</param>
  /// <param name="text">Text to indent.</param>
  /// <returns>Indented text.</returns>
  public static string Tab(int numTabs, string text) => Tab(numTabs) + text;

  /// <summary>Joins each line with a newline character.</summary>
  /// <param name="lines">Lines to join.</param>
  /// <returns>Combined text.</returns>
  public static string Lines(IEnumerable<string> lines)
    => string.Join(
      "\n",
      lines
    );

  /// <summary>
  /// Joins each line of text, indenting each line after the first line by
  /// the given number of <paramref name="tabs" />.
  /// </summary>
  /// <param name="tabs">Indentation level.</param>
  /// <param name="lines">Lines to join and indent.</param>
  /// <returns>Combined text.</returns>
  public static string Lines(int tabs, IEnumerable<string> lines)
  => string.Join(
    "\n",
    lines.Take(1).Concat(lines.Skip(1).Select(line => Tab(tabs) + line))
  );

  /// <summary>
  /// Normalizes the whitespace for the given code string. Does not run a full
  /// formatting operation (i.e., this only fixes indentation and spacing).
  /// </summary>
  /// <param name="code">Code to format.</param>
  /// <returns>Formatted code.</returns>
  public static string Format(string code) {
    var tree = CSharpSyntaxTree.ParseText(code);
    var root = tree.GetRoot();
    return root
      .NormalizeWhitespace(indentation: Tab(1))
      .ToFullString()
      .ReplaceLineEndings();
  }

  /// <summary>
  /// Returns the given <paramref name="code" /> string if
  /// <paramref name="condition" /> is true, otherwise returns
  /// <paramref name="fallback" />, which is the empty string by default.
  /// </summary>
  /// <param name="condition">Condition to check.</param>
  /// <param name="code">Code to return if condition is true.</param>
  /// <param name="fallback">Code to return if condition is false. Default is
  /// the empty string.</param>
  /// <returns><paramref name="code" /> if <paramref name="condition" /> is
  /// true, <paramref name="fallback" /> otherwise.</returns>
  public static string If(
    bool condition, string code, string fallback = ""
  ) => condition ? code : fallback;
}
