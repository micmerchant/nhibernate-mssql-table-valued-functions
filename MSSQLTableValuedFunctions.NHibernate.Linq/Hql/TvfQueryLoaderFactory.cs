using NHibernate.Engine;
using NHibernate.Hql.Ast.ANTLR;
using NHibernate.Hql.Ast.ANTLR.Tree;
using NHibernate.Loader.Hql;

namespace MSSQLTableValuedFunctions.NHibernate.Linq.Hql;

/// <summary>
/// Creates query loaders which supports MSSQL Table-Valued Functions.
/// </summary>
public class TvfQueryLoaderFactory: IQueryLoaderFactory
{
    /// <summary>
    /// Creates a query loader which supports MSSQL Table-Valued Functions.
    /// </summary>
    /// <param name="queryTranslatorImpl"></param>
    /// <param name="sessionFactoryImplementor"></param>
    /// <param name="selectClause"></param>
    /// <returns></returns>
    public IQueryLoader Create(QueryTranslatorImpl queryTranslatorImpl,
                               ISessionFactoryImplementor sessionFactoryImplementor,
                               SelectClause selectClause)
    {
        return new TvfQueryLoader(queryTranslatorImpl,
                                  sessionFactoryImplementor,
                                  selectClause);
    }
}