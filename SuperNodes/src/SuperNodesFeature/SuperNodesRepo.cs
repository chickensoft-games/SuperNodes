namespace SuperNodes.SuperNodesFeature;

using SuperNodes.Common.Services;

/// <summary>
/// Handles logic for generating SuperNodes.
/// </summary>
public interface ISuperNodesRepo {
  /// <summary>Common operations needed for syntax nodes.</summary>
  ICodeService CodeService { get; }
}

/// <summary>
/// Handles logic for generating SuperNodes.
/// </summary>
public class SuperNodesRepo : ISuperNodesRepo {
  public ICodeService CodeService { get; }

  /// <summary>
  /// Create a new PowerUpsRepo.
  /// </summary>
  /// <param name="syntaxOps">Common operations needed for syntax nodes.</param>
  public SuperNodesRepo(ICodeService syntaxOps) {
    CodeService = syntaxOps;
  }
}
