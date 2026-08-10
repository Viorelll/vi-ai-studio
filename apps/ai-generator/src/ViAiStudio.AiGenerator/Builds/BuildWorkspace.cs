using System.IO.Compression;
using System.Text;
using Microsoft.Extensions.Options;
using ViAiStudio.AiGenerator.Generation;
using ViAiStudio.AiGenerator.Sandbox;

namespace ViAiStudio.AiGenerator.Builds;

/// <summary>
/// One build's scratch directory: the generated repository on disk, which the
/// sandbox containers bind-mount and compile. Holds the authoritative copy of
/// the project between repair rounds -- the model only ever returns the files
/// it changed, and those are overlaid here.
/// </summary>
public sealed class BuildWorkspace : IDisposable
{
    private readonly Dictionary<string, GeneratedFile> files = new(StringComparer.OrdinalIgnoreCase);

    private BuildWorkspace(string path, string hostPath)
    {
        Path = path;
        HostPath = hostPath;
    }

    /// <summary>Directory as this process sees it.</summary>
    public string Path { get; }

    /// <summary>Directory as the Docker daemon sees it (differs when this service runs containerized).</summary>
    public string HostPath { get; }

    public IReadOnlyList<GeneratedFile> Files => files.Values.OrderBy(f => f.Path, StringComparer.Ordinal).ToList();

    public IReadOnlyList<string> FileTree => files.Values
        .Select(f => f.Path)
        .OrderBy(path => path, StringComparer.Ordinal)
        .ToList();

    public static BuildWorkspace Create(Guid generationId, SandboxOptions options)
    {
        var directoryName = $"build-{generationId:n}";
        var path = System.IO.Path.Combine(options.WorkspaceRoot, directoryName);
        Directory.CreateDirectory(path);

        var hostRoot = string.IsNullOrWhiteSpace(options.HostWorkspaceRoot) ? options.WorkspaceRoot : options.HostWorkspaceRoot;
        var hostPath = $"{hostRoot.TrimEnd('/', '\\')}/{directoryName}";

        return new BuildWorkspace(path, hostPath);
    }

    /// <summary>
    /// Overlays a set of generated files: new paths are added, existing ones
    /// replaced. Returns how many actually *changed* -- a repair round that
    /// re-sends byte-identical content has made no progress, and the caller
    /// uses a zero here to break the loop instead of re-running a verification
    /// step that is guaranteed to fail the same way.
    /// </summary>
    public int ApplyFiles(IEnumerable<GeneratedFile> generatedFiles)
    {
        var changed = 0;
        foreach (var file in generatedFiles)
        {
            var fullPath = ResolveWithin(file.Path);
            if (fullPath is null) continue;

            var normalized = NormalizePath(file.Path);
            if (files.TryGetValue(normalized, out var existing) && existing.Content == file.Content)
            {
                continue;
            }

            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(fullPath)!);
            File.WriteAllText(fullPath, file.Content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

            files[normalized] = file with { Path = normalized };
            changed++;
        }
        return changed;
    }

    public byte[] CreateArchive()
    {
        using var stream = new MemoryStream();
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var file in Files)
            {
                var entry = zip.CreateEntry(file.Path, CompressionLevel.Optimal);
                using var entryStream = entry.Open();
                using var writer = new StreamWriter(entryStream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                writer.Write(file.Content);
            }
        }
        return stream.ToArray();
    }

    private static string NormalizePath(string path) => path.Replace('\\', '/').TrimStart('/');

    /// <summary>
    /// Maps a model-supplied relative path to an absolute one, returning null
    /// if it would escape the workspace. ProjectCodeGenerator already filters
    /// these, but this is the last line before untrusted text becomes a file
    /// write, so it re-checks against the resolved path rather than the string.
    /// </summary>
    private string? ResolveWithin(string relativePath)
    {
        var candidate = System.IO.Path.GetFullPath(System.IO.Path.Combine(Path, NormalizePath(relativePath)));
        var root = System.IO.Path.GetFullPath(Path) + System.IO.Path.DirectorySeparatorChar;
        return candidate.StartsWith(root, StringComparison.OrdinalIgnoreCase) ? candidate : null;
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(Path)) Directory.Delete(Path, recursive: true);
        }
        catch (IOException)
        {
            // A container may still hold a handle briefly; the workspace root is
            // temp storage, so leaving it behind is preferable to failing a build.
        }
    }
}

public sealed class BuildWorkspaceFactory(IOptions<SandboxOptions> options)
{
    public BuildWorkspace Create(Guid generationId)
    {
        Directory.CreateDirectory(options.Value.WorkspaceRoot);
        return BuildWorkspace.Create(generationId, options.Value);
    }
}
