using System.Text.Json;
using RulesEngine.Models;

namespace RulesEngine_Demo.Approaches.JsonWorkflow;

public static class JsonWorkflowLoader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public static List<Workflow> LoadDirectory(string directory)
    {
        if (!Directory.Exists(directory))
            throw new DirectoryNotFoundException(directory);

        var list = new List<Workflow>();
        foreach (var file in Directory.EnumerateFiles(directory, "*.json")
                     .OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
            list.AddRange(LoadFile(file));
        return list;
    }

    public static IEnumerable<Workflow> LoadFile(string path)
    {
        var json = File.ReadAllText(path);
        using var doc = JsonDocument.Parse(json, new JsonDocumentOptions
        {
            CommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        });

        if (doc.RootElement.ValueKind == JsonValueKind.Array)
            return JsonSerializer.Deserialize<List<Workflow>>(json, Options)
                   ?? throw new InvalidOperationException(path);

        var single = JsonSerializer.Deserialize<Workflow>(json, Options)
                     ?? throw new InvalidOperationException(path);
        return [single];
    }
}
