﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AVFoundation;
using CoreGraphics;
using CoreImage;
using CoreMedia;
using CoreVideo;
using Firebase.MLKit.Vision;
using Foundation;
using AudioToolbox;
using UIKit;
using System.Drawing;
using System.Threading;


namespace GoogleVisionBarCodeScanner
{
    internal sealed class UICameraPreview : UIView
    {
        public event Action<List<BarcodeResult>> OnDetected;
        AVCaptureVideoPreviewLayer previewLayer;
        CaptureVideoDelegate captureVideoDelegate;
        //CameraOptions cameraOptions;
        public AVCaptureSession CaptureSession { get; private set; }
        AVCaptureVideoDataOutput VideoDataOutput { get; set; }

        //public UICameraPreview(CameraOptions options)
        //{
        //    cameraOptions = options;
        //    IsPreviewing = false;
        //    Initialize();
        //}

        public UICameraPreview(bool defaultTorchOn, bool vibrationOnDetected, bool startScanningOnCreate)
        {
            //cameraOptions = options;
            Initialize(defaultTorchOn, vibrationOnDetected, startScanningOnCreate);
        }
        public override void RemoveFromSuperview()
        {
            base.RemoveFromSuperview();
            //Off the torch when exit page
            if (GoogleVisionBarCodeScanner.Methods.IsTorchOn())
                GoogleVisionBarCodeScanner.Methods.ToggleFlashlight();
            //Stop the capture session if not null
            try
            {
                if (CaptureSession != null)
                    CaptureSession.StopRunning();
            }
            catch
            {

            }

        }

        public override void LayoutSubviews()
        {
            base.LayoutSubviews();
            setPreviewOrientation();
        }
        private void updatePreviewLayer(AVCaptureConnection layer, AVCaptureVideoOrientation orientation)
        {
            layer.VideoOrientation = orientation;
            previewLayer.Frame = Bounds;
        }
        private void setPreviewOrientation()
        {
            var connection = previewLayer.Connection;
            if (connection != null)
            {
                var currentDevice = UIDevice.CurrentDevice;
                UIInterfaceOrientation orientation = UIApplication.SharedApplication.Windows.FirstOrDefault()?.WindowScene?.InterfaceOrientation ?? UIInterfaceOrientation.Portrait;
                
                var previewLayerConnection = connection;
                if (previewLayerConnection.SupportsVideoOrientation)
                {
                    switch (orientation)
                    {
                        case UIInterfaceOrientation.Portrait:
                            updatePreviewLayer(previewLayerConnection, AVCaptureVideoOrientation.Portrait);
                            break;
                        case UIInterfaceOrientation.LandscapeRight:
                            updatePreviewLayer(previewLayerConnection, AVCaptureVideoOrientation.LandscapeLeft);
                            break;
                        case UIInterfaceOrientation.LandscapeLeft:
                            updatePreviewLayer(previewLayerConnection, AVCaptureVideoOrientation.LandscapeRight);
                            break;
                        case UIInterfaceOrientation.PortraitUpsideDown:
                            updatePreviewLayer(previewLayerConnection, AVCaptureVideoOrientation.PortraitUpsideDown);
                            break;
                        default:
                            updatePreviewLayer(previewLayerConnection, AVCaptureVideoOrientation.Portrait);
                            break;
                    }
                }
            }
        }
        void Initialize(bool defaultTorchOn, bool vibrationOnDetected, bool startScanningOnCreate)
        {
            Configuration.IsBarcodeScanning = startScanningOnCreate;
            CaptureSession = new AVCaptureSession();
            CaptureSession.BeginConfiguration();
            this.AutoresizingMask = UIViewAutoresizing.FlexibleDimensions;
            previewLayer = new AVCaptureVideoPreviewLayer(CaptureSession)
            {
                Frame = this.Bounds,
                VideoGravity = AVLayerVideoGravity.ResizeAspectFill
            };
            var videoDevices = AVCaptureDevice.DevicesWithMediaType(AVMediaType.Video);
            var cameraPosition = AVCaptureDevicePosition.Back;
            //var cameraPosition = (cameraOptions == CameraOptions.Front) ? AVCaptureDevicePosition.Front : AVCaptureDevicePosition.Back;
            var device = videoDevices.FirstOrDefault(d => d.Position == cameraPosition);


            if (device == null)
                return;

            NSError error;
            var input = new AVCaptureDeviceInput(device, out error);

            CaptureSession.AddInput(input);
            CaptureSession.SessionPreset = AVFoundation.AVCaptureSession.Preset1280x720;
            Layer.AddSublayer(previewLayer);

            CaptureSession.CommitConfiguration();



            VideoDataOutput = new AVCaptureVideoDataOutput
            {
                AlwaysDiscardsLateVideoFrames = true,
                WeakVideoSettings = new CVPixelBufferAttributes { PixelFormatType = CVPixelFormatType.CV32BGRA }
                    .Dictionary
            };


            captureVideoDelegate = new CaptureVideoDelegate(vibrationOnDetected);
            captureVideoDelegate.OnBarcodeDetected += (list) =>
            {
                InvokeOnMainThread(() => {
                    //CaptureSession.StopRunning();
                    this.OnDetected?.Invoke(list);
                });

            };
            VideoDataOutput.SetSampleBufferDelegateQueue(captureVideoDelegate, CoreFoundation.DispatchQueue.MainQueue);

            CaptureSession.AddOutput(VideoDataOutput);
            InvokeOnMainThread(() =>
            {
                CaptureSession.StartRunning();
                //Torch on by default
                if (defaultTorchOn && !GoogleVisionBarCodeScanner.Methods.IsTorchOn())
                    GoogleVisionBarCodeScanner.Methods.ToggleFlashlight();
            });


        }

        public class CaptureVideoDelegate : AVCaptureVideoDataOutputSampleBufferDelegate
        {
            public event Action<List<BarcodeResult>> OnBarcodeDetected;
            public event Action<List<TextResult>> OnTextDetected;
            VisionBarcodeDetector barcodeDetector;
            VisionTextRecognizer textDetector;
            VisionImageMetadata metadata;
            VisionApi vision;
            bool _vibrationOnDetected = true;
            int scanIntervalInMs = 1000;
            long lastAnalysisTime = DateTimeOffset.MinValue.ToUnixTimeMilliseconds();
            long lastRunTime = DateTimeOffset.MinValue.ToUnixTimeMilliseconds();

            float Width;
            float Height;

            public CaptureVideoDelegate(bool vibrationOnDetected)
            {
                _vibrationOnDetected = vibrationOnDetected;
                metadata = new VisionImageMetadata();
                vision = VisionApi.Create();
                barcodeDetector = vision.GetBarcodeDetector(Configuration.BarcodeDetectorSupportFormat);
                textDetector = vision.GetOnDeviceTextRecognizer();
                // Using back-facing camera
                var devicePosition = AVCaptureDevicePosition.Back;
                var deviceOrientation = UIDevice.CurrentDevice.Orientation;
                switch (deviceOrientation)
                {
                    case UIDeviceOrientation.Portrait:
                        metadata.Orientation = devicePosition == AVCaptureDevicePosition.Front ? VisionDetectorImageOrientation.LeftTop : VisionDetectorImageOrientation.RightTop;
                        break;
                    case UIDeviceOrientation.LandscapeLeft:
                        metadata.Orientation = devicePosition == AVCaptureDevicePosition.Front ? VisionDetectorImageOrientation.BottomLeft : VisionDetectorImageOrientation.TopLeft;
                        break;
                    case UIDeviceOrientation.PortraitUpsideDown:
                        metadata.Orientation = devicePosition == AVCaptureDevicePosition.Front ? VisionDetectorImageOrientation.RightBottom : VisionDetectorImageOrientation.LeftBottom;
                        break;
                    case UIDeviceOrientation.LandscapeRight:
                        metadata.Orientation = devicePosition == AVCaptureDevicePosition.Front ? VisionDetectorImageOrientation.TopRight : VisionDetectorImageOrientation.BottomRight;
                        break;
                    case UIDeviceOrientation.FaceUp:
                    case UIDeviceOrientation.FaceDown:
                    case UIDeviceOrientation.Unknown:
                        metadata.Orientation = VisionDetectorImageOrientation.LeftTop;
                        break;
                }
            }


            private static UIImage GetImageFromSampleBuffer(CMSampleBuffer sampleBuffer)
            {
                // Get a pixel buffer from the sample buffer
                using (var pixelBuffer = sampleBuffer.GetImageBuffer() as CVPixelBuffer)
                {
                    // Lock the base address
                    if (pixelBuffer != null)
                    {
                        pixelBuffer.Lock(CVPixelBufferLock.None);

                        // Prepare to decode buffer
                        var flags = CGBitmapFlags.PremultipliedFirst | CGBitmapFlags.ByteOrder32Little;

                        // Decode buffer - Create a new colorspace
                        using (var cs = CGColorSpace.CreateDeviceRGB())
                        {
                            // Create new context from buffer
                            using (var context = new CGBitmapContext(pixelBuffer.BaseAddress,
                                pixelBuffer.Width,
                                pixelBuffer.Height,
                                8,
                                pixelBuffer.BytesPerRow,
                                cs,
                                (CGImageAlphaInfo)flags))
                            {
                                // Get the image from the context
                                using (var cgImage = context.ToImage())
                                {
                                    // Unlock and return image
                                    pixelBuffer.Unlock(CVPixelBufferLock.None);
                                    return UIImage.FromImage(cgImage);
                                }
                            }
                        }
                    }
                    else
                    {
                        return null;
                    }
                }
            }

            private void releaseSampleBuffer(CMSampleBuffer sampleBuffer)
            {
                if (sampleBuffer != null)
                {
                    sampleBuffer.Dispose();
                    sampleBuffer = null;
                }
            }
            public override void DidOutputSampleBuffer(AVCaptureOutput captureOutput, CMSampleBuffer sampleBuffer, AVCaptureConnection connection)
            {
                lastRunTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                if (lastRunTime - lastAnalysisTime > scanIntervalInMs && Configuration.IsBarcodeScanning)
                {
                    lastAnalysisTime = lastRunTime;
                    try
                    {
                        var image = GetImageFromSampleBuffer(sampleBuffer);
                        if (image == null) return;
                        Width = (float)image.Size.Width;
                        Height = (float)image.Size.Height;
                        var visionImage = new VisionImage(image) { Metadata = metadata };
                        releaseSampleBuffer(sampleBuffer);
                        DetectBarcodeActionAsync(visionImage);
                    }
                    catch (Exception exception)
                    {
                        System.Diagnostics.Debug.WriteLine(exception.Message);
                    }
                }
                releaseSampleBuffer(sampleBuffer);
            }
            private async void DetectBarcodeActionAsync(VisionImage image)
            {
                if (Configuration.IsBarcodeScanning)
                {
                    try
                    {
                        VisionBarcode[] barcodes = await barcodeDetector.DetectAsync(image);
                        if (barcodes == null || barcodes.Length == 0)
                        {
                            return;
                        }
                        Console.WriteLine($"Successfully read barcode");
                        Configuration.IsBarcodeScanning = false;
                        if (_vibrationOnDetected)
                            SystemSound.Vibrate.PlayAlertSound();
                        List<BarcodeResult> resultList = new List<BarcodeResult>();

                        foreach (var barcode in barcodes)
                        {
                            var points = barcode.CornerPoints.ToList().ConvertAll(nsvalue => nsvalue.PointFValue);

                            resultList.Add(new BarcodeResult
                            {
                                BarcodeType = Methods.ConvertBarcodeResultTypes(barcode.ValueType),
                                Value = barcode.DisplayValue,
                                Points = points.Select(p => (p.X / (double)Width, p.Y / (double)Height)).ToList()
                            });
                        }
                        OnBarcodeDetected?.Invoke(resultList);
                    }
                    catch (Exception exception)
                    {
                        System.Diagnostics.Debug.WriteLine(exception.Message);
                    }
                }


            }
            private async void DetectTextActionAsync(VisionImage image)
            {
                if (Configuration.IsTextScanning)
                {
                    try
                    {
                        VisionText processed = await textDetector.ProcessImageAsync(image);
                        var blocks = processed.Blocks;
                        if (blocks == null || blocks.Length == 0)
                        {
                            return;
                        }
                        Console.WriteLine($"Successfully read text");
                        Configuration.IsTextScanning = false;
                        if (_vibrationOnDetected)
                            SystemSound.Vibrate.PlayAlertSound();
                        var resultList = new List<TextResult>();

                        foreach (var textBlock in blocks)
                        {
                            var points = textBlock.CornerPoints.ToList().ConvertAll(nsvalue => nsvalue.PointFValue);

                            resultList.Add(new TextResult
                            {
                                Value = textBlock.Text,
                                Points = points.Select(p => (p.X / (double)Width, p.Y / (double)Height)).ToList()
                            });
                        }
                        OnTextDetected?.Invoke(resultList);
                    }
                    catch (Exception exception)
                    {
                        System.Diagnostics.Debug.WriteLine(exception.Message);
                    }
                }


            }
        }

    }
}

