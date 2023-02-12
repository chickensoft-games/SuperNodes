namespace SuperNodes.Common.Models;

using System.Collections.Immutable;

/// <summary>
/// Represents an item given to the generator's Execute method. Because of the
/// way incremental generator pipelines work, this is used to represent the
/// information that is needed to generate the implementation of a single
/// SuperNode.
/// </summary>
/// <param name="Node">The SuperNode description to be generated.</param>
/// <param name="PowerUps">A dictionary of PowerUps, keyed by the PowerUp's
/// fully resolved name (e.g., <c>global::SomeNamespace.SomePowerUp</c>).
/// </param>
public record GenerationItem(
  SuperNode Node,
  ImmutableDictionary<string, PowerUp> PowerUps
);
