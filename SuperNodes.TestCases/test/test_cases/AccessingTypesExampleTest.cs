namespace AccessingTypesExample;

using System;
using Godot;
using GoDotTest;
using SuperNodes.Types;

[SuperNode]
public partial class MySuperNode : Node {
  /// <summary>This property will be serialized!</summary>
  public string MyName { get; } = nameof(MySuperNode);

  public override partial void _Notification(int what);

  private readonly ISerializer _serializer = new MySerializer();

  public void OnReady() {
    foreach (var memberName in PropertiesAndFields.Keys) {
      var member = PropertiesAndFields[memberName];

      if (!member.IsReadable || member.IsField) { continue; }

      var value = GetScriptPropertyOrField(memberName);
      var serializerHelper = new MySerializerHelper(_serializer, value);
      var result = GetScriptPropertyOrFieldType(memberName, serializerHelper);
      if (!result) {
        throw new InvalidOperationException(
          $"Failed to serialize {memberName}."
        );
      }
    }
  }
}

public class MySerializerHelper : ITypeReceiver<bool> {
  public ISerializer Serializer { get; }
  public dynamic Value { get; }

  public MySerializerHelper(ISerializer serializer, dynamic value) {
    Serializer = serializer;
    Value = value;
  }

  public bool Receive<TSerialize>()
    => Serializer.Serialize<TSerialize>(Value);
}

// [PowerUp]
// public abstract partial class MyPowerUp : Node {
//   private readonly ISerializer _serializer = new MySerializer();

//   #region StaticReflectionStubs

//   [PowerUpIgnore]
//   public abstract ImmutableDictionary<string, ScriptPropertyOrField>
//     PropertiesAndFields { get; }

//   [PowerUpIgnore]
//   public abstract dynamic GetScriptPropertyOrField(string name);

//   [PowerUpIgnore]
//   public abstract void SetScriptPropertyOrField(string name, dynamic value);

//   [PowerUpIgnore]
//   public abstract TResult GetScriptPropertyOrFieldType<TResult>(
//     string scriptProperty, ITypeReceiver<TResult> receiver
//   );

//   #endregion StaticReflectionStubs

//   public void OnMyPowerUp(int what) {
//     if (what == NotificationReady) {
//       foreach (var memberName in PropertiesAndFields.Keys) {
//         var member = PropertiesAndFields[memberName];

//         if (!member.IsReadable || member.IsField) { continue; }

//         var value = GetScriptPropertyOrField(memberName);
//         var serializerHelper = new MySerializerHelper(_serializer, value);
//         var result = GetScriptPropertyOrFieldType(memberName, serializerHelper);
//         if (!result) {
//           throw new InvalidOperationException(
//             $"Failed to serialize {memberName}."
//           );
//         }
//       }
//     }
//   }
// }

public interface ISerializer {
  bool Serialize<T>(T value);
  T Deserialize<T>(dynamic value);
}

public class MySerializer : ISerializer {
  public bool Serialize<T>(T value) => true; // Dummy implementation.
  public T Deserialize<T>(dynamic value) => default!; // Dummy implementation.
}

public class AdvancedGenericPowerUpExampleTest : TestClass {
  public AdvancedGenericPowerUpExampleTest(Node testScene) : base(testScene) { }

  [Test]
  public void Runs() {
    var mySuperNode = new MySuperNode();
    mySuperNode._Notification((int)Node.NotificationReady);
  }
}
