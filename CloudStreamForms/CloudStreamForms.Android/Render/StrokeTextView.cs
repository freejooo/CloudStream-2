using Android.Content;
using Android.Graphics;
using Android.Text;
using Android.Util;
using Android.Views;
using Android.Widget;
using static Android.Graphics.Paint;

namespace CloudStreamForms.Droid.Render
{
	class StrokeTextView : TextView
	{
		public TextView borderText = null;
		public StrokeTextView(Context context) : base(context)
		{
			borderText = new TextView(context);

			init();
		}
		public StrokeTextView(Context context, IAttributeSet attrs) : base(context, attrs)
		{
			borderText = new TextView(context, attrs);
			init();
		}
		public StrokeTextView(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
		{
			borderText = new TextView(context, attrs, defStyle);
			init();
		}



		public void init()
		{
			TextPaint tp1 = borderText.Paint;
			tp1.StrokeWidth = 5;         // sets the stroke width                        
			tp1.SetStyle(Style.Stroke);
			borderText.SetTextColor(Color.Black);  // set the stroke color
			borderText.Gravity = Gravity;
		}


		public override ViewGroup.LayoutParams LayoutParameters { get => base.LayoutParameters; set => base.LayoutParameters = value; }

		protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
		{
			string tt = borderText.Text;


			if (tt == null || !tt.Equals(this.Text)) {
				//borderText.Text = Text;
				this.PostInvalidate();
			}

			base.OnMeasure(widthMeasureSpec, heightMeasureSpec);
			borderText.Measure(widthMeasureSpec, heightMeasureSpec);
		}


		public void ChangeText()
		{
			/*
			if (borderText != null) {
				borderText.SetText(Text.ToArray(), 0, Text.Length);
				//borderText.PostInvalidate();
			}*/
		}

		protected override void OnLayout(bool changed, int left, int top, int right, int bottom)
		{
			base.OnLayout(changed, left, top, right, bottom);
			borderText.Layout(left, top, right, bottom);
		}

		protected override void OnDraw(Canvas canvas)
		{
			/*
            Paint strokePaint = new Paint();
            strokePaint.SetARGB(255, 0, 0, 0);
            strokePaint.TextAlign = Align.Center;
            strokePaint.TextSize = (16);
            strokePaint.SetTypeface(Typeface.DefaultBold);
			strokePaint.SetStyle(Style.Stroke);
            strokePaint.StrokeWidth = (2);

            Paint textPaint = new Paint();
            textPaint.SetARGB(255, 255, 255, 255);
            textPaint.TextAlign = Align.Center;
            textPaint.TextSize = (16);
            textPaint.SetTypeface(Typeface.DefaultBold);
             */

			borderText.Draw(canvas);
			base.OnDraw(canvas);
		}
	}

	class StrokeTextView2 : TextView
	{
		public int ShadowStr = 1;
		public StrokeTextView2(Context context) : base(context)
		{
		}
		public override ViewGroup.LayoutParams LayoutParameters { get => base.LayoutParameters; set => base.LayoutParameters = value; }

		protected override void OnDraw(Canvas canvas)
		{
			for (int i = 0; i < ShadowStr; i++) {
				base.OnDraw(canvas);
			}
			//base.OnDraw(canvas);
		}
	}

}