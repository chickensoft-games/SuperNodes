namespace SuperNodes.TestCases;

using System.Collections.Generic;
using System.Collections.Immutable;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using SuperNodes.Types;

public class MyModel { }

// SuperNode that applies a generic PowerUp.
[SuperNode(typeof(GenericPowerUp<string, MyModel>))]
public partial class GenericPowerUpNode : Node {
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
public abstract partial class GenericPowerUp<TA, TB>
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

    // Deduce the string name of a property with generics since SuperNodes
    // replaces the generic
    // parameters of a PowerUp with the generic arguments given to it from a
    // SuperNode at build-time. Additionally, typeof also works at
    // compile-time, allowing us to avoid having to use reflection at runtime.
    //
    // Whew!
    // var genericPropertyName = nameof(IGenericPowerUp<TA, TB>) +
    //   "<" + this.TypeParam(typeof(TA)) + ", " + this.TypeParam(typeof(TB)) +
    //   ">." + nameof(IGenericPowerUp<TA, TB>.A);

    var genericPropertyName = nameof(IOtherInterface<TA>.A);
    var isAanA
      = GetScriptPropertyOrFieldType(genericPropertyName, typeReceiver);

    isAanA.ShouldBeTrue();
  }

#pragma warning disable RCS1158, CA1000

  // Stubs for the generated static reflection tables generated on SuperNodes.
  //
  // We know that SuperNodes will generate these, so we mark them with
  // [PowerUpIgnore] to prevent them from being copied over to the SuperNode
  // we're applied to. If we didn't, we'd have a compile-time error.

  [PowerUpIgnore]
  public static ImmutableDictionary<string, ScriptPropertyOrField>
    PropertiesAndFields { get; } = default!;

  [PowerUpIgnore]
  public TResult GetScriptPropertyOrFieldType<TResult>(
    string scriptProperty, ITypeReceiver<TResult> receiver
  ) => default!;

#pragma warning restore CA1000, RCS1158

}

public class GenericPowerUpTest : TestClass {
  public GenericPowerUpTest(Node testScene) : base(testScene) { }

  [Test]
  public void GenericPowerUpWorks() {
    var node = new GenericPowerUpNode();
    var generic = (IGenericPowerUp<string, MyModel>)node;
    var other = (IOtherInterface<string>)node;
    generic.A = "Hello";
    generic.B = new MyModel();
    other.A = "World!";
    generic.A.ShouldBe("Hello");
    other.A.ShouldBe("World!");
    node._Notification((int)Node.NotificationReady);
    node.Called.ShouldBe(new[] { nameof(GenericPowerUp<string, MyModel>) });
  }
}
