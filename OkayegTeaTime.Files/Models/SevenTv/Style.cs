using System.Text.Json.Serialization;

namespace OkayegTeaTime.Files.Models.SevenTv;

#nullable disable

public sealed class Style
{
    [JsonPropertyName("color")]
    public int Color { get; set; }

    [JsonPropertyName("paint")]
    public object Paint { get; set; }
}
