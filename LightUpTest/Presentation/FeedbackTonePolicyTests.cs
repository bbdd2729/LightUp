using LightUpUI.Presentation;

namespace LightUpTest.Presentation;

public sealed class FeedbackTonePolicyTests
{
    [Fact]
    public void Busy_state_takes_priority_over_status_text()
    {
        Assert.Equal(FeedbackTone.Busy, FeedbackTonePolicy.FromStatus("保存失败", isBusy: true));
    }

    [Theory]
    [InlineData("已保存", FeedbackTone.Success)]
    [InlineData("操作成功", FeedbackTone.Success)]
    [InlineData("请确认删除", FeedbackTone.Warning)]
    [InlineData("操作已取消", FeedbackTone.Warning)]
    [InlineData("打开失败", FeedbackTone.Error)]
    [InlineData("目标不可用", FeedbackTone.Error)]
    [InlineData("输入应用名称开始搜索", FeedbackTone.Info)]
    public void Status_text_maps_to_the_expected_feedback_tone(string status, FeedbackTone expected)
    {
        Assert.Equal(expected, FeedbackTonePolicy.FromStatus(status));
    }

    [Fact]
    public void Empty_status_is_informational()
    {
        Assert.Equal(FeedbackTone.Info, FeedbackTonePolicy.FromStatus(null));
        Assert.Equal(FeedbackTone.Info, FeedbackTonePolicy.FromStatus("  "));
    }
}
