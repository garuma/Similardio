using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Renderscripts;

using Runnable = Java.Lang.Runnable;

namespace Similardio
{
	public class ArtistItemView : FrameLayout
	{
		int DefaultTranslation;
		double score;

		ImageView artistImage;
		TextView artistName;
		ImageButton questionButton;
		TextView libraryHeader;
		FrameLayout drawer;
		ProgressBar artistImageLoading;
		TextView artistScore;
		ArrayAdapter similarArtistAdapter;
		ListView similarArtistList;

		Bitmap originalImage;
		Bitmap blurredImage;

		bool opened;

		public long VersionID;

		public ArtistItemView (Context context) :
			base (context)
		{
			Initialize ();
		}

		public ArtistItemView (Context context, IAttributeSet attrs) :
			base (context, attrs)
		{
			Initialize ();
		}

		public ArtistItemView (Context context, IAttributeSet attrs, int defStyle) :
			base (context, attrs, defStyle)
		{
			Initialize ();
		}

		void Initialize ()
		{
			DefaultTranslation = 208.ToPixels ();
			var inflater = Context.GetSystemService (Context.LayoutInflaterService).JavaCast<LayoutInflater> ();
			inflater.Inflate (Resource.Layout.ArtistItemLayout, this, true);
			artistImage = FindViewById<ImageView> (Resource.Id.ArtistBgImage);
			artistName = FindViewById<TextView> (Resource.Id.artistName);
			questionButton = FindViewById<ImageButton> (Resource.Id.questionBtn);
			libraryHeader = FindViewById<TextView> (Resource.Id.libraryHeaderText);
			drawer = FindViewById<FrameLayout> (Resource.Id.ArtistDrawer);
			artistImageLoading = FindViewById<ProgressBar> (Resource.Id.artistImageLoading);
			artistScore = FindViewById<TextView> (Resource.Id.ArtistScore);

			questionButton.SetImageResource (Resource.Drawable.question_btn);
			questionButton.Click += (s, e) => Opened = !Opened;

			similarArtistList = FindViewById<ListView> (Resource.Id.similarArtistsList);
			similarArtistAdapter = new ArrayAdapter (Context, Resource.Layout.SimilarArtistItem);
			similarArtistList.Adapter = similarArtistAdapter;
		}

		public string ArtistName {
			get {
				return artistName.Text;
			}
			set {
				artistName.Text = value;
			}
		}

		public double Score {
			get {
				return score;
			}
			set {
				score = value;
				score = Math.Min (Math.Max (0, score), 1);
				artistScore.Text = ((int)(score * 100)).ToString () + "%";
			}
		}

		public void SetSimilarArtists (string[] similarArtists)
		{
			similarArtistAdapter.Clear ();
			var array = similarArtists.Select (sa => (Java.Lang.Object)new Java.Lang.String (sa)).ToArray ();
			similarArtistAdapter.AddAll (array);
		}

		public void SetArtistImage (Bitmap bmp, bool immediate = false)
		{
			artistImage.SetImageBitmap (bmp);
			originalImage = bmp;
			blurredImage = null;
			if (!immediate) {
				artistImageLoading.Visibility = ViewStates.Invisible;
				artistImage.Alpha = 0;
				artistImage.Visibility = ViewStates.Visible;
				artistImage.Animate ().Alpha (1).Start ();
			} else {
				artistImageLoading.Visibility = ViewStates.Invisible;
				artistImage.Visibility = ViewStates.Visible;
			}
		}

		Bitmap BlurImage (Bitmap input)
		{
			try {
				var rsScript = RenderScript.Create (Context);
				var alloc = Allocation.CreateFromBitmap (rsScript, input);
				var blur = ScriptIntrinsicBlur.Create (rsScript, alloc.Element);
				blur.SetRadius (12);
				blur.SetInput (alloc);
				var result = Bitmap.CreateBitmap (input.Width, input.Height, input.GetConfig ());
				var outAlloc = Allocation.CreateFromBitmap (rsScript, result);
				blur.ForEach (outAlloc);
				outAlloc.CopyTo (result);
				rsScript.Destroy ();
				return result;
			} catch (Exception e) {
				Log.Error ("Blurrer", "Error while trying to blur, fallbacking. " + e.ToString ());
				return input;
			}
		}

		public async void SetBlurry (bool blur)
		{
			if (blurredImage == null && blur)
				blurredImage = await Task.Run (() => BlurImage (originalImage));
			artistImage.SetImageBitmap (blur ? blurredImage : originalImage);
		}

		public void Reset ()
		{
			artistImage.Visibility = ViewStates.Invisible;
			artistImageLoading.Visibility = ViewStates.Visible;
			opened = false;
			SetBlurry (false);
			drawer.TranslationY = DefaultTranslation;
			questionButton.SetImageResource (Resource.Drawable.question_btn);
			libraryHeader.Alpha = 0;
			libraryHeader.Visibility = ViewStates.Invisible;
			artistName.Visibility = ViewStates.Visible;
			artistName.Alpha = 1;
		}

		public bool Opened {
			get {
				return opened;
			}
			set {
				if (opened == value)
					return;
				opened = value;
				SetBlurry (opened);
				if (opened) {
					artistScore.Visibility = ViewStates.Invisible;
					questionButton.SetImageResource (Resource.Drawable.close_btn);
					artistName.Visibility = ViewStates.Invisible;
					libraryHeader.Alpha = 0;
					libraryHeader.Visibility = ViewStates.Visible;
					drawer.Animate ().TranslationY (0).Start ();
					libraryHeader.Animate ().Alpha (1).Start ();
				} else {
					questionButton.SetImageResource (Resource.Drawable.question_btn);
					drawer.Animate ().TranslationY (DefaultTranslation).Start ();
					libraryHeader.Animate ().Alpha (0).WithEndAction (new Runnable (() => {
						libraryHeader.Visibility = ViewStates.Invisible;
						artistName.Alpha = 0;
						artistName.Visibility = ViewStates.Visible;
						artistName.Animate ().Alpha (1).Start ();
						artistScore.Visibility = ViewStates.Visible;
					})).Start ();
				}
			}
		}
	}
}

