using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Library.Installers;
using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using NexusMods.Sdk.Games;
using NexusMods.Sdk.Library;
using NexusMods.Sdk.Loadouts;

namespace NexusMods.Collections;

/// <summary>
/// Installeur de secours pour les items de collection qu'aucun autre installeur
/// ne supporte (ex: fichiers "nus" non-archive comme les .archive bruts).
/// Plutôt que de faire échouer toute l'installation de la collection, il dépose
/// le(s) fichier(s) dans un dossier par défaut du jeu, à la manière de Vortex.
/// Voir https://github.com/Nexus-Mods/NexusMods.App/issues/2553
/// </summary>
public sealed class FallbackCollectionInstaller : ALibraryFileInstaller
{
    private readonly GamePath _defaultInstallFolder;

    public FallbackCollectionInstaller(IServiceProvider serviceProvider, GamePath defaultInstallFolder)
        : base(serviceProvider, serviceProvider.GetRequiredService<ILogger<FallbackCollectionInstaller>>())
    {
        _defaultInstallFolder = defaultInstallFolder;
    }

    // On accepte n'importe quel fichier (archive ou nu) pour ne jamais retomber dans le vide.
    public override bool IsSupportedLibraryFile(LibraryFile.ReadOnly libraryFile) => true;

    public override ValueTask<InstallerResult> ExecuteAsync(
        LibraryFile.ReadOnly libraryFile,
        LoadoutItemGroup.New loadoutGroup,
        ITransaction transaction,
        Loadout.ReadOnly loadout,
        CancellationToken cancellationToken)
    {
        var loadoutId = loadout.LoadoutId;
        var baseFolder = _defaultInstallFolder;

        if (libraryFile.TryGetAsLibraryArchive(out var libraryArchive))
        {
            // Archive : on place chaque fichier enfant en préservant la structure interne.
            foreach (var child in libraryArchive.Children)
            {
                var file = child.AsLibraryFile();
                _ = new LoadoutFile.New(transaction, out var fileId)
                {
                    Hash = file.Hash,
                    Size = file.Size,
                    LoadoutItemWithTargetPath = new LoadoutItemWithTargetPath.New(transaction, fileId)
                    {
                        TargetPath = (fileId, baseFolder.LocationId, baseFolder.Path.Join(child.Path)),
                        LoadoutItem = new LoadoutItem.New(transaction, fileId)
                        {
                            Name = child.Path,
                            LoadoutId = loadoutId,
                            ParentId = loadoutGroup.Id,
                        },
                    },
                };
            }
        }
        else
        {
            // Fichier nu unique : on le dépose directement dans le dossier par défaut.
            var fileName = RelativePath.FromUnsanitizedInput(libraryFile.AsLibraryItem().Name);
            _ = new LoadoutFile.New(transaction, out var fileId)
            {
                Hash = libraryFile.Hash,
                Size = libraryFile.Size,
                LoadoutItemWithTargetPath = new LoadoutItemWithTargetPath.New(transaction, fileId)
                {
                    TargetPath = (fileId, baseFolder.LocationId, baseFolder.Path.Join(fileName)),
                    LoadoutItem = new LoadoutItem.New(transaction, fileId)
                    {
                        Name = fileName,
                        LoadoutId = loadoutId,
                        ParentId = loadoutGroup.Id,
                    },
                },
            };
        }

        return ValueTask.FromResult<InstallerResult>(new Success());
    }
}
