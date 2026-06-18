using System.Drawing;
using QRCoder;

namespace WaterMarkTool.Services;

public static class QrCodeGenerator
{
  public static Bitmap Generate(string content, int pixelsPerModule = 8)
  {
    if (string.IsNullOrWhiteSpace(content))
    {
      content = " ";
    }

    using var generator = new QRCodeGenerator();
    using var data = generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.M);
    using var qrCode = new QRCode(data);
    using var qrBitmap = qrCode.GetGraphic(pixelsPerModule);
    return new Bitmap(qrBitmap);
  }
}
