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
  /// <param name="powerUpClassName">PowerUp class name.</param>
  /// <param name="superNodeClassName">SuperNode class name.</param>
  /// <returns>PowerUp rewriter.</returns>
  PowerUpRewriter CreatePowerUpRewriter(
    ImmutableDictionary<string, string> typeParameters,
    string powerUpClassName,
    string superNodeClassName
  );
}

public class PowerUpGeneratorService
  : ChickensoftGenerator, IPowerUpGeneratorService {
  public PowerUpRewriter CreatePowerUpRewriter(
    ImmutableDictionary<string, string> typeParameters,
    string powerUpClassName,
    string superNodeClassName
  ) => new DefaultPowerUpRewriter(
    typeParameters: typeParameters,
    powerUpClassName: powerUpClassName,
    superNodeClassName: superNodeClassName
  );
}
