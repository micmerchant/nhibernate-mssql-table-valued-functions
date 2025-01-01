using System;
using System.Collections.Generic;
using System.Linq;
using MSSQLTableValuedFunctions.NHibernate.Linq.Parameters;
using NHibernate.Dialect;
using NHibernate.Engine;
using NHibernate.Hql;
using NHibernate.Hql.Ast.ANTLR;
using NHibernate.Hql.Ast.ANTLR.Tree;
using NHibernate.Loader.Hql;
using NHibernate.Param;
using NHibernate.SqlCommand;
using NHibernate.Util;

namespace MSSQLTableValuedFunctions.NHibernate.Linq.Hql;

/// <summary>
/// QueryLoader to support MSSQL Table-Valued Functions.
/// </summary>
public sealed class TvfQueryLoader: QueryLoader
{
	public TvfQueryLoader(QueryTranslatorImpl queryTranslator,
	                      ISessionFactoryImplementor factory,
	                      SelectClause selectClause)
        :base(queryTranslator, factory, selectClause)
	{

	}
    
    /// <summary>
    /// Overrides the CreateSqlCommand method from <see cref="QueryLoader"/> to replace the Table-Valued Function
    /// parameters in a post step.
    ///
    /// Returns a new sql command.
    /// </summary>
    /// <param name="queryParameters"></param>
    /// <param name="session"></param>
    /// <returns></returns>
    public override ISqlCommand CreateSqlCommand(QueryParameters queryParameters, ISessionImplementor session)
    {
        // A distinct-copy of parameter specifications collected during query construction
        var parameterSpecs = GetParameterSpecifications().ToList();
        SqlString sqlString = SqlString.Copy();

        sqlString = ExpandTableValuedFunctionParameters(sqlString, 
                                                        parameterSpecs,
                                                        queryParameters,
                                                        session);
        
        // dynamic-filter parameters: during the creation of the SqlString of allLoader implementation, filters can be added as SQL_TOKEN/string for this reason we have to re-parse the SQL.
        sqlString = ExpandDynamicFilterParameters(sqlString, parameterSpecs, session);
        AdjustQueryParametersForSubSelectFetching(sqlString, parameterSpecs, queryParameters);

        // Add limits
        sqlString = AddLimitsParametersIfNeeded(sqlString, parameterSpecs, queryParameters, session);

        // The PreprocessSQL method can modify the SqlString but should never add parameters (or we have to override it)
        sqlString = PreprocessSQL(sqlString, queryParameters, session.Factory.Dialect);

        // After the last modification to the SqlString we can collect all parameters types (there are cases where we can't infer the type during the creation of the query)
        ResetEffectiveExpectedType(parameterSpecs, queryParameters);

        return new SqlCommandImpl(sqlString, parameterSpecs, queryParameters, session.Factory);
    }

    /// <summary>
    /// Expands the query parameters with the passed in MSSQL Table-Valued query parameters.
    /// </summary>
    /// <param name="sqlString"></param>
    /// <param name="parameterSpecs"></param>
    /// <param name="queryParameter"></param>
    /// <param name="session"></param>
    /// <returns></returns>
    private SqlString ExpandTableValuedFunctionParameters(SqlString sqlString,
                                                          List<IParameterSpecification> parameterSpecs,
                                                          QueryParameters queryParameter,
                                                          ISessionImplementor session)
    {
        if(!HasHqlVariable(sqlString))
        {
            return sqlString;
        }
        
        Dialect dialect = session.Factory.Dialect;
		string symbols = ParserHelper.HqlSeparators + dialect.OpenQuote + dialect.CloseQuote;

        var tvfParameterSpecifications = new List<TvfParameterSpecification>();
		var result = new SqlStringBuilder();
		foreach(var sqlPart in sqlString)
		{
			var parameter = sqlPart as Parameter;
			if(parameter != null)
			{
				result.Add(parameter);
				continue;
			}

			var sqlFragment = sqlPart.ToString();
			var tokens = new StringTokenizer(sqlFragment, symbols, true);

			foreach(string token in tokens)
			{
				if(!IsHqlVariable(token))
				{
					result.Add(token);
					continue;
				}

				string parameterName = token.Substring(1);
				if(!queryParameter.NamedParameters.TryGetValue(parameterName, out var tvfParameter))
				{
					result.Add(token);
					continue;
				}

                var tvfParameterSpecification = new TvfParameterSpecification(parameterName,
                                                                              tvfParameter.Value,
                                                                              tvfParameter.Type);
                
                var filterParameterFragment = SqlString.Parse("?");
                var parameters = filterParameterFragment.GetParameters().ToArray();
                var sqlParameterPos = 0;
                var paramTrackers = tvfParameterSpecification.GetIdsForBackTrack(session.Factory);
                foreach (var paramTracker in paramTrackers)
                {
                    parameters[sqlParameterPos++].BackTrack = paramTracker;
                }
                
                tvfParameterSpecifications.Add(tvfParameterSpecification);
				result.Add(filterParameterFragment);
			}
		}

        parameterSpecs.InsertRange(0, tvfParameterSpecifications);
        
		return result.ToSqlString();
    }

    private static bool HasHqlVariable(SqlString sqlString)
    {
	    return sqlString.IndexOf(ParserHelper.HqlVariablePrefix, 0, sqlString.Length, StringComparison.Ordinal) >= 0;
    }

    private static bool IsHqlVariable(string value)
    {
	    return value.StartsWith(ParserHelper.HqlVariablePrefix, StringComparison.Ordinal);
    }
}