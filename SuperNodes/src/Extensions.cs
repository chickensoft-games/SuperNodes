namespace SuperNodes;

using System;
using System.Text;

public static class Extensions {
  // The ReplaceLineEndings function is ported from modern .NET code since it
  // doesn't exist in netstandard2.0.

  public static char[] NewLineChars =
        new[] { '\r', '\n', '\f', '\u0085', '\u2028', '\u2029' };

  internal const int STACK_ALLOC_CHAR_BUFFER_SIZE_LIMIT = 256;

  public static string ReplaceLineEndings(this string str)
    => ReplaceLineEndings(str, Environment.NewLine);

  internal static string ReplaceLineEndings(
    string str, string replacementText
  ) {
    var idxOfFirstNewlineChar = IndexOfNewlineChar(
      str.AsSpan(), out var stride
    );
    if (idxOfFirstNewlineChar < 0) {
      return str;
    }
    var firstSegment = str.AsSpan(0, idxOfFirstNewlineChar);
    var remaining = str.AsSpan(idxOfFirstNewlineChar + stride);
    var builder = new StringBuilder(STACK_ALLOC_CHAR_BUFFER_SIZE_LIMIT);
    while (true) {
      var idx = IndexOfNewlineChar(remaining, out stride);
      if (idx < 0) { break; } // no more newline chars
      builder.Append(replacementText)
        .Append(new string(remaining.Slice(0, idx).ToArray()));
      remaining = remaining.Slice(idx + stride);
    }
    return string.Concat(
      new string(firstSegment.ToArray()),
      builder.ToString(),
      replacementText,
      new string(remaining.ToArray())
    );
  }

  internal static int IndexOfNewlineChar(
    ReadOnlySpan<char> text, out int stride
  ) {
    stride = default;
    var idx = text.IndexOfAny(NewLineChars);
    if ((uint)idx < (uint)text.Length) {
      stride = 1;
      if (text[idx] == '\r') {
        var nextCharIdx = idx + 1;
        if (
          (uint)nextCharIdx < (uint)text.Length && text[nextCharIdx] == '\n'
        ) {
          stride = 2;
        }
      }
    }

    return idx;
  }
}
