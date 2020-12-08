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

		bool autoCancel = true;

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
					autoCancel = false;
					//Container.SetOnTouchListener(new OnTouchListener(LongPressedEffect.GetCommand(Element), LongPressedEffect.GetCommandParameter(Element)));
				}
				_attached = true;
			}
		}

		bool isHolding = false;

		float lastX = -1;
		float lastY = -1;
		readonly Android.Graphics.Rect _rect = new Android.Graphics.Rect();
		readonly int[] _location = new int[2];
		bool IsViewInBounds(Android.Views.View v, int x, int y)
		{
			v.GetDrawingRect(_rect);
			v.GetLocationOnScreen(_location);
			_rect.Offset(_location[0], _location[1]);
			return _rect.Contains(x, y);
		}

		//https://stackoverflow.com/questions/7414065/android-scale-animation-on-view
		private void Container_Touch(object sender, Android.Views.View.TouchEventArgs e)
		{
			const float scaleDownTo = 0.98f;
			const float fadeTo = 0.7f;
			const int duration = 50;

			var ani = new Android.Views.Animations.ScaleAnimation(scaleDownTo, 1f, scaleDownTo, 1f, Android.Views.Animations.Dimension.RelativeToSelf, 0.5f, Android.Views.Animations.Dimension.RelativeToSelf, 0.5f);
			var ani2 = new Android.Views.Animations.ScaleAnimation(1f, scaleDownTo, 1f, scaleDownTo, Android.Views.Animations.Dimension.RelativeToSelf, 0.5f, Android.Views.Animations.Dimension.RelativeToSelf, 0.5f);
			var fadeAni = new Android.Views.Animations.AlphaAnimation(fadeTo, 1f);
			var fadeAni2 = new Android.Views.Animations.AlphaAnimation(1f, fadeTo);

			ani.FillAfter = true;
			ani.Duration = duration;
			ani2.FillAfter = true;
			ani2.Duration = duration;

			fadeAni.FillAfter = true;
			fadeAni.Duration = duration;
			fadeAni2.FillAfter = true;
			fadeAni2.Duration = duration;

			Android.Views.Animations.AnimationSet onCancelAni = new Android.Views.Animations.AnimationSet(true);
			onCancelAni.AddAnimation(fadeAni);
			onCancelAni.AddAnimation(ani);

			Android.Views.Animations.AnimationSet onHoldAni = new Android.Views.Animations.AnimationSet(true);
			onHoldAni.AddAnimation(fadeAni2);
			onHoldAni.AddAnimation(ani2);

			onHoldAni.FillAfter = true;
			onCancelAni.FillAfter = true;
			onHoldAni.Duration = duration;
			onCancelAni.Duration = duration;
			var command = LongPressedEffect.GetCommand(Element);
			var s = (Android.Views.View)sender;

			void Cancel()
			{
				if (isHolding) {
					isHolding = false;
					s.StartAnimation(onCancelAni);
				}
			}

			void Start()
			{
				if (!isHolding) {
					isHolding = true;
					s.StartAnimation(onHoldAni);
				}
			}
			System.Console.WriteLine("ACTION:" + e.Event.Action + "|" + e.Event.Action.HasFlag(Android.Views.MotionEventActions.Cancel));
			e.Handled = true;

			switch (e.Event.Action) {
				case Android.Views.MotionEventActions.ButtonPress:
					break;
				case Android.Views.MotionEventActions.ButtonRelease:
					if (isHolding) {
						Cancel();
						command?.Execute(LongPressedEffect.GetCommandParameter(Element));
					}
					break;
				case Android.Views.MotionEventActions.Cancel:
					Cancel();
					e.Handled = true;
					break;

				case Android.Views.MotionEventActions.Down:

					lastX = e.Event.RawX;
					lastY = e.Event.RawY;
					Start();
					break;
				case Android.Views.MotionEventActions.HoverEnter:
					break;
				case Android.Views.MotionEventActions.HoverExit:
					Cancel(); break;
				case Android.Views.MotionEventActions.HoverMove:
					break;
				case Android.Views.MotionEventActions.Mask:
					break;
				case Android.Views.MotionEventActions.Move:
					if (!autoCancel) {
						if (System.MathF.Sqrt(System.MathF.Pow(e.Event.RawX - lastX, 2) + System.MathF.Pow(e.Event.RawY - lastY, 2)) > 5) {
							Cancel();
						}
						else if (!IsViewInBounds(s, (int)e.Event.RawX, (int)e.Event.RawY)) {
							Cancel();
						}
					}
					/*if (!((Android.Views.View)sender).ClipBounds.Contains(new Android.Graphics.Rect(x - 1, y + 1, x - 1, y - 1))) {
						Cancel();
					}*/
					break;
				case Android.Views.MotionEventActions.Outside:
					Cancel(); break;
				case Android.Views.MotionEventActions.Up:
					if (isHolding) {
						Cancel();
						e.Handled = true;
						command?.Execute(LongPressedEffect.GetCommandParameter(Element));
					}
					break;
				case Android.Views.MotionEventActions.Scroll:
					Cancel();
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
		/*private void Control_LongClick(object sender, Android.Views.View.LongClickEventArgs e)
		{
			//Console.WriteLine("Invoking long click command");
			var command = LongPressedEffect.LongGetCommand(Element);
			command?.Execute(LongPressedEffect.LongGetCommandParameter(Element));
		}*/

		/// <summary>
		/// Clean the event handler on detach
		/// </summary>
		protected override void OnDetached()
		{
			if (_attached) {
				if (Control != null) {
					Control.Touch -= Container_Touch;

					/*
					Control.LongClickable = true;
					Control.LongClick -= Control_LongClick;*/
				}
				else {
					Container.Touch -= Container_Touch;
					/*
					Container.LongClickable = true;
					Container.LongClick -= Control_LongClick;*/
				}
				_attached = false;
			}
		}
	}
}