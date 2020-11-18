using CloudStreamForms.Droid.Effects;
using CloudStreamForms.Effects;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
//https://alexdunn.org/2017/12/27/xamarin-tip-xamarin-forms-long-press-effect/
[assembly: ResolutionGroupName("CloudStreamForms")]
[assembly: ExportEffect(typeof(AndroidLongPressedEffect), "LongPressedEffect")]
namespace CloudStreamForms.Droid.Effects
{
	/// <summary>
	/// Android long pressed effect.
	/// </summary>
	public class AndroidLongPressedEffect : PlatformEffect
	{
		private bool _attached;

		/// <summary>
		/// Initializer to avoid linking out
		/// </summary>
		public static void Initialize() { }

		/// <summary>
		/// Initializes a new instance of the
		/// <see cref="T:Yukon.Application.AndroidComponents.Effects.AndroidLongPressedEffect"/> class.
		/// Empty constructor required for the odd Xamarin.Forms reflection constructor search
		/// </summary>
		public AndroidLongPressedEffect()
		{
		}

		/// <summary>
		/// Apply the handler
		/// </summary>
		protected override void OnAttached()
		{
			//because an effect can be detached immediately after attached (happens in listview), only attach the handler one time.
			if (!_attached) {
				if (Control != null) {
					//Control.LongClickable = true;
					//Control.LongClick += Control_LongClick;
					Control.Touch += Container_Touch;

				}
				else {
					//Container.LongClickable = true;
					//Container.LongClick += Control_LongClick;
					Container.Touch += Container_Touch;
				}
				_attached = true;
			}
		}
		//https://stackoverflow.com/questions/7414065/android-scale-animation-on-view
		private void Container_Touch(object sender, Android.Views.View.TouchEventArgs e)
		{
			const float scaleDownTo = 0.95f;
			const int duration = 50;

			var ani = new Android.Views.Animations.ScaleAnimation(scaleDownTo, 1f, scaleDownTo, 1f, Android.Views.Animations.Dimension.RelativeToSelf, 0f, Android.Views.Animations.Dimension.RelativeToSelf, 1f);
			var ani2 = new Android.Views.Animations.ScaleAnimation(1f, scaleDownTo, 1f, scaleDownTo,Android.Views.Animations.Dimension.RelativeToSelf,0f, Android.Views.Animations.Dimension.RelativeToSelf,1f);

			ani.FillAfter = true;
			ani.Duration = duration;
			ani2.FillAfter = true;
			ani2.Duration = duration;
			var s = (Android.Views.View)sender;
			switch (e.Event.Action) {
				case Android.Views.MotionEventActions.ButtonPress:
					break;
				case Android.Views.MotionEventActions.ButtonRelease:
					break;
				case Android.Views.MotionEventActions.Cancel:
					s.StartAnimation(ani);
					break;
				case Android.Views.MotionEventActions.Down:
					s.StartAnimation(ani2);
					break;
				case Android.Views.MotionEventActions.HoverEnter:
					break;
				case Android.Views.MotionEventActions.HoverExit:
					break;
				case Android.Views.MotionEventActions.HoverMove:
					break;
				case Android.Views.MotionEventActions.Mask:
					break;
				case Android.Views.MotionEventActions.Move:
					break;
				case Android.Views.MotionEventActions.Outside:
					break;
				case Android.Views.MotionEventActions.Up: 
					s.StartAnimation(ani);

					var command = LongPressedEffect.GetCommand(Element);
					command?.Execute(LongPressedEffect.GetCommandParameter(Element));
					break;
				default:
					break;
			}
		}

		/// <summary>
		/// Invoke the command if there is one
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="e">E.</param>
		private void Control_LongClick(object sender, Android.Views.View.LongClickEventArgs e)
		{
			 //Console.WriteLine("Invoking long click command");
			var command = LongPressedEffect.GetCommand(Element);
			command?.Execute(LongPressedEffect.GetCommandParameter(Element));
		}

		/// <summary>
		/// Clean the event handler on detach
		/// </summary>
		protected override void OnDetached()
		{
			if (_attached) {
				if (Control != null) {
					Control.LongClickable = true;
					Control.LongClick -= Control_LongClick;
				}
				else {
					Container.LongClickable = true;
					Container.LongClick -= Control_LongClick;
				}
				_attached = false;
			}
		}
	}
}