using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NHibernate.Engine.Query;
using NHibernate.Linq;
using NHibernate.Param;

namespace MSSQLTableValuedFunctions.NHibernate.Linq.ExtensionMethods;

/// <summary>
/// Provides nhibernate linq expression extensions to support parameter handling via reflection.
/// </summary>
internal static class NhLinqExpressionExtensions
{
    private static readonly Type NhLinqExpressionType = typeof(NhLinqExpression);
    
    /// <summary>
    /// Gets the value of the NamedParameters property of the given linq expression.
    ///
    /// The NamedParameters are currently not public accessible so they are resolved via reflection.
    /// </summary>
    /// <param name="linqExpression"></param>
    /// <returns></returns>
    public static IDictionary<string, NamedParameter> GetNamedParameters(this NhLinqExpression linqExpression)
    {
        var namedParametersProperty = NhLinqExpressionType.GetProperty("NamedParameters",
                                                                       BindingFlags.Instance | BindingFlags.NonPublic)!;

        var  namedParametersPropertyValue = namedParametersProperty.GetValue(linqExpression)!;
        var namedParameters = (namedParametersPropertyValue as IDictionary<string, NamedParameter>)!;
        return namedParameters;
    }

    /// <summary>
    /// Expands the NamedParameters of the given linq expression with the given MSSQL Table-Valued Function parameters.
    ///
    /// The NamedParameters are currently not public accessible so they are changed via reflection.
    /// </summary>
    /// <param name="linqExpression"></param>
    /// <param name="tvfParameters"></param>
    /// <returns></returns>
    public static void ExpandNamedParameters(this NhLinqExpression linqExpression,
                                             IEnumerable<NamedParameter> tvfParameters)
    {
        var namedParametersField = NhLinqExpressionType.GetField("<NamedParameters>k__BackingField",
                                                                 BindingFlags.Instance | BindingFlags.NonPublic)!;

        var namedParametersFieldValue = namedParametersField.GetValue(linqExpression);
        var existingNamedParameters = (namedParametersFieldValue as IDictionary<string, NamedParameter>)!;
            
        var expandedNamedParameters = existingNamedParameters.Concat(tvfParameters.Select(p => new KeyValuePair<string, NamedParameter>(p.Name, p)))
                                                             .ToDictionary(p => p.Key,
                                                                           p => p.Value);
        namedParametersField.SetValue(linqExpression,
                                      expandedNamedParameters);
    }
    
    /// <summary>
    /// Expands the ParameterDescriptors of the given linq expression with the given MSSQL Table-Valued Function parameters.
    ///
    /// The ParameterDescriptors are currently not public settable so they are changed via reflection.
    /// </summary>
    /// <param name="linqExpression"></param>
    /// <param name="namedParameters"></param>
    /// <returns></returns>
    public static void ExpandParameterDescriptors(this NhLinqExpression linqExpression,
                                                  IEnumerable<NamedParameter> namedParameters)
    {
        var parameterDescriptorsProperty = NhLinqExpressionType.GetProperty(nameof(NhLinqExpression.ParameterDescriptors),
                                                                            BindingFlags.Instance | BindingFlags.Public)!;

        var parameterDescriptorsPropertyValue = parameterDescriptorsProperty.GetValue(linqExpression);
        var existingParameterDescriptors = (parameterDescriptorsPropertyValue as IList<NamedParameterDescriptor>)! ?? Array.Empty<NamedParameterDescriptor>();

        var expandedParameterDescriptors = namedParameters.Select(p => new NamedParameterDescriptor(p.Name,
                                                                                                    p.Type,
                                                                                                    false))
                                                          .Concat(existingParameterDescriptors)
                                                          .ToList();
        parameterDescriptorsProperty.SetValue(linqExpression,
                                              expandedParameterDescriptors);
    }
}