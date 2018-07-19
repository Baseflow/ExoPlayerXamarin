/*
 * Copyright (C) 2016 The Android Open Source Project
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.Util;
using Android.Views;
using Android.Widget;
using Com.Google.Android.Exoplayer2.Source;
using Com.Google.Android.Exoplayer2.Trackselection;
using Java.Lang;

namespace Com.Google.Android.Exoplayer2.Demo
{
	/**
	 * Helper class for displaying track selection dialogs.
	 */
	internal sealed class TrackSelectionHelper : Object, View.IOnClickListener, IDialogInterfaceOnClickListener
	{
		private static readonly ITrackSelectionFactory FIXED_FACTORY = new FixedTrackSelection.Factory();
		private static readonly ITrackSelectionFactory RANDOM_FACTORY = new RandomTrackSelection.Factory();

		private readonly MappingTrackSelector selector;
		private readonly ITrackSelectionFactory adaptiveTrackSelectionFactory;

		private MappingTrackSelector.MappedTrackInfo trackInfo;
		private int rendererIndex;
		private TrackGroupArray trackGroups;
		private bool[] trackGroupsAdaptive;
		private bool isDisabled;
		//private MappingTrackSelector.SelectionOverride _override;

		private CheckedTextView disableView;
		private CheckedTextView defaultView;
		private CheckedTextView enableRandomAdaptationView;
		private CheckedTextView[][] trackViews;

		/**
		 * @param selector The track selector.
		 * @param adaptiveTrackSelectionFactory A factory for adaptive {@link TrackSelection}s, or null
		 *     if the selection helper should not support adaptive tracks.
		 */
		public TrackSelectionHelper(MappingTrackSelector selector,
			ITrackSelectionFactory adaptiveTrackSelectionFactory)
		{
			this.selector = selector;
			this.adaptiveTrackSelectionFactory = adaptiveTrackSelectionFactory;
		}

		/**
		 * Shows the selection dialog for a given renderer.
		 *
		 * @param activity The parent activity.
		 * @param title The dialog's title.
		 * @param trackInfo The current track information.
		 * @param rendererIndex The index of the renderer.
		 */
		public void showSelectionDialog(Activity activity, string title, MappingTrackSelector.MappedTrackInfo trackInfo,
			int rendererIndex)
		{
			this.trackInfo = trackInfo;
			this.rendererIndex = rendererIndex;

			trackGroups = trackInfo.GetTrackGroups(rendererIndex);
			trackGroupsAdaptive = new bool[trackGroups.Length];
			for (int i = 0; i < trackGroups.Length; i++)
			{
				trackGroupsAdaptive[i] = adaptiveTrackSelectionFactory != null
					&& trackInfo.GetAdaptiveSupport(rendererIndex, i, false)
								!= RendererCapabilities.AdaptiveNotSupported
					&& trackGroups.Get(i).Length > 1;
			}

			/*isDisabled = selector.GetRendererDisabled(rendererIndex);

			_override = selector.GetSelectionOverride(rendererIndex, trackGroups);*/

			AlertDialog.Builder builder = new AlertDialog.Builder(activity);
			builder.SetTitle(title)
				.SetView(buildView(builder.Context))
				   .SetPositiveButton(global::Android.Resource.String.Ok, this)
				   .SetNegativeButton(global::Android.Resource.String.Cancel, delegate { })
				.Create()
				.Show();
		}

		private View buildView(Context context)
		{
			var inflater = LayoutInflater.From(context);
			var view = inflater.Inflate(Resource.Layout.track_selection_dialog, null);
			var root = (ViewGroup)view.FindViewById(Resource.Id.root);

			TypedArray attributeArray = context.Theme.ObtainStyledAttributes(
				new int[] { global::Android.Resource.Attribute.SelectableItemBackground });
			var selectableItemBackgroundResourceId = attributeArray.GetResourceId(0, 0);
			attributeArray.Recycle();

			// View for disabling the renderer.
			disableView = (CheckedTextView)inflater.Inflate(
				global::Android.Resource.Layout.SimpleListItemSingleChoice, root, false);
			disableView.SetBackgroundResource(selectableItemBackgroundResourceId);
			disableView.SetText(Resource.String.selection_disabled);
			disableView.Focusable = true;
			disableView.SetOnClickListener(this);
			root.AddView(disableView);

			// View for clearing the override to allow the selector to use its default selection logic.
			defaultView = (CheckedTextView)inflater.Inflate(
				global::Android.Resource.Layout.SimpleListItemSingleChoice, root, false);
			defaultView.SetBackgroundResource(selectableItemBackgroundResourceId);
			defaultView.SetText(Resource.String.selection_default);
			defaultView.Focusable = true;
			defaultView.SetOnClickListener(this);
			root.AddView(inflater.Inflate(Resource.Layout.list_divider, root, false));
			root.AddView(defaultView);

			// Per-track views.
			var haveAdaptiveTracks = false;
			trackViews = new CheckedTextView[trackGroups.Length][];
			for (var groupIndex = 0; groupIndex < trackGroups.Length; groupIndex++)
			{
				var group = trackGroups.Get(groupIndex);
				var groupIsAdaptive = trackGroupsAdaptive[groupIndex];
				haveAdaptiveTracks |= groupIsAdaptive;
				trackViews[groupIndex] = new CheckedTextView[group.Length];
				for (var trackIndex = 0; trackIndex < group.Length; trackIndex++)
				{
					if (trackIndex == 0)
					{
						root.AddView(inflater.Inflate(Resource.Layout.list_divider, root, false));
					}
					int trackViewLayoutId = groupIsAdaptive ? global::Android.Resource.Layout.SimpleListItemMultipleChoice
						: global::Android.Resource.Layout.SimpleListItemSingleChoice;
					var trackView = (CheckedTextView)inflater.Inflate(
						trackViewLayoutId, root, false);
					trackView.SetBackgroundResource(selectableItemBackgroundResourceId);
					trackView.Text = DemoUtil.BuildTrackName(group.GetFormat(trackIndex));
					if (trackInfo.GetTrackFormatSupport(rendererIndex, groupIndex, trackIndex)
						== RendererCapabilities.FormatHandled)
					{
						trackView.Focusable = true;
						trackView.Tag = Pair.Create(groupIndex, trackIndex);
						trackView.SetOnClickListener(this);
					}
					else
					{
						trackView.Focusable = false;
						trackView.Enabled = false;
					}
					trackViews[groupIndex][trackIndex] = trackView;
					root.AddView(trackView);
				}
			}

			if (haveAdaptiveTracks)
			{
				// View for using random adaptation.
				enableRandomAdaptationView = (CheckedTextView)inflater.Inflate(
					global::Android.Resource.Layout.SimpleListItemMultipleChoice, root, false);
				enableRandomAdaptationView.SetBackgroundResource(selectableItemBackgroundResourceId);
				enableRandomAdaptationView.SetText(Resource.String.enable_random_adaptation);
				enableRandomAdaptationView.SetOnClickListener(this);
				root.AddView(inflater.Inflate(Resource.Layout.list_divider, root, false));
				root.AddView(enableRandomAdaptationView);
			}

			UpdateViews();
			return view;
		}

		private void UpdateViews()
		{
			disableView.Checked = isDisabled;
			defaultView.Checked = !isDisabled && _override == null;
			for (var i = 0; i < trackViews.Length; i++)
			{
				for (var j = 0; j < trackViews[i].Length; j++)
				{
					trackViews[i][j].Checked = _override != null && _override.GroupIndex == i
						&& _override.ContainsTrack(j);
				}
			}
			if (enableRandomAdaptationView != null)
			{
				var enableView = !isDisabled && _override != null && _override.Length > 1;
				enableRandomAdaptationView.Enabled = enableView;
				enableRandomAdaptationView.Focusable = enableView;
				if (enableView)
				{
					enableRandomAdaptationView.Checked = !isDisabled && _override.Factory is RandomTrackSelection.Factory;
				}
			}
		}

		// DialogInterface.OnClickListener

		public void OnClick(IDialogInterface dialog, int which)
		{
			selector.SetRendererDisabled(rendererIndex, isDisabled);
			if (_override != null)
			{
				selector.SetSelectionOverride(rendererIndex, trackGroups, _override);
			}
			else
			{
				selector.ClearSelectionOverrides(rendererIndex);
			}
		}

		// View.OnClickListener

		public void OnClick(View view)
		{
			if (view == disableView)
			{
				isDisabled = true;
				_override = null;
			}
			else if (view == defaultView)
			{
				isDisabled = false;
				_override = null;
			}
			else if (view == enableRandomAdaptationView)
			{
				setOverride(_override.GroupIndex, _override.Tracks.ToArray(), !enableRandomAdaptationView.Checked);
			}
			else
			{
				isDisabled = false;

				var tag = (Pair)view.Tag;
				var groupIndex = (int)tag.First;
				var trackIndex = (int)tag.Second;
				if (!trackGroupsAdaptive[groupIndex] || _override == null
				|| _override.GroupIndex != groupIndex)
				{
					_override = new MappingTrackSelector.SelectionOverride(FIXED_FACTORY, groupIndex, trackIndex);
				}
				else
				{
					// The group being modified is adaptive and we already have a non-null override.
					var isEnabled = ((CheckedTextView)view).Checked;
					var overrideLength = _override.Length;
					if (isEnabled)
					{
						// Remove the track from the override.
						if (overrideLength == 1)
						{
							// The last track is being removed, so the override becomes empty.
							_override = null;
							isDisabled = true;
						}
						else
						{
							setOverride(groupIndex, getTracksRemoving(_override, trackIndex),
							enableRandomAdaptationView.Checked);
						}
					}
					else
					{
						// Add the track to the override.
						setOverride(groupIndex, getTracksAdding(_override, trackIndex),
							enableRandomAdaptationView.Checked);
					}
				}
			}
			// Update the views with the new state.
			UpdateViews();
		}

		private void setOverride(int group, int[] tracks, bool enableRandomAdaptation)
		{
			var factory = tracks.Length == 1 ? FIXED_FACTORY
				: (enableRandomAdaptation ? RANDOM_FACTORY : adaptiveTrackSelectionFactory);
			_override = new MappingTrackSelector.SelectionOverride(factory, group, tracks);
		}

		// Track array manipulation.

		private static int[] getTracksAdding(MappingTrackSelector.SelectionOverride _override, int addedTrack)
		{
			var tracks = new List<int>(_override.Tracks)
			{
				addedTrack
			};
			return tracks.ToArray();
		}

		private static int[] getTracksRemoving(MappingTrackSelector.SelectionOverride _override, int removedTrack)
		{
			var tracks = new int[_override.Length - 1];
			var trackCount = 0;
			for (var i = 0; i < tracks.Length + 1; i++)
			{
				int track = _override.Tracks[i];
				if (track != removedTrack)
				{
					tracks[trackCount++] = track;
				}
			}
			return tracks;
		}
	}
}
