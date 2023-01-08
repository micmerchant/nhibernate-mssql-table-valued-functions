using System.Collections.Generic;
using EnsureThat;
using MSSQLTableValuedFunctions.NHibernate.Linq.ExtensionMethods;
using NHibernate;
using NHibernate.Engine;
using NHibernate.Hql;
using NHibernate.Hql.Ast.ANTLR;
using NHibernate.Hql.Ast.ANTLR.Tree;
using NHibernate.Linq;

namespace MSSQLTableValuedFunctions.NHibernate.Linq.Hql;

/// <summary>
/// Creates query translators which support MSSQL Table-Valued Functions.
/// </summary>
public sealed class TvfQueryTranslatorFactory: IQueryTranslatorFactory
{
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
        var queryTranslator = new QueryTranslatorImpl(queryIdentifier, parser, filters, factory);
        var translator = new TvfQueryTranslatorDecorator(queryTranslator,
                                                         factory,
                                                         linqExpression);

        return translator;
    }
}