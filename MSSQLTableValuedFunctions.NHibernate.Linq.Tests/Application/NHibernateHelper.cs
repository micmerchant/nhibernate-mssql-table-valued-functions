using MSSQLTableValuedFunctions.NHibernate.Linq.Tests.Tests.SimpleTableValuedFunction;
using NHibernate;
using NHibernate.Cfg;

namespace MSSQLTableValuedFunctions.NHibernate.Linq.Tests.Application;

/// <summary>
/// Session factory helper class.
/// </summary>
public static class NHibernateHelper
{
    private static ISessionFactory? _sessionFactory;
    private static ISessionFactory SessionFactory
    {
        get
        {
            if(_sessionFactory == null)
            {
                var configuration = new Configuration();
                configuration.Configure();
                configuration.AddAssembly(typeof(Date).Assembly);
                _sessionFactory = configuration.BuildSessionFactory();
            }
            return _sessionFactory;
        }
    }
 

    public static ISession OpenSession()
    {
        return SessionFactory.OpenSession();
    }
}