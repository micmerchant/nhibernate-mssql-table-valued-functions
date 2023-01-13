using System.Collections.Generic;
using NHibernate.Engine.Query;

namespace MSSQLTableValuedFunctions.NHibernate.Linq.Parameters;

/// <summary>
/// Equality comparer to compare NamedParameterDescriptor objects.
/// </summary>
public class NamedParameterDescriptorComparer: IEqualityComparer<NamedParameterDescriptor>
{
    public bool Equals(NamedParameterDescriptor? left, NamedParameterDescriptor? right)
    {
        if(ReferenceEquals(left, right)) return true;
        if(ReferenceEquals(left, null)) return false;
        if(ReferenceEquals(right, null)) return false;
        if(left.GetType() != right.GetType()) return false;
        return left.Name == right.Name;
    }

    public int GetHashCode(NamedParameterDescriptor obj)
    {
        return obj.Name.GetHashCode();
    }
}