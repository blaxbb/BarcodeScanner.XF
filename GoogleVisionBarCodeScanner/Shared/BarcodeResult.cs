﻿using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace GoogleVisionBarCodeScanner
{
    public class BarcodeResult : IScanResult
    {
        public BarcodeTypes BarcodeType { get; set; }
        public string Value { get; set; }
        public List<(double x, double y)> Points { get; set; }

        public override string ToString()
        {
            return $"{Value}\n{BarcodeType}";
        }
    }
}
