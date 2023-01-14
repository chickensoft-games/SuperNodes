# SuperNodes

[![Chickensoft Badge][chickensoft-badge]][chickensoft-website] [![Discord](https://img.shields.io/badge/Chickensoft%20Discord-%237289DA.svg?style=flat&logo=discord&logoColor=white)][discord] ![line coverage][line-coverage]

Supercharge your Godot nodes with power ups and third party source generators.

SuperNodes is a source generator for Godot 4 projects written in C#. By adding just two lines of boilerplate code to each of your node scripts, you can use multiple lifecycle-aware third-party source generators harmoniously and add additional state to multiple types of nodes by injecting methods and properties, something that isn't possible with [default interface implementations][default-interfaces] alone.

Essentially, SuperNodes makes it possible for other unofficial Godot source generators to [play nicely with each other and the official Godot source generators][godot-generator-problems]. SuperNodes also provides a mechanism for you to create PowerUps (similar to "mixins" and "traits" in other languages). These PowerUps can be applied (or "mixed-in") to nodes that are descendants of the same ancestor class that the PowerUp extends.

Naturally, there are a few caveats, and you should only use PowerUps to create wide-reaching behavior to complement other systems, such as automatic ECS integration, logging and analytics, serialization systems, or adding additional state (properties) to nodes. If you create PowerUps for game logic, you risk adding methods and properties whose identifiers conflict with each other (a classic problem when mimicking multiple-inheritance).

> Need help with source generators, SuperNodes, and PowerUps?? Join the [Chickensoft Discord][Discord] and we'll be happy to help you out!

## Installation

Simply add SuperNodes as an analyzer dependency to your C# project.

```xml
<ItemGroup>
  <!-- Include SuperNodes as a Source Generator -->
  <PackageReference Include="Chickensoft.SuperNodes" Version="{LATEST_VERSION}" PrivateAssets="all" OutputItemType="analyzer" />
</ItemGroup>
```

## Enhanced Nodes

To turn your ordinary Godot script class into a SuperNode, add the `[SuperNode]` attribute and a partial method signature for the `_Notification` method.

```csharp
namespace MyProject;

using Godot;
using SuperNodes;

[SuperNode]
public partial class MySuperNode : Node {
  // This line has to be included in every script that wants to be a SuperNode.
  public override partial void _Notification(long what);
  
  // ... rest of your script
}
```

The SuperNodes generator will inject the `[SuperNode]` attribute into the codebase, so don't worry if you get an error when you first try to use it. Once the source generators run, the error should go away.

Your IDE will probably trigger the source generation automatically, but if it doesn't you can simply run `dotnet build`.

Under the hood, SuperNodes will generate an implementation for the Godot `_Notification` method, allowing it to observe the node's lifecycle events, such as `Ready`, `Process`, `EnterTree`, etc. You can still override the Godot version of those methods, but you can't implement `_Notification` yourself. For the full list of lifecycle handlers, [see below][#lifecycle-handlers].

Alternatively, SuperNodes will call any method you've defined that matches a Godot node or object notification and begins with the word `On`, such as `OnReady`, `OnProcess`, `OnWmMouseEnter`, `OnSceneInstantiated` etc. This allows you to easily and consistently define method signatures in C# idiomatically, if that's important to you.

> To view generated code in your project that's using source generators, include the following in your `.csproj` file:
>
> ```xml
> <PropertyGroup>
>   <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
>   <CompilerGeneratedFilesOutputPath>.generated</CompilerGeneratedFilesOutputPath>
> </PropertyGroup>
> ```

Just defining a SuperNode doesn't do much. Let's make it useful!

## Using Compatible Source Generators

As mentioned previously, SuperNodes can help you use multiple third-party source generators which want to observe a node's lifecycle events in harmony.

Suppose you want to use a source generator which prints out a message when your node is ready. We'll call this hypothetical source generator `PrintOnReady`.

To tell SuperNodes about your generator, add `PrintOnReady` to the `[SuperNode]` attribute:

```csharp
namespace MyProject;

using Godot;
using SuperNodes;

[SuperNode("PrintOnReady")]
public partial class MySuperNode : Node {
  public override partial void _Notification(long what);
  
  public void OnReady() {
    GD.Print("PrintOnReady will have already printed out that I'm ready.");
  }
  // ...
}
```

If the third party source generator wants to be compatible with SuperNodes, all it has to do is generate a partial implementation of your node's script class that contains a method named `PrintOnReady`. The `PrintOnReady` method will be called whenever `_Notification` is.

```csharp
// Hypothetical generated output of our imaginary PrintOnReady generator.
namespace MyProject;

using Godot;

public partial class MySuperNode {
  public void PrintOnReady(long what) {
    if (what == NotificationReady) {
      GD.Print($"{Name} is ready.");
    }
  }
}
```

> If you're looking to make your source generator compatible with SuperNodes, simply name your generated notification lifecycle method the same name as your source generator so it's easy for users to add it to their nodes with the `[SuperNode]` attribute.
>
> If all of us source generator authors follow that convention, we can have a really good time — and nobody's source generators will conflict with anyone else's!

SuperNodes will itself generate another partial implementation which will call the given `PrintOnReady` method, as well as the declared lifecycle handlers in the script itself:

```csharp
#nullable enable
using Godot;

namespace MyProject
{
  public partial class MySuperNode
  {
    public override partial void _Notification(long what)
    {
      // Invoke declared lifecycle method handlers from other generators.
      PrintOnReady(what);
      // Invoke any notification handlers declared in the script.
      switch (what)
      {
        case NotificationReady:
          OnReady();
          break;
        default:
          break;
      }
    }
  }
}
#nullable disable
```

### Multiple Source Generators

SuperNodes can invoke generated implementations for multiple source generators. Just put the names of the methods that should be called in the `[SuperNode]` attribute, like so:

```csharp
[SuperNode("GeneratedMethod1", "GeneratorMethod2")]
public partial class MySuperNode : Node { /* ... */ }
```

## PowerUps

If you can't find a source generator that meets your needs (and you can't be bothered to make your own), you can define "PowerUps" that can be applied to your SuperNodes.

To make a PowerUp, make a Godot node subclass, mark it with the `[PowerUp]` attribute, and create a method with the signature `public void On{NameOfYourPowerUp}(long what)`. The `OnMyPowerUp` method will be called from any generated SuperNodes implementations for SuperNodes that use this PowerUp, ensuring your PowerUp can respond to the node's lifecycle changes.

> Like the `[SuperNode]` attribute, the `[PowerUp]` attribute is generated by `SuperNodes` and may not exist until your IDE runs the source generators next. If you want to force the source generators to execute, simply run `dotnet build` in your project. 

For example: here's a custom PowerUp which prints a message whenever it enters or exits the scene tree.

```csharp
namespace MyProject;

using Godot;
using SuperNodes;

[PowerUp]
public partial class PrintOnTreePowerUp : Node {
  public void OnPrintOnTreePowerUp(long what) {
    if (what == NotificationEnterTree) {
      PrintOnTreePowerUpEnteredTree();
    }
    else if (what == NotificationExitTree) {
      PrintOnTreePowerUpExitedTree();
    }
  }

  // Any custom methods or properties you define in your PowerUp will be
  // copied into the SuperNode that uses it verbatim.
  private void PrintOnTreePowerUpEnteredTree() {
    GD.Print($"{Name} entered the tree.");
  }

  private void PrintOnTreePowerUpExitedTree() {
    GD.Print($"{Name} exited the tree.");
  }
}
```

To apply a PowerUp to a SuperNode, add the name of the PowerUp to the `[SuperNode]` attribute's list. The node below is the same one as demonstrated above, but it uses both the `PrintOnReady` generator and our new `PrintOnTreePowerUp`:

```csharp
namespace MyProject;

using Godot;
using SuperNodes;

[SuperNode("PrintOnReady", nameof(PrintOnTreePowerUp))]
public partial class MySuperNode : Node {
  public override partial void _Notification(long what);
  
  public void OnReady() {
    GD.Print("PrintOnReady will have already printed out that I'm ready.");
  }
  // ...
}
```

SuperNodes will then generate a mixed-in partial implementation of your node's script class called `MySuperNode.PrintOnTreePowerUp.g.cs` that looks something like this:

```csharp
#nullable enable
using Godot;
using MyPowerUps;

namespace MyProject
{
  public partial class MySuperNode
  {
    public void OnPrintOnTreePowerUp(long what)
    {
      if (what == NotificationEnterTree)
      {
        PrintOnTreePowerUpEnteredTree();
      }
      else if (what == NotificationExitTree)
      {
        PrintOnTreePowerUpExitedTree();
      }
    }

    // Any custom methods or properties you define in your power-up will be
    // copied into the SuperNode that uses it verbatim.
    private void PrintOnTreePowerUpEnteredTree() {
      GD.Print($"{Name} entered the tree.");
    }
    private void PrintOnTreePowerUpExitedTree() {
      GD.Print($"{Name} exited the tree.");
    }
  }
}
#nullable disable
```

The code from the PowerUp is essentially duplicated exactly, but the class is changed to be a partial implementation of the script that the PowerUp is applied to. Anything inside the PowerUp will get copied over exactly.

Any namespaces your PowerUp defines in its file will also get copied over.

## PowerUp Constraints

PowerUps can only be applied to nodes that are descendants (or distant descendants) of a particular Godot node class.

For example, if you tried to apply a PowerUp which extended `Node3D` to a `Node2D`, you will get a warning from SuperNodes:

```csharp
[PowerUp]
public class MyPowerUp : Node3D { /* ... */ }

[SuperNode]
public partial class MySuperNode : Node2D { /* .. */ }

// This won't work: SuperNodes will report a problem because MySuperNode
// doesn't have Node3D anywhere in its base class hierarchy!
```

## PowerUps and Source Generators

SuperNodes can apply both PowerUps and Invoke generated methods from other source generators, as long as none of the PowerUps have the same name as the generated source methods.

SuperNodes calls generated methods and applied power-ups in the order they are specified in the `[SuperNode]` attribute.

For example:

```csharp
[SuperNode("Gen1", nameof(MyPowerUp), "Gen2", nameof("OtherPowerUp"))]
public partial class MySuperNode : Node { /* ... */ }
```

SuperNodes will perform invocations in the following order:

- `Gen1` generated method implementation from another generator
- `OnMyPowerUp` from the mixed-in `MyPowerUp` 
- `Gen2` generated method implementation from another generator
- `OnOtherPowerUp` from the mixed-in `OtherPowerUp`
- Any defined script handlers, such as `OnReady`, `OnProcess`, etc.

## Lifecycle Handlers

The following list contains every possible lifecycle handlers you can implement in your SuperNode. Each one corresponds to a `Notification` type found in `Godot.Node` or `Godot.Object`.

If Godot's notifications are updated or renamed, new versions of SuperNodes can be released that adapt accordingly. 

Note that `OnProcess` and `OnPhysicsProcess` are special cases that each have a single `double delta` parameter that is supplied by `GetProcessDeltaTime()` and `GetPhysicsProcessDeltaTime()`, respectively.

- **`Godot.Object` Notifications**
  - `OnPostinitialize` = `NotificationPostinitialize`
  - `OnPredelete` = `NotificationPredelete`
- **`Godot.Node` Notifications**
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

## Credits

This project would not have been possible without all the amazing resources at [csharp-generator-resources][generators].

SuperNodes was heavily inspired by [PartialMixins] and [GodotSharp.SourceGenerators], along with many other projects and tutorials.

Special thanks to those in the Godot and Chickensoft Discord Servers for supplying tips, information, and help along the way!

<!-- Links -->

<!-- Header -->
[chickensoft-badge]: https://chickensoft.games/images/chickensoft/chickensoft_badge.svg
[chickensoft-website]: https://chickensoft.games
[discord]: https://discord.gg/gSjaPgMmYW
[line-coverage]: https://raw.githubusercontent.com/chickensoft-games/SuperNodes/main/SuperNodes.Tests/reports/line_coverage.svg
[branch-coverage]: https://raw.githubusercontent.com/chickensoft-games/SuperNodes/main/SuperNodes.Tests/reports/branch_coverage.svg

<!-- Content -->
[godot-generator-problems]: https://github.com/godotengine/godot/issues/66597
[default-interfaces]: https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-8.0/default-interface-methods
[generators]: https://github.com/amis92/csharp-source-generators
[PartialMixins]: https://github.com/LokiMidgard/PartialMixins
[GodotSharp.SourceGenerators]: https://github.com/Cat-Lips/GodotSharp.SourceGenerators
