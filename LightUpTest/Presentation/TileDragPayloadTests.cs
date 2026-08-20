using LightUpUI.Presentation;

namespace LightUpTest.Presentation;

public sealed class TileDragPayloadTests
{
    [Fact]
    public void Create_and_parse_round_trip_a_tile_identifier()
    {
        var payload = TileDragPayload.Create("tile-123");

        var parsed = TileDragPayload.TryParse(payload, out var tileId);

        Assert.True(parsed);
        Assert.Equal("tile-123", tileId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("plain text")]
    [InlineData("lightup-tile:")]
    [InlineData("other:tile-123")]
    public void TryParse_rejects_non_tile_payloads(string payload)
    {
        var parsed = TileDragPayload.TryParse(payload, out var tileId);

        Assert.False(parsed);
        Assert.Equal(string.Empty, tileId);
    }
}
