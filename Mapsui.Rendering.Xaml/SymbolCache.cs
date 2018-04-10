﻿using System;
using System.Collections.Generic;
using System.IO;
using Mapsui.Styles;
using System.Windows.Media.Imaging;
using System.Windows.Media;

namespace Mapsui.Rendering.Xaml
{
    public class SymbolCache : Dictionary<int, ImageSource>, ISymbolCache
    {
        public ImageSource GetOrCreate(int bitmapId)
        {
            if (ContainsKey(bitmapId)) return this[bitmapId];

            var obj = BitmapRegistry.Instance.Get(bitmapId);

            if (obj is Atlas atlas)
            {
                if (GetOrCreate(atlas.BitmapId) == null)
                    throw new AccessViolationException("Atlas bitmap unknown");

                var bitmapSource = new CroppedBitmap((BitmapImage)GetOrCreate(atlas.BitmapId),
                    new System.Windows.Int32Rect(atlas.X, atlas.Y, atlas.Width, atlas.Height));

                var encoder = new PngBitmapEncoder();
                var memoryStream = new MemoryStream();
                var bImg = new BitmapImage();

                encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
                encoder.Save(memoryStream);

                memoryStream.Position = 0;

                return this[bitmapId] = memoryStream.ToBitmapImage();
            }
            else
            {
                var stream = (Stream) obj;
                byte[] buffer = new byte[4];

                stream.Position = 0;
                stream.Read(buffer, 0, 4);

                if (System.Text.Encoding.UTF8.GetString(buffer).ToLower().Equals("<svg"))
                {
                    stream.Position = 0;
                    var image = Svg2Xaml.SvgReader.Load(stream);
                    // Freeze the DrawingImage for performance benefits.
                    image.Freeze();
                    return this[bitmapId] = image;
                }
                else
                    return this[bitmapId] = stream.ToBitmapImage();
            }
        }

        public Size GetSize(int bitmapId)
        {
            var brush = GetOrCreate(bitmapId);

            return new Size(brush.Width, brush.Height);
        }
    }
}