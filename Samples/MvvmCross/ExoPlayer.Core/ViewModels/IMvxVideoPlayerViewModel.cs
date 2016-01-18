using MvvmCross.ExoPlayer.Models;
using MvvmCross.Core.ViewModels;

namespace MvvmCross.ExoPlayer.ViewModels
{
	public interface IMvxVideoPlayerViewModel : IMvxViewModel
	{
		bool LoadingItem { get; }
		MvxVideoItem VideoItem { get; }
	}
}