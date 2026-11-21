namespace CollegeErp.Domain.Common;

/// <summary>Base type for every persisted entity that has an integer identity.</summary>
public abstract class Entity
{
    public int Id { get; set; }
}

/// <summary>Entities that record when they were created.</summary>
public interface IAuditable
{
    DateTime CreatedAt { get; set; }
}
