using NexusMods.Paths;

namespace NexusMods.Sdk.Games;

/// <summary>
/// A mod detected on disk by an <see cref="IModDiscoverer"/>.
/// </summary>
/// <param name="Type">Discoverer-defined type tag (e.g. "RedMod", "Archive"). Identifies the convention used to detect the mod.</param>
/// <param name="Name">Display name of the mod, parsed from its manifest when available, otherwise derived from the file or folder name.</param>
/// <param name="Version">Mod version when the manifest provides one. <c>null</c> when the convention has no version concept.</param>
/// <param name="RootPath">Filesystem path of the mod's root - typically a directory containing the manifest, but may be a single file for archive-style mods.</param>
/// <param name="Files">All files that compose the mod, used for hashing and import.</param>
/// <param name="Description">Optional human-readable description from the manifest.</param>
public record DiscoveredMod(
    string Type,
    string Name,
    string? Version,
    AbsolutePath RootPath,
    IReadOnlyList<AbsolutePath> Files,
    string? Description = null
);
