using LightUpUI.Presentation;

namespace LightUpTest.Presentation;

public sealed class TileDropPolicyTests
{
    [Theory]
    [InlineData(true, "https://example.com", TileExternalDropKind.File)]
    [InlineData(false, "https://example.com", TileExternalDropKind.Url)]
    [InlineData(false, "plain text", TileExternalDropKind.InvalidText)]
    [InlineData(false, "", TileExternalDropKind.None)]
    public void Classify_recognizes_supported_and_rejected_external_payloads(
        bool containsFiles,
        string text,
        TileExternalDropKind expected)
    {
        var result = TileDropPolicy.Classify(containsFiles, text);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Classify_ignores_the_internal_tile_drag_payload()
    {
        var result = TileDropPolicy.Classify(false, TileDragPayload.Create("tile-123"));

        Assert.Equal(TileExternalDropKind.None, result);
    }

    [Theory]
    [InlineData(TileExternalDropKind.File, "释放以添加文件或文件夹")]
    [InlineData(TileExternalDropKind.Url, "释放以添加网站快捷方式")]
    [InlineData(TileExternalDropKind.InvalidText, "仅支持文件、文件夹或 HTTP(S) 地址")]
    public void GetFeedback_returns_a_clear_drag_message(TileExternalDropKind kind, string expected)
    {
        Assert.Equal(expected, TileDropPolicy.GetFeedback(kind));
    }
}
