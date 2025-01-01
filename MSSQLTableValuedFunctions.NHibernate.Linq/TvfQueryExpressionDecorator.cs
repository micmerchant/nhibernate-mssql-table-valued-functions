using System;
using System.Collections.Generic;
using System.Linq;
using EnsureThat;
using NHibernate.Engine;
using NHibernate.Engine.Query;
using NHibernate.Hql.Ast.ANTLR.Tree;
using NHibernate.Linq;
using NHibernate.Param;
using NHibernate.Type;

namespace MSSQLTableValuedFunctions.NHibernate.Linq;

/// <summary>
/// Query expression decorator to support MSSQL Table-Valued Functions.
/// </summary>
internal sealed class TvfQueryExpressionDecorator: ILinqQueryExpression
{
    private class NamedParameterDescriptorEqualityComparer : IEqualityComparer<NamedParameterDescriptor>
    {
        public bool Equals(NamedParameterDescriptor? left, NamedParameterDescriptor? right)
        {
            if(right is null && left is null)
            {
                return true;
            }

            if(left == null || right == null)
            {
                return false;
            }

            if(left.Name == right.Name &&
               Equals(left.ExpectedType, right.ExpectedType) &&
               left.JpaStyle == right.JpaStyle)
            {
                return true;
            }

            return false;
        }

        public int GetHashCode(NamedParameterDescriptor p)
        {
            unchecked
            {
                var hashCode = p.Name.GetHashCode();
                hashCode = (hashCode * 397) ^ (p.ExpectedType != null ? p.ExpectedType.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ p.JpaStyle.GetHashCode();
                return hashCode;
            }
        }
    }
    
    internal NhLinqExpression Decoratee { get; }
    internal List<NamedParameter> TvfParameters { get; }
    
    public string Key => Decoratee.Key;
    public Type Type => Decoratee.Type;
    public IList<NamedParameterDescriptor> ParameterDescriptors => GetParameterDescriptors();
    public ExpressionToHqlTranslationResults ExpressionToHqlTranslationResults => Decoratee.ExpressionToHqlTranslationResults;
    
    /// <summary>
    /// Used for parameter/query verification after query preparation.
    /// </summary>
    public IDictionary<string, Tuple<object, IType>> ParameterValuesByName => GetParameterValuesByName();
    
    /// <summary>
    /// Used for parameter/query verification after query preparation.
    /// </summary>
    public IDictionary<string, NamedParameter> NamedParameters => GetNamedParameters();

    public TvfQueryExpressionDecorator(NhLinqExpression queryExpression, 
                                       IEnumerable<NamedParameter> tvfParameters)
    {
        Ensure.That(queryExpression).IsNotNull();
        Ensure.That(tvfParameters).IsNotNull();
        
        Decoratee = queryExpression;
        TvfParameters = tvfParameters.ToList();
        
        Ensure.That(TvfParameters).HasItems();
    }

    public IASTNode Translate(ISessionFactoryImplementor sessionFactory, bool filter)
    {
        return Decoratee.Translate(sessionFactory, filter);
    }

    public IDictionary<string, Tuple<IType, bool>> GetNamedParameterTypes()
    {
        return Decoratee.GetNamedParameterTypes();
    }

    public void CopyExpressionTranslation(NhLinqExpressionCache cache)
    {
        Decoratee.CopyExpressionTranslation(cache);
    }

    private IList<NamedParameterDescriptor> GetParameterDescriptors()
    {
        var parameters = TvfParameters.Select(p => new NamedParameterDescriptor(p.Name,
                                                                                p.Type,
                                                                                false))
                                      .Union(Decoratee.ParameterDescriptors, new NamedParameterDescriptorEqualityComparer())
                                      .ToList();

        return parameters;
    }

    private IDictionary<string, Tuple<object, IType>> GetParameterValuesByName()
    {
        var parameters = Decoratee.ParameterValuesByName
                                  .Union(TvfParameters.Select(p => new KeyValuePair<string, Tuple<object, IType>>(p.Name,
                                                                                                                  Tuple.Create(p.Value, p.Type))))
                                  .ToDictionary(p => p.Key,
                                                p => p.Value);

        return parameters;
    }

    private IDictionary<string, NamedParameter> GetNamedParameters()
    {
        var parameters = (Decoratee.NamedParameters ?? new Dictionary<string, NamedParameter>())
                         .Union(TvfParameters.Select(p => new KeyValuePair<string, NamedParameter>(p.Name, p)))
                         .ToDictionary(p => p.Key,
                                       p => p.Value);

        return parameters;
    }
}