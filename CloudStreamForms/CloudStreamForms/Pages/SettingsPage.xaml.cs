using CloudStreamForms.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace CloudStreamForms.Pages
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class SettingsPage : ContentPage
    {
        public struct SettingsHolder
        {
            public string header;
            public SettingsItem[] settings;
        }

        public class SettingsItem
        {
            public string img;
            public string mainTxt;
            public string descriptTxt;
        }

        public static SettingsHolder UISettings = new SettingsHolder() {
            header = "UI",
            settings = new SettingsItem[] {
                new SettingsItem() { img= "MainSearchIcon.png",mainTxt="QuickSearch",descriptTxt="Search every caracter"}
            },
        };

        public static List<SettingsHolder> settings = new List<SettingsHolder>() {
            UISettings,

        };

        public SettingsPage()
        {
            InitializeComponent();
            BackgroundColor = Settings.BlackRBGColor;
            int counter = 0;

            void AddChild(View v)
            {
                AddTextGrid.Children.Add(v);
                Grid.SetRow(v, counter);
                counter++;
            }

            foreach (var set in settings) {
                AddChild(new Label() { Text = set.header, FontSize = 17, FontAttributes = FontAttributes.Bold, TranslationX = 10, Margin = new Thickness(10, 20, 10, 10), TextColor = Color.White });

                foreach (var subSet in set.settings) {

                    var mainLabel = new Label() { Text = subSet.mainTxt, VerticalOptions = LayoutOptions.Center, FontSize = 16, TextColor = Color.FromHex("#e6e6e6") };
                    var sublabel = new Label() { Text = subSet.descriptTxt, VerticalOptions = LayoutOptions.Center, FontSize = 12, TextColor = Color.FromHex("#AAA") };
                    var _img = new FFImageLoading.Forms.CachedImage() {
                        HorizontalOptions = LayoutOptions.Start,
                        WidthRequest = 25,
                        HeightRequest = 25,
                        Margin = new Thickness(5),
                        TranslationX = 5,
                        VerticalOptions = LayoutOptions.Center,
                        Source = App.GetImageSource(subSet.img),
                    };

                    var subGrid = new Grid() {
                        HeightRequest = 50,
                        RowSpacing = -10,
                        VerticalOptions = LayoutOptions.Center,
                      
                    };
                    subGrid.Children.Add(mainLabel);
                    if(subSet.descriptTxt.IsClean()) {
                        subGrid.Children.Add(sublabel); 
                    }

                    List<View> mainChilds = new List<View>() {
                          new Button() {
                            BackgroundColor = Color.FromHex( "#141414")
                        },

                        new Grid() {
                            InputTransparent = true,
                            ColumnSpacing=10,

                            ColumnDefinitions = new ColumnDefinitionCollection() {
                                new ColumnDefinition() {
                                    Width = GridLength.Auto
                                },
                                new ColumnDefinition() {
                                    Width = GridLength.Star
                                },
                                new ColumnDefinition() {
                                    Width = GridLength.Star
                                }
                            },
                            Children = {
                               _img,
                               subGrid,
                            }
                        }
                    };
                    if (true) {
                        mainChilds.Insert(1, new Switch() {
                            VerticalOptions = LayoutOptions.Center,
                            HorizontalOptions = LayoutOptions.End,
                        });
                    };

                    var _grid = new Grid() {
                        HeightRequest = 70,
                    };
                    foreach (var _child in mainChilds) {
                        _grid.Children.Add(_child);
                    }
                    AddChild(_grid);
                    if (subSet.descriptTxt.IsClean()) {
                        Grid.SetRow(sublabel, 1);
                    }
                    Grid.SetColumn(subGrid, 1);
                }
            }


        }
    }
}