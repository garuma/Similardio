using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;

namespace Similardio
{
	public class RdioListFragment : ListFragment
	{
		bool shownToast;

		public override void OnViewCreated (View view, Bundle savedInstanceState)
		{
			base.OnViewCreated (view, savedInstanceState);
			ListView.ItemLongClick += (sender, e) => {
				var artist = ((ArtistAdapter)ListAdapter).GetArtistData (e.Position);
				var name = artist.Name.Replace (' ', '_');
				name = Android.Net.Uri.Encode (name);
				var uri = "http://www.rdio.com/artist/" + name;
				var intent = new Intent (Intent.ActionView,
				                         Android.Net.Uri.Parse (uri));
				intent.SetPackage ("com.rdio.android.ui");
				StartActivity (intent);
			};
		}

		public override void OnListItemClick (ListView l, View v, int position, long id)
		{
			if (shownToast)
				return;
			shownToast = true;
			Toast.MakeText (Activity, "Long-click to launch in Rdio", ToastLength.Long).Show ();
		}
	}
}

