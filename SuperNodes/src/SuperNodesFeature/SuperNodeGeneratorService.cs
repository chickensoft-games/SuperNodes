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
        "public ImmutableDictionary<string, ScriptPropertyOrField> " +
          "PropertiesAndFields",
        $"{Tab(1)}=> ScriptPropertiesAndFields;",
        "",
        "public static ImmutableDictionary<string, ScriptPropertyOrField> " +
          "ScriptPropertiesAndFields { get; }",
        $"{Tab(1)}= new Dictionary<string, ScriptPropertyOrField>() {{"
      };

    for (var propI = 0; propI < propsAndFields.Length; propI++) {
      var propOrField = propsAndFields[propI];
      var propComma
        = propOrField == propsAndFields[propsAndFields.Length - 1] ? "" : ",";
      fields.Add(
        $"{Tab(1)}[\"{propOrField.NameReference}\"] " +
        "= new ScriptPropertyOrField("
      );
      fields.Add($"{Tab(2)}Name: \"{propOrField.NameReference}\",");
      fields.Add($"{Tab(2)}Type: typeof({propOrField.Type}),");
      fields.Add(
        $"{Tab(2)}IsField: {propOrField.IsField.ToString().ToLower()},"
      );
      fields.Add(
        $"{Tab(2)}IsMutable: {propOrField.IsMutable.ToString().ToLower()},"
      );
      fields.Add(
        $"{Tab(2)}IsReadable: {propOrField.IsReadable.ToString().ToLower()},"
      );

      // Convert attributes into a
      // Dictionary<string, ImmutableArray<ScriptAttributeDescription>>
      // where the key is the attribute type and the value is an array of
      // ScriptAttributeDescriptions
      var attributes = propOrField.Attributes
        .GroupBy(attr => attr.Type)
        .ToDictionary(
          group => group.Key,
            group => group.Select(attr => new AttributeDescription(
              Name: attr.Name,
              Type: attr.Type,
              ArgumentExpressions: attr.ArgumentExpressions
          )).ToImmutableArray()
        );

      if (attributes.Count > 0) {
        fields.Add(
          $"{Tab(2)}new Dictionary<string, " +
          "ImmutableArray<ScriptAttributeDescription>>() {"
        );

        var attrTypes = attributes.Keys.ToArray();

        for (var attrI = 0; attrI < attrTypes.Length; attrI++) {
          var attrType = attrTypes[attrI];
          var attrComma = attrI == attrTypes.Length - 1 ? "" : ",";
          fields.Add(
            $"{Tab(3)}[\"{attrType}\"] = new ScriptAttributeDescription[] {{"
          );
          var attrDescriptions = attributes[attrType];
          for (var descI = 0; descI < attrDescriptions.Length; descI++) {
            var attrDesc = attrDescriptions[descI];
            var descComma = descI == attrDescriptions.Length - 1 ? "" : ",";
            var argumentExpressions = attrDesc.ArgumentExpressions.IsEmpty
              ? new string[] {
                  $"{Tab(5)}ArgumentExpressions: ImmutableArray<dynamic>.Empty"
                }
              : new string[] {
                $"{Tab(5)}ArgumentExpressions: new dynamic[] {{",
                $"{Tab(6)}{string.Join(", ", attrDesc.ArgumentExpressions)},",
                $"{Tab(5)}}}.ToImmutableArray()"
              };
            fields.AddRange(new string[] {
              $"{Tab(4)}new ScriptAttributeDescription(",
              $"{Tab(5)}Name: \"{attrDesc.Name}\",",
              $"{Tab(5)}Type: typeof({attrDesc.Type}),",
            });
            fields.AddRange(argumentExpressions);
            fields.Add($"{Tab(4)}){descComma}");
          }
          fields.Add($"{Tab(3)}}}.ToImmutableArray(){attrComma}");
        }

        fields.Add(
          $"{Tab(2)}}}.ToImmutableDictionary()"
        );
      }
      else {
        fields.Add(
          $"{Tab(2)}ImmutableDictionary<string, " +
            "ImmutableArray<ScriptAttributeDescription>>.Empty"
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
    var getTypeFn = new List<string>() {
      "public TResult GetScriptPropertyOrFieldType<TResult>(",
      $"{Tab(1)}string scriptProperty, ITypeReceiver<TResult> receiver",
      ") => ReceiveScriptPropertyOrFieldType(scriptProperty, receiver);",
      "",
      "public static TResult ReceiveScriptPropertyOrFieldType<TResult>(",
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
      "public dynamic GetScriptPropertyOrField(string scriptProperty) {",
      $"{Tab(1)}switch (scriptProperty) {{",
    };

    // Only allow readable fields to be accessed.
    propsAndFields = propsAndFields.Where(
      propOrField => propOrField.IsReadable
    ).ToImmutableArray();

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
      "public void SetScriptPropertyOrField(" +
      "string scriptProperty, dynamic value) {",
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
