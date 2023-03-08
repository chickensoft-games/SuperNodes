namespace Microsoft.CodeAnalysis.Shared.Extensions;
using System.Collections.Immutable;
using System.Linq;

// Borrowed from
// sourceroslyn.io/#Microsoft.CodeAnalysis.Workspaces/ISymbolExtensions.cs
// Microsoft didn't make them public. :'(
// MIT License.

public static class ISymbolExtensionsThatShouldHaveBeenPublic {
  public static ImmutableArray<ISymbol>
    ExplicitOrImplicitInterfaceImplementations(
      this ISymbol symbol
  ) {
    if (
      symbol.Kind is not SymbolKind.Method and not
      SymbolKind.Property and not
      SymbolKind.Event
    ) {
      return ImmutableArray<ISymbol>.Empty;
    }

    var containingType = symbol.ContainingType;
    var query =
      from iface in containingType.AllInterfaces
      from interfaceMember in iface.GetMembers()
      let impl = containingType
        .FindImplementationForInterfaceMember(interfaceMember)
      where SymbolEqualityComparer.Default.Equals(symbol, impl)
      select interfaceMember;
    return query.ToImmutableArray();
  }
}
