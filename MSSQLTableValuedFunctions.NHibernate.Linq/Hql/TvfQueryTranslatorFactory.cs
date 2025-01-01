using System;
using System.Collections.Generic;
using System.Linq;
using EnsureThat;
using NHibernate;
using NHibernate.Engine;
using NHibernate.Hql;
using NHibernate.Hql.Ast.ANTLR;
using NHibernate.Hql.Ast.ANTLR.Tree;
using NHibernate.Linq;
using NHibernate.Param;
using NHibernate.Type;

namespace MSSQLTableValuedFunctions.NHibernate.Linq.Hql;

/// <summary>
/// Creates query translators which support MSSQL Table-Valued Functions.
/// </summary>
public sealed class TvfQueryTranslatorFactory : IQueryTranslatorFactory
{
    /// <summary>
    /// Creates a query translator which supports MSSQL Table-Valued Functions.
    /// </summary>
    /// <param name="queryExpression"></param>
    /// <param name="collectionRole"></param>
    /// <param name="shallow"></param>
    /// <param name="filters"></param>
    /// <param name="factory"></param>
    /// <returns></returns>
    public IQueryTranslator[] CreateQueryTranslators(IQueryExpression queryExpression,
                                                     string? collectionRole,
                                                     bool shallow,
                                                     IDictionary<string, IFilter> filters,
                                                     ISessionFactoryImplementor factory)
    {
        Ensure.That(queryExpression).IsNotNull();
        Ensure.That(factory).IsNotNull();

        return CreateQueryTranslators(queryExpression,
                                      queryExpression.Translate(factory, collectionRole != null),
                                      queryExpression.Key,
                                      collectionRole,
                                      shallow,
                                      filters,
                                      factory);
    }

    private static IQueryTranslator[] CreateQueryTranslators(IQueryExpression queryExpression,
                                                             IASTNode ast,
                                                             string queryIdentifier,
                                                             string? collectionRole,
                                                             bool shallow,
                                                             IDictionary<string, IFilter> filters,
                                                             ISessionFactoryImplementor factory)
    {
        var polymorphicParsers = AstPolymorphicProcessor.Process(ast, factory);

        IQueryTranslator[] translators = new IQueryTranslator[polymorphicParsers.Length];
        for(int i = 0; i < polymorphicParsers.Length; i++)
        {
            var parser = polymorphicParsers[i];

            IFilterTranslator translator = CreateTranslator(queryIdentifier,
                                                            filters,
                                                            factory,
                                                            parser,
                                                            queryExpression);
            if(collectionRole == null)
            {
                translator.Compile(factory.Settings.QuerySubstitutions, shallow);
            }
            else
            {
                translator.Compile(collectionRole, factory.Settings.QuerySubstitutions, shallow);
            }

            translators[i] = translator;
        }

        return translators;
    }

    /// <summary>
    /// Creates a <see cref="TvfQueryTranslatorDecorator"/> configured according to the given query expression.
    /// </summary>
    /// <param name="queryIdentifier"></param>
    /// <param name="filters"></param>
    /// <param name="factory"></param>
    /// <param name="parser"></param>
    /// <param name="queryExpression"></param>
    /// <returns></returns>
    private static TvfQueryTranslatorDecorator CreateTranslator(string queryIdentifier,
                                                                IDictionary<string, IFilter> filters,
                                                                ISessionFactoryImplementor factory,
                                                                IASTNode parser,
                                                                IQueryExpression queryExpression)
    {
        IDictionary<string, Tuple<IType, bool>> namedParameterTypes = new Dictionary<string, Tuple<IType, bool>>();
        if(queryExpression is ILinqQueryExpression linqQueryExpression)
        {
            namedParameterTypes = linqQueryExpression.GetNamedParameterTypes();
        }

        IEnumerable<NamedParameter> tvfParameters = Enumerable.Empty<NamedParameter>();
        if(queryExpression is TvfQueryExpressionDecorator tvfQueryExpression)
        {
            tvfParameters = tvfQueryExpression.TvfParameters;
        }
        
        return CreateTvfQueryTranslator(queryIdentifier,
                                        filters,
                                        factory,
                                        parser,
                                        namedParameterTypes,
                                        tvfParameters);
    }

    private static TvfQueryTranslatorDecorator CreateTvfQueryTranslator(string queryIdentifier,
                                                                        IDictionary<string, IFilter> filters,
                                                                        ISessionFactoryImplementor factory,
                                                                        IASTNode parser,
                                                                        IDictionary<string, Tuple<IType, bool>> namedParameterTypes,
                                                                        IEnumerable<NamedParameter> tvfParameters)
    {
        var queryTranslator = CreateQueryTranslator(queryIdentifier,
                                                    filters,
                                                    factory,
                                                    parser,
                                                    namedParameterTypes);

        var tvfQueryTranslator = new TvfQueryTranslatorDecorator(queryTranslator,
                                                                 factory,
                                                                 tvfParameters);

        return tvfQueryTranslator;
    }

    private static QueryTranslatorImpl CreateQueryTranslator(string queryIdentifier,
                                                             IDictionary<string, IFilter> filters,
                                                             ISessionFactoryImplementor factory,
                                                             IASTNode parser,
                                                             IDictionary<string, Tuple<IType, bool>> namedParameterTypes)
    {
        return new QueryTranslatorImpl(queryIdentifier,
                                       parser,
                                       filters,
                                       factory,
                                       new TvfQueryLoaderFactory(),
                                       namedParameterTypes);
    }
}