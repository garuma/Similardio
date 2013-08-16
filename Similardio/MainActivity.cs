using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;

using Rdio.Android.Sdk;
using Rdio.Android.Sdk.Services;

using Log = Android.Util.Log;

namespace Similardio
{
	[Activity (Label = "Similardio", MainLauncher = true, Theme = "@android:style/Theme.Holo.Light.NoActionBar")]
	public class MainActivity : Activity, IRdioListener
	{
		bool hasTokens;
		ArtistAdapter adapter;
		LoadingFragment loadingFragment;
		ListFragment list;

		LastFmApi lastFmApi;
		RdioAPI rdioApi;

		TaskCompletionSource<bool> rdioReady = new TaskCompletionSource<bool> ();

		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);
			DensityExtensions.Initialize (this);

			SetContentView (Resource.Layout.Main);

			loadingFragment = new LoadingFragment ();
			var t = FragmentManager.BeginTransaction ();
			t.Add (Resource.Id.mainContainer, loadingFragment, "loading");
			t.Commit ();
			RetrieveArtist ();
		}

		void ShowList ()
		{
			adapter = new ArtistAdapter (this);
			list = new RdioListFragment ();

			var t = FragmentManager.BeginTransaction ();
			t.SetTransition (FragmentTransit.FragmentClose);
			t.Replace (Resource.Id.mainContainer, list, "artist-list");
			t.Commit ();
		}

		async void RetrieveArtist ()
		{
			var artists = await Task.Run (() => DoRetrieve (new UIProgress (this)).Result);
			ShowList ();
			foreach (var artist in artists)
				adapter.Data.Add (artist);
			list.ListAdapter = adapter;
		}

		async Task<ArtistData[]> DoRetrieve (IProgress<Action> uiProgress)
		{
			lastFmApi = new LastFmApi ();
			var prefs = GetPreferences (FileCreationMode.Private);
			hasTokens = prefs.GetString ("accessToken", null) != null;
			if (!hasTokens)
				uiProgress.Report (() => loadingFragment.SetExtraExplanation ("(will need more time to login)"));
			rdioApi = new RdioAPI (Keys.RdioKey,
			                       Keys.RdioSecret,
			                       prefs.GetString ("accessToken", null),
			                       prefs.GetString ("accessSecret", null),
			                       this, this);
			await rdioReady.Task.ConfigureAwait (false);
			var args = new object[] {
				new Org.Apache.Http.Message.BasicNameValuePair ("sort", "playCount"),
				new Org.Apache.Http.Message.BasicNameValuePair ("count", "30")
			};

			var artistsCompletion = new TaskCompletionSource<RdioArtist[]> ();
			var callback = new LambdaRdioCallback (success: json => {
				var array = json.GetJSONArray ("result");
				var result = Enumerable.Range (0, array.Length ()).Select (i => {
					var o = array.GetJSONObject (i);
					return new RdioArtist {
						Key = o.GetString ("key"),
						Name = o.GetString ("name")
					};
				}).ToArray ();
				artistsCompletion.SetResult (result);
			});
			rdioApi.ApiCall ("getArtistsInCollection", args, callback);
			var rdioArtists = await artistsCompletion.Task.ConfigureAwait (false);

			uiProgress.Report (() => loadingFragment.ChangeLoading (Resource.Drawable.lastfm_logo, "Scouting Lastfm"));
			var artists = new Dictionary<ArtistData, List<SimilarityLink>> ();
			int rdioArtistIndex = rdioArtists.Length;
			var counter = 0;
			var existingArtists = new HashSet<string> (rdioArtists.Select (ra => ra.Name));
			foreach (var rdioArtist in rdioArtists) {
				var arts = await lastFmApi.GetSimilarArtistsAsync (rdioArtist.Name).ConfigureAwait (false);
				var newExplanation = "(" + (++counter) + " out of " + rdioArtists.Length + ")";
				uiProgress.Report (() => loadingFragment.SetExtraExplanation (newExplanation));
				foreach (var a in arts.Where (a => !existingArtists.Contains (a.Name))) {
					List<SimilarityLink> links;
					if (!artists.TryGetValue (a, out links))
						artists[a] = links = new List<SimilarityLink> ();
					links.Add (new SimilarityLink {
						Strength = a.Match,
						RdioArtistIndex = rdioArtistIndex,
						RdioArtistName = rdioArtist.Name
					});
				}
				rdioArtistIndex--;
			}
			return SynthetizeLinks (artists, rdioArtists.Length);
		}

		ArtistData[] SynthetizeLinks (Dictionary<ArtistData, List<SimilarityLink>> links, int maxRdioIndex)
		{
			return links.Select (node => {
				var ad = node.Key;
				ad.SimilarArtists = node.Value.Select (v => v.RdioArtistName).ToArray ();
				// Final AD match is the mean of all links calculated power.
				// A link power is a value composed for 60% of the original link strength
				// and 40% for the Rdio artist position it links to
				ad.Match = node.Value
					.Select (link => Math.Min (1, .6 * link.Strength + .4 * (((double)link.RdioArtistIndex) / maxRdioIndex)))
					.Average ();
				return ad;
			}).OrderByDescending (ad => ad.Match).ToArray ();
		}

		public void OnRdioAuthorised (string accessToken, string accessSecret)
		{
			var prefs = GetPreferences (FileCreationMode.Private);
			using (var editor = prefs.Edit ()) {
				editor.PutString ("accessToken", accessToken);
				editor.PutString ("accessSecret", accessSecret);
				editor.Commit ();
			}
			rdioReady.TrySetResult (true);
		}

		public void OnRdioReady ()
		{
			if (hasTokens)
				rdioReady.TrySetResult (true);
		}

		public void OnRdioUserAppApprovalNeeded (Intent authIntent)
		{
			try {
				StartActivityForResult (authIntent, 100);
			} catch (ActivityNotFoundException) {
				Log.Error ("Auth", "Rdio App not found");
			}
		}

		public void OnRdioUserPlayingElsewhere ()
		{

		}

		protected override void OnActivityResult (int requestCode, Result resultCode, Intent data)
		{
			if ((int)resultCode == RdioAPI.ResultAuthorisationRejected) {
				Finish ();
				return;
			} else if (data != null && (int)resultCode == RdioAPI.ResultAuthorisationAccepted) {
				rdioApi.SetTokenAndSecret (data);
			}
		}
	}

	class LambdaRdioCallback : Java.Lang.Object, IRdioApiCallback
	{
		Action<Org.Json.JSONObject> success;
		Action<string, Exception> failure;

		public LambdaRdioCallback (Action<Org.Json.JSONObject> success = null, Action<string, Exception> failure = null)
		{
			this.success = success;
			this.failure = failure;
		}

		public void OnApiFailure (string p0, Java.Lang.Exception p1)
		{
			if (failure != null)
				failure (p0, p1);
		}

		public void OnApiSuccess (Org.Json.JSONObject p0)
		{
			if (success != null)
				success (p0);
		}
	}

	class UIProgress : IProgress<Action>
	{
		Activity activity;

		public UIProgress (Activity activity)
		{
			this.activity = activity;
		}

		public void Report (Action action)
		{
			activity.RunOnUiThread (action);
		}
	}

	struct RdioArtist {
		public string Name { get; set; }
		public string Key { get; set; }
	}

	struct SimilarityLink {
		public double Strength;
		public int RdioArtistIndex;
		public string RdioArtistName;
	}
}


