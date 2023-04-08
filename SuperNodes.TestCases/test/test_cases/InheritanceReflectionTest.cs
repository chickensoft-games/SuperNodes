namespace SuperNodes.TestCases;

using System;
using System.Collections.Generic;
using Godot;
using GoDotTest;
using Shouldly;
using SuperNodes.Types;

public partial class MyInheritanceNode2D : Node2D {
  [Obsolete("this is going away")]
  public int MyObsoleteValue { get; }
}

[SuperNode]
public partial class MyInheritanceNode : MyInheritanceNode2D {
  public override partial void _Notification(int what);
}

public class MyInheritanceNodeTest : TestClass {
  public MyInheritanceNodeTest(Node testScene) : base(testScene) { }

  // Currently, SuperNodes does not support static generation for inherited
  // members. When that is implemented, this test can be updated accordingly.

  [Test]
  public void InheritedNodeDoesNotHaveSuperclassPropsAndFields()
    => Should.Throw<KeyNotFoundException>(
      () => MyInheritanceNode.ScriptPropertiesAndFields["MyObsoleteValue"]
    );
}
