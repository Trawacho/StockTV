using Microsoft.AspNetCore.Components;
using StockTvBlazor.Components.ViewModels;
using StockTvBlazor.Services;

namespace StockTvBlazor.Components.Pages;

public partial class Turnier : MirrorableGamePageBase<TurnierViewModel>
{
	[Inject] private MatchService _matchService { get; set; } = default!;

	[Inject] private TurnierViewModel _viewModel { get; set; } = default!;

	protected override TurnierViewModel ViewModel => _viewModel;

	protected override IGameInputService GameService => _matchService;

	private bool TeamNamesAvailable => ViewModel.TeamNamesAvailable;
}
