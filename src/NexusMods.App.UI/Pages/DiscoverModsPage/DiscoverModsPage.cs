using JetBrains.Annotations;
using NexusMods.Abstractions.Serialization.Attributes;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.Sdk.Loadouts;

namespace NexusMods.App.UI.Pages.DiscoverModsPage;

[JsonName("NexusMods.App.UI.Pages.DiscoverModsPage.DiscoverModsPageContext")]
public record DiscoverModsPageContext : IPageFactoryContext
{
    public required LoadoutId LoadoutId { get; init; }
}

[UsedImplicitly]
public class DiscoverModsPageFactory : APageFactory<IDiscoverModsViewModel, DiscoverModsPageContext>
{
    public DiscoverModsPageFactory(IServiceProvider serviceProvider) : base(serviceProvider) { }

    public static readonly PageFactoryId StaticId = PageFactoryId.From(Guid.Parse("e8f6c5d4-3b2a-4c1d-9e8f-7a6b5c4d3e2f"));
    public override PageFactoryId Id => StaticId;

    public override IDiscoverModsViewModel CreateViewModel(DiscoverModsPageContext context)
    {
        return new DiscoverModsViewModel(ServiceProvider, context.LoadoutId);
    }
}
