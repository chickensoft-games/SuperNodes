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
    SuperNodeGenerationItem item
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
  string GenerateStaticReflection(
    GenerationItem item, ImmutableArray<PowerUp> appliedPowerUps
  );
}

/// <summary>Super node generator.</summary>
public class SuperNodeGenerator : ChickensoftGenerator, ISuperNodeGenerator {
  /// <summary>Imports required for static reflection tables.</summary>
  public readonly ImmutableArray<string> StaticUsings = ImmutableArray.Create(
    "System",
    "System.Collections.Generic",
    "System.Collections.Immutable",
    "SuperNodes.Types"
  );

  public ISuperNodeGeneratorService SuperNodeGeneratorService { get; }

  public SuperNodeGenerator(
    ISuperNodeGeneratorService superNodeGeneratorService
  ) {
    SuperNodeGeneratorService = superNodeGeneratorService;
  }

  public string GenerateSuperNode(
    SuperNodeGenerationItem item
  ) {
    var node = item.SuperNode;
    var powerUps = item.PowerUps;

    var lifecycleInvocations = SuperNodeGeneratorService
      .GenerateLifecycleInvocations(node.LifecycleHooks, powerUps);

    var handlers = SuperNodeGeneratorService
      .GenerateNotificationHandlers(node.NotificationHandlers);

    return Format($$"""
    #pragma warning disable
    #nullable enable
    using SuperNodes.Types;

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
    #pragma warning restore
    """).Clean();
  }

  public string GenerateStaticReflection(
    GenerationItem item, ImmutableArray<PowerUp> appliedPowerUps
  ) {
    var superItem = item.SuperObj;
    var powerUpHooks = superItem.PowerUpHooksByFullName;
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
    var propsAndFields = superItem.PropsAndFields
      .Concat(propsAndFieldsFromPowerUps)
      .OrderBy(propOrField => propOrField.Name)
      .ToImmutableArray();

    // Combine usings from the node script and all of its applied power-ups.
    // (For any imported constants used in attribute constructors in the tables)
    var allUsings = superItem.Usings
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
      .GenerateGetType(superItem.Name, propsAndFields);

    var getPropertyOrFieldFn = SuperNodeGeneratorService
      .GenerateGetPropertyOrField(superItem.Name, propsAndFields);

    var setPropertyOrFieldFn = SuperNodeGeneratorService
      .GenerateSetPropertyOrField(superItem.Name, propsAndFields);

    var typeDeclarationKeyword = superItem.IsRecord ? "record" : "class";
    var @interface = superItem is SuperNode
      ? "ISuperNode"
      : "ISuperObject";

    return Format($$"""
    #pragma warning disable
    #nullable enable
    {{usings}}

    {{If(
      superItem.Namespace is not null,
      $$"""namespace {{superItem.Namespace}} {"""
    )}}
      partial {{typeDeclarationKeyword}} {{superItem.Name}} : {{@interface}} {
        {{propsAndFieldsReflectionTable}}

        {{getTypeFn}}

        {{getPropertyOrFieldFn}}

        {{setPropertyOrFieldFn}}
      }
    {{If(
      superItem.Namespace is not null,
      "}"
    )}}
    #nullable disable
    #pragma warning restore
    """);
  }
}
