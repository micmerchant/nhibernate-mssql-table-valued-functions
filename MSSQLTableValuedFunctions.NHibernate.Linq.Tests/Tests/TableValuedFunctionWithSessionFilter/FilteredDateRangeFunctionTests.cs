using FluentAssertions;
using MSSQLTableValuedFunctions.NHibernate.Linq.ExtensionMethods;
using MSSQLTableValuedFunctions.NHibernate.Linq.Tests.Application;
using NHibernate;

namespace MSSQLTableValuedFunctions.NHibernate.Linq.Tests.Tests.TableValuedFunctionWithSessionFilter;

[TestFixture(Description = "Tests the DateRange Table-Valued-Function with a session filter.")]
internal sealed class FilteredDateRangeFunctionTests
{
    [Test]
    public void FullDateRangeTest()
    {
        using(ISession session = NHibernateHelper.OpenSession())
            using(ITransaction transaction = session.BeginTransaction())
            {
                session.EnableFilter("DateFilter")
                       .SetParameter("ForDateParameter", DateTime.Today);
                
                var dateRange = session.Query<DateFiltered>()
                                       .SetParameter("startDate", DateTime.Today.AddDays(-7))
                                       .SetParameter("endDate", DateTime.Today.AddDays(7))
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
                session.EnableFilter("DateFilter")
                       .SetParameter("ForDateParameter", DateTime.Today);
                
                var dateRange = session.Query<DateFiltered>()
                                       .SetParameter("startDate", DateTime.Today.AddDays(-7))
                                       .SetParameter("endDate", DateTime.Today.AddDays(7))
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
                session.EnableFilter("DateFilter")
                       .SetParameter("ForDateParameter", DateTime.Today);
                
                var dates = new List<DateTime> { DateTime.Today.AddDays(1), DateTime.Today.AddDays(2) };
                var dateRange = session.Query<DateFiltered>()
                                       .SetParameter("startDate", DateTime.Today.AddDays(-7))
                                       .SetParameter("endDate", DateTime.Today.AddDays(7))
                                       .Where(dr => dates.Contains(dr.ForDate))
                                       .ToList();

                transaction.Rollback();
                
                dateRange.Should().HaveCount(2);
            }
    }
}