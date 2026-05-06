namespace LearnerDataStorybook.Models;

public class Assertion
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "Sql";
    public string? ConnectionName { get; set; }
    public string? Query { get; set; }
    public string? QueryFile { get; set; }
    public List<AssertionExpectation> Expected { get; set; } = [];
}

public class AssertionExpectation
{
    public string Field { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
