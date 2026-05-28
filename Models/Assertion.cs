using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LearnerDataStorybook.Models;

public class Assertion
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "Sql";
    public string? ConnectionName { get; set; }
    public string? Query { get; set; }
    public string? QueryFile { get; set; }
    [JsonConverter(typeof(AssertionExpectationConverter))]
    public List<AssertionExpectation> Expected { get; set; } = [];
}

public class AssertionExpectation
{
    public string Field { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

// Accepts either a bare string shorthand ("4" → Count=4) or the full array form.
public class AssertionExpectationConverter : JsonConverter<List<AssertionExpectation>>
{
    public override List<AssertionExpectation> ReadJson(JsonReader reader, Type objectType, List<AssertionExpectation>? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var token = JToken.Load(reader);

        if (token.Type == JTokenType.String)
            return [new AssertionExpectation { Field = "Count", Value = token.Value<string>()! }];

        return token.ToObject<List<AssertionExpectation>>(serializer) ?? [];
    }

    public override void WriteJson(JsonWriter writer, List<AssertionExpectation>? value, JsonSerializer serializer)
        => serializer.Serialize(writer, value);
}
