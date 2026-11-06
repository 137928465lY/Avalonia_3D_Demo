using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace AnyCAD.Demo
{
    /// <summary>单个示例的中文元数据缓存；搜索/标签过滤都在缓存上做，不重复实例化。</summary>
    public class SampleMeta
    {
        public Type Type = null!;
        public string Name = "";
        public string RootName = "";
        public string Group = "";
        public string Title = "";
        public string Description = "";
        public List<string> Tags = new();
    }

    public class TestCaseLoader : TestCaseLoaderBase
    {
        /// <summary>全部示例元数据，按 bundle -> 加载顺序。</summary>
        public static readonly List<SampleMeta> Samples = new();

        /// <summary>标签过滤选项："全部" + 出现次数最多的前 10 个标签。</summary>
        public static readonly List<string> AllTags = new();

        public static void Register(TreeView tv)
        {
            Samples.Clear();
            Collect("窗体示例", h => TestCase.ForEachCase(h, Assembly.GetExecutingAssembly()));
            Collect("基础功能", h => ForEachCase(h));
            Collect("运动仿真", h => TestCaseLoaderSimulate.ForEachCase(h));
            Collect("加工制造", h => TestCaseLoaderMfg.ForEachCase(h));
            Collect("点云处理", h => TestCaseLoaderPtc.ForEachCase(h));
            Collect("高级建模", h => TestCaseLoaderQuickSolid.ForEachCase(h));

            AllTags.Clear();
            AllTags.Add("全部");
            foreach (var g in Samples.SelectMany(s => s.Tags)
                .GroupBy(t => t)
                .OrderByDescending(g => g.Count())
                .ThenBy(g => g.Key, StringComparer.Ordinal)
                .Take(10))
            {
                AllTags.Add(g.Key);
            }

            PopulateTree(tv, "全部");
        }

        static void Collect(string rootName, Action<TestCase.TestCaseHandler> forEach)
        {
            forEach((type, name, group) =>
            {
                var meta = ReadMetadata(type, name);
                meta.RootName = rootName;
                meta.Group = group;
                Samples.Add(meta);
            });
        }

        /// <summary>实例化用例类型读取中文元数据；任何异常兜底为 Title=英文名、Tags 空列表，单示例失败不中断。</summary>
        static SampleMeta ReadMetadata(Type type, string fallbackName)
        {
            var meta = new SampleMeta { Type = type, Name = fallbackName, Title = fallbackName };
            try
            {
                var instance = (TestCase)Activator.CreateInstance(type)!;
                meta.Title = string.IsNullOrEmpty(instance.Title) ? fallbackName : instance.Title;
                meta.Description = instance.Description ?? "";
                meta.Tags = instance.Tags == null ? new List<string>() : new List<string>(instance.Tags);
            }
            catch
            {
                meta.Title = fallbackName;
                meta.Description = "";
                meta.Tags = new List<string>();
            }
            return meta;
        }

        /// <summary>按标签过滤结果重建树：4 个根节点与分组结构保留，叶子 Tag 仍为 CLR Type。</summary>
        public static void PopulateTree(TreeView tv, string tag)
        {
            tv.BeginUpdate();
            try
            {
                tv.Nodes.Clear();
                foreach (var rootGroup in Samples.GroupBy(s => s.RootName))
                {
                    var rootNode = tv.Nodes.Add(rootGroup.Key);
                    foreach (var g in rootGroup.GroupBy(s => s.Group))
                    {
                        var groupNode = rootNode.Nodes.Add(GetUIName(g.Key));
                        foreach (var s in g)
                        {
                            if (!Match(s, tag))
                                continue;
                            var node = groupNode.Nodes.Add($"{s.Title} ({s.Name})");
                            node.Tag = s.Type;
                            node.ToolTipText = string.IsNullOrEmpty(s.Description)
                                ? string.Join("、", s.Tags)
                                : $"{s.Description}｜{string.Join("、", s.Tags)}";
                        }
                        if (groupNode.Nodes.Count == 0)
                            rootNode.Nodes.Remove(groupNode);
                    }
                    if (rootNode.Nodes.Count == 0)
                        tv.Nodes.Remove(rootNode);
                }
                tv.ExpandAll();
            }
            finally
            {
                tv.EndUpdate();
            }
        }

        /// <summary>标签匹配：tag="全部" 时全部通过；否则示例 Tags 必须包含该标签（Tags null 短路）。</summary>
        static bool Match(SampleMeta s, string tag)
        {
            if (!string.Equals(tag, "全部", StringComparison.Ordinal)
                && (s.Tags == null || !s.Tags.Contains(tag, StringComparer.Ordinal)))
                return false;

            return true;
        }
    }
}