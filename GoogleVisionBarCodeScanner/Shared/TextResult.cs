using System;
using System.Collections.Generic;
using System.Text;

namespace GoogleVisionBarCodeScanner
{
    public class TextResult : IScanResult
    {
        public string Value { get; set; }
        public List<(double x, double y)> Points { get; set; }

        public override string ToString()
        {
            return Value;
        }
    }
}
