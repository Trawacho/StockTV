using Microsoft.AspNetCore.Components;
using StockTvBlazor.Components.ViewModels;
using StockTvBlazor.Services;

namespace StockTvBlazor.Components.Pages;

public partial class BestOf : MirrorableGamePageBase<BestOfViewModel>
{
	[Inject] private MatchService _matchService { get; set; } = default!;

	[Inject] private BestOfViewModel _viewModel { get; set; } = default!;

	protected override BestOfViewModel ViewModel => _viewModel;

	protected override IGameInputService GameService => _matchService;

	private bool TeamNamesAvailable => ViewModel.TeamNamesAvailable;
}
