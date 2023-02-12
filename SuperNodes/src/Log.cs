namespace SuperNodes;

using System.Collections.Generic;

/// <summary>
/// Simple, but effective.
/// Inspired by https://dev.to/panoukos41/debugging-c-source-generators-1flm.
/// </summary>
public class Log {
  protected List<string> Logs { get; } = new();

  public void Print(string msg) {
#if DEBUG
    Logs.Add("//\t" + msg);
#endif
#pragma warning disable RCS1134
    return;
#pragma warning restore RCS1134
  }

  public void Clear() => Logs.Clear();
  public string Contents => string.Join("\n", Logs);
}
