using GoogleVisionBarCodeScanner;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms.PlatformConfiguration.iOSSpecific;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using Xamarin.Forms.Shapes;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace SampleApp
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class Page4 : ContentPage, INotifyPropertyChanged
    {
        Dictionary<string, RealtimeBarcodeInfo> BarcodeInfo = new Dictionary<string, RealtimeBarcodeInfo>();

        public Page4()
        {
            InitializeComponent();
            BindingContext = this;
            GoogleVisionBarCodeScanner.Methods.SetSupportBarcodeFormat(BarcodeFormats.All);
            On<Xamarin.Forms.PlatformConfiguration.iOS>().SetUseSafeArea(true);
            Task.Run(ScheduleTick);
        }

        private async Task ScheduleTick()
        {
            await Task.Run(DoTick)
                .ContinueWith(async (task) => await ScheduleTick());
        }

        private async Task DoTick()
        {
            await Device.InvokeOnMainThreadAsync(() =>
            {
                List<string> toRemove = new List<string>();

                foreach (var kvp in BarcodeInfo)
                {
                    var info = kvp.Value;

                    if(DateTime.Now - info.LastScanned > TimeSpan.FromMilliseconds(500))
                    {
                        toRemove.Add(kvp.Key);
                        continue;
                    }

                    info.CurrentPosition = new Point(
                                                        Lerp(info.CurrentPosition.X, info.TargetPosition.X, .1d),
                                                        Lerp(info.CurrentPosition.Y, info.TargetPosition.Y, .1d)
                                                    );
                    info.Grid.Margin = new Thickness(info.CurrentPosition.X, info.CurrentPosition.Y, 0, 0);
                    info.TextLabel.Text = $"[{info.TargetPosition.X:0.00}, {info.TargetPosition.Y:0.00}]\n{info.Text}";
                }

                foreach(var remove in toRemove)
                {
                    var info = BarcodeInfo[remove];
                    grid.Children.Remove(info.OuterFrame);
                    BarcodeInfo.Remove(remove);
                }
            });


            await Task.Delay(1000 / 30);
        }


        private async void CancelButton_Clicked(object sender, EventArgs e)
        {
            await Navigation.PopModalAsync();
        }

        private void FlashlightButton_Clicked(object sender, EventArgs e)
        {
            GoogleVisionBarCodeScanner.Methods.ToggleFlashlight();
        }

        private async void CameraView_OnBarcodeDetected(object sender, GoogleVisionBarCodeScanner.OnBarcodeDetectedEventArg e)
        {
            Device.BeginInvokeOnMainThread(() => GoogleVisionBarCodeScanner.Methods.SetIsScanning(true));

            List<GoogleVisionBarCodeScanner.BarcodeResult> barcodes = e.BarcodeResults;

            foreach (var barcode in barcodes)
            {
                RealtimeBarcodeInfo info;
                
                var match = Regex.Match(barcode.Value, "\\d+\\.*\\d*$");
                if (!match.Success)
                {
                    continue;
                }
                if (!BarcodeInfo.ContainsKey(barcode.Value))
                {
                    //found new barcode

                    info = await CreateRealtimeInfo(barcode);
                }
                else
                {
                    info = BarcodeInfo[barcode.Value];
                }

                info.LastScanned = DateTime.Now;
                info.TargetPosition = GetTarget(barcode);
            }
        }

        Point GetTarget(IScanResult obj) => new Point(obj.Points[0].x * grid.Width, obj.Points[0].y * grid.Height);

        private async Task<RealtimeBarcodeInfo> CreateRealtimeInfo(IScanResult obj)
        {
            Point target = GetTarget(obj);
            RealtimeBarcodeInfo info = new RealtimeBarcodeInfo()
            {
                Text = obj.ToString(),
                CurrentPosition = target
            };
            BarcodeInfo[obj.Value] = info;

            await Device.InvokeOnMainThreadAsync(() =>
            {
                /*
                 * Create GUI for new barcode
                 * 
                 * Frame - transparent, fully covers camera view
                 *  -Grid  - margin is set on this to adjust position
                 *   -Label - Content
                 */

                var frame = new Frame()
                {
                    Padding = 0,
                    BackgroundColor = Color.Transparent
                };

                var barcodeGrid = new Grid()
                {
                    BackgroundColor = obj is BarcodeResult ? Color.Cyan : Color.GreenYellow,
                    Opacity = .85d,
                    Margin = new Thickness(0, 0, 0, 0),
                    WidthRequest = 200,
                    HeightRequest = 75,
                    HorizontalOptions = LayoutOptions.Start,
                    VerticalOptions = LayoutOptions.Start
                };
                barcodeGrid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Star });

                frame.Content = barcodeGrid;

                var textLabel = new Label()
                {
                    Text = info.Text
                };

                barcodeGrid.Children.Add(textLabel);
                Grid.SetRow(textLabel, 0);

                info.OuterFrame = frame;
                info.Grid = barcodeGrid;
                info.TextLabel = textLabel;

                grid.Children.Add(frame);
                Grid.SetRow(frame, 1);
            });
            return info;
        }

        private double Lerp(double current, double target, double dt)
        {
            return (1 - dt) * current + (dt * target);
        }

        protected bool SetProperty<T>(ref T backingStore, T value,
            [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;

            backingStore = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        private async void CameraView_OnTextDetected(object sender, OnTextDetectedEventArg e)
        {
            Device.BeginInvokeOnMainThread(() => GoogleVisionBarCodeScanner.Methods.SetIsScanning(true));

            List<GoogleVisionBarCodeScanner.TextResult> barcodes = e.TextResults;

            foreach (var barcode in barcodes)
            {
                RealtimeBarcodeInfo info;

                var match = Regex.Match(barcode.Value, "\\d+\\.*\\d*$");
                if (!match.Success)
                {
                    continue;
                }
                if (!BarcodeInfo.ContainsKey(barcode.Value))
                {
                    //found new barcode

                    info = await CreateRealtimeInfo(barcode);
                }
                else
                {
                    info = BarcodeInfo[barcode.Value];
                }

                info.LastScanned = DateTime.Now;
                info.TargetPosition = GetTarget(barcode);
            }
        }
    }
}