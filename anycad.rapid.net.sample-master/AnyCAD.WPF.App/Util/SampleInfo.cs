using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Windows.Media.Imaging;

namespace AnyCAD.WPF
{
    /// <summary>
    /// 一个可浏览的示例条目：对应一个 TestCase 类型 + showcase 截图。
    /// 供 Gallery 面板的卡片绑定使用。Thumbnail 异步加载，加载完成后通过属性变更通知 UI。
    /// </summary>
    internal partial class SampleInfo : ObservableObject
    {
        /// <summary>用例的 CLR 类型，运行时传给 Demo.TestCase.CreateTest。</summary>
        public Type SampleType { get; init; } = null!;

        /// <summary>用例显示名（英文，如 "Loft"、"100000Box"）。</summary>
        public string Name { get; init; } = "";

        /// <summary>分组英文（Geometry/Graphics/Analysis/Interaction/Simulation/Interop/Welcome 等）。</summary>
        public string Group { get; init; } = "";

        /// <summary>所属 bundle：Basic / Adv / QuickSolid。</summary>
        public string Bundle { get; init; } = "";

        /// <summary>bundle 的短徽标文字，显示在卡片角落。</summary>
        public string BundleBadge { get; init; } = "";

        /// <summary>分组的中文显示名（造型/显示/分析/交互/仿真...）。</summary>
        public string GroupDisplayName { get; init; } = "";

        /// <summary>缩略图（已按窄宽度解码，节省内存）；无截图或尚未加载时为 null。后台异步回填。</summary>
        [ObservableProperty] private BitmapImage? _thumbnail;

        /// <summary>中文短标题（示例构造函数中定义；读取失败时退化为英文 Name）。</summary>
        public string Title { get; init; } = "";

        /// <summary>中文一句描述；读取失败时为空串。</summary>
        public string Description { get; init; } = "";

        /// <summary>中文标签（通常 2 个，首个为领域标签）；读取失败时为空列表。</summary>
        public List<string> Tags { get; init; } = new();

        /// <summary>难度等级：Basic=1、Advanced/QuickSolid=2、读取失败=0。</summary>
        public int Level { get; init; }
    }
}
