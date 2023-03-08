namespace SuperNodes.TestCases;

using System.Collections.Generic;

/// <summary>
/// Apply this to nodes and PowerUps that should track the call order of
/// lifecycle hooks.
/// </summary>
public interface ITestCaseVerifiable {
  List<string> Called { get; }
}
