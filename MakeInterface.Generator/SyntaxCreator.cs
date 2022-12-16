﻿using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Data;

namespace MakeInterface.Generator;
internal static class SyntaxCreator
{
    internal static SyntaxToken CreateTrivia()
    {
        return SyntaxFactory.Token(
                SyntaxFactory.TriviaList(
                    new[]{
                        SyntaxFactory.Comment("// <auto-generated/>"),
                        SyntaxFactory.Trivia(
                            SyntaxFactory.PragmaWarningDirectiveTrivia(
                                SyntaxFactory.Token(SyntaxKind.DisableKeyword),
                                true)),
                        SyntaxFactory.Trivia(
                            SyntaxFactory.NullableDirectiveTrivia(
                                SyntaxFactory.Token(SyntaxKind.EnableKeyword),
                                true))}),
                SyntaxKind.NamespaceKeyword,
                SyntaxFactory.TriviaList());
    }

    internal static MemberDeclarationSyntax CreateMethod(IMethodSymbol methodSymbol, Dictionary<string, string> interfaceNamespaceMap)
    {
        var parameters = CreateParameterList(methodSymbol);
        var returnTypeName = GetTypeName(methodSymbol.ReturnType, interfaceNamespaceMap);

        var methodDeclaration = SyntaxFactory.MethodDeclaration(
            SyntaxFactory.ParseTypeName(returnTypeName),
            SyntaxFactory.Identifier(methodSymbol.Name))
            .WithParameterList(SyntaxFactory.ParameterList(parameters))
            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));

        if (methodSymbol.IsGenericMethod)
        {
            var typeParameters = GetTypeParameters(methodSymbol);

            methodDeclaration = methodDeclaration.WithTypeParameterList(SyntaxFactory.TypeParameterList(typeParameters.Item1));
            if (typeParameters.Item2.Any())
                methodDeclaration = methodDeclaration.WithConstraintClauses(typeParameters.Item2);

        }

        return methodDeclaration;
    }

    private static string GetTypeName(ITypeSymbol type, Dictionary<string, string> interfaceNamespaceMap)
    {
        var typeName = type.ToDisplayString();

        if (type is INamedTypeSymbol namedTypeSymbol && namedTypeSymbol.TypeArguments.Length > 0)
        {
            foreach (var typeArgument in namedTypeSymbol.TypeArguments)
            {
                typeName = typeName.Replace(typeArgument.ToDisplayString(), GetTypeName(typeArgument, interfaceNamespaceMap));
            }
        }

        if (interfaceNamespaceMap.TryGetValue(typeName, out var interfaceTypeName))
            typeName = typeName.Replace(typeName, interfaceTypeName);

        return typeName;
    }

    private static SeparatedSyntaxList<ParameterSyntax> CreateParameterList(IMethodSymbol methodSymbol)
    {
        var parameters = new List<ParameterSyntax>();
        foreach (var parameter in methodSymbol.Parameters)
        {
            // Parse the type name of the parameter using the ParseTypeName method
            var parameterType = SyntaxFactory.ParseTypeName(parameter.Type.ToDisplayString());

            var parameterSyntax = SyntaxFactory.Parameter(SyntaxFactory.Identifier(parameter.Name));


            // Add any applicable modifiers to the parameterSyntax
            if (parameter.IsParams)
            {
                parameterSyntax = parameterSyntax.WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.ParamsKeyword)));
            }
            if (parameter.IsOptional)
            {
                SyntaxKind kind;
                if (parameter.Type.SpecialType == SpecialType.System_Nullable_T)
                    kind = SyntaxKind.NullLiteralExpression;
                else
                    kind = SyntaxKind.DefaultLiteralExpression;

                parameterSyntax = parameterSyntax.WithDefault(SyntaxFactory.EqualsValueClause(SyntaxFactory.LiteralExpression(kind)));
            }
            if (parameter.RefKind != RefKind.None)
            {
                SyntaxToken refOrOutKeyword = SyntaxFactory.Token(parameter.RefKind == RefKind.Ref ? SyntaxKind.RefKeyword : SyntaxKind.OutKeyword);
                parameterSyntax = parameterSyntax.WithModifiers(SyntaxFactory.TokenList(refOrOutKeyword));
            }

            parameterSyntax = parameterSyntax.WithType(parameterType); // Pass the resulting TypeSyntax node to the WithType method

            parameters.Add(parameterSyntax);
        }
        return SyntaxFactory.SeparatedList(parameters);
    }

    private static (SeparatedSyntaxList<TypeParameterSyntax>, SyntaxList<TypeParameterConstraintClauseSyntax>) GetTypeParameters(IMethodSymbol methodSymbol)
    {
        var typeParameters = new List<TypeParameterSyntax>();
        var constraintClauses = new List<TypeParameterConstraintClauseSyntax>();
        foreach (var typeArgument in methodSymbol.TypeArguments)
        {
            var typeParameter = SyntaxFactory.TypeParameter(
                SyntaxFactory.Identifier(typeArgument.Name));

            // Find the type parameter symbol for the type argument
            var typeParameterSymbol = methodSymbol.TypeParameters
                .FirstOrDefault(x => x.Name == typeArgument.Name);

            if (typeParameterSymbol != null && typeParameterSymbol.ConstraintTypes.Any())
            {
                // Add constraint clauses for the type parameter
                var constraints = new List<TypeConstraintSyntax>();
                foreach (var constraint in typeParameterSymbol.ConstraintTypes)
                {
                    constraints.Add(SyntaxFactory.TypeConstraint(SyntaxFactory.ParseTypeName(constraint.ToDisplayString())));
                }

                var constraintClause = SyntaxFactory.TypeParameterConstraintClause(
                                           SyntaxFactory.IdentifierName(typeArgument.Name),
                                           SyntaxFactory.SeparatedList<TypeParameterConstraintSyntax>(constraints));

                constraintClauses.Add(constraintClause);
            }

            typeParameters.Add(typeParameter);
        }
        return (SyntaxFactory.SeparatedList(typeParameters), SyntaxFactory.List(constraintClauses));
    }

    internal static MemberDeclarationSyntax CreateProperty(IPropertySymbol propertySymbol, Dictionary<string, string> interfaceNamespaceMap)
    {
        var accessors = new List<AccessorDeclarationSyntax> {
                                SyntaxFactory.AccessorDeclaration(
                                    SyntaxKind.GetAccessorDeclaration)
                                .WithSemicolonToken(
                                    SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                                };

        if (propertySymbol.SetMethod != null)
        {
            accessors.Add(
                SyntaxFactory.AccessorDeclaration(
                    SyntaxKind.SetAccessorDeclaration)
                .WithSemicolonToken(
                    SyntaxFactory.Token(SyntaxKind.SemicolonToken)));
        }

        var returnTypeName = GetTypeName(propertySymbol.Type, interfaceNamespaceMap);

        return SyntaxFactory.PropertyDeclaration(
            SyntaxFactory.ParseTypeName(returnTypeName),
            SyntaxFactory.Identifier(propertySymbol.Name))
            .WithAccessorList(SyntaxFactory.AccessorList(SyntaxFactory.List(accessors)));
    }
}
