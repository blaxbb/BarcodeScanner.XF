using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace GoogleVisionBarCodeScanner
{
    public class BarcodeResult
    {
        public BarcodeTypes BarcodeType { get; set; }
        public string DisplayValue { get; set; }
        public List<(double x, double y)> Points { get; set; }
    }
}
