using AnyCAD.Demo;
using System;
using System.Collections.Generic;

namespace AnyCAD.WPF
{
    /// <summary>
    /// 聚合三个 bundle（Basic / Adv / QuickSolid）下的所有 TestCase，
    /// 为 Gallery 面板生成带截图路径的 SampleInfo 列表。
    /// </summary>
    internal static class TestCaseLoader
    {
        /// <summary>bundle 短徽标 -> 中文</summary>
        static readonly Dictionary<string, string> BundleBadges = new()
        {
            { "Basic", "基础" },
            { "Adv",   "高级" },
            { "Mfg",   "制造" },
            { "Ptc",   "点云" },
            { "QuickSolid", "QS" },
        };

        /// <summary>group 英文 -> 中文（覆盖 TestCaseLoaderBase.GetUIName 未包含的项）</summary>
        static readonly Dictionary<string, string> ExtraGroupNames = new()
        {
            { "Interop",  "数据交换" },
            { "Welcome",  "欢迎" },
            { "Curve",    "曲线" },
        };

        /// <summary>收集全部 bundle 的用例。</summary>
        public static List<SampleInfo> LoadAll()
        {
            var list = new List<SampleInfo>();
            CollectInto("Basic",       TestCaseLoaderBase.ForEachCase,        list);
            CollectInto("Adv",        TestCaseLoaderSimulate.ForEachCase,    list);
            CollectInto("Mfg",        TestCaseLoaderMfg.ForEachCase,         list);
            CollectInto("Ptc",        TestCaseLoaderPtc.ForEachCase,         list);
            CollectInto("QuickSolid", TestCaseLoaderQuickSolid.ForEachCase,  list);

            // 排序：bundle -> group 中文 -> 名称
            list.Sort((a, b) =>
            {
                int c = string.CompareOrdinal(a.Bundle, b.Bundle);
                if (c != 0) return c;
                c = string.CompareOrdinal(a.GroupDisplayName, b.GroupDisplayName);
                if (c != 0) return c;
                return string.CompareOrdinal(a.Name, b.Name);
            });
            return list;
        }

        static void CollectInto(string bundle,
            Action<AnyCAD.Demo.TestCase.TestCaseHandler> forEach,
            List<SampleInfo> sink)
        {
            string badge = BundleBadges.TryGetValue(bundle, out var b) ? b : bundle;
            forEach((type, name, group) =>
            {
                string groupUi = GetGroupDisplayName(group);
                var meta = ReadMetadata(type, name);
                sink.Add(new SampleInfo
                {
                    SampleType = type,
                    Name = name,
                    Group = group,
                    Bundle = bundle,
                    BundleBadge = badge,
                    GroupDisplayName = groupUi,
                    // 缩略图改为异步加载：见 MainViewModel.LoadThumbnailsAsync，
                    // 避免在 UI 线程同步解码 120 张 PNG 拖慢启动。
                    Title = meta.Title,
                    Description = meta.Description,
                    Tags = meta.Tags,
                    Level = meta.Level,
                });
            });
        }

        /// <summary>实例化用例类型读取中文元数据（Title/Description/Tags/Level）。
        /// 任何异常都兜底为 Title=name、Tags=空列表、Level=0、Description=空串，
        /// 单实例失败不得中断整体加载。</summary>
        static (string Title, string Description, List<string> Tags, int Level) ReadMetadata(Type type, string fallbackName)
        {
            try
            {
                var instance = (AnyCAD.Demo.TestCase)Activator.CreateInstance(type)!;
                var tags = instance.Tags == null
                    ? new List<string>()
                    : new List<string>(instance.Tags);
                return (
                    string.IsNullOrEmpty(instance.Title) ? fallbackName : instance.Title,
                    instance.Description ?? "",
                    tags,
                    instance.Level);
            }
            catch
            {
                return (fallbackName, "", new List<string>(), 0);
            }
        }

        static string GetGroupDisplayName(string group)
        {
            // 优先复用 AnyCAD.Basic 里已有的中文化映射
            try { return TestCaseLoaderBase.GetUIName(group); }
            catch { /* QuickSolid 工程不引用时兜底 */ }
            if (ExtraGroupNames.TryGetValue(group, out var ui)) return ui;
            return group;
        }
    }
}
