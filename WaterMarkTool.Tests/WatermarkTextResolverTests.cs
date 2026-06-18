using WaterMarkTool.Models;
using WaterMarkTool.Services;

namespace WaterMarkTool.Tests;

public class WatermarkTextResolverTests
{
    private static WatermarkRenderContext CreateContext(
        int index = 2,
        int total = 5,
        int frameIndex = 3,
        int frameTotal = 10,
        string fileName = "photo.jpg",
        string customText = "")
    {
        return new WatermarkRenderContext
        {
            Index = index,
            Total = total,
            FrameIndex = frameIndex,
            FrameTotal = frameTotal,
            FileName = fileName,
            CustomText = customText
        };
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ContainsPlaceholders_ReturnsFalse_ForBlankText(string? text)
    {
        Assert.False(WatermarkTextResolver.ContainsPlaceholders(text));
    }

    [Theory]
    [InlineData("plain text")]
    [InlineData("no variables here")]
    public void ContainsPlaceholders_ReturnsFalse_WhenNoPlaceholders(string text)
    {
        Assert.False(WatermarkTextResolver.ContainsPlaceholders(text));
    }

    [Theory]
    [InlineData("{index}")]
    [InlineData("{frameTotal}")]
    [InlineData("{xx}")]
    [InlineData("prefix {date} suffix")]
    [InlineData("contains XX")]
    public void ContainsPlaceholders_ReturnsTrue_WhenPlaceholderPresent(string text)
    {
        Assert.True(WatermarkTextResolver.ContainsPlaceholders(text));
    }

    [Fact]
    public void Resolve_ReturnsEmpty_ForBlankText()
    {
        Assert.Equal(string.Empty, WatermarkTextResolver.Resolve(null, CreateContext()));
        Assert.Equal(string.Empty, WatermarkTextResolver.Resolve("  ", CreateContext()));
    }

    [Fact]
    public void Resolve_ReplacesIndexTotalFrameAndFrameTotal()
    {
        var context = CreateContext();
        var result = WatermarkTextResolver.Resolve("{index}/{total} frame {frame}/{frameTotal}", context);

        Assert.Equal("2/5 frame 3/10", result);
    }

    [Fact]
    public void Resolve_IsCaseInsensitive_ForFrameTotal()
    {
        var context = CreateContext();
        var result = WatermarkTextResolver.Resolve("{FrameTotal} {FRAMETOTAL}", context);

        Assert.Equal("10 10", result);
    }

    [Fact]
    public void Resolve_ReplacesDateTimeAndFilename()
    {
        var context = CreateContext(fileName: "vacation.png");
        var result = WatermarkTextResolver.Resolve("{filename}", context);

        Assert.Equal("vacation", result);
    }

    [Fact]
    public void Resolve_UsesFallbackFilename_WhenEmpty()
    {
        var context = CreateContext(fileName: string.Empty);
        var result = WatermarkTextResolver.Resolve("{filename}", context);

        Assert.Equal("图片", result);
    }

    [Fact]
    public void Resolve_XxPlaceholder_FallsBackToXx_WhenCustomTextEmpty()
    {
        var result = WatermarkTextResolver.Resolve("{xx}", CreateContext());

        Assert.Equal("XX", result);
    }

    [Fact]
    public void Resolve_XxPlaceholder_UsesCustomText()
    {
        var result = WatermarkTextResolver.Resolve("{xx}", CreateContext(customText: "ABC"));

        Assert.Equal("ABC", result);
    }

    [Fact]
    public void Resolve_ReplacesLiteralXx_WithCustomText()
    {
        var result = WatermarkTextResolver.Resolve("编号XX结束", CreateContext(customText: "123"));

        Assert.Equal("编号123结束", result);
    }

    [Fact]
    public void Resolve_PreservesUnknownPlaceholders()
    {
        var result = WatermarkTextResolver.Resolve("{unknown} {not_a_placeholder}", CreateContext());

        Assert.Equal("{unknown} {not_a_placeholder}", result);
    }

    [Fact]
    public void Resolve_ReplacesMixedPlaceholders()
    {
        var context = CreateContext(customText: "VAR");
        var result = WatermarkTextResolver.Resolve("No.{index}/{total}-{xx}", context);

        Assert.Equal("No.2/5-VAR", result);
    }
}
