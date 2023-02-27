namespace SuperNodes.TestCases;

using System.Collections.Generic;
using System.Collections.Immutable;
using Godot;
using GoDotTest;
using Shouldly;

// SuperNode that applies a generic PowerUp.
[SuperNode(typeof(GenericPowerUp<string, bool>))]
public partial class NodeWithGenericPowerUp : Node {
  public override partial void _Notification(int what);
}

public interface IGenericPowerUp<TA, TB> {
  TA A { get; set; }
  TB B { get; set; }
}

public interface IOtherInterface<TA> {
  TA A { get; set; }
}

[PowerUp]
public partial class GenericPowerUp<TA, TB>
  : Node, IGenericPowerUp<TA, TB>, IOtherInterface<TA>, ITestCaseVerifiable {
  public List<string> Called { get; } = new();

  public TA A { get; set; } = default!;
  public TB B { get; set; } = default!;

  TA IOtherInterface<TA>.A { get; set; } = default!;

  internal readonly struct TypeReceiver<TC> : ITypeReceiver<bool> {
    public TC Value { get; }

    public TypeReceiver(TC value) {
      Value = value;
    }

    public bool Receive<T>() => Value is TA;
  }

  public void OnGenericPowerUp(int what) {
    Called.Add(nameof(GenericPowerUp<TA, TB>));

    // Receive the generic type parameter of the property A in our custom
    // type receiver. We can't create generic lambda expressions, so we have
    // to use an ITypeReceiver object (supplied by SuperNodes).
    var typeReceiver = new TypeReceiver<TA>(A);

    // Make sure the type of A is TA — without reflection!
    var isAanA
      = GetScriptPropertyOrFieldType(
        nameof(IGenericPowerUp<TA, TB>.A), typeReceiver
      );

    isAanA.ShouldBeTrue();
  }

  // Stubs for the generated static reflection tables generated on SuperNodes
#pragma warning disable RCS1158
  internal static ImmutableDictionary<string, ScriptPropertyOrField>
    PropertiesAndFields { get; } = default!;

  internal static TResult GetScriptPropertyOrFieldType<TResult>(
    string scriptProperty, ITypeReceiver<TResult> receiver
  ) => default!;
#pragma warning restore RCS1158

}

public class GenericPowerUpTest : TestClass {
  public GenericPowerUpTest(Node testScene) : base(testScene) { }

  [Test]
  public void GenericPowerUpWorks() {
    var node = new NodeWithGenericPowerUp();
    var generic = (IGenericPowerUp<string, bool>)node;
    var other = (IOtherInterface<string>)node;
    generic.A = "Hello";
    generic.B = true;
    other.A = "World!";
    generic.A.ShouldBe("Hello");
    other.A.ShouldBe("World!");
    node._Notification((int)Node.NotificationReady);
    node.Called.ShouldBe(new[] { nameof(NodeWithGenericPowerUp) });
  }
}
