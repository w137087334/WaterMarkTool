namespace WaterMarkTool.Models;

public record WatermarkTemplate(string Name, string Text);

public static class WatermarkTemplates
{
    public static IReadOnlyList<WatermarkTemplate> All { get; } =
    [
        new("办事模板", "仅用于办理XX业务，他用无效"),
        new("认证模板", "仅用于XX认证，他用无效"),
        new("复印核验", "复印件与原件相符"),
        new("证件模板", "该证件仅供XX查看，不得他用"),
        new("内部资料", "内部资料，请勿外传"),
        new("版权声明", "版权所有，严禁外传")
    ];
}
