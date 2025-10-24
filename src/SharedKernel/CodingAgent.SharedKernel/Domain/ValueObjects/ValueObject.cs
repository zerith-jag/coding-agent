namespace CodingAgent.SharedKernel.Domain.ValueObjects;

/// <summary>
/// Base class for value objects in the domain model.
/// Value objects are immutable and compared by their properties rather than identity.
/// </summary>
public abstract class ValueObject
{
    /// <summary>
    /// Gets the components that define the equality of this value object.
    /// </summary>
    /// <returns>An enumerable of objects that define the value object's equality.</returns>
    protected abstract IEnumerable<object?> GetEqualityComponents();

    /// <summary>
    /// Determines whether the specified value object is equal to the current value object.
    /// </summary>
    public override bool Equals(object? obj)
    {
        if (obj == null || obj.GetType() != GetType())
        {
            return false;
        }

        var other = (ValueObject)obj;

        return GetEqualityComponents()
            .SequenceEqual(other.GetEqualityComponents());
    }

    /// <summary>
    /// Returns the hash code for the value object based on its equality components.
    /// </summary>
    public override int GetHashCode()
    {
        return GetEqualityComponents()
            .Select(x => x?.GetHashCode() ?? 0)
            .Aggregate((x, y) => x ^ y);
    }

    /// <summary>
    /// Equality operator for value objects.
    /// </summary>
    public static bool operator ==(ValueObject? left, ValueObject? right)
    {
        if (left is null && right is null)
        {
            return true;
        }

        if (left is null || right is null)
        {
            return false;
        }

        return left.Equals(right);
    }

    /// <summary>
    /// Inequality operator for value objects.
    /// </summary>
    public static bool operator !=(ValueObject? left, ValueObject? right)
    {
        return !(left == right);
    }
}
