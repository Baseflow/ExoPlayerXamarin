using Cirrious.MvvmCross.ViewModels;
using MvvmCross.ExoPlayer.Models;

namespace MvvmCross.ExoPlayer.ViewModels
{
	public interface IMvxVideoPlayerViewModel : IMvxViewModel
	{
		bool LoadingItem { get; }
		MvxVideoItem VideoItem { get; }
	}
}