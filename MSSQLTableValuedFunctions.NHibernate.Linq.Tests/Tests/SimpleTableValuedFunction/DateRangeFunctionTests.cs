using FluentAssertions;
using MSSQLTableValuedFunctions.NHibernate.Linq.ExtensionMethods;
using MSSQLTableValuedFunctions.NHibernate.Linq.Tests.Application;
using NHibernate;

namespace MSSQLTableValuedFunctions.NHibernate.Linq.Tests.Tests.SimpleTableValuedFunction;

[TestFixture(Description = "Tests the DateRange Table-Valued-Function.")]
internal sealed class DateRangeFunctionTests
{
    [Test]
    public void FullDateRangeTest()
    {
        using(ISession session = NHibernateHelper.OpenSession())
            using(ITransaction transaction = session.BeginTransaction())
            {
                var dateRange = session.Query<Date>()
                                       .SetParameter("startDate", DateTime.Today)
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
                var dateRange = session.Query<Date>()
                                       .SetParameter("startDate", DateTime.Today)
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
                var dates = new List<DateTime> { DateTime.Today.AddDays(1), DateTime.Today.AddDays(2) };
                var dateRange = session.Query<Date>()
                                       .SetParameter("startDate", DateTime.Today)
                                       .SetParameter("endDate", DateTime.Today.AddDays(7))
                                       .Where(dr => dates.Contains(dr.ForDate))
                                       .ToList();

                transaction.Rollback();
                
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