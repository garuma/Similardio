using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http;

using Android.Widget;
using Android.Views;
using Android.Content;
using Android.Graphics;

namespace Similardio
{
	public class ArtistAdapter : BaseAdapter
	{
		ObservableCollection<ArtistData> data;
		BitmapCache cache;
		HttpClient client = new HttpClient ();
		Context context;

		public ArtistAdapter (Context ctx)
		{
			context = ctx;
			data = new ObservableCollection<ArtistData> ();
			data.CollectionChanged += (sender, e) => NotifyDataSetChanged ();
			cache = BitmapCache.CreateCache (ctx, "similardio-artist");
		}

		public ICollection<ArtistData> Data {
			get {
				return data;
			}
		}

		public override Java.Lang.Object GetItem (int position)
		{
			return new Java.Lang.String (data[position].Name);
		}

		public override long GetItemId (int position)
		{
			return data [position].MbID.GetHashCode ();
		}

		public ArtistData GetArtistData (int position)
		{
			return data [position];
		}

		public override View GetView (int position, View convertView, ViewGroup parent)
		{
			var view = EnsureView (convertView);
			var artist = data [position];

			view.ArtistName = artist.Name;
			view.Score = artist.Match;
			view.SetSimilarArtists (artist.SimilarArtists);
			RetrieveArtistPicture (view, artist);

			return view;
		}

		async void RetrieveArtistPicture (ArtistItemView view, ArtistData artist)
		{
			Bitmap bmp = null;
			if (cache.TryGet (artist.PictureUrl, out bmp)) {
				view.SetArtistImage (bmp, immediate: true);
				return;
			}
			var id = view.VersionID;
			bmp = await FetchBitmap (artist.PictureUrl);
			if (view.VersionID == id)
				view.SetArtistImage (bmp, immediate: false);
		}

		async Task<Bitmap> FetchBitmap (string url)
		{
			var imgData = await client.GetByteArrayAsync (url).ConfigureAwait (false);
			var bmp = BitmapFactory.DecodeByteArray (imgData, 0, imgData.Length);
			cache.AddOrUpdate (url, bmp, TimeSpan.FromDays (30));
			return bmp;
		}

		ArtistItemView EnsureView (View convertView)
		{
			var view = convertView as ArtistItemView;
			if (view == null)
				view = new ArtistItemView (context);
			else {
				Interlocked.Increment (ref view.VersionID);
				view.Reset ();
			}
			return view;
		}

		public override int Count {
			get {
				return data.Count;
			}
		}

		public override bool HasStableIds {
			get {
				return true;
			}
		}

		public override bool AreAllItemsEnabled ()
		{
			return true;
		}

		public override bool IsEnabled (int position)
		{
			return true;
		}
	}
}

