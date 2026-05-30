using System.Collections.Frozen;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.Diagnostics.Emitters;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.Abstractions.Loadouts.Synchronizers.Rules;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using NexusMods.Sdk.Games;
using NexusMods.Sdk.Loadouts;

namespace NexusMods.Games.FileHashes.Emitters;

public class UndeployableLoadoutDueToMissingGameFiles : ILoadoutDiagnosticEmitter
{
    private readonly IConnection _connection;

    public UndeployableLoadoutDueToMissingGameFiles(IServiceProvider serviceProvider)
    {
        _connection = serviceProvider.GetRequiredService<IConnection>();
    }

    public IAsyncEnumerable<Diagnostic> Diagnose(Loadout.ReadOnly loadout, CancellationToken cancellationToken) => throw new NotSupportedException();
    public async IAsyncEnumerable<Diagnostic> Diagnose(Loadout.ReadOnly loadout, FrozenDictionary<GamePath, SyncNode> syncTree, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await Task.Yield();

        var sb = new StringBuilder();

        var totalSize = Size.Zero;
        var count = 0;

        foreach (var (gamePath, node) in syncTree)
        {
            if (node.SourceItemType is not LoadoutSourceItemType.Game || !node.Actions.HasFlag(Actions.WarnOfUnableToExtract)) continue;
            if (IsDynamicStoreManifest(gamePath)) continue;
            totalSize += node.Loadout.Size;
            count++;

            sb.AppendLine($"* `{gamePath}`");
        }

        if (count > 0)
        {
            yield return Diagnostics.CreateUndeployableLoadoutDueToMissingGameFiles(
                Size: totalSize,
                FileCount: count,
                Game: loadout.InstallationInstance.Game.DisplayName,
                Files: sb.ToString(),
                Store: loadout.Installation.Store.Value,
                Version: loadout.GameVersion.ToString()
            );
        }
    }

    /// <summary>
    /// Returns true for files that store-specific launchers (GOG/Heroic, etc.)
    /// regenerate locally and that legitimately can be missing or different from
    /// the version recorded in the file-hashes index. We don't want to block
    /// loadout deployment over these.
    /// </summary>
    private static bool IsDynamicStoreManifest(GamePath gamePath)
    {
        // GOG's per-install manifest files (e.g. goggame-1423049311.info / .hashdb).
        var name = gamePath.FileName.ToString();
        if (!name.StartsWith("goggame-", StringComparison.OrdinalIgnoreCase)) return false;
        return name.EndsWith(".info", StringComparison.OrdinalIgnoreCase)
            || name.EndsWith(".hashdb", StringComparison.OrdinalIgnoreCase)
            || name.EndsWith(".ico", StringComparison.OrdinalIgnoreCase)
            || name.EndsWith(".id", StringComparison.OrdinalIgnoreCase)
            || name.EndsWith(".sdb", StringComparison.OrdinalIgnoreCase)
            || name.EndsWith(".script", StringComparison.OrdinalIgnoreCase);
    }
}
