namespace MSSQLTableValuedFunctions.NHibernate.Linq.Tests.Tests.StandardQuery
{
    public class Customer
    {
        private string _customerId = null!;

        public virtual string CustomerId
        {
            get { return _customerId; }
            set { _customerId = value; }
        }
    }
}