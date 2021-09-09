using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace GoogleVisionBarCodeScanner
{
    public class CameraView : View
    {
        public static BindableProperty VibrationOnDetectedProperty = BindableProperty.Create(nameof(VibrationOnDetected), typeof(bool), typeof(CameraView), true);
        public bool VibrationOnDetected
        {
            get
            {
                return (bool)GetValue(VibrationOnDetectedProperty);
            }
            set
            {
                SetValue(VibrationOnDetectedProperty, value);
            }
        }


        public static BindableProperty DefaultTorchOnProperty = BindableProperty.Create(nameof(DefaultTorchOn), typeof(bool), typeof(CameraView), false);
        public bool DefaultTorchOn
        {
            get
            {
                return (bool)GetValue(DefaultTorchOnProperty);
            }
            set
            {
                SetValue(DefaultTorchOnProperty, value);
            }
        }

        public static BindableProperty AutoStartScanningProperty = BindableProperty.Create(nameof(AutoStartScanning), typeof(bool), typeof(CameraView), true);
        public bool AutoStartScanning
        {
            get
            {
                return (bool)GetValue(AutoStartScanningProperty);
            }
            set
            {
                SetValue(AutoStartScanningProperty, value);
            }
        }

        public static BindableProperty RequestedFPSProperty = BindableProperty.Create(nameof(RequestedFPS), typeof(float?), typeof(CameraView), null);
        /// <summary>
        /// Only Android will be reflected this setting
        /// </summary>
        public float? RequestedFPS
        {
            get
            {
                return (float?)GetValue(RequestedFPSProperty);
            }
            set
            {
                SetValue(RequestedFPSProperty, value);
            }
        }


        public event EventHandler<OnBarcodeDetectedEventArg> OnBarcodeDetected;
        public void TriggerOnBarcodeDetected(List<BarcodeResult> barCodeResults)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                OnBarcodeDetected?.Invoke(this, new OnBarcodeDetectedEventArg { BarcodeResults = barCodeResults });
            });
        }

        public event EventHandler<OnTextDetectedEventArg> OnTextDetected;
        public void TriggerOnTextDetected(List<TextResult> textResults)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                OnTextDetected?.Invoke(this, new OnTextDetectedEventArg { TextResults = textResults });
            });
        }
    }
    
    public class OnBarcodeDetectedEventArg : EventArgs
    {
        public List<BarcodeResult> BarcodeResults { get; set; }
        public OnBarcodeDetectedEventArg()
        {
            BarcodeResults = new List<BarcodeResult>();
        }
    }

    public class OnTextDetectedEventArg : EventArgs
    {
        public List<TextResult> TextResults { get; set; }
        public OnTextDetectedEventArg()
        {
            TextResults = new List<TextResult>();
        }
    }
}
