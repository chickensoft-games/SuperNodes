namespace SuperNodes.Common.Models;

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

/// <summary>
/// Represents a PowerUp. A PowerUp is a class that can be applied to a
/// SuperNode. Unlike default interface implementations, PowerUps can add
/// additional state to a SuperNode, similar to mixins or traits in other
/// programming languages. Essentially, anything in a PowerUp's source code
/// is copied over into the implementation of the SuperNode that it is applied
/// to.
/// </summary>
/// <param name="Namespace">Fully qualified namespace containing the node
/// (without the <c>global::</c> prefix).</param>
/// <param name="Name">Name of the PowerUp class (not fully qualified). Combine
/// with <paramref name="Namespace" /> to determine the fully resolved name.
/// </param>
/// <param name="FullName">Fully resolved name of the PowerUp class.</param>
/// <param name="Location">The location of the class declaration syntax node
/// that corresponds to the PowerUp.</param>
/// <param name="BaseClass">Fully resolved base class of the PowerUp class,
/// if any. The base class is used as a constraint when applying the PowerUp
/// to a SuperNode. If the SuperNode the PowerUp is being applied to isn't a
/// subtype of the PowerUp's base class, an error is shown to the developer.
/// </param>
/// <param name="TypeParameters">An array of the declared type parameters for
/// the PowerUp class (in order), if any.</param>
/// <param name="Interfaces">Array of fully qualified interfaces implemented by
/// the PowerUp class. When applying a PowerUp to a SuperNode, any interfaces
/// implemented by that PowerUp are also applied to the SuperNode.</param>
/// <param name="Source">Source text of the PowerUp. This is re-parsed during
/// generation and adapted into the code needed to apply the PowerUp to a given
/// SuperNode.</param>
/// <param name="PropsAndFields">Information about properties and fields
/// declared in the PowerUp. We track these fields so that static reflection
/// tables can be generated at build time, allowing scripts and PowerUps to
/// introspect their properties and fields (and the attributes applied to them)
/// without having to use reflection.</param>
/// <param name="Usings">All of the using imports defined for the PowerUp
/// script.</param>
/// <param name="HasOnPowerUpMethod">True if the PowerUp has declared a
/// lifecycle method handler that should be called when the SuperNode it has
/// been applied to receives a Godot lifecycle notification.</param>
public record PowerUp(
  string Namespace,
  string Name,
  string FullName,
  Location Location,
  string BaseClass,
  ImmutableArray<string> TypeParameters,
  ImmutableHashSet<string> Interfaces,
  string Source,
  ImmutableArray<PropOrField> PropsAndFields,
  IImmutableSet<string> Usings,
  bool HasOnPowerUpMethod
) {
  /// <summary>True if the PowerUp has generic parameters.</summary>
  public bool IsGeneric => TypeParameters.Length > 0;
}
