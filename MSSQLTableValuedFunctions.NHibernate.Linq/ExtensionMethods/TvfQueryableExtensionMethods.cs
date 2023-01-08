using System;
using System.Linq;
using EnsureThat;

namespace MSSQLTableValuedFunctions.NHibernate.Linq.ExtensionMethods;

/// <summary>
/// Provides queryable extensions to support MSSQL Table-Valued Functions.
/// </summary>
public static class TvfQueryableExtensionMethods
{
    /// <summary>
    /// Sets a MSSQL Table-Valued parameter.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="name"></param>
    /// <param name="value"></param>
    /// <typeparam name="TSource"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotSupportedException"></exception>
    /// <exception cref="ArgumentNullException"></exception>
    public static IQueryable<TSource> SetParameter<TSource, TValue>(this IQueryable<TSource> source,
                                                                    string name,
                                                                    TValue value)
    {
        Ensure.That(source).IsNotNull();
        Ensure.That(source.Provider).IsNotNull();
        Ensure.That(name).IsNotNullOrEmpty();
        
        if(source.Provider is not TvfQueryProvider provider)
        {
            throw new NotSupportedException($"Source {nameof(source.Provider)} must be a {nameof(TvfQueryProvider)}");
        }
        
        if(string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentNullException(nameof(name));
        }

        provider.SetParameter(name, value);

        return source;
    }
}