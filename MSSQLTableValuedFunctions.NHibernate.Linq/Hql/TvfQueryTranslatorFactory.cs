using System;
using System.Collections.Generic;
using System.Linq;
using EnsureThat;
using MSSQLTableValuedFunctions.NHibernate.Linq.ExtensionMethods;
using NHibernate;
using NHibernate.Engine;
using NHibernate.Hql;
using NHibernate.Hql.Ast.ANTLR;
using NHibernate.Hql.Ast.ANTLR.Tree;
using NHibernate.Linq;
using NHibernate.Param;
using NHibernate.Type;
using BindingFlags = System.Reflection.BindingFlags;

namespace MSSQLTableValuedFunctions.NHibernate.Linq.Hql;

/// <summary>
/// Creates query translators which support MSSQL Table-Valued Functions.
/// </summary>
public sealed class TvfQueryTranslatorFactory: IQueryTranslatorFactory
{
    private static readonly Type QueryTranslatorImplType = typeof(QueryTranslatorImpl);
    
    public IQueryTranslator[] CreateQueryTranslators(IQueryExpression queryExpression,
                                                     string? collectionRole,
                                                     bool shallow,
                                                     IDictionary<string, IFilter> filters,
                                                     ISessionFactoryImplementor factory)
    {
        Ensure.That(queryExpression).IsNotNull();
        Ensure.That(factory).IsNotNull();

        var astNode = queryExpression.Translate(factory, collectionRole != null);
        if(queryExpression is NhLinqExpression linqExpression)
        {
            var namedParameters = linqExpression.GetNamedParameters();
            linqExpression.ExpandParameterDescriptors(namedParameters.Values);
        }

        return CreateQueryTranslators(queryExpression, 
                                      astNode, 
                                      queryExpression.Key,
                                      collectionRole,
                                      shallow, 
                                      filters, 
                                      factory);
    }

    
    static IQueryTranslator[] CreateQueryTranslators(IQueryExpression queryExpression,
                                                     IASTNode ast,
                                                     string queryIdentifier,
                                                     string? collectionRole,
                                                     bool shallow,
                                                     IDictionary<string, IFilter> filters,
                                                     ISessionFactoryImplementor factory)
    {
        var polymorphicParsers = AstPolymorphicProcessor.Process(ast, factory);

        var linqExpression = queryExpression as NhLinqExpression;

        IQueryTranslator[] translators = new IQueryTranslator[polymorphicParsers.Length];
        for(int i = 0; i < polymorphicParsers.Length; i++)
        {
            var parser = polymorphicParsers[i];

            TvfQueryTranslatorDecorator translator = CreateTranslator(queryIdentifier, 
                                                                      filters, 
                                                                      factory,
                                                                      parser,
                                                                      linqExpression);
            if (collectionRole == null)
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
    /// Creates a <see cref="TvfQueryTranslatorDecorator"/>.
    /// </summary>
    /// <param name="queryIdentifier"></param>
    /// <param name="filters"></param>
    /// <param name="factory"></param>
    /// <param name="parser"></param>
    /// <param name="linqExpression"></param>
    /// <returns></returns>
    private static TvfQueryTranslatorDecorator CreateTranslator(string queryIdentifier,
                                                                IDictionary<string, IFilter> filters,
                                                                ISessionFactoryImplementor factory, 
                                                                IASTNode parser,
                                                                NhLinqExpression? linqExpression)
    {
        var queryTranslator = CreateQueryTranslator(queryIdentifier,
                                                    filters,
                                                    factory,
                                                    parser,
                                                    linqExpression);
        var translator = new TvfQueryTranslatorDecorator(queryTranslator,
                                                         factory,
                                                         linqExpression);

        return translator;
    }

    /// <summary>
    /// Creates a QueryTranslatorImpl and ensures that the named parameters field is properly set when needed.
    /// </summary>
    /// <param name="queryIdentifier"></param>
    /// <param name="filters"></param>
    /// <param name="factory"></param>
    /// <param name="parser"></param>
    /// <param name="linqExpression"></param>
    /// <returns></returns>
    private static QueryTranslatorImpl CreateQueryTranslator(string queryIdentifier,
                                                             IDictionary<string, IFilter> filters,
                                                             ISessionFactoryImplementor factory, 
                                                             IASTNode parser,
                                                             NhLinqExpression? linqExpression)
    {
        if(linqExpression != null)
        {
            IDictionary<string, NamedParameter> namedParameters = linqExpression.GetNamedParameters();
            return CreateQueryTranslator(queryIdentifier, filters, factory, parser, namedParameters);
        }
        
        return CreateQueryTranslator(queryIdentifier, filters, factory, parser);
    }

    private static QueryTranslatorImpl CreateQueryTranslator(string queryIdentifier,
                                                             IDictionary<string, IFilter> filters,
                                                             ISessionFactoryImplementor factory, 
                                                             IASTNode parser)
    {
        return new QueryTranslatorImpl(queryIdentifier, parser, filters, factory);
    }
    
    private static QueryTranslatorImpl CreateQueryTranslator(string queryIdentifier,
                                                             IDictionary<string, IFilter> filters,
                                                             ISessionFactoryImplementor factory, 
                                                             IASTNode parser,
                                                             IDictionary<string, NamedParameter> namedParameters)
    {
        IDictionary<string, Tuple<IType, bool>> convertedNamedParameters = namedParameters.Values
                                                                                          .Distinct()
                                                                                          .ToDictionary(p => p.Name, 
                                                                                                        p => Tuple.Create(p.Type, true));
        var constructorInfo = QueryTranslatorImplType.GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance,
                                                                     null,
                                                                     new []
                                                                     {
                                                                         queryIdentifier.GetType(),
                                                                         parser.GetType(),
                                                                         filters.GetType(),
                                                                         factory.GetType(),
                                                                         convertedNamedParameters.GetType()
                                                                     },
                                                                     null)!;

        var constructed = constructorInfo.Invoke(new object []
                                                 {
                                                     queryIdentifier,
                                                     parser,
                                                     filters,
                                                     factory,
                                                     convertedNamedParameters
                                                 });

        var queryTranslator = (constructed as QueryTranslatorImpl)!;
        return queryTranslator;
    }
}