using System;
using System.Collections.Generic;
using System.Text;

namespace GoogleVisionBarCodeScanner
{
    public interface IScanResult
    {
        List<(double x, double y)> Points { get; set; }
        string Value { get; set; }
    }
}
