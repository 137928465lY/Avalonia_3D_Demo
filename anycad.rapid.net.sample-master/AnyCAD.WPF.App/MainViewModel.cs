using AnyCAD.Drawing;
using AnyCAD.Foundation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace AnyCAD.WPF
{
    internal partial class MainViewModel : ObservableObject
    {
        IRenderView mRenderView;

        // ---------- Gallery 数据 ----------

        /// <summary>全部用例（加载一次后不再变动）。</summary>
        public ObservableCollection<SampleInfo> AllSamples { get; } = new();

        /// <summary>经搜索与标签过滤后展示在网格里的用例。</summary>
        public ObservableCollection<SampleInfo> FilteredSamples { get; } = new();

        /// <summary>标签 chips（第一项固定为"全部"；按出现次数降序、再按中文序确定性排序，最多 10 个）。</summary>
        public ObservableCollection<string> AllTags { get; } = new();

        [ObservableProperty] private string _SelectedTag = "全部";
        [ObservableProperty] private string _SearchText = string.Empty;
        [ObservableProperty] private SampleInfo? _SelectedSample;

        // ---------- 状态栏 ----------

        [ObservableProperty] string _MousePosition = string.Empty;
        [ObservableProperty] string _SelectionInfo = string.Empty;

        public MainViewModel(IRenderView view)
        {
            mRenderView = view;

            var all = TestCaseLoader.LoadAll();
            foreach (var s in all) AllSamples.Add(s);

            // 标签 chips：全部示例 Tags 去重，按出现次数降序、再按字符串序确定性排序，前置"全部"；最多展示 10 个标签选项
            AllTags.Add("全部");
            foreach (var t in all.SelectMany(x => x.Tags)
                .GroupBy(t => t)
                .OrderByDescending(g => g.Count())
                .ThenBy(g => g.Key, StringComparer.Ordinal)
                .Take(10))
            {
                AllTags.Add(t.Key);
            }

            ApplyFilter();

            // 缩略图异步加载：UI 线程先把卡片骨架画出来，后台逐张解码 PNG，
            // 加载完的图片再回到 UI 线程回填 Thumbnail，避免启动卡顿。
            _ = LoadThumbnailsAsync();
        }

        /// <summary>
        /// 后台逐张加载 showcase 缩略图。文件 IO 与位图解码都在 ThreadPool 线程执行，
        /// 仅在回填 Thumbnail 属性时切回 UI 线程。
        /// </summary>
        async Task LoadThumbnailsAsync()
        {
            var snapshot = AllSamples.ToArray();
            var dispatcher = System.Windows.Application.Current?.Dispatcher;
            if (dispatcher == null) return;

            foreach (var s in snapshot)
            {
                BitmapImage? bmp = null;
                try
                {
                    bmp = await Task.Run(() => ShowcaseLocator.TryLoadThumbnail(s.Group, s.Name)).ConfigureAwait(true);
                }
                catch { /* 单张失败不影响其他 */ }

                if (bmp != null)
                {
                    await dispatcher.BeginInvoke(new Action(() => s.Thumbnail = bmp));
                }
            }
        }

        partial void OnSearchTextChanged(string value) => ApplyFilter();
        partial void OnSelectedTagChanged(string value) => ApplyFilter();

        void ApplyFilter()
        {
            IEnumerable<SampleInfo> q = AllSamples;

            if (!string.Equals(SelectedTag, "全部", StringComparison.Ordinal))
                q = q.Where(s => s.Tags != null && s.Tags.Contains(SelectedTag, StringComparer.Ordinal));

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                string key = SearchText.Trim();
                // 命中任一字段即匹配：Name/Group（原有）+ Title/Description/Tags 任意标签/SampleType 类名
                q = q.Where(s =>
                    s.Name.Contains(key, StringComparison.OrdinalIgnoreCase) ||
                    s.Group.Contains(key, StringComparison.OrdinalIgnoreCase) ||
                    (s.Title ?? "").Contains(key, StringComparison.OrdinalIgnoreCase) ||
                    (s.Description ?? "").Contains(key, StringComparison.OrdinalIgnoreCase) ||
                    (s.Tags != null && s.Tags.Any(t => t != null && t.Contains(key, StringComparison.OrdinalIgnoreCase))) ||
                    (s.SampleType?.Name ?? "").Contains(key, StringComparison.OrdinalIgnoreCase));
            }

            FilteredSamples.Clear();
            foreach (var s in q) FilteredSamples.Add(s);
        }

        // ---------- 渲染视图回调 ----------

        public void ViewReady()
        {
            mRenderView.SetHilightingCallback((PickedResult pr) =>
            {
                var pt = pr.GetItem().GetPosition();
                MousePosition = $"{pt.x} {pt.y} {pt.z}";
                return true;
            });

            mRenderView.SetAnimationCallback((ViewerListener.AnimationHandler)((float timer) =>
            {
                Demo.TestCase.RunAnimation(mRenderView, timer);
            }));
        }

        public void UpdateSelectionInfo(PickedResult pr)
        {
            var ss = mRenderView.SelectionManager.GetSelection();
            var count = ss.GetCount();
            var msg = $"Count: {count}";
            if (count == 1)
            {
                for (var itr = ss.CreateIterator(); itr.More(); itr.Next())
                {
                    var item = itr.Current();
                    msg += $"\nNodeId: {item.GetNodeId()}";
                    msg += $"\nObjectId: {item.GetId().GetId().Value}";
                    msg += $"\nUserId: {item.GetUserId()}";
                    msg += $"\nShapeId: {item.GetShapeIndex()}";
                    msg += $"\nPrimitiveId: {item.GetPrimitiveIndex()}";
                    msg += $"\nType: {item.GetShapeFilter().ToString()}";
                    msg += $"\nTopoShapeId: {item.GetTopoShapeId().ToString()}";
                    var pt = item.GetPosition();
                    msg += $"\nPosition: {pt.x} {pt.y} {pt.z}";

                    var shapeNode = BrepSceneNode.Cast(item.GetNode());
                    if (shapeNode != null)
                    {
                        var trf = shapeNode.GetWorldTransform().ToTrsf();
                        trf.Invert();
                        var pointOnShape = pt.ToPnt().Transformed(trf);
                        switch (item.GetShapeFilter())
                        {
                            case EnumShapeFilter.Edge:
                                {
                                    var edge = shapeNode.GetTopoShape().FindChild(EnumTopoShapeType.Topo_EDGE, (int)item.GetTopoShapeId());
                                    if (edge != null)
                                    {
                                        var pc = new ParametricCurve(edge);
                                        msg += $"\nEdge: {pc.GetCurveType().ToString()}";

                                        var epc = new ExtremaPointCurve();
                                        if (epc.Initialize(edge, pointOnShape) && epc.GetPointCount() > 0)
                                        {
                                            var param = epc.GetParameter(0);
                                            msg += $"\nU: {param}";
                                        }
                                    }
                                }
                                break;
                            case EnumShapeFilter.Face:
                                {
                                    var face = shapeNode.GetTopoShape().FindChild(EnumTopoShapeType.Topo_FACE, (int)item.GetTopoShapeId());
                                    if (face != null)
                                    {
                                        var ps = new ParametricSurface(face);
                                        msg += $"\nFace: {ps.GetSurfaceType().ToString()}";

                                        var uv = ps.ComputeClosestPoint(pointOnShape, GP.Resolution(), GP.Resolution());
                                        msg += $"\nUV: {uv.x} {uv.y}";
                                    }
                                }
                                break;
                            default:
                                msg += "\n^_^";
                                break;
                        }
                    }
                }
            }

            SelectionInfo = msg;
        }

        [RelayCommand]
        void OnNewScene() => mRenderView.ClearAll();

        [RelayCommand]
        void OnOpenModel()
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                DefaultExt = ".stp",
                Filter = "Models (*.igs;*.iges;*.stp;*.step;*.brep;*.stl)|*.igs;*.iges;*.stp;*.step;*.brep;*.stl"
            };
            if (dlg.ShowDialog() != true) return;

            var shape = ShapeIO.Open(dlg.FileName);
            if (shape == null) return;
            var node = BrepSceneNode.Create(shape, null, null, 0, false);
            if (node == null) return;

            mRenderView.ShowSceneNode(node);
            mRenderView.ZoomAll();
        }

        [RelayCommand]
        void OnOpenMesh()
        {
            var fileName = DialogUtil.OpenFileDialog("打开", SceneIO.ReaderFilters());
            if (fileName.IsEmpty()) return;

            var node = SceneIO.Load(fileName);
            if (node == null) return;

            mRenderView.ShowSceneNode(node);
            mRenderView.ZoomAll();
        }

        [RelayCommand]
        void OnOpenDrawing()
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                DefaultExt = ".json",
                Filter = "Drawing (*.json)|*.json"
            };
            if (dlg.ShowDialog() != true) return;

            var drawing = DrawingDb.Load(dlg.FileName);
            if (drawing != null) drawing.Show(mRenderView);
        }

        [RelayCommand]
        void OnPickEdge() => mRenderView.ViewContext.SetPickFilter(EnumShapeFilter.Edge);
    }
}
