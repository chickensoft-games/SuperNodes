namespace SuperNodes.SuperNodesFeature.Services;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using SuperNodes.Common.Models;
using SuperNodes.SuperNodesFeature.Models;

public interface ISuperNodesCodeService {
  /// <summary>
  /// Examines the SuperNode attribute data for any mentioned lifecycle hooks.
  /// </summary>
  /// <param name="attribute">SuperNode attribute data found on a class symbol.
  /// </param>
  /// <returns>Lifecycle hook information.</returns>
  LifecycleHooksResponse GetLifecycleHooks(AttributeData attribute);
}

public class SuperNodesCodeService : ISuperNodesCodeService {
  public LifecycleHooksResponse GetLifecycleHooks(AttributeData attribute) {
    var lifecycleHooks = new List<IGodotNodeLifecycleHook>();
    var powerUpHooksByFullName = new Dictionary<string, PowerUpHook>();

    var args = attribute.ConstructorArguments;
    if (args.Length == 1) {
      // SuperNode attribute technically only requires 1 argument which
      // should be an array of compile-time constants. We only support
      // two kinds of parameters: strings and types (via typeof).
      var arg = args[0];
      foreach (var constant in arg.Values) {
        var constantType = constant.Type;
        if (constantType?.Name == "String") {
          // Found a lifecycle method. This can be the name of a method
          // to call from another generator or a method from a PowerUp.
          var stringValue = (string)constant.Value!;
          lifecycleHooks.Add(new LifecycleMethodHook(stringValue));
        }
        else if (constantType?.Name == "Type") {
          // We found a typeof(SomePowerUp<a, b, ...>) expression. It may
          // or may not have generic args. The important part is that we know
          // this must be a specific PowerUp (possibly with generics) that
          // needs to be applied to the node script.
          var typeValue = (INamedTypeSymbol)constant.Value!;
          // convert from PowerUp<bool, string> to the less concrete type
          // parameters like PowerUp<TA, TB>.
          var typeWithGenericParams = typeValue.ConstructedFrom;
          var fullName = typeWithGenericParams.ToDisplayString(
            SymbolDisplayFormat.FullyQualifiedFormat
          );
          var powerUpHook = new PowerUpHook(
            fullName,
            typeValue.TypeArguments.Select(arg => arg.ToDisplayString(
              SymbolDisplayFormat.FullyQualifiedFormat
            )).ToImmutableArray()
          );
          lifecycleHooks.Add(powerUpHook);
          powerUpHooksByFullName[fullName] = powerUpHook;
        }
      }
    }

    return new LifecycleHooksResponse(
      lifecycleHooks.ToImmutableArray(),
      powerUpHooksByFullName.ToImmutableDictionary()
    );
  }
}
