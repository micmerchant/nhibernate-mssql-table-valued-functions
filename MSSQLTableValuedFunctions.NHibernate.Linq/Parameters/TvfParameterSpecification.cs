using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using NHibernate.Engine;
using NHibernate.Param;
using NHibernate.SqlCommand;
using NHibernate.Type;

namespace MSSQLTableValuedFunctions.NHibernate.Linq.Parameters;

/// <summary>
/// Parameter specification for MSSQL Table-Valued-Function parameters.
/// </summary>
internal sealed class TvfParameterSpecification: IParameterSpecification
{
    private const string TvfParameterIdTemplate = "<tvfp-{0}_span{1}>";
    private const string TvfParameterDisplayTemplate = "table-valued-parameter={0}";
    
    private readonly string _parameterName;
    private readonly object _parameterValue;
    
    public IType ExpectedType { get; set; }

    public TvfParameterSpecification(string name,
                                     object value,
                                     IType expectedType)
    {
        Ensure.That(name).IsNotNull();
        Ensure.That(expectedType).IsNotNull();
        
        _parameterName = name;
        _parameterValue = value;
        ExpectedType = expectedType;
    }

    public Task BindAsync(DbCommand command, 
                          IList<Parameter> sqlQueryParametersList, 
                          QueryParameters queryParameters,
                          ISessionImplementor session, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }
        
        return BindAsync(command, sqlQueryParametersList, 0, sqlQueryParametersList, queryParameters, session, cancellationToken);
    }

    
    public async Task BindAsync(DbCommand command, 
                                IList<Parameter> multiSqlQueryParametersList, 
                                int singleSqlParametersOffset,
                                IList<Parameter> sqlQueryParametersList, 
                                QueryParameters queryParameters, 
                                ISessionImplementor session,
                                CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string backTrackId = GetIdsForBackTrack(session.Factory).First();

        // the same filterName-parameterName can appear more than once in the whole query
        foreach (int position in multiSqlQueryParametersList.GetEffectiveParameterLocations(backTrackId))
        {
            await ExpectedType.NullSafeSetAsync(command, 
                                                _parameterValue, 
                                                position, session, 
                                                cancellationToken)
                              .ConfigureAwait(false);
        }
    }
    

    public void Bind(DbCommand command, 
                     IList<Parameter> sqlQueryParametersList,
                     QueryParameters queryParameters,
                     ISessionImplementor session)
    {
        Bind(command, sqlQueryParametersList, 0, sqlQueryParametersList, queryParameters, session);
    }
    

    public void Bind(DbCommand command,
                     IList<Parameter> multiSqlQueryParametersList, 
                     int singleSqlParametersOffset,
                     IList<Parameter> sqlQueryParametersList,
                     QueryParameters queryParameters,
                     ISessionImplementor session)
    {
        string backTrackId = GetIdsForBackTrack(session.Factory).First();
        
        // the same filterName-parameterName can appear more than once in the whole query
        foreach (int position in multiSqlQueryParametersList.GetEffectiveParameterLocations(backTrackId))
        {
            ExpectedType.NullSafeSet(command,
                                     _parameterValue,
                                     position,
                                     session);
        }
    }
    

    public string RenderDisplayInfo()
    {
        return string.Format(TvfParameterDisplayTemplate, _parameterName);
    }
    

    public IEnumerable<string> GetIdsForBackTrack(IMapping sessionFactory)
    {
        yield return string.Format(TvfParameterIdTemplate, _parameterName, 0);
    }
}