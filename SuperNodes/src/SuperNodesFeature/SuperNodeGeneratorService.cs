namespace SuperNodes.SuperNodesFeature;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using SuperNodes.Common.Models;
using SuperNodes.Common.Utils;

public interface ISuperNodeGeneratorService {
  /// <summary>
  /// Gets a dictionary of PowerUp full names to their corresponding maps of
  /// type parameters to type arguments.
  /// </summary>
  /// <param name="appliedPowerUps">PowerUps being applied containing type
  /// parameters.</param>
  /// <param name="powerUpHooks">PowerUp hooks containing type arguments.
  /// </param>
  /// <returns>A map of PowerUp full names to a map of type parameters to
  /// type arguments for the corresponding PowerUp.</returns>
  ImmutableDictionary<string, ImmutableDictionary<string, string>>
    GetTypeParameterSubstitutions(
      ImmutableArray<PowerUp> appliedPowerUps,
      ImmutableDictionary<string, PowerUpHook> powerUpHooks
    );

  /// <summary>
  /// Substitutes any references to a PowerUp's generic type parameters with
  /// the actual type argument given to the applied PowerUp.
  /// <br />
  /// This replaces references to type parameters in the types of properties
  /// and fields, as well as matching generic parameters in implemented
  /// interfaces.
  /// </summary>
  /// <param name="appliedPowerUps">Applied PowerUps.</param>
  /// <param name="typeParameterSubstitutions">Map of PowerUp full names to
  /// maps of type parameters for those PowerUps to their corresponding type
  /// arguments.</param>
  /// <returns>Modified array of PowerUps where interfaces and property return
  /// that are type parameters are replaced with their actual type arguments.
  /// </returns>
  ImmutableArray<PropOrField> SubstituteTypeParametersInPowerUps(
    ImmutableArray<PowerUp> appliedPowerUps,
    ImmutableDictionary<string, ImmutableDictionary<string, string>>
      typeParameterSubstitutions
  );

  /// <summary>
  /// Generates the lifecycle invocations for a SuperNode as a list of source
  /// strings.
  /// </summary>
  /// <param name="lifecycleHooks">Lifecycle hooks from the SuperNode.</param>
  /// <param name="powerUps">PowerUps table.</param>
  /// <returns>Array of source strings representing lifecycle hook invocations.
  /// </returns>
  ImmutableArray<string> GenerateLifecycleInvocations(
    ImmutableArray<IGodotNodeLifecycleHook> lifecycleHooks,
    ImmutableDictionary<string, PowerUp> powerUps
  );

  /// <summary>
  /// Generates the notification handlers for a SuperNode as a list of source
  /// strings.
  /// </summary>
  /// <param name="notificationHandlers">Notification handlers from the
  /// SuperNode.</param>
  /// <returns>Array of source strings representing notification handlers.
  /// </returns>
  ImmutableArray<string> GenerateNotificationHandlers(
    ImmutableArray<string> notificationHandlers
  );

  /// <summary>
  /// Generates the static reflection tables for the given properties and fields
  /// for a SuperNode and all its applied PowerUps.
  /// </summary>
  /// <param name="propsAndFields">Combined props and fields of the SuperNode
  /// and any PowerUps applied to it.</param>
  /// <returns>Array of source strings representing the static reflection tables
  /// for the SuperNode and its applied PowerUps.</returns>
  ImmutableArray<string> GenerateStaticPropsAndFields(
    ImmutableArray<PropOrField> propsAndFields
  );

  /// <summary>
  /// Generates the static GetType method for a SuperNode that allows the
  /// developer to receive the generic type of a property or field by name.
  /// </summary>
  /// <param name="superNodeName">Name of the SuperNode.</param>
  /// <param name="propsAndFields">Combined props and fields of the SuperNode
  /// and any PowerUps applied to it.</param>
  /// <returns>Array of source strings representing the implementation of the
  /// GetType method.</returns>
  ImmutableArray<string> GenerateGetType(
    string superNodeName, ImmutableArray<PropOrField> propsAndFields
  );

  /// <summary>
  /// Generates the static GetPropertyOrField method for a SuperNode that allows
  /// the developer to receive the value of a property or field by name.
  /// </summary>
  /// <param name="superNodeName">Name of the SuperNode.</param>
  /// <param name="propsAndFields">Combined props and fields of the SuperNode
  /// and any PowerUps applied to it.</param>
  /// <returns>Array of source strings representing the implementation of the
  /// method.</returns>
  ImmutableArray<string> GenerateGetPropertyOrField(
    string superNodeName,
    ImmutableArray<PropOrField> propsAndFields
  );

  /// <summary>
  /// Generates the static SetPropertyOrField method for a SuperNode that allows
  /// the developer to set the value of a property or field by name.
  /// </summary>
  /// <param name="superNodeName">Name of the SuperNode.</param>
  /// <param name="propsAndFields">Combined props and fields of the SuperNode
  /// and any PowerUps applied to it.</param>
  /// <returns>Array of source strings representing the implementation of the
  /// method.</returns>
  ImmutableArray<string> GenerateSetPropertyOrField(
    string superNodeName,
    ImmutableArray<PropOrField> propsAndFields
  );
}

public class SuperNodeGeneratorService
  : ChickensoftGenerator, ISuperNodeGeneratorService {
  public ImmutableDictionary<string, ImmutableDictionary<string, string>>
    GetTypeParameterSubstitutions(
      ImmutableArray<PowerUp> appliedPowerUps,
      ImmutableDictionary<string, PowerUpHook> powerUpHooks
    ) => appliedPowerUps.ToImmutableDictionary(
      powerUp => powerUp.FullName,
      powerUp => powerUp.TypeParameters.Zip(
        powerUpHooks[powerUp.FullName].TypeArguments,
        (param, arg) => (param, arg)
      ).ToImmutableDictionary(
        pair => pair.param, pair => pair.arg
      )
    );

  public ImmutableArray<PropOrField> SubstituteTypeParametersInPowerUps(
    ImmutableArray<PowerUp> appliedPowerUps,
    ImmutableDictionary<string, ImmutableDictionary<string, string>>
      typeParameterSubstitutions
  ) => appliedPowerUps
    .Select(appliedPowerUp => {
      var propsAndFields = appliedPowerUp.PropsAndFields.Select(
        propOrField => {
          var typeParamMapping = typeParameterSubstitutions[
            appliedPowerUp.FullName
          ];
          return propOrField with {
            Reference = PropOrField.SubstituteTypeParameters(
              propOrField.Reference, typeParamMapping, propOrField.NameParts
            ),
            Type = PropOrField.SubstituteTypeParameters(
              propOrField.Type, typeParamMapping, propOrField.TypeParts
            )
          };
        }
      ).ToImmutableArray();

      return appliedPowerUp with { PropsAndFields = propsAndFields };
    }).SelectMany(powerUp => powerUp.PropsAndFields).ToImmutableArray();

  public ImmutableArray<string> GenerateLifecycleInvocations(
    ImmutableArray<IGodotNodeLifecycleHook> lifecycleHooks,
    ImmutableDictionary<string, PowerUp> powerUps
  ) => lifecycleHooks.Where(
    hook => hook is not PowerUpHook powerUpHook ||
      powerUps[powerUpHook.FullName].HasOnPowerUpMethod
  ).Select(
    hook => {
      if (hook is PowerUpHook powerUpHook) {
        return $"On{powerUps[powerUpHook.FullName].Name}(what);";
      }
      var lifecycleMethodHook = (LifecycleMethodHook)hook;
      return $"{lifecycleMethodHook.Name}(what);";
    }
  ).ToImmutableArray();

  public ImmutableArray<string> GenerateNotificationHandlers(
    ImmutableArray<string> notificationHandlers
  ) {
    // Create a switch/case for every implemented notification handler, such
    // as OnReady, OnPhysicsProcess, etc.
    var handlers = new List<string>();
    if (notificationHandlers.Length > 0) {
      handlers.Add("switch ((long)what) {");
      foreach (var handler in notificationHandlers) {
        var method = Constants.LifecycleMethods[handler];
        handlers.Add(
          $"  case {method.GodotNotification}:"
        );
        handlers.Add($"    {handler}({string.Join(", ", method.Args)});");
        handlers.Add("    break;");
      }
      handlers.Add("  default:");
      handlers.Add("    break;");
      handlers.Add("}");
    }
    return handlers.ToImmutableArray();
  }

  public ImmutableArray<string> GenerateStaticPropsAndFields(
    ImmutableArray<PropOrField> propsAndFields
  ) {
    var fields = new List<string>() {
      "/// <summary>",
      "/// A list of all properties and fields on this node script, along with",
      "/// basic information about the member and its attributes.",
      "/// This is provided to allow PowerUps to access script member data",
      "/// without having to resort to reflection.",
      "/// </summary>",
      "internal static ImmutableDictionary<string, ScriptPropertyOrField> " +
        "PropertiesAndFields { get; }",
      $"{Tab(1)}= new Dictionary<string, ScriptPropertyOrField>() {{"
    };

    for (var propI = 0; propI < propsAndFields.Length; propI++) {
      var propOrField = propsAndFields[propI];
      var propComma
        = propOrField == propsAndFields[propsAndFields.Length - 1] ? "" : ",";
      fields.Add($"{Tab(1)}[\"{propOrField.NameReference}\"] = new ScriptPropertyOrField(");
      fields.Add($"{Tab(2)}\"{propOrField.NameReference}\",");
      fields.Add($"{Tab(2)}typeof({propOrField.Type}),");
      fields.Add($"{Tab(2)}{propOrField.IsField.ToString().ToLower()},");
      var attributes = propOrField.Attributes;
      if (attributes.Length > 0) {
        fields.Add(
          $"{Tab(2)}new Dictionary<string, ScriptAttributeDescription>() {{"
        );
        foreach (var attribute in attributes) {
          var attrComma = attribute == attributes.Last() ? "" : ",";
          fields.Add($"{Tab(3)}[\"{attribute.Type}\"] =");
          fields.Add($"{Tab(4)}new ScriptAttributeDescription(");
          fields.Add($"{Tab(5)}\"{attribute.Name}\",");
          fields.Add($"{Tab(5)}typeof({attribute.Type}),");
          var args = attribute.ArgumentExpressions;
          if (args.Length > 0) {
            fields.Add($"{Tab(5)}ImmutableArray.Create<dynamic>(");
            foreach (var arg in args) {
              var argComma
                = arg == attribute.ArgumentExpressions.Last() ? "" : ",";
              fields.Add(Tab(6) + arg + argComma);
            }
            fields.Add($"{Tab(5)})");
          }
          else {
            fields.Add($"{Tab(5)}Array.Empty<dynamic>().ToImmutableArray()");
          }
          fields.Add($"{Tab(4)}){attrComma}");
        }
        fields.Add($"{Tab(2)}}}.ToImmutableDictionary()");
      }
      else {
        fields.Add(
          $"{Tab(2)}ImmutableDictionary<string, " +
            "ScriptAttributeDescription>.Empty"
        );
      }
      fields.Add($"{Tab(1)}){propComma}");
    }

    fields.Add($"{Tab(1)}}}.ToImmutableDictionary();");

    return fields.ToImmutableArray();
  }

  public ImmutableArray<string> GenerateGetType(
    string superNodeName, ImmutableArray<PropOrField> propsAndFields
  ) {
    var getTypeFn = new List<string> {
      "/// <summary>",
      "/// Calls the given type receiver with the generic type of the given",
      "/// script property or field. Generated by SuperNodes.",
      "/// </summary>",
      "/// <typeparam name=\"TResult\">The return type of the type receiver's",
      "/// receive method.</typeparam>",
      "/// <param name=\"scriptProperty\">The name of the script property or " +
        "field",
      "/// to get the type of.</param>",
      "/// <param name=\"receiver\">The type receiver to call with the type",
      "/// of the script property or field.</param>",
      "/// <returns>The result of the type receiver's receive method." +
        "</returns>",
      "/// <exception cref=\"System.ArgumentException\">Thrown if the given " +
        "script",
      "/// property or field does not exist.</exception>",
      "internal static TResult GetScriptPropertyOrFieldType<TResult>(",
      $"{Tab(1)}string scriptProperty, ITypeReceiver<TResult> receiver",
      ") {",
      $"{Tab(1)}switch (scriptProperty) {{",
    };

    if (propsAndFields.Length > 0) {
      foreach (var propOrField in propsAndFields) {
        getTypeFn.AddRange(new string[] {
          $"{Tab(2)}case \"{propOrField.NameReference}\":",
          $"{Tab(3)}return receiver.Receive<{propOrField.Type}>();"
        });
      }
    }

    getTypeFn.AddRange(new string[] {
      $"{Tab(2)}default:",
      $"{Tab(3)}throw new System.ArgumentException(",
      $"{Tab(4)}$\"No field or property named '{{scriptProperty}}' was " +
        $"found on {superNodeName}.\"",
      $"{Tab(3)});",
      $"{Tab(1)}}}",
      "}"
    });

    return getTypeFn.ToImmutableArray();
  }

  public ImmutableArray<string> GenerateGetPropertyOrField(
    string superNodeName,
    ImmutableArray<PropOrField> propsAndFields
  ) {
    var getPropertyOrFieldFn = new List<string> {
      "/// <summary>",
      "/// Gets the value of the given script property or field. Generated by",
      "/// SuperNodes.",
      "/// </summary>",
      "/// <typeparam name=\"TResult\">The type of the script property or",
      "/// field to get the value of.</typeparam>",
      "/// <param name=\"scriptProperty\">The name of the script property or",
      "/// field to get the value of.</param>",
      "/// <returns>The value of the script property or field.</returns>",
      "/// <exception cref=\"System.ArgumentException\">Thrown if the given",
      "/// script property or field does not exist.</exception>",
      "internal dynamic GetScriptPropertyOrField(",
      $"{Tab(1)}string scriptProperty",
      ") {",
      $"{Tab(1)}switch (scriptProperty) {{",
    };

    foreach (var propOrField in propsAndFields) {
      getPropertyOrFieldFn.AddRange(new string[] {
        $"{Tab(2)}case \"{propOrField.NameReference}\":",
        $"{Tab(3)}return {propOrField.NameInstance};"
      });
    }

    getPropertyOrFieldFn.AddRange(new string[] {
      $"{Tab(2)}default:",
      $"{Tab(3)}throw new System.ArgumentException(",
      $"{Tab(4)}$\"No field or property named '{{scriptProperty}}' was " +
        $"found on {superNodeName}.\"",
      $"{Tab(3)});",
      $"{Tab(1)}}}",
      "}"
    });

    return getPropertyOrFieldFn.ToImmutableArray();
  }

  public ImmutableArray<string> GenerateSetPropertyOrField(
    string superNodeName,
    ImmutableArray<PropOrField> propsAndFields
  ) {
    var setPropertyOrFieldFn = new List<string> {
      "/// <summary>",
      "/// Sets the value of the given script property or field. Generated by",
      "/// SuperNodes.",
      "/// </summary>",
      "/// <typeparam name=\"TResult\">The type of the script property or",
      "/// field to set the value of.</typeparam>",
      "/// <param name=\"scriptProperty\">The name of the script property or",
      "/// field to set the value of.</param>",
      "/// <param name=\"value\">The value to set the script property or",
      "/// field to.</param>",
      "/// <exception cref=\"System.ArgumentException\">Thrown if the given",
      "/// script property or field does not exist.</exception>",
      "internal void SetScriptPropertyOrField(",
      $"{Tab(1)}string scriptProperty, dynamic value",
      ") {",
      $"{Tab(1)}switch (scriptProperty) {{",
    };

    // Only allow mutable fields to be set.
    propsAndFields = propsAndFields.Where(propOrField => propOrField.IsMutable)
      .ToImmutableArray();

    foreach (var propOrField in propsAndFields) {
      setPropertyOrFieldFn.AddRange(new string[] {
        $"{Tab(2)}case \"{propOrField.NameReference}\":",
        $"{Tab(3)}{propOrField.NameInstance} = value;",
        $"{Tab(3)}break;"
      });
    }

    setPropertyOrFieldFn.AddRange(new string[] {
      $"{Tab(2)}default:",
      $"{Tab(3)}throw new System.ArgumentException(",
      $"{Tab(4)}$\"No field or property named '{{scriptProperty}}' was " +
        $"found on {superNodeName}.\"",
      $"{Tab(3)});",
      $"{Tab(1)}}}",
      "}"
    });

    return setPropertyOrFieldFn.ToImmutableArray();
  }
}
