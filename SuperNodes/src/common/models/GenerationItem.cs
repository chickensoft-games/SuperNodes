namespace SuperNodes.Common.Models;

using System.Collections.Immutable;

/// <summary>
/// Represents a SuperNode/Object item given to the generator's Execute method.
/// Because of the way incremental generator pipelines work, this is used to
/// represent the information that is needed to generate the implementation of
/// a single SuperNode/Object.
/// </summary>
/// <param name="SuperObj">The SuperNode/Object description to be generated.
/// </param>
/// <param name="PowerUps">A dictionary of PowerUps, keyed by the PowerUp's
/// fully resolved name (e.g., <c>global::SomeNamespace.SomePowerUp</c>).
/// </param>
public record GenerationItem(
  SuperBase SuperObj, ImmutableDictionary<string, PowerUp> PowerUps
);

/// <summary>
/// Represents a SuperNode item given to the generator's Execute method.
/// Because of the way incremental generator pipelines work, this is used to
/// represent the information that is needed to generate the implementation of
/// a single SuperNode.
/// </summary>
/// <param name="SuperNode">The SuperNode description to be generated.</param>
/// <param name="PowerUps">A dictionary of PowerUps, keyed by the PowerUp's
/// fully resolved name (e.g., <c>global::SomeNamespace.SomePowerUp</c>).
/// </param>
public record SuperNodeGenerationItem(
  SuperNode SuperNode,
  ImmutableDictionary<string, PowerUp> PowerUps
) : GenerationItem(SuperNode, PowerUps);

/// <summary>
/// Represents a SuperObject item given to the generator's Execute method.
/// Because of the way incremental generator pipelines work, this is used to
/// represent the information that is needed to generate the implementation of
/// a single SuperObject.
/// </summary>
/// <param name="SuperObject">The SuperObject description to be generated.
/// </param>
/// <param name="PowerUps">A dictionary of PowerUps, keyed by the PowerUp's
/// fully resolved name (e.g., <c>global::SomeNamespace.SomePowerUp</c>).
/// </param>
public record SuperObjectGenerationItem(
  SuperObject SuperObject,
  ImmutableDictionary<string, PowerUp> PowerUps
) : GenerationItem(SuperObject, PowerUps);
