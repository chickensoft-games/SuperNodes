# SuperNodes

[![Chickensoft Badge][chickensoft-badge]][chickensoft-website] [![Discord][discord-badge]][discord] ![line coverage][line-coverage] ![branch coverage][branch-coverage]

---

<p align="center">
<img alt="SuperNodes Logo" src="SuperNodes/icon.png" width="200">
</p>

*Supercharge your Godot nodes with lifecycle-aware mixins, third party source generators, script introspection, and dynamic property manipulation — all without runtime reflection!*

---

SuperNodes is a source generator that helps give superpowers to C# scripts on Godot nodes.

- ✅ Apply PowerUps (i.e, mixin other classes) to your scripts that add instance data — you are no longer limited to only adding methods via default interface implementations!
- ✅ Call lifecycle-aware generated methods from other compatible third-party source generators. Because of the way Godot's official source generators work, it's hard to use multiple source generators that want to observe a node's lifecycle events. SuperNodes solves this problem by providing a mechanism for third-party source generators to hook into the node's lifecycle events.
- ✅ Minimal boilerplate — just two additional lines of code.
- ✅ Inspect script properties and fields (including their attributes) during execution using generated tables — no runtime reflection required!
- ✅ Get and set the value of script properties and fields via their string name at runtime.
- ✅ Receive the type of a script property or field at runtime as a reified type (i.e., as a type parameter).
- ✅ No runtime reflection — PowerUps and static reflection tables are generated at compile-time.
- ✅ Compatible with source-only nuget packages. The included `SharedPowerUps` example describes how to create and consume a source-code only package. Source-only packages are added directly as source code into the project that references them, allowing the source generators in that project to read the included source code as if it had been written in the project itself. This is useful for creating PowerUps that are shared across multiple projects.
- ✅ Well tested. SuperNodes has 100% line and branch coverage from unit tests, as well as a suite of test cases for fixing bugs, regressions, and integration testing in real-world scenarios. If you find an issue, let us know and we'll get it fixed. It's very important to us to provide good support and ensure SuperNodes is both stable and reliable.

## 📦 Installation

Simply add SuperNodes as an analyzer dependency to your C# project.

```xml
<ItemGroup>
  <!-- Include SuperNodes as a Source Generator -->
  <PackageReference Include="Chickensoft.SuperNodes" Version="{LATEST_VERSION}" PrivateAssets="all" OutputItemType="analyzer" />
</ItemGroup>
```

## 🔮 Superpowers for C# Scripts

Background: many programming languages allow you to combine the contents of one class with another class using features such as [`mixins`][mixins], [`traits`][traits] or even [`templates`][templates] and [`macros`][macros].

C# has a similar (but not as powerful) feature called [default interface implementations][default-interfaces], but it doesn't let you add instance data to a class. More on that in [this section](#🧐-okay-but-how).

To make up for these shortcomings in C#, the SuperNodes generator allows you to create `PowerUps`. PowerUps can add any kind of additional instance data (fields, properties, events, static members, etc) to a C# Godot script, bypassing the limitations of default interface implementations.

If that sounds fun, keep reading!

## 🔋 PowerUps

A *PowerUp* is any class with a `[PowerUp]` attribute whose contents can be copied into any `SuperNode` that it might be applied to. Likewise, a *SuperNode* is just a Godot script class with a `[SuperNode]` attribute on it.

Sometimes programming language patterns are more easily illustrated with code. Below is a simple SuperNode script that applies a *PowerUp* that performs actions in response to lifecycle events. While it's not exactly very useful by itself, but it demonstrates some of the basic concepts which can be built upon to create more complex behaviors.

Making a SuperNode is simple: just add the `[SuperNode]` attribute to your script class and a partial method signature for the `_Notification` method. At build time (or whenever the source generators run), the SuperNodes generator will generate a partial implementation of your script class that contains the contents of any PowerUps that have been applied to it, as well as an implementation of the `_Notification` method to hook into your node script's lifecycle events.

To create a PowerUp, simply declare another class and give it the `[PowerUp]` attribute.

```csharp
// SimpleExampleNode.cs — your node's script file.

using Godot;

[SuperNode(typeof(SimplePowerUp))]
public partial class SimpleExampleNode : Node {
  public override partial void _Notification(int what);

  public void OnReady() => SomeMethod();

  public void OnProcess(double delta) => SomeMethod();

  public void SomeMethod() {
    if (LastNotification == NotificationReady) {
      GD.Print("We are getting ready.");
    }
    else if (LastNotification == NotificationProcess) {
      GD.Print("We are processing a frame.");
    }
  }
}

// A PowerUp that logs some of the main lifecycle events of a node.

[PowerUp]
public partial class SimplePowerUp : Node {
  public long LastNotification { get; private set; }

  public void OnSimplePowerUp(int what) {
    switch ((long)what) {
      case NotificationReady:
        GD.Print("PowerUp is ready!");
        break;
      case NotificationEnterTree:
        GD.Print("I'm in the tree!");
        break;
      case NotificationExitTree:
        GD.Print("I'm out of the tree!");
        break;
      default:
        break;
    }
    LastNotification = what;
  }
}
```

The `[SuperNode]` and `[PowerUp]` attributes are automatically injected into your codebase when the SuperNodes generator builds your project — typically while you're editing it in an IDE or whenever you run `dotnet build`.

For the `SimpleExampleNode` above, SuperNodes will generate a special implementation file at compile-time named `SimpleExampleNode_SimplePowerUp.g.cs` that looks something like the snippet below. Note how the contents of the PowerUp have been copied over into a partial implementation of the script the PowerUp was applied to.

```csharp
// SimpleExampleNode_SimplePowerUp.g.cs — generated PowerUp implementation.
#nullable enable
using Godot;

partial class SimpleExampleNode
{
  public long LastNotification { get; private set; }

  public void OnSimplePowerUp(int what)
  {
    switch ((long)what)
    {
      case NotificationReady:
        GD.Print("PowerUp is ready!");
        break;
      case NotificationEnterTree:
        GD.Print("I'm in the tree!");
        break;
      case NotificationExitTree:
        GD.Print("I'm out of the tree!");
        break;
      default:
        break;
    }

    LastNotification = what;
  }
}
#nullable disable
```

In addition, SuperNodes will also generate a `SimpleExampleNode.g.cs` file that contains an implementation of the partial `_Notification` method available on all Godot node scripts. By implementing this method, SuperNodes has the ability to monitor all lifecycle events the node receives, as well as call the PowerUp's `OnSimplePowerUp` method when the node receives a lifecycle event.

```csharp
// SimpleExampleNode.g.cs — generated script lifecycle implementation.
#nullable enable
using Godot;

partial class SimpleExampleNode {
  public override partial void _Notification(int what) {
    // Invoke declared lifecycle method handlers.
    OnSimplePowerUp(what);

    // Invoke any notification handlers declared in the script.
    switch ((long)what) {
      case NotificationReady:
        OnReady();
        break;
      case NotificationProcess:
        OnProcess(GetProcessDeltaTime());
        break;
      default:
        break;
    }
  }
}
#nullable disable
```

*By adding just two lines of boilerplate code to our node script* (the `[SuperNode]` attribute and the partial `_Notification` method declaration), **we have effectively mixed-in another class to our script** that has **full access to our node's lifecycle events**!

SuperNodes can do a *lot* more than just that, however. Let's keep going!

## 🔄 Lifecycle Handlers

SuperNodes will always generate an implementation for the Godot `_Notification` method that your script declares, allowing it to observe the node's lifecycle events, such as `Ready`, `Process`, `EnterTree`, etc. You can still override the Godot version of those methods, such as `_Process`, `_EnterTree`, etc, but you can't implement `_Notification` yourself. Instead, SuperNodes will call any method named `OnNotification` in your script from `_Notification` that also receives the notification type as an argument.

Instead of implementing lifecycle methods like `_Ready` or `_Process` yourself, you can let SuperNodes call any method you've defined that matches a Godot node or object notification and begins with the word `On`, such as `OnReady`, `OnProcess`, `OnWmMouseEnter`, `OnSceneInstantiated` etc. This allows you to easily and consistently define method signatures in C# idiomatically, if that's important to you. For the full list of lifecycle handlers, [see below](#🛟-full-list-of-lifecycle-handlers).

> To view generated code in your project that's using source generators, include the following in your `.csproj` file:
>
> ```xml
> <PropertyGroup>
>   <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
>   <CompilerGeneratedFilesOutputPath>.generated</CompilerGeneratedFilesOutputPath>
> </PropertyGroup>
> ```

## 🎰 Source Generators with Godot

Because of the way [Godot's own source generators are designed][godot-generator-problems], it's not easily possible to use multiple third-party source generators which want to observe a node's lifecycle events in harmony.

SuperNodes makes it possible to use other source generators alongside it by letting you declare lifecycle methods that should be called from the implemented SuperNode's `_Notification` method.

Essentially, SuperNodes makes it possible for other unofficial Godot source generators to [play nicely with each other and the official Godot source generators][godot-generator-problems].

Suppose you want to use a source generator which prints out a message when your node is ready. We'll call this hypothetical source generator `PrintOnReady`.

To tell SuperNodes about your generator, add `PrintOnReady` to the `[SuperNode]` attribute:

```csharp
using Godot;

[SuperNode("PrintOnReady")]
public partial class MySuperNode : Node {
  public override partial void _Notification(int what);

  public void OnReady()
    => GD.Print("PrintOnReady will have already printed out that I'm ready.");
}
```

If the `PrintOnReady` source generator wants to be compatible with SuperNodes, all it has to do is generate a partial implementation of your node's script class that contains a method named `PrintOnReady`. The `PrintOnReady` method will be called whenever `_Notification` is from the SuperNode's generated implementation.

```csharp
// Hypothetical generated output of our imaginary PrintOnReady generator.
public partial class MySuperNode {
  public void PrintOnReady(int what) {
    if (what == NotificationReady) {
      GD.Print($"{Name} is ready.");
    }
  }
}
```

> If you're looking to make your source generator compatible with SuperNodes, simply name your generated notification lifecycle method the same name as your source generator so that it's easy for users to add it to their nodes with the `[SuperNode]` attribute.
>
> If all of us source generator authors follow that convention, we can have a really good time — and nobody's source generators will conflict with anyone else's!

SuperNodes will itself generate another partial implementation which will call the given `PrintOnReady` method, as well as the declared lifecycle handlers in the script itself:

```csharp
#nullable enable
using Godot;

partial class MySuperNode {
  public override partial void _Notification(int what) {
    // Invoke declared lifecycle method handlers.
    PrintOnReady(what);

    // Invoke any notification handlers declared in the script.
    switch ((long)what) {
      case NotificationReady:
        OnReady();
        break;
      default:
        break;
    }
  }
}
```

### Multiple Source Generators

SuperNodes can invoke generated implementations for multiple source generators. Simply put the names of each method that should be called in the `[SuperNode]` attribute:

```csharp
[SuperNode("GeneratorOne", "GeneratorTwo")]
public partial class MySuperNode : Node { /* ... */ }
```

You can also mix-and-match PowerUps and source generator method names. The order of invocations will be preserved.

```csharp
[SuperNode("GeneratedMethod1", typeof(MyPowerUp), "GeneratorMethod2")]
public partial class MySuperNode : Node { /* ... */ }
```

The generated implementation will respect the order of invocations specified in the `[SuperNode]` attribute:

```csharp
partial class MySuperNode {
  public override partial void _Notification(int what) {
    // Invoke declared lifecycle method handlers.
    GeneratedMethod1(what);
    OnMyPowerUp(what);
    GeneratorMethod2(what);

    // Invoke any notification handlers declared in the script.
    switch ((long)what) {
      // ...
    }
  }
}
```

## 🧐 Okay, but how?

The astute reader may have noticed that we added a property in our first example to our `SimpleExampleNode` named `LastNotification`, despite the fact that adding properties to existing classes is not supported by C#.

How is this possible? Since SuperNodes is a source generator, it can examine both the SuperNode and the PowerUp at compile-time, create a copy of the PowerUp, edit it, and turn it into a specific partial implementation of the SuperNode, effectively simulating a [mixins] (for the most part). You might also know similar patterns from other languages, such as [traits], macros, static metaprogramming, or templates.

> Wait: what about C#'s support for *default interface implementations*? Unfortunately, default interface implementations cannot be used to add instance data to a class. That is, you cannot add fields (and by extent auto properties) to an existing class using default interface implementations. Essentially, default interface implementations only allow you to implement missing methods, not add state.
>
> > Interfaces may not contain instance state. While static fields are now permitted, instance fields are not permitted in interfaces. Instance auto-properties are not supported in interfaces, as they would implicitly declare a hidden field. — Love, Microsoft
>
> If you'd like to know more about the limitations of default interface implementations, check out [this article][default-interfaces-limitations].

## 💎 Mixins and Caveats

If you apply two PowerUps to a node that both declare the same member, you will get a compile time error.

```csharp
[SuperNode(typeof(SimplePowerUp), typeof(ConflictingSimplePowerUp))]
public partial class SimpleExampleNode : Node {
  public override partial void _Notification(int what);
  // ...
}

[PowerUp]
public partial class ConflictingSimplePowerUp : Node {
  public long LastNotification { get; private set; }
}
```

> `The type 'SimpleExampleNode' already contains a definition for 'LastNotification'`

Once again, the clever reader may recognize this as the classic "diamond problem" from [multiple inheritance][multiple-inheritance]. There's no easy way for SuperNodes to resolve this, so our recommendation is to avoid this situation altogether by simply saying "don't apply conflicting PowerUps." If you're getting so fancy with PowerUps that you run into naming clashes, please reconsider your approach.

> Generated mixins is big hammer to swing, so we recommend reserving PowerUps for systems that may apply to many scripts, such as serialization, dependency injection, logging, analytics, or integration with other components.

That being said, C# does provide a way to resolve naming conflicts between interfaces through [explicit interface implementations][explicit-interface-implementations]. Unfortunately, using explicit interface implementation syntax in your scripts [breaks Godot's own source generators][explicit-interface-godot-bug] (at the time of this writing, anyways), so you can't use this technique to bypass naming conflicts.

## 🪫 Supercharging PowerUps

If you can't find a source generator that meets your needs (and you can't be bothered to make your own), you might be able to create a PowerUp that does what you want. While PowerUps can't replace source generators, they do offer a number of features that can make advanced use cases easier to implement.

In short, PowerUps can:

- ✅ Add instance data to your scripts.
- ✅ Receive generic type parameters.
- ✅ Apply implemented interfaces.
- ✅ Constrain the types of SuperNodes they can be applied to.
- ✅ Inspect and manipulate all of the fields and properties on a SuperNode at runtime — without using reflection!

### 🪩 Static Reflection

The SuperNodes generator has a secret trick: it will generate a table of all the fields and properties of each SuperNode script at compile-time, including the fields and properties that come from PowerUps it has applied.

Let's take a look at a SuperNode with a PowerUp that inspects its own fields and properties:

```csharp
namespace IntrospectiveExample;

using System.Collections.Immutable;
using System.Linq;
using Godot;

[SuperNode(typeof(IntrospectivePowerUp))]
public partial class IntrospectiveNode : Node2D {
  public override partial void _Notification(int what);

  [Export(PropertyHint.MultilineText)]
  public string MyDescription { get; set; } = nameof(IntrospectiveNode);
}

[PowerUp]
public abstract partial class IntrospectivePowerUp : Node {
  // These stubs are equivalent to the static reflection utilities that
  // the SuperNodes generator will create. We mark them with [PowerUpIgnore] to
  // prevent them from being copied to the SuperNode this PowerUp is applied to.

  [PowerUpIgnore]
  internal static ImmutableDictionary<string, ScriptPropertyOrField>
    PropertiesAndFields =
      ImmutableDictionary<string, ScriptPropertyOrField>.Empty;

  [PowerUpIgnore]
  internal abstract TResult GetScriptPropertyOrFieldType<TResult>(
    string scriptProperty, ITypeReceiver<TResult> receiver
  );

  [PowerUpIgnore]
  internal abstract dynamic GetScriptPropertyOrField(string scriptProperty);

  [PowerUpIgnore]
  internal abstract void SetScriptPropertyOrField(
    string scriptProperty, dynamic value
  );

  // A type receiver which checks the type of a value when the reified type
  // is given to its Receive method.
  //
  // Note that ITypeReceiver is generated by SuperNodes.
  private class CheckValueType : ITypeReceiver<bool> {
    public dynamic Value { get; }

    public CheckValueType(dynamic value) {
      Value = value;
    }

    public bool Receive<T>() => Value is T;
  }

  public void OnIntrospectivePowerUp(int what) {
    if (what == NotificationReady) {
      var numberOfPropsAndFields = PropertiesAndFields.Count;
      GD.Print($"I have {numberOfPropsAndFields} properties and fields.");

      if (numberOfPropsAndFields > 0) {
        var prop = PropertiesAndFields.First();
        var myData = "hello, world"!;
        var myDataIsSameTypeAsProp = GetScriptPropertyOrFieldType(
          prop.Key, new CheckValueType(myData)
        );
      }
    }
  }
}
```

> The `IntrospectivePowerUp` has to declare static "stubs" that correspond to each generated reflection property or method created by the SuperNodes generator so that it will successfully compile. These stubs are marked with the `[PowerUpIgnore]` attribute to prevent them from being copied over into the SuperNode that the PowerUp is applied to. If they weren't ignored, you'd end up with a duplicate definition error.

The SuperNodes generator will produce an implementation for `IntrospectiveNode` containing static information about each property or field in the SuperNode, as well as utility methods for getting and setting values (for readable members and writable members, respectively).

```csharp
#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Godot;

namespace IntrospectiveExample {
  partial class IntrospectiveNode {
    /// <summary>
    /// A list of all properties and fields on this node script, along with
    /// basic information about the member and its attributes.
    /// This is provided to allow PowerUps to access script member data
    /// without having to resort to reflection.
    /// </summary>
    internal static ImmutableDictionary<string, ScriptPropertyOrField> PropertiesAndFields { get; }
      = new Dictionary<string, ScriptPropertyOrField>() {
      ["MyDescription"] = new ScriptPropertyOrField(
        "MyDescription",
        typeof(string),
        false,
        true,
        true,
        new Dictionary<string, ScriptAttributeDescription>() {
          ["global::Godot.ExportAttribute"] =
            new ScriptAttributeDescription(
              "ExportAttribute",
              typeof(global::Godot.ExportAttribute),
              ImmutableArray.Create<dynamic>(
                Godot.PropertyHint.MultilineText,
                ""
              )
            )
        }.ToImmutableDictionary()
      )
      }.ToImmutableDictionary();

    /// <summary>
    /// Calls the given type receiver with the generic type of the given
    /// script property or field. Generated by SuperNodes.
    /// </summary>
    /// <typeparam name="TResult">The return type of the type receiver's
    /// receive method.</typeparam>
    /// <param name="scriptProperty">The name of the script property or field
    /// to get the type of.</param>
    /// <param name="receiver">The type receiver to call with the type
    /// of the script property or field.</param>
    /// <returns>The result of the type receiver's receive method.</returns>
    /// <exception cref="System.ArgumentException">Thrown if the given script
    /// property or field does not exist.</exception>
    internal static TResult GetScriptPropertyOrFieldType<TResult>(
      string scriptProperty, ITypeReceiver<TResult> receiver
    ) {
      switch (scriptProperty) {
        case "MyDescription":
          return receiver.Receive<string>();
        default:
          throw new System.ArgumentException(
            $"No field or property named '{scriptProperty}' was found on IntrospectiveNode."
          );
      }
    }

    /// <summary>
    /// Gets the value of the given script property or field. Generated by
    /// SuperNodes.
    /// </summary>
    /// <typeparam name="TResult">The type of the script property or
    /// field to get the value of.</typeparam>
    /// <param name="scriptProperty">The name of the script property or
    /// field to get the value of.</param>
    /// <returns>The value of the script property or field.</returns>
    /// <exception cref="System.ArgumentException">Thrown if the given
    /// script property or field does not exist.</exception>
    internal dynamic GetScriptPropertyOrField(
      string scriptProperty
    ) {
      switch (scriptProperty) {
        case "MyDescription":
          return MyDescription;
        default:
          throw new System.ArgumentException(
            $"No field or property named '{scriptProperty}' was found on IntrospectiveNode."
          );
      }
    }

    /// <summary>
    /// Sets the value of the given script property or field. Generated by
    /// SuperNodes.
    /// </summary>
    /// <typeparam name="TResult">The type of the script property or
    /// field to set the value of.</typeparam>
    /// <param name="scriptProperty">The name of the script property or
    /// field to set the value of.</param>
    /// <param name="value">The value to set the script property or
    /// field to.</param>
    /// <exception cref="System.ArgumentException">Thrown if the given
    /// script property or field does not exist.</exception>
    internal void SetScriptPropertyOrField(
      string scriptProperty, dynamic value
    ) {
      switch (scriptProperty) {
        case "MyDescription":
          MyDescription = value;
          break;
        default:
          throw new System.ArgumentException(
            $"No field or property named '{scriptProperty}' was found on IntrospectiveNode."
          );
      }
    }
  }
}
#nullable disable
```

### Accessing Attributes on Properties and Fields

The `PropertiesAndFields` dictionary contains a `ScriptPropertyOrField` object for each property or field in the SuperNode. This object contains information about the member, including its name, type, readability, mutability, and attributes (as well as their arguments). Accessing member attributes opens the door for custom serialization systems, member initialization, or anything else you can dream up!

### Accessing Types

The `GetScriptPropertyOrFieldType` is a special utility that will call an `ITypeReceiver` object with the type of a script property or field as a generic type parameter. In certain situations, having access to the generic type parameter of a type is extremely useful for invoking other methods that may expect that type. Typically, converting a `Type` object into a type parameter [requires reflection or code generation at runtime][generic-types-reflection].

### Getting and Setting Values

## 🛑 PowerUp Constraints

PowerUps can only be applied to nodes that are descendants (or distant descendants) of a particular Godot node class.

For example, if you tried to apply a PowerUp which extended `Node3D` to a `Node2D`, you will get a warning from SuperNodes:

```csharp
[PowerUp]
public class MyPowerUp : Node3D { /* ... */ }

[SuperNode(typeof(MyPowerUp))]
public partial class MySuperNode : Node2D { /* .. */ }

// This won't work: SuperNodes will report a problem because MySuperNode
// doesn't have Node3D anywhere in its base class hierarchy!
```

## 🔋 + 🎰 PowerUps and Source Generators

SuperNodes can apply both PowerUps and Invoke generated methods from other source generators, as long as none of the PowerUps have the same name as the generated source methods.

SuperNodes calls generated methods and applied power-ups in the order they are specified in the `[SuperNode]` attribute.

For example:

```csharp
[SuperNode("Gen1", typeof(MyPowerUp), "Gen2", typeof(OtherPowerUp))]
public partial class MySuperNode : Node { /* ... */ }
```

SuperNodes will perform invocations in the following order:

- `Gen1` generated method implementation from another generator
- `OnMyPowerUp` from the mixed-in `MyPowerUp`
- `Gen2` generated method implementation from another generator
- `OnOtherPowerUp` from the mixed-in `OtherPowerUp`
- Any defined script handlers, such as `OnReady`, `OnProcess`, etc.

## 🛟 Full List of Lifecycle Handlers

The following list contains every possible lifecycle handlers you can implement in your SuperNode. Each one corresponds to a `Notification` type found in `Godot.Node` or `Godot.Object`.

If Godot's notifications are updated or renamed, new versions of SuperNodes can be released that adapt accordingly.

Note that `OnProcess` and `OnPhysicsProcess` are special cases that each have a single `double delta` parameter that is supplied by `GetProcessDeltaTime()` and `GetPhysicsProcessDeltaTime()`, respectively.

- **`Godot.Object` Notifications**
  - `OnPostinitialize` = `NotificationPostinitialize`
  - `OnPredelete` = `NotificationPredelete`
- **`Godot.Node` Notifications**
  - `OnNotification( what)` = `override _Notification( what)`
  - `OnEnterTree` = `NotificationEnterTree`
  - `OnWmWindowFocusIn` = `NotificationWmWindowFocusIn`
  - `OnWmWindowFocusOut` = `NotificationWmWindowFocusOut`
  - `OnWmCloseRequest` = `NotificationWmCloseRequest`
  - `OnWmSizeChanged` = `NotificationWmSizeChanged`
  - `OnWmDpiChange` = `NotificationWmDpiChange`
  - `OnVpMouseEnter` = `NotificationVpMouseEnter`
  - `OnVpMouseExit` = `NotificationVpMouseExit`
  - `OnOsMemoryWarning` = `NotificationOsMemoryWarning`
  - `OnTranslationChanged` = `NotificationTranslationChanged`
  - `OnWmAbout` = `NotificationWmAbout`
  - `OnCrash` = `NotificationCrash`
  - `OnOsImeUpdate` = `NotificationOsImeUpdate`
  - `OnApplicationResumed` = `NotificationApplicationResumed`
  - `OnApplicationPaused` = `NotificationApplicationPaused`
  - `OnApplicationFocusIn` = `NotificationApplicationFocusIn`
  - `OnApplicationFocusOut` = `NotificationApplicationFocusOut`
  - `OnTextServerChanged` = `NotificationTextServerChanged`
  - `OnWmMouseExit` = `NotificationWmMouseExit`
  - `OnWmMouseEnter` = `NotificationWmMouseEnter`
  - `OnWmGoBackRequest` = `NotificationWmGoBackRequest`
  - `OnEditorPreSave` = `NotificationEditorPreSave`
  - `OnExitTree` = `NotificationExitTree`
  - `OnMovedInParent` = `NotificationMovedInParent`
  - `OnReady` = `NotificationReady`
  - `OnEditorPostSave` = `NotificationEditorPostSave`
  - `OnUnpaused` = `NotificationUnpaused`
  - `OnPhysicsProcess(double delta)` = `NotificationPhysicsProcess`
  - `OnProcess(double delta)` = `NotificationProcess`
  - `OnParented` = `NotificationParented`
  - `OnUnparented` = `NotificationUnparented`
  - `OnPaused` = `NotificationPaused`
  - `OnDragBegin` = `NotificationDragBegin`
  - `OnDragEnd` = `NotificationDragEnd`
  - `OnPathRenamed` = `NotificationPathRenamed`
  - `OnInternalProcess` = `NotificationInternalProcess`
  - `OnInternalPhysicsProcess` = `NotificationInternalPhysicsProcess`
  - `OnPostEnterTree` = `NotificationPostEnterTree`
  - `OnDisabled` = `NotificationDisabled`
  - `OnEnabled` = `NotificationEnabled`
  - `OnSceneInstantiated` = `NotificationSceneInstantiated`

## ⚡️ Tips and Tricks

### 🔏 Interface Implementations

PowerUps can be used to implement an interface on any SuperNode that applies them.

```csharp
public interface IMyInterface { /* ... */ }

[PowerUp]
public class MyPowerUp : Node, IMyInterface { /* ... */ }

[SuperNode(typeof(MyPowerUp))]
public partial class MySuperNode : Node2D { /* .. */ }

/// SuperNodes will generate a partial implementation of MySuperNode in
/// MySuperNode.MyPowerUp.g.cs that makes MySuperNode implement IMyInterface.
```

### 🔌 Sharing PowerUps in Separate Packages

PowerUps can be distributed as source-only nuget packages! An example repository, `SharedPowerUps` is included to illustrate how to create a source-only nuget package.

The included example project, `SuperNodes.Example`, shows how to reference a source-only nuget package. Source-only packages have to be carefully designed so that they are fed into the consuming package's source generators. You can [read all about it here](SharedPowerUps/README.md).

### ♻️ Generic PowerUps

PowerUps can specify type parameters that will be substituted at runtime for the specific type arguments given in the `[SuperNode]` attribute.

```csharp
namespace MyProject;

using System;
using Godot;

[PowerUp]
public partial class HasParentOfType<TParent> : Node {
  public void OnHasParentOfType( what) {
    if (what == NotificationReady && GetParent() is not TParent) {
      throw new InvalidOperationException(
        $"MyPowerUp can only be used on a child of {typeof(TParent)}"
      );
    }
  }
}

[SuperNode(typeof(HasParentOfType<Node3D>))]
public partial class MyNode : Node {
  public override partial void _Notification( what);
}
```

The code above describes a PowerUp that checks to see if its parent node is the expected type once it is ready. `HasParentOfType` receives one generic type argument which represents the type of parent to check for.

At build time, the SuperNodes generator will substitute every reference to `TParent` for `Node3D` when the PowerUp is applied to the SuperNode `MyNode`, since `Node3D` is specified as the generic type argument in the `[SuperNode(typeof(HasParentOfType<Node3D>))]` attribute.

> The process of substituting generic type arguments at build time is similar to features found in other programming languages, such as C++ templates, but different from reified types available at runtime that are found in most managed languages like Java, C#, and Dart.

SuperNodes will generate an implementation file named `MyProject.MyNode_HasParentOfType.g.cs` which will contain the following:

```csharp
#nullable enable
using Godot;
using System;

namespace MyProject
{
  partial class MyNode
  {
    public void OnHasParentOfType( what)
    {
      if (what == NotificationReady && GetParent() is not global::Godot.Node3D)
      {
        throw new InvalidOperationException($"MyPowerUp can only be used on a child of {typeof(global::Godot.Node3D)}");
      }
    }
  }
}
#nullable disable

```

## 🙏 Credits

This project would not have been possible without all the amazing resources at [csharp-generator-resources][generators].

Special thanks to those in the Godot and Chickensoft Discord Servers for supplying tips, information, and help along the way!

<!-- Links -->

<!-- Header -->
[chickensoft-badge]: https://chickensoft.games/images/chickensoft/chickensoft_badge.svg
[chickensoft-website]: https://chickensoft.games
[discord]: https://discord.gg/gSjaPgMmYW
[discord-badge]: https://img.shields.io/badge/Chickensoft%20Discord-%237289DA.svg?style=flat&logo=discord&logoColor=white
[line-coverage]: https://raw.githubusercontent.com/chickensoft-games/SuperNodes/main/SuperNodes.Tests/reports/line_coverage.svg
[branch-coverage]: https://raw.githubusercontent.com/chickensoft-games/SuperNodes/main/SuperNodes.Tests/reports/branch_coverage.svg

<!-- Content -->
[godot-generator-problems]: https://github.com/godotengine/godot/issues/66597
[generators]: https://github.com/amis92/csharp-source-generators
[default-interfaces]: https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-8.0/default-interface-methods
[default-interfaces-limitations]: https://jeremybytes.blogspot.com/2019/09/c-8-interfaces-properties-and-default.html
[mixins]: https://en.wikipedia.org/wiki/Mixin
[traits]: https://en.wikipedia.org/wiki/Trait_(computer_programming)
[templates]: https://en.wikipedia.org/wiki/Template_(C++)
[macros]: https://en.wikipedia.org/wiki/Macro_(computer_science)
[multiple-inheritance]: https://en.wikipedia.org/wiki/Multiple_inheritance
[explicit-interface-implementations]: https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/interfaces/explicit-interface-implementation
[explicit-interface-godot-bug]: https://github.com/godotengine/godot/issues/74093
[generic-types-reflection]: https://learn.microsoft.com/en-us/dotnet/framework/reflection-and-codedom/how-to-examine-and-instantiate-generic-types-with-reflection
