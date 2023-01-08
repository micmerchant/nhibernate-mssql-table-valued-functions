using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using MSSQLTableValuedFunctions.NHibernate.Linq.ExtensionMethods;
using NHibernate;
using NHibernate.Engine;
using NHibernate.Linq;
using NHibernate.Param;

namespace MSSQLTableValuedFunctions.NHibernate.Linq;

/// <summary>
/// Query provider to support MSSQL Table-Valued Functions.
/// </summary>
// ReSharper disable once ClassNeverInstantiated.Global
public sealed class TvfQueryProvider: DefaultQueryProvider
{
    private readonly Dictionary<string, NamedParameter> _tvfParameters = new();
    
    public TvfQueryProvider(ISessionImplementor session)
        : base(session)
    {
    }

    public TvfQueryProvider(ISessionImplementor session, object collection)
        :base(session, collection)
    {
        
    }
    
    private TvfQueryProvider(ISessionImplementor session, 
                             object collection, 
                             NhQueryableOptions options)
        : base(session, collection, options)
    {
    }


    /// <summary>
    /// Sets a query parameter.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="value"></param>
    /// <typeparam name="TValue"></typeparam>
    internal void SetParameter<TValue>(string name, 
                                       TValue value)
    {
        if(_tvfParameters.TryGetValue(name, out NamedParameter? existingParameter))
        {
            if(existingParameter.Value.Equals(value))
            {
                return;
            }

            throw new ArgumentException($"Table-Valued Function parameter '{name}' already registered with different value '{existingParameter.Value}'.");
        }
        
        _tvfParameters.Add(name,
                           new NamedParameter(name,
                                              value,
                                              NHibernateUtil.GuessType(typeof(TValue))));
    }


    protected override NhLinqExpression PrepareQuery(Expression expression, out IQuery query)
    {
        if(!_tvfParameters.Any())
        {
            return base.PrepareQuery(expression, out query);
        }

        var nhLinqExpression = new NhLinqExpression(expression, Session.Factory);
        nhLinqExpression.ExpandNamedParameters(_tvfParameters.Values);

        if (Collection == null)
        {
            query = Session.CreateQuery(nhLinqExpression);
        }
        else
        {
            query = Session.CreateFilter(Collection, nhLinqExpression);
        }

        var namedParameters = nhLinqExpression.GetNamedParameters();
        foreach (var parameterName in query.NamedParameters)
        {
            var parameter = namedParameters[parameterName];
            
            if (parameter.IsCollection)
            {
                query.SetParameterList(parameter.Name, (IEnumerable) parameter.Value);
            }
            else
            {
                query.SetParameter(parameter.Name, parameter.Value);
            }
        }
        
        SetResultTransformerAndAdditionalCriteria(query, nhLinqExpression, nhLinqExpression.ParameterValuesByName);

        return nhLinqExpression;
    }
    
    protected override IQueryProvider CreateWithOptions(NhQueryableOptions options)
    {
        return new TvfQueryProvider(Session, Collection, options);
    }
}