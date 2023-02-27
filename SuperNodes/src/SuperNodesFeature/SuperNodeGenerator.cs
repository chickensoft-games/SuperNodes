namespace SuperNodes.SuperNodesFeature;

using System.Collections.Immutable;
using System.Linq;
using SuperNodes.Common.Models;
using SuperNodes.Common.Utils;

/// <summary>Super node generator.</summary>
public interface ISuperNodeGenerator {
  /// <summary>
  /// Generates the primary implementation of a SuperNode containing the
  /// lifecycle implementation and notification handling logic.
  /// </summary>
  /// <param name="item">Generation item containing the SuperNode information
  /// and table of PowerUps.</param>
  /// <returns>Generated source string.</returns>
  string GenerateSuperNode(
    GenerationItem item
  );

  /// <summary>
  /// Generates source containing the static reflection tables for a SuperNode's
  /// properties and fields (and the properties and fields of any applied
  /// PowerUps).
  /// </summary>
  /// <param name="item">Generation item containing the SuperNode information
  /// and table of PowerUps.</param>
  /// <param name="appliedPowerUps">PowerUps to be applied to the SuperNode.
  /// </param>
  /// <returns>Generated source string.</returns>
  string GenerateSuperNodeStatic(
    GenerationItem item, ImmutableArray<PowerUp> appliedPowerUps
  );
}

/// <summary>Super node generator.</summary>
public class SuperNodeGenerator : ChickensoftGenerator, ISuperNodeGenerator {
  /// <summary>Imports required for static reflection tables.</summary>
  public readonly ImmutableArray<string> StaticUsings = ImmutableArray.Create(
    "System",
    "System.Collections.Generic",
    "System.Collections.Immutable"
  );

  public ISuperNodeGeneratorService SuperNodeGeneratorService { get; }

  public SuperNodeGenerator(
    ISuperNodeGeneratorService superNodeGeneratorService
  ) {
    SuperNodeGeneratorService = superNodeGeneratorService;
  }

  public string GenerateSuperNode(
    GenerationItem item
  ) {
    var node = item.SuperNode;
    var powerUps = item.PowerUps;

    var lifecycleInvocations = SuperNodeGeneratorService
      .GenerateLifecycleInvocations(node.LifecycleHooks, powerUps);

    var handlers = SuperNodeGeneratorService
      .GenerateNotificationHandlers(node.NotificationHandlers);

    return Format($$"""
    #nullable enable
    using Godot;

    {{If(
      node.Namespace is not null,
      $$"""namespace {{node.Namespace}} {"""
    )}}
      partial class {{node.Name}} {
        public override partial void _Notification(int what) {
          {{If(
          lifecycleInvocations.Length > 0,
          "// Invoke declared lifecycle method handlers."
          )}}
          {{If(lifecycleInvocations.Length > 0, lifecycleInvocations)}}
          {{If(
            node.HasOnNotificationMethodHandler,
            "// Invoke the notification handler in the script.",
            "OnNotification(what);"
          )}}
          {{If(
          handlers.Length > 0,
          "// Invoke any notification handlers declared in the script."
          )}}
          {{If(handlers.Length > 0, handlers)}}
        }
      }
    {{If(
      node.Namespace is not null,
      "}"
    )}}
    #nullable disable
    """).Clean();
  }

  public string GenerateSuperNodeStatic(
    GenerationItem item, ImmutableArray<PowerUp> appliedPowerUps
  ) {
    var node = item.SuperNode;
    var powerUpHooks = node.PowerUpHooksByFullName;
    var typeParameterSubstitutions = SuperNodeGeneratorService
      .GetTypeParameterSubstitutions(
        appliedPowerUps,
        powerUpHooks
      );

    var propsAndFieldsFromPowerUps = SuperNodeGeneratorService
      .SubstituteTypeParametersInPowerUps(
        appliedPowerUps,
        typeParameterSubstitutions
      );

    // Combine properties and fields from the node script and all of its
    // applied power-ups.
    var propsAndFields = node.PropsAndFields
      .Concat(propsAndFieldsFromPowerUps)
      .OrderBy(propOrField => propOrField.Name)
      .ToImmutableArray();

    // Combine usings from the node script and all of its applied power-ups.
    // (For any imported constants used in attribute constructors in the tables)
    var allUsings = node.Usings
      .Concat(appliedPowerUps.SelectMany(powerUp => powerUp.Usings))
      .Concat(StaticUsings)
      .Distinct();

    var usings = allUsings
      .Where(@using => @using.StartsWith("System"))
      .OrderBy(@using => @using)
      .Concat(
        allUsings
          .Where(@using => !@using.StartsWith("System"))
          .OrderBy(@using => @using)
      )
      .Select(@using => $"using {@using};");

    var propsAndFieldsReflectionTable = SuperNodeGeneratorService
      .GenerateStaticPropsAndFields(propsAndFields);

    var getTypeFn = SuperNodeGeneratorService
      .GenerateGetType(node.Name, propsAndFields);

    var getPropertyOrFieldFn = SuperNodeGeneratorService
      .GenerateGetPropertyOrField(node.Name, propsAndFields);

    var setPropertyOrFieldFn = SuperNodeGeneratorService
      .GenerateSetPropertyOrField(node.Name, propsAndFields);

    return Format($$"""
    #nullable enable
    {{usings}}

    {{If(
      node.Namespace is not null,
      $$"""namespace {{node.Namespace}} {"""
    )}}
      partial class {{node.Name}} {
        {{propsAndFieldsReflectionTable}}

        {{getTypeFn}}

        {{getPropertyOrFieldFn}}

        {{setPropertyOrFieldFn}}
      }
    {{If(
      node.Namespace is not null,
      "}"
    )}}
    #nullable disable
    """);
  }
}
