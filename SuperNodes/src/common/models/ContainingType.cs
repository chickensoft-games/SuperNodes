namespace SuperNodes.Common.Models;

using System;
using Microsoft.CodeAnalysis;

public enum ContainingTypeKind {
  Record,
  Class,
}

public record ContainingType(
  string FullName,
  ContainingTypeKind Kind,
  Accessibility Accessibility,
  bool IsPartial
) {
  public string TypeDeclarationKeyword => GetTypeDeclarationKeyword(Kind);

  public string AccessibilityKeywords =>
    GetAccessibilityKeywords(Accessibility);

  public static string GetTypeDeclarationKeyword(
    ContainingTypeKind kind
  ) => kind switch {
    ContainingTypeKind.Record => "record",
    ContainingTypeKind.Class => "class",
    _ => throw new ArgumentException($"Unknown ContainingTypeKind: {kind}"),
  };

  public static string GetAccessibilityKeywords(
    Accessibility accessibility
  ) => accessibility switch {
    Accessibility.Public => "public",
    Accessibility.Protected => "protected",
    Accessibility.Internal => "internal",
    Accessibility.ProtectedOrInternal => "protected internal",
    Accessibility.Private => "private",
    Accessibility.ProtectedAndInternal => "private protected",
    Accessibility.NotApplicable or _ =>
      throw new ArgumentException($"Unknown Accessibility: {accessibility}"),
  };
}
