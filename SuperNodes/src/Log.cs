namespace SuperNodes;

using System.Collections.Generic;


// Simple, but effective.
// Inspired by https://dev.to/panoukos41/debugging-c-source-generators-1flm.
public class Log {
  protected List<string> _logs { get; } = new();

  public void Print(string msg) {
#if DEBUG
    _logs.Add("//\t" + msg);
#endif
    return;
  }

  public void Clear() => _logs.Clear();
  public string Contents => string.Join("\n", _logs);
}
