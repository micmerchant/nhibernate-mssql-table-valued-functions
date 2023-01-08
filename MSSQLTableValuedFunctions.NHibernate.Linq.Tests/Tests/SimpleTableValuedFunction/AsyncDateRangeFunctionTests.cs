using FluentAssertions;
using MSSQLTableValuedFunctions.NHibernate.Linq.ExtensionMethods;
using MSSQLTableValuedFunctions.NHibernate.Linq.Tests.Application;
using NHibernate;
using NHibernate.Linq;

namespace MSSQLTableValuedFunctions.NHibernate.Linq.Tests.Tests.SimpleTableValuedFunction;

[TestFixture(Description = "Tests the DateRange Table-Valued-Function.")]
internal sealed class AsyncDateRangeFunctionTests
{
    [Test]
    public async Task FullDateRangeTest()
    {
        using(ISession session = NHibernateHelper.OpenSession())
            using(ITransaction transaction = session.BeginTransaction())
            {
                var dateRange = await session.Query<Date>()
                                             .SetParameter("startDate", DateTime.Today)
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
                var dateRange = await session.Query<Date>()
                                             .SetParameter("startDate", DateTime.Today)
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
                var dates = new List<DateTime> { DateTime.Today.AddDays(1), DateTime.Today.AddDays(2) };
                var dateRange = await session.Query<Date>()
                                       .SetParameter("startDate", DateTime.Today)
                                       .SetParameter("endDate", DateTime.Today.AddDays(7))
                                       .Where(dr => dates.Contains(dr.ForDate))
                                       .ToListAsync();

                await transaction.RollbackAsync();
                
                dateRange.Should().HaveCount(2);
                dateRange.First().ForDate.Should().Be(DateTime.Today.AddDays(1));
                dateRange.Last().ForDate.Should().Be(DateTime.Today.AddDays(2));
            }
    }
    
    
    [Test]
    public void NullFilteredDateRangeTest()
    {
        using(ISession session = NHibernateHelper.OpenSession())
            using(ITransaction transaction = session.BeginTransaction())
            {
                var dates = new List<DateTime> { DateTime.Today.AddDays(1), DateTime.Today.AddDays(2) };
                var dateRange = session.Query<Date>()
                                       .SetParameter("startDate", DateTime.Today)
                                       .SetParameter("endDate", DateTime.Today.AddDays(7))
                                       // ReSharper disable once ConditionIsAlwaysTrueOrFalse
#pragma warning disable CS8073
                                       .Where(dr => dr.ForDate != null)
#pragma warning restore CS8073
                                       .ToList();

                transaction.Rollback();
                
                dateRange.Should().HaveCount(8);
            }
    }
}