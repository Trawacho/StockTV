using Microsoft.AspNetCore.Components;
using StockTvBlazor.Components.ViewModels;
using StockTvBlazor.Services;

namespace StockTvBlazor.Components.Pages;

public partial class Ziel : MirrorableGamePageBase<ZielViewModel>
{
	[Inject] private ZielService _zielService { get; set; } = default!;

	[Inject] private ZielViewModel _viewModel { get; set; } = default!;

	protected override ZielViewModel ViewModel => _viewModel;

	protected override IGameInputService GameService => _zielService;
}
