namespace SuperNodes.PowerUpsFeature;

using System.Collections.Immutable;
using SuperNodes.Common.Utils;

public interface IPowerUpGeneratorService {
  /// <summary>
  /// Creates a rewriter to convert a PowerUp class into a partial
  /// implementation of a SuperNode.
  /// </summary>
  /// <param name="typeParameters">Map of PowerUp's type parameters to type
  /// arguments.</param>
  /// <returns>PowerUp rewriter.</returns>
  PowerUpRewriter CreatePowerUpRewriter(
    ImmutableDictionary<string, string> typeParameters
  );
}

public class PowerUpGeneratorService
  : ChickensoftGenerator, IPowerUpGeneratorService {
  public PowerUpRewriter CreatePowerUpRewriter(
    ImmutableDictionary<string, string> typeParameters
  ) => new DefaultPowerUpRewriter(
    typeParameters: typeParameters
  );
}
