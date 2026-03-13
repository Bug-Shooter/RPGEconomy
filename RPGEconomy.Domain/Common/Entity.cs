namespace RPGEconomy.Domain.Common;
public abstract class Entity
{
    public int Id { get; private set; }

    protected Entity() => Id = 0;

    protected Entity(int id) => Id = id;

    public bool IsNew => Id == 0;

    public override bool Equals(object? obj)
    {
        if (obj is not Entity other) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetType() != other.GetType()) return false;
        if (IsNew || other.IsNew) return false;
        return Id == other.Id;
    }

    public override int GetHashCode() => Id.GetHashCode();
}
