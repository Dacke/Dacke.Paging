namespace Dacke.Paging.UnitTests.Data;

// Supporting test structures
public class TestEntity {
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public int Age { get; set; }
    public DateTime Birthday { get; set; }
    public decimal Score { get; set; }
    public int? Ranking { get; set; }
    public Status Status { get; set; }
    public Guid UniqueId { get; set; }
}