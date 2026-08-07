using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ViAiStudio.Infrastructure.Persistence;

/// <summary>
/// Maps a <see cref="List{T}"/> of strings to a single jsonb column. Used for
/// the small, denormalized string lists on <see cref="Domain.Entities.SpecificationPhase"/>
/// and <see cref="Domain.Entities.Generation"/> that don't warrant their own tables.
/// </summary>
public static class JsonStringListConverter
{
    public static ValueConverter<List<string>, string> Converter { get; } = new(
        list => JsonSerializer.Serialize(list, (JsonSerializerOptions?)null),
        json => JsonSerializer.Deserialize<List<string>>(json, (JsonSerializerOptions?)null) ?? new List<string>());

    public static ValueComparer<List<string>> Comparer { get; } = new(
        (a, b) => (a ?? new List<string>()).SequenceEqual(b ?? new List<string>()),
        list => list.Aggregate(0, (hash, item) => HashCode.Combine(hash, item.GetHashCode())),
        list => list.ToList());
}
