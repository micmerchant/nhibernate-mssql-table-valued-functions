using System;
using System.Reflection;
using NHibernate.Param;

namespace MSSQLTableValuedFunctions.NHibernate.Linq.ExtensionMethods;

/// <summary>
/// Provides NamedParameter extensions to support parameter handling via reflection.
/// </summary>
public static class NamedParameterExtensions
{
    private static readonly Type NamedParameterType = typeof(NamedParameter);

    /// <summary>
    /// Gets the value of the IsGuessedType property of the given named parameter.
    ///
    /// The IsGuessedType is currently not public accessible so they are resolved via reflection.
    /// </summary>
    /// <param name="namedParameter"></param>
    /// <returns></returns>
    public static bool IsGuessedType(this NamedParameter namedParameter)
    {
        var guessTypeProperty = NamedParameterType.GetProperty("IsGuessedType",
                                                               BindingFlags.Instance | BindingFlags.NonPublic)!;
        var guessTypeValue = guessTypeProperty.GetValue(namedParameter)!;
        bool guessedType = (bool)guessTypeValue;
        return guessedType;
    }
}