using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Loadouts;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.CLI;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Sdk.Games;
using NexusMods.Sdk.Loadouts;
using NexusMods.UI.Sdk.Icons;
using R3;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using ReactiveCommand = R3.ReactiveCommand;

namespace NexusMods.App.UI.Pages.DiscoverModsPage;

[UsedImplicitly]
public class DiscoverModsViewModel : APageViewModel<IDiscoverModsViewModel>, IDiscoverModsViewModel
{
    private readonly IServiceProvider _serviceProvider;
    private readonly LoadoutId _loadoutId;
    private readonly IConnection _connection;
    private readonly IModDiscoverer? _discoverer;
    private readonly ModImporter _modImporter;

    private readonly ObservableCollection<DiscoveredModItemViewModel> _items = new();
    public ReadOnlyObservableCollection<DiscoveredModItemViewModel> Items { get; }

    [Reactive] public string StatusText { get; set; } = "";
    [Reactive] public bool IsBusy { get; set; }
    [Reactive] public bool CanImport { get; set; }

    public ReactiveCommand<Unit> CommandScan { get; }
    public ReactiveCommand<Unit> CommandImport { get; }
    public ReactiveCommand<Unit> CommandSelectAll { get; }
    public ReactiveCommand<Unit> CommandDeselectAll { get; }

    public DiscoverModsViewModel(IServiceProvider serviceProvider, LoadoutId loadoutId)
        : base(serviceProvider.GetRequiredService<IWindowManager>())
    {
        _serviceProvider = serviceProvider;
        _loadoutId = loadoutId;
        _connection = serviceProvider.GetRequiredService<IConnection>();
        _modImporter = serviceProvider.GetRequiredService<ModImporter>();

        Items = new ReadOnlyObservableCollection<DiscoveredModItemViewModel>(_items);

        TabIcon = IconValues.Search;
        TabTitle = "Discover mods";

        var loadout = Loadout.Load(_connection.Db, _loadoutId);
        var gameId = loadout.InstallationInstance.Game.GameId;

        _discoverer = serviceProvider.GetServices<IModDiscoverer>()
            .FirstOrDefault(d => d.GameId == gameId);

        if (_discoverer is null)
        {
            StatusText = $"No mod discoverer available for game '{loadout.InstallationInstance.Game.DisplayName}'.";
        }

        CommandScan = new ReactiveCommand<Unit>(async (_, ct) => await ScanAsync(ct));
        CommandImport = new ReactiveCommand<Unit>(async (_, ct) => await ImportAsync(ct));
        CommandSelectAll = new ReactiveCommand<Unit>(_ => SetSelection(true));
        CommandDeselectAll = new ReactiveCommand<Unit>(_ => SetSelection(false));

        this.WhenActivated((System.Reactive.Disposables.CompositeDisposable _) =>
        {
            if (_discoverer is not null && _items.Count == 0)
            {
                CommandScan.Execute(Unit.Default);
            }
        });
    }

    private async Task ScanAsync(CancellationToken cancellationToken)
    {
        if (_discoverer is null) return;

        _items.Clear();
        CanImport = false;
        IsBusy = true;
        StatusText = "Scanning…";

        try
        {
            var loadout = Loadout.Load(_connection.Db, _loadoutId);
            var gameFolder = loadout.InstallationInstance.Locations[LocationId.Game].Path;

            await foreach (var mod in _discoverer.DiscoverAsync(gameFolder, cancellationToken))
            {
                _items.Add(new DiscoveredModItemViewModel(mod));
            }

            StatusText = _items.Count == 0
                ? "No mods found in the game folder."
                : $"Found {_items.Count} mod(s).";
            CanImport = _items.Count > 0;
        }
        catch (Exception e)
        {
            StatusText = $"Scan failed: {e.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ImportAsync(CancellationToken cancellationToken)
    {
        var selected = _items.Where(i => i.IsSelected).ToList();
        if (selected.Count == 0)
        {
            StatusText = "Nothing selected.";
            return;
        }

        IsBusy = true;
        CanImport = false;

        var imported = 0;
        var failed = 0;
        var processed = 0;

        foreach (var item in selected)
        {
            cancellationToken.ThrowIfCancellationRequested();
            processed++;
            StatusText = $"Importing {processed}/{selected.Count}: {item.NameText}";
            item.StatusText = "…";

            var result = await _modImporter.ImportAsync(item.Mod, _loadoutId, cancellationToken);
            switch (result.Outcome)
            {
                case ImportOutcome.Installed:
                    item.StatusText = "✓ Imported";
                    imported++;
                    break;
                case ImportOutcome.NoInstallerMatched:
                    item.StatusText = "✗ No installer matched";
                    failed++;
                    break;
                case ImportOutcome.Failed:
                    item.StatusText = $"✗ {result.ErrorMessage}";
                    failed++;
                    break;
            }
        }

        StatusText = $"Imported {imported}/{selected.Count}. {failed} failed.";
        IsBusy = false;
        CanImport = true;
    }

    private void SetSelection(bool value)
    {
        foreach (var item in _items) item.IsSelected = value;
    }
}
