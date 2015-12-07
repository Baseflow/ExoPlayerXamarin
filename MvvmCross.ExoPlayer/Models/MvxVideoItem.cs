namespace MvvmCross.ExoPlayer.Models
{
	public class MvxVideoItem
	{
		public enum ContentType
		{
			Hls,
			Other
		}

		public string Url { get; private set; }
		public ContentType Type { get; private set; }

		public MvxVideoItem(string url, ContentType type)
		{
			Url = url;
			Type = type;
		}
	}
}