using System.Text.Json;

namespace GDP.Persistence.Models;

public class Layer
{
    public string Id { get; set; }

    public string Name { get; set; }

    public string? Description { get; set; }

    public JsonDocument Layout { get; set; }

    public JsonDocument Style { get; set; }
}