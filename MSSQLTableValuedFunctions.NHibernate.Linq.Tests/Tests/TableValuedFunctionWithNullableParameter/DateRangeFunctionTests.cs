using FluentAssertions;
using MSSQLTableValuedFunctions.NHibernate.Linq.ExtensionMethods;
using MSSQLTableValuedFunctions.NHibernate.Linq.Tests.Application;
using NHibernate;

namespace MSSQLTableValuedFunctions.NHibernate.Linq.Tests.Tests.TableValuedFunctionWithNullableParameter;

[TestFixture(Description = "Tests the DateRangeNullableParameter Table-Valued-Function with a nullable parameter.")]
internal sealed class DateRangeFunctionTests
{
    [Test]
    public void FullDateRangeTest()
    {
        using(ISession session = NHibernateHelper.OpenSession())
            using(ITransaction transaction = session.BeginTransaction())
            {
                var dateRange = session.Query<NullableDate>()
                                       .SetParameter("startDate", DateTime.Today)
                                       .SetParameter("endDate", DateTime.Today.AddDays(7))
                                       .SetParameter("nullableDate", (DateTime?)null)
                                       .ToList();

                transaction.Rollback();

                dateRange.Should().HaveCount(8);
            }
    }
    
    
    [Test]
    public void GreaterOrEqualFilteredDateRangeTest()
    {
        using(ISession session = NHibernateHelper.OpenSession())
            using(ITransaction transaction = session.BeginTransaction())
            {
                var dateRange = session.Query<NullableDate>()
                                       .SetParameter("startDate", DateTime.Today)
                                       .SetParameter("endDate", DateTime.Today.AddDays(7))
                                       .SetParameter("nullableDate", (DateTime?)null)
                                       .Where(dr => dr.ForDate == DateTime.Today.AddDays(3))
                                       .ToList();
                
                transaction.Rollback();
                
                dateRange.Should().HaveCount(1);
                dateRange.First().ForDate.Should().Be(DateTime.Today.AddDays(3));
            }
    }
    
    
    [Test]
    public void ContainsFilteredDateRangeTest()
    {
        using(ISession session = NHibernateHelper.OpenSession())
            using(ITransaction transaction = session.BeginTransaction())
            {
                var dates = new List<DateTime> { DateTime.Today.AddDays(1), DateTime.Today.AddDays(2) };
                var dateRange = session.Query<NullableDate>()
                                       .SetParameter("startDate", DateTime.Today)
                                       .SetParameter("endDate", DateTime.Today.AddDays(7))
                                       .SetParameter("nullableDate", (DateTime?)null)
                                       .Where(dr => dates.Contains(dr.ForDate))
                                       .ToList();

                transaction.Rollback();
                
                dateRange.Should().HaveCount(2);
            }
    }
}