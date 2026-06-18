using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace WaterMarkTool.Services;

internal static class PixelConverter
{
    public static Bitmap ToGdiBitmap(Image<Rgba32> image)
    {
        var bitmap = new Bitmap(image.Width, image.Height, PixelFormat.Format32bppArgb);
        var pixelBytes = new byte[image.Width * image.Height * 4];
        image.CopyPixelDataTo(pixelBytes);
        SwapRedBlueChannels(pixelBytes);

        var bounds = new System.Drawing.Rectangle(0, 0, image.Width, image.Height);
        var bitmapData = bitmap.LockBits(bounds, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

        try
        {
            var rowBytes = image.Width * 4;
            for (var y = 0; y < image.Height; y++)
            {
                Marshal.Copy(pixelBytes, y * rowBytes, bitmapData.Scan0 + (y * bitmapData.Stride), rowBytes);
            }
        }
        finally
        {
            bitmap.UnlockBits(bitmapData);
        }

        return bitmap;
    }

    /// <summary>
    /// Converts a single (already composed) GIF frame to a GDI bitmap, flattening transparency
    /// onto a white background in a single pixel pass written directly into the GDI buffer. This
    /// avoids the previous per-frame PNG round-trip and extra bitmap copies, which is critical for
    /// large animations with hundreds of frames (memory + speed). Pixels matching the GIF
    /// background color (regions ImageSharp composes for RestoreToBackground disposal) are also
    /// treated as transparent and flattened to white, matching typical viewer behavior.
    /// </summary>
    public static Bitmap GifFrameToGdiBitmap(Image<Rgba32> animation, int frameIndex, Rgba32? backgroundColor = null)
    {
        var width = animation.Width;
        var height = animation.Height;
        var frame = animation.Frames[frameIndex];

        var hasBackground = backgroundColor.HasValue;
        var bg = backgroundColor.GetValueOrDefault();

        var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        var bounds = new System.Drawing.Rectangle(0, 0, width, height);
        var bitmapData = bitmap.LockBits(bounds, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

        try
        {
            var stride = bitmapData.Stride;
            var scan0 = bitmapData.Scan0;
            frame.ProcessPixelRows(accessor =>
            {
                var rowBuffer = new byte[width * 4];
                for (var y = 0; y < accessor.Height; y++)
                {
                    var sourceRow = accessor.GetRowSpan(y);
                    for (var x = 0; x < sourceRow.Length; x++)
                    {
                        var pixel = sourceRow[x];
                        byte r, g, b;
                        if (pixel.A == 0 ||
                            (hasBackground && pixel.A == 255 && pixel.R == bg.R && pixel.G == bg.G && pixel.B == bg.B))
                        {
                            r = g = b = 255;
                        }
                        else if (pixel.A == 255)
                        {
                            r = pixel.R;
                            g = pixel.G;
                            b = pixel.B;
                        }
                        else
                        {
                            int a = pixel.A;
                            int inv = 255 - a;
                            r = (byte)((pixel.R * a + 255 * inv) / 255);
                            g = (byte)((pixel.G * a + 255 * inv) / 255);
                            b = (byte)((pixel.B * a + 255 * inv) / 255);
                        }

                        var offset = x * 4;
                        rowBuffer[offset] = b;
                        rowBuffer[offset + 1] = g;
                        rowBuffer[offset + 2] = r;
                        rowBuffer[offset + 3] = 255;
                    }

                    Marshal.Copy(rowBuffer, 0, scan0 + (y * stride), rowBuffer.Length);
                }
            });
        }
        finally
        {
            bitmap.UnlockBits(bitmapData);
        }

        return bitmap;
    }

    public static Image<Rgba32> FromGdiBitmap(Bitmap bitmap)
    {
        Bitmap? converted = null;

        try
        {
            var source = bitmap;
            if (bitmap.PixelFormat != PixelFormat.Format32bppArgb)
            {
                converted = new Bitmap(bitmap.Width, bitmap.Height, PixelFormat.Format32bppArgb);
                using var graphics = Graphics.FromImage(converted);
                graphics.DrawImage(bitmap, 0, 0, bitmap.Width, bitmap.Height);
                source = converted;
            }

            var pixelBytes = new byte[source.Width * source.Height * 4];
            var bounds = new System.Drawing.Rectangle(0, 0, source.Width, source.Height);
            var bitmapData = source.LockBits(bounds, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            try
            {
                var rowBytes = source.Width * 4;
                for (var y = 0; y < source.Height; y++)
                {
                    Marshal.Copy(bitmapData.Scan0 + (y * bitmapData.Stride), pixelBytes, y * rowBytes, rowBytes);
                }

                SwapRedBlueChannels(pixelBytes);
            }
            finally
            {
                source.UnlockBits(bitmapData);
            }

            return SixLabors.ImageSharp.Image.LoadPixelData<Rgba32>(pixelBytes, source.Width, source.Height);
        }
        finally
        {
            converted?.Dispose();
        }
    }

    /// <summary>
    /// ImageSharp Rgba32 is RGBA byte order; GDI Format32bppArgb is BGRA in memory.
    /// </summary>
    private static void SwapRedBlueChannels(byte[] pixelBytes)
    {
        for (var i = 0; i < pixelBytes.Length; i += 4)
        {
            (pixelBytes[i], pixelBytes[i + 2]) = (pixelBytes[i + 2], pixelBytes[i]);
        }
    }
}
