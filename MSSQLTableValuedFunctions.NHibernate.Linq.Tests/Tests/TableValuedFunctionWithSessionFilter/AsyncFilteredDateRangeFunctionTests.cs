using FluentAssertions;
using MSSQLTableValuedFunctions.NHibernate.Linq.ExtensionMethods;
using MSSQLTableValuedFunctions.NHibernate.Linq.Tests.Application;
using NHibernate;
using NHibernate.Linq;

namespace MSSQLTableValuedFunctions.NHibernate.Linq.Tests.Tests.TableValuedFunctionWithSessionFilter;

[TestFixture(Description = "Tests the DateRange Table-Valued-Function with a session filter.")]
internal sealed class AsyncFilteredDateRangeFunctionTests
{
    [Test]
    public async Task FullDateRangeTest()
    {
        using(ISession session = NHibernateHelper.OpenSession())
            using(ITransaction transaction = session.BeginTransaction())
            {
                session.EnableFilter("DateFilter")
                       .SetParameter("ForDateParameter", DateTime.Today);

                var dateRange = await session.Query<DateFiltered>()
                                             .SetParameter("startDate", DateTime.Today.AddDays(-7))
                                             .SetParameter("endDate", DateTime.Today.AddDays(7))
                                             .ToListAsync();

                await transaction.RollbackAsync();

                dateRange.Should().HaveCount(8);
            }
    }
    
    
    [Test]
    public async Task GreaterOrEqualFilteredDateRangeTest()
    {
        using(ISession session = NHibernateHelper.OpenSession())
            using(ITransaction transaction = session.BeginTransaction())
            {
                session.EnableFilter("DateFilter")
                       .SetParameter("ForDateParameter", DateTime.Today);

                var dateRange = await session.Query<DateFiltered>()
                                             .SetParameter("startDate", DateTime.Today.AddDays(-7))
                                             .SetParameter("endDate", DateTime.Today.AddDays(7))
                                             .Where(dr => dr.ForDate == DateTime.Today.AddDays(3))
                                             .ToListAsync();
                
                await transaction.RollbackAsync();
                
                dateRange.Should().HaveCount(1);
                dateRange.First().ForDate.Should().Be(DateTime.Today.AddDays(3));
            }
    }
    
    
    [Test]
    public async Task ContainsFilteredDateRangeTest()
    {
        using(ISession session = NHibernateHelper.OpenSession())
            using(ITransaction transaction = session.BeginTransaction())
            {
                session.EnableFilter("DateFilter")
                       .SetParameter("ForDateParameter", DateTime.Today);
                
                var dates = new List<DateTime> { DateTime.Today.AddDays(1), DateTime.Today.AddDays(2) };
                var dateRange = await session.Query<DateFiltered>()
                                             .SetParameter("startDate", DateTime.Today.AddDays(-7))
                                             .SetParameter("endDate", DateTime.Today.AddDays(7))
                                             .Where(dr => dates.Contains(dr.ForDate))
                                             .ToListAsync();

                await transaction.RollbackAsync();
                
                dateRange.Should().HaveCount(2);
            }
    }
}