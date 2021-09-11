using Firebase.MLKit.Vision;
using System;
using System.Collections.Generic;
using System.Text;

namespace GoogleVisionBarCodeScanner
{
    internal class Configuration
    {
        internal static bool IsBarcodeScanning = true;
        internal static bool IsTextScanning = true;
        public static VisionBarcodeDetectorOptions BarcodeDetectorSupportFormat = new VisionBarcodeDetectorOptions(VisionBarcodeFormat.All);
    }
}
