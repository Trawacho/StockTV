using Microsoft.AspNetCore.Components;
using StockTvBlazor.Components.ViewModels;
using StockTvBlazor.Services;

namespace StockTvBlazor.Components.Pages;

public partial class Training : MirrorableGamePageBase<TrainingViewModel>
{
	[Inject] private MatchService _matchService { get; set; } = default!;

	[Inject] private TrainingViewModel _viewModel { get; set; } = default!;

	protected override TrainingViewModel ViewModel => _viewModel;

	protected override IGameInputService GameService => _matchService;
}
