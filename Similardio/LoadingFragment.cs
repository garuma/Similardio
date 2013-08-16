using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Android.Animation;

using Runnable = Java.Lang.Runnable;

namespace Similardio
{
	public class LoadingFragment : Fragment
	{
		ImageView logoImage;
		TextView explanation;
		TextView extraExplanation;

		AnimatorSet logoAnimation;
		bool showExtraOnStartup;

		public override void OnCreate (Bundle savedInstanceState)
		{
			base.OnCreate (savedInstanceState);
		}

		public override View OnCreateView (LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
		{
			var view = inflater.Inflate (Resource.Layout.LoadingScreen, container, false);
			logoImage = view.FindViewById<ImageView> (Resource.Id.logoImage);
			explanation = view.FindViewById<TextView> (Resource.Id.explText);
			extraExplanation = view.FindViewById<TextView> (Resource.Id.extraExpl);
			if (showExtraOnStartup)
				extraExplanation.Visibility = ViewStates.Visible;
			return view;
		}

		public override void OnViewCreated (View view, Bundle savedInstanceState)
		{
			base.OnViewCreated (view, savedInstanceState);

			var interpolator = new Android.Views.Animations.AccelerateDecelerateInterpolator ();

			var xDelta = 12.ToPixels ();
			var yDelta = 8.ToPixels ();

			var transX = ObjectAnimator.OfFloat (logoImage, "translationX", -xDelta, xDelta);
			var transY = ObjectAnimator.OfFloat (logoImage, "translationY", -yDelta, 0, -yDelta);
			var rot = ObjectAnimator.OfFloat (logoImage, "rotation", -1.2f, 1.2f);

			// Ad infinitam
			transX.RepeatCount = transY.RepeatCount = rot.RepeatCount = -1;
			transX.RepeatMode = transY.RepeatMode = rot.RepeatMode = ValueAnimatorRepeatMode.Reverse;

			logoAnimation = new AnimatorSet ();
			logoAnimation.PlayTogether (transX, transY, rot);
			logoAnimation.SetDuration (800);
			logoAnimation.SetInterpolator (interpolator);
			logoAnimation.Start ();
		}

		public void ChangeLoading (int logoResID, string text)
		{
			logoImage.Animate ().Alpha (0).SetDuration (400).WithEndAction (new Runnable (() => {
				logoImage.SetImageResource (logoResID);
				logoImage.Animate ().Alpha (1).SetDuration (300).Start ();
				var length = View.Width;
				explanation.Animate ().TranslationX (length).SetDuration (150).WithEndAction (new Runnable (() => {
					explanation.Text = text;
					explanation.TranslationX = -length;
					explanation.Animate ().TranslationX (0).SetDuration (150).Start ();
				})).Start ();
				if (extraExplanation.Visibility == ViewStates.Visible)
					extraExplanation.Animate ()
						.Alpha (0)
						.SetDuration (150)
						.WithEndAction (new Runnable (() => extraExplanation.Visibility = ViewStates.Invisible)).Start ();
			})).Start ();
		}

		public void SetExtraExplanation (string extra)
		{
			if (extraExplanation == null) {
				showExtraOnStartup = true;
				return;
			}
			extraExplanation.Text = extra;
			if (extraExplanation.Visibility != ViewStates.Visible) {
				extraExplanation.Alpha = 0;
				extraExplanation.Visibility = ViewStates.Visible;
				extraExplanation.Animate ().Alpha (1).SetDuration (150).Start ();
			}
		}
	}
}

