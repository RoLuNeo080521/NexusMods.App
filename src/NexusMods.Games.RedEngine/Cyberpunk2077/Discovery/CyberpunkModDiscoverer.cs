using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using NexusMods.Paths;
using NexusMods.Sdk.Games;

namespace NexusMods.Games.RedEngine.Cyberpunk2077.Discovery;

/// <summary>
/// Discovers Cyberpunk 2077 mods already present in the game folder.
/// Handles the two main conventions:
/// - RedMods: <c>mods/&lt;modname&gt;/info.json</c> (manifest-based)
/// - Archive mods: <c>archive/pc/mod/*.archive</c> (single file)
/// </summary>
public class CyberpunkModDiscoverer : IModDiscoverer
{
    public GameId GameId => Cyberpunk2077Game.GameId;

    public async IAsyncEnumerable<DiscoveredMod> DiscoverAsync(
        AbsolutePath gameFolder,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var mod in DiscoverRedModsAsync(gameFolder, cancellationToken))
            yield return mod;

        foreach (var mod in DiscoverArchiveMods(gameFolder, cancellationToken))
            yield return mod;
    }

    private static async IAsyncEnumerable<DiscoveredMod> DiscoverRedModsAsync(
        AbsolutePath gameFolder,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var modsFolder = gameFolder / "mods";
        if (!modsFolder.DirectoryExists()) yield break;

        foreach (var modDir in modsFolder.EnumerateDirectories(recursive: false))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var infoJson = modDir / "info.json";
            if (!infoJson.FileExists) continue;

            RedModInfo? info;
            try
            {
                await using var stream = infoJson.Read();
                info = await JsonSerializer.DeserializeAsync<RedModInfo>(stream, cancellationToken: cancellationToken);
            }
            catch
            {
                // Malformed info.json - skip, but don't bring the whole scan down.
                continue;
            }

            if (info is null || string.IsNullOrWhiteSpace(info.Name)) continue;

            var files = modDir.EnumerateFiles(recursive: true).ToList();
            yield return new DiscoveredMod(
                Type: "RedMod",
                Name: info.Name,
                Version: string.IsNullOrWhiteSpace(info.Version) ? null : info.Version,
                RootPath: modDir,
                Files: files,
                Description: info.Description
            );
        }
    }

    private static IEnumerable<DiscoveredMod> DiscoverArchiveMods(
        AbsolutePath gameFolder,
        CancellationToken cancellationToken)
    {
        var archiveModFolder = gameFolder / "archive" / "pc" / "mod";
        if (!archiveModFolder.DirectoryExists()) yield break;

        foreach (var file in archiveModFolder.EnumerateFiles(recursive: false))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!file.Extension.Equals(ArchiveExtension)) continue;

            yield return new DiscoveredMod(
                Type: "Archive",
                Name: file.GetFileNameWithoutExtension(),
                Version: null,
                RootPath: file,
                Files: [file]
            );
        }
    }

    private static readonly Extension ArchiveExtension = new(".archive");

    private sealed class RedModInfo
    {
        [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
        [JsonPropertyName("version")] public string Version { get; set; } = string.Empty;
        [JsonPropertyName("description")] public string? Description { get; set; }
    }
}
