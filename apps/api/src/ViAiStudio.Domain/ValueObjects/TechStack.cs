namespace ViAiStudio.Domain.ValueObjects;

/// <summary>
/// The stack a specification (and each of its generated builds) is scaffolded
/// with. Every option besides the chosen default is presented as
/// "coming soon" in the wizard, so this is a closed set today rather than a
/// free-text field.
/// </summary>
public sealed record TechStack(string Backend, string Ui, string Database, string Infra, string UiStyle)
{
    public static TechStack Default { get; } = new(
        Backend: ".NET Web API",
        Ui: "Next.js",
        Database: "PostgreSQL",
        Infra: "Docker",
        UiStyle: "Tailwind");

    /// <summary>The four chips shown next to a specification in list views.</summary>
    public IReadOnlyList<string> Chips => [Backend, Ui, Database, Infra];
}
