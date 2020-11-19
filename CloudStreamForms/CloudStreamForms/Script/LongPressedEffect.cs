using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;
using Xamarin.Forms;

namespace CloudStreamForms.Effects
{
    /// <summary>
    /// Long pressed effect. Used for invoking commands on long press detection cross platform
    /// </summary>
    public class LongPressedEffect : RoutingEffect
    {
        public LongPressedEffect() : base("CloudStreamForms.LongPressedEffect")
        {
        }

        public static readonly BindableProperty CommandProperty = BindableProperty.CreateAttached("Command", typeof(ICommand), typeof(LongPressedEffect), (object)null);
        public static ICommand GetCommand(BindableObject view)
        {
            return (ICommand)view.GetValue(CommandProperty);
        }

        public static void SetCommand(BindableObject view, ICommand value)
        {
            view.SetValue(CommandProperty, value);
        }


        public static readonly BindableProperty CommandParameterProperty = BindableProperty.CreateAttached("CommandParameter", typeof(object), typeof(LongPressedEffect), (object)null);
        public static object GetCommandParameter(BindableObject view)
        {
            return view.GetValue(CommandParameterProperty);
        }

        public static void SetCommandParameter(BindableObject view, object value)
        {
            view.SetValue(CommandParameterProperty, value);
        }
        /*

        public static readonly BindableProperty LongCommandProperty = BindableProperty.CreateAttached("LongCommand", typeof(ICommand), typeof(LongPressedEffect), (object)null);
        public static ICommand LongGetCommand(BindableObject view)
        {
            return (ICommand)view.GetValue(LongCommandProperty);
        }

        public static void LongSetCommand(BindableObject view, ICommand value)
        {
            view.SetValue(LongCommandProperty, value);
        }

        public static readonly BindableProperty LongCommandParameterProperty = BindableProperty.CreateAttached("LongCommandParameter", typeof(object), typeof(LongPressedEffect), (object)null);
        public static object LongGetCommandParameter(BindableObject view)
        {
            return view.GetValue(LongCommandParameterProperty);
        }

        public static void LongSetCommandParameter(BindableObject view, object value)
        {
            view.SetValue(LongCommandParameterProperty, value);
        }*/
    }
}
