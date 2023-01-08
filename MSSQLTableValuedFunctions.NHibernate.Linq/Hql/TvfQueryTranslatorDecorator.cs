using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using NHibernate;
using NHibernate.Engine;
using NHibernate.Engine.Query;
using NHibernate.Event;
using NHibernate.Hql;
using NHibernate.Hql.Ast.ANTLR;
using NHibernate.Linq;
using NHibernate.Loader;
using NHibernate.Type;

namespace MSSQLTableValuedFunctions.NHibernate.Linq.Hql;

/// <summary>
/// Query translator decorator to support MSSQL Table-Valued Functions.
/// </summary>
internal sealed class TvfQueryTranslatorDecorator: IFilterTranslator
{
    private static readonly Type DecoratedTranslatorType = typeof(QueryTranslatorImpl);

    private readonly QueryTranslatorImpl _decoratee;
    private readonly ISessionFactoryImplementor _factory;
    private readonly NhLinqExpression? _linqExpression;

    private TvfQueryLoader? _decoratedQueryLoader;
    
    public TvfQueryTranslatorDecorator(QueryTranslatorImpl translator,
                                       ISessionFactoryImplementor factory,
                                       NhLinqExpression? linqExpression)
    {
        Ensure.That(translator).IsNotNull();
        Ensure.That(factory).IsNotNull();
        
        _decoratee = translator;
        _factory = factory;
        _linqExpression = linqExpression;
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
        var parameterMetaData = _decoratee.BuildParameterMetadata();
        var expandedParameterMetaData = ExpandParameterMetadata(parameterMetaData);

        return expandedParameterMetaData;
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

    public Loader Loader => _decoratee.Loader;

    public IType[] ActualReturnTypes => _decoratee.ActualReturnTypes;

    public void Compile(IDictionary<string, string> replacements, bool shallow)
    {
        _decoratee.Compile(replacements, shallow);

        if(_decoratee.SqlAST.NeedsExecutor)
        {
            return;
        }
        
        _decoratedQueryLoader = new TvfQueryLoader(_decoratee,
                                                   _factory,
                                                   _decoratee.SqlAST.Walker.SelectClause);
        ReplaceQueryLoader(_decoratee, _decoratedQueryLoader);
    }

    public void Compile(string collectionRole, IDictionary<string, string> replacements, bool shallow)
    {
        _decoratee.Compile(collectionRole, replacements, shallow);
        
        if(_decoratee.SqlAST.NeedsExecutor)
        {
            return;
        }
        
        _decoratedQueryLoader = new TvfQueryLoader(_decoratee,
                                                   _factory,
                                                   _decoratee.SqlAST.Walker.SelectClause);
        ReplaceQueryLoader(_decoratee, _decoratedQueryLoader);
    }
    
    
    /// <summary>
    /// Replaces the current QueryLoader of the <see cref="QueryTranslatorImpl"/> with a <see cref="TvfQueryLoader"/>.
    ///
    /// The QueryLoader is currently not injectable or public accessible so it's replaced via reflection.
    /// </summary>
    /// <param name="translator"></param>
    /// <param name="decoratedQueryLoader"></param>
    private void ReplaceQueryLoader(QueryTranslatorImpl translator,
                                    TvfQueryLoader decoratedQueryLoader)
    {
        var queryLoaderField = DecoratedTranslatorType.GetField("_queryLoader",
                                                                BindingFlags.Instance | BindingFlags.NonPublic)!;
        
        queryLoaderField.SetValue(translator, decoratedQueryLoader);
    }
    
    private ParameterMetadata ExpandParameterMetadata(ParameterMetadata parameterMetadata)
    {
        int ordinalParameterCount = parameterMetadata.OrdinalParameterCount;
    
        var ordinalParameterDescriptors = new List<OrdinalParameterDescriptor>();
        for(int i = 0; i < ordinalParameterCount; i++)
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

        if(_linqExpression == null)
        {
            return new ParameterMetadata(ordinalParameterDescriptors,
                                         namedParameterDescriptors);
        }
            
        foreach(NamedParameterDescriptor parameterDescriptor in _linqExpression.ParameterDescriptors)
        {
            if(namedParameterDescriptors.ContainsKey(parameterDescriptor.Name))
            {
                // parameter is already added
                continue;
            }
    
            if(parameterDescriptor.ExpectedType == null)
            {
                // only evaluated parameters can be added
                continue;
            }
            
            namedParameterDescriptors.Add(parameterDescriptor.Name, parameterDescriptor);
        }
        
        return new ParameterMetadata(ordinalParameterDescriptors,
                                     namedParameterDescriptors);
    }
}