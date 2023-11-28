namespace PlainOldSuperObjectTest;

using System;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using SuperNodes.Types;

public record CommonBaseType { }
public interface IGreeter { }

[SuperObject(typeof(Greeter))]
public partial record MyPlainOldRecordType : CommonBaseType { }

public partial class GrandparentType<T> {
  public partial class ParentType {
    [SuperObject(typeof(Greeter))]
    public partial record MyNestedPlainOldRecordType : CommonBaseType { }
  }
}

[PowerUp]
public record Greeter : CommonBaseType, IGreeter {
  public void Greet() => Console.WriteLine("Hello, world!");
}

public class PlainOldSuperObjectTest : TestClass {
  public PlainOldSuperObjectTest(Node testScene) : base(testScene) { }

  [Test]
  public void PlainOldObjectsWorkAsSuperObjects() {
    var obj = new MyPlainOldRecordType();

    obj.ShouldBeOfType<MyPlainOldRecordType>();
    obj.ShouldBeAssignableTo<IGreeter>();
  }

  [Test]
  public void NestedPlainOldObjectsWorkAsSuperObjects() {
    var obj =
      new GrandparentType<string>.ParentType.MyNestedPlainOldRecordType();

    obj.ShouldBeOfType<
      GrandparentType<string>.ParentType.MyNestedPlainOldRecordType
    >();
    obj.ShouldBeAssignableTo<IGreeter>();
  }
}
