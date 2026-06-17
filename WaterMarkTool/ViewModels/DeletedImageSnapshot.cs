using System.Drawing;
using WaterMarkTool.Models;

namespace WaterMarkTool.ViewModels;

internal sealed class DeletedImageSnapshot : IDisposable
{
    public required WatermarkImageItem Item { get; init; }
    public int Index { get; init; }

    public void Dispose()
    {
        Item.Dispose();
    }
}
