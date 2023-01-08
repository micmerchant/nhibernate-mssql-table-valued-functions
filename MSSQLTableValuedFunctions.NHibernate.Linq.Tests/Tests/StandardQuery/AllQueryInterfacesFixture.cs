using MSSQLTableValuedFunctions.NHibernate.Linq.Tests.Application;
using NHibernate;

namespace MSSQLTableValuedFunctions.NHibernate.Linq.Tests.Tests.StandardQuery;

[TestFixture(Description = "Tests a standard query for all query interfaces.")]
internal sealed class AllQueryInterfacesFixture
{
	[Test(Description = "Tests criteria queries.")]
	public void CriteriaQueryTest()
	{
		using ISession session = NHibernateHelper.OpenSession();
		using ITransaction transaction = session.BeginTransaction();
		{
			var customers = session.CreateCriteria(typeof(Customer))
			                       .List<Customer>();

			transaction.Rollback();

			Assert.IsEmpty(customers);
		}
	}
		
	[Test(Description = "Tests future queries.")]
	public void FutureQueryTest()
	{
		using ISession session = NHibernateHelper.OpenSession();
		using ITransaction transaction = session.BeginTransaction();
		{
			var customers = session.CreateQuery("select c from Customer c")
			                       .Future<Customer>()
			                       .ToList();

			transaction.Rollback();

			Assert.IsEmpty(customers);
		}
	}
		
	[Test(Description = "Tests HQL queries.")]
	public void HqlQueryTest()
	{
		using ISession session = NHibernateHelper.OpenSession();
		using ITransaction transaction = session.BeginTransaction();
		{
			var customers = session.CreateQuery("select c from Customer c")
			                       .List<Customer>();

			transaction.Rollback();

			Assert.IsEmpty(customers);
		}
	}

	[Test(Description = "Tests LINQ queries.")]
	public void LinqQueryTest()
	{
		using ISession session = NHibernateHelper.OpenSession();
		using ITransaction transaction = session.BeginTransaction();
		{
			var customers = session.Query<Customer>()
			                       .ToList();

			transaction.Rollback();

			Assert.IsEmpty(customers);
		}
	}

	[Test(Description = "Tests query over queries.")]
	public void QueryOverQueryTest()
	{
		using ISession session = NHibernateHelper.OpenSession();
		using ITransaction transaction = session.BeginTransaction();
		{
			var customers = session.QueryOver<Customer>()
			                       .List<Customer>();

			transaction.Rollback();

			Assert.IsEmpty(customers);
		}
	}
}