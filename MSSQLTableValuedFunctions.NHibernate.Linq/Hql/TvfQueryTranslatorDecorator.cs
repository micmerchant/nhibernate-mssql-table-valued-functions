using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using NHibernate;
using NHibernate.Engine;
using NHibernate.Engine.Query;
using NHibernate.Event;
using NHibernate.Hql;
using NHibernate.Hql.Ast.ANTLR;
using NHibernate.Loader;
using NHibernate.Param;
using NHibernate.Type;

namespace MSSQLTableValuedFunctions.NHibernate.Linq.Hql;

/// <summary>
/// Query translator decorator to support MSSQL Table-Valued Functions.
/// </summary>
internal sealed class TvfQueryTranslatorDecorator: IFilterTranslator
{
    private readonly QueryTranslatorImpl _decoratee;
    private readonly IEnumerable<NamedParameter> _tvfParameters;

    public TvfQueryTranslatorDecorator(QueryTranslatorImpl translator,
                                       ISessionFactoryImplementor factory,
                                       IEnumerable<NamedParameter> tvfParameters)
    {
        Ensure.That(translator).IsNotNull();
        Ensure.That(factory).IsNotNull();
        Ensure.That(tvfParameters).IsNotNull();
        
        _decoratee = translator;
        _tvfParameters = tvfParameters;
    }

    public Task<IList> ListAsync(ISessionImplementor session, QueryParameters queryParameters, CancellationToken cancellationToken)
    {
        return _decoratee.ListAsync(session, queryParameters, cancellationToken);
    }

    public Task<IEnumerable> GetEnumerableAsync(QueryParameters queryParameters, IEventSource session, CancellationToken cancellationToken)
    {
        return _decoratee.GetEnumerableAsync(queryParameters, session, cancellationToken);
    }

    public Task<int> ExecuteUpdateAsync(QueryParameters queryParameters, ISessionImplementor session,
                                        CancellationToken cancellationToken)
    {
        return _decoratee.ExecuteUpdateAsync(queryParameters, session, cancellationToken);
    }

    public IList List(ISessionImplementor session, QueryParameters queryParameters)
    {
        return _decoratee.List(session, queryParameters);
    }

    public IEnumerable GetEnumerable(QueryParameters queryParameters, IEventSource session)
    {
        return _decoratee.GetEnumerable(queryParameters, session);
    }

    public int ExecuteUpdate(QueryParameters queryParameters, ISessionImplementor session)
    {
        return _decoratee.ExecuteUpdate(queryParameters, session);
    }

    public string[][] GetColumnNames()
    {
        return _decoratee.GetColumnNames();
    }

    public ParameterMetadata BuildParameterMetadata()
    {
        var parameterMetadata = _decoratee.BuildParameterMetadata();
        return ExpandParameterMetadata(parameterMetadata);
    }

    public ISet<string> QuerySpaces => _decoratee.QuerySpaces;

    public string SQLString => _decoratee.SQLString;

    public IList<string> CollectSqlStrings => _decoratee.CollectSqlStrings;

    public string QueryString => _decoratee.QueryString;

    public IDictionary<string, IFilter> EnabledFilters => _decoratee.EnabledFilters;

    public IType[] ReturnTypes => _decoratee.ReturnTypes;

    public string[] ReturnAliases => _decoratee.ReturnAliases;

    public bool ContainsCollectionFetches => _decoratee.ContainsCollectionFetches;

    public bool IsManipulationStatement => _decoratee.IsManipulationStatement;

#pragma warning disable CS0618
    public Loader Loader => _decoratee.Loader;
#pragma warning restore CS0618

    public IType[] ActualReturnTypes => _decoratee.ActualReturnTypes;

    public void Compile(IDictionary<string, string> replacements, bool shallow)
    {
        _decoratee.Compile(replacements, shallow);
    }

    public void Compile(string collectionRole, IDictionary<string, string> replacements, bool shallow)
    {
        _decoratee.Compile(collectionRole, replacements, shallow);
    }
    
    /// <summary>
    /// Expands the parameter meta data with the MSSQL Table-Valued parameters provided by the query expression.
    /// </summary>
    /// <param name="parameterMetadata"></param>
    /// <returns></returns>
    private ParameterMetadata ExpandParameterMetadata(ParameterMetadata parameterMetadata)
    {
        int ordinalParameterCount = parameterMetadata.OrdinalParameterCount;

        var ordinalParameterDescriptors = new List<OrdinalParameterDescriptor>();
        for(int i = 1; i <= ordinalParameterCount; i++)
        {
            var ordinalParameterDescriptor = parameterMetadata.GetOrdinalParameterDescriptor(i);
            ordinalParameterDescriptors.Add(ordinalParameterDescriptor);
        }

        Dictionary<string, NamedParameterDescriptor> namedParameterDescriptors = new();
        foreach(string parameterName in parameterMetadata.NamedParameterNames)
        {
            var namedParameterDescriptor = parameterMetadata.GetNamedParameterDescriptor(parameterName);
            namedParameterDescriptors.Add(namedParameterDescriptor.Name, namedParameterDescriptor);
        }
        
        // add Table-Valued Parameters
        foreach(NamedParameter tvfParameter in _tvfParameters)
        {
            if(namedParameterDescriptors.ContainsKey(tvfParameter.Name))
            {
                // parameter is already added
                continue;
            }
            
            namedParameterDescriptors.Add(tvfParameter.Name, new NamedParameterDescriptor(tvfParameter.Name,
                                                                                          tvfParameter.Type,
                                                                                          false));
        }
        
        return new ParameterMetadata(ordinalParameterDescriptors,
                                     namedParameterDescriptors);
    }
}