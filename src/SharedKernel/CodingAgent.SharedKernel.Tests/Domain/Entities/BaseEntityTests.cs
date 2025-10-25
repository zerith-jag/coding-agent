using CodingAgent.SharedKernel.Domain.Entities;
using FluentAssertions;

namespace CodingAgent.SharedKernel.Tests.Domain.Entities;

// Test implementation of BaseEntity for testing purposes
public class TestEntity : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public TestEntity() : base()
    {
    }

    public TestEntity(Guid id) : base(id)
    {
    }

    public void UpdateName(string name)
    {
        Name = name;
        MarkAsUpdated();
    }
}

[Trait("Category", "Unit")]
public class BaseEntityTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithNewGuid()
    {
        // Arrange & Act
        var entity = new TestEntity();

        // Assert
        entity.Id.Should().NotBeEmpty();
        entity.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        entity.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Constructor_WithId_ShouldInitializeWithProvidedId()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var entity = new TestEntity(id);

        // Assert
        entity.Id.Should().Be(id);
        entity.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        entity.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void MarkAsUpdated_ShouldUpdateTimestamp()
    {
        // Arrange
        var entity = new TestEntity();
        var originalUpdatedAt = entity.UpdatedAt;
        Thread.Sleep(10); // Small delay to ensure different timestamp

        // Act
        entity.UpdateName("New Name");

        // Assert
        entity.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public void Equals_WithSameId_ShouldReturnTrue()
    {
        // Arrange
        var id = Guid.NewGuid();
        var entity1 = new TestEntity(id) { Name = "Entity1" };
        var entity2 = new TestEntity(id) { Name = "Entity2" };

        // Act & Assert
        entity1.Equals(entity2).Should().BeTrue();
        (entity1 == entity2).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithDifferentId_ShouldReturnFalse()
    {
        // Arrange
        var entity1 = new TestEntity();
        var entity2 = new TestEntity();

        // Act & Assert
        entity1.Equals(entity2).Should().BeFalse();
        (entity1 == entity2).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithEmptyId_ShouldReturnFalse()
    {
        // Arrange
        var entity1 = new TestEntity(Guid.Empty);
        var entity2 = new TestEntity(Guid.Empty);

        // Act & Assert
        entity1.Equals(entity2).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithNull_ShouldReturnFalse()
    {
        // Arrange
        var entity = new TestEntity();

        // Act & Assert
        entity.Equals(null).Should().BeFalse();
        (entity == null).Should().BeFalse();
        (null == entity).Should().BeFalse();
    }

    [Fact]
    public void NotEquals_WithDifferentIds_ShouldReturnTrue()
    {
        // Arrange
        var entity1 = new TestEntity();
        var entity2 = new TestEntity();

        // Act & Assert
        (entity1 != entity2).Should().BeTrue();
    }

    [Fact]
    public void GetHashCode_WithSameId_ShouldReturnSameHashCode()
    {
        // Arrange
        var id = Guid.NewGuid();
        var entity1 = new TestEntity(id);
        var entity2 = new TestEntity(id);

        // Act & Assert
        entity1.GetHashCode().Should().Be(entity2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_WithDifferentIds_ShouldReturnDifferentHashCodes()
    {
        // Arrange
        var entity1 = new TestEntity();
        var entity2 = new TestEntity();

        // Act & Assert
        entity1.GetHashCode().Should().NotBe(entity2.GetHashCode());
    }
}
