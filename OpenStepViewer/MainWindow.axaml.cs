using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using OpenStepViewer.Geom;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace OpenStepViewer
{
    public partial class MainWindow : Window
    {
        StepModel _model;
        Tessellator _tess = new Tessellator { Deflection = 0.8 };
        bool _wire;
        Avalonia.Point? _last;

        JointRecognizer.ChainResult _chain;
        readonly List<JointItem> _jointItems = new List<JointItem>();
        int _solidCount;
        int[] _solidStart = System.Array.Empty<int>();
        int[] _solidCounts = System.Array.Empty<int>();

        public MainWindow()
        {
            InitializeComponent();
            // 命令行传入 STEP 文件则自动加载
            var cliArgs = System.Environment.GetCommandLineArgs();
            try
            {
                System.IO.File.WriteAllText("osv_gui.log", "argc=" + cliArgs.Length + " args=" + string.Join(" | ", cliArgs));
            }
            catch { }
            if (cliArgs.Length >= 2)
            {
                var p = cliArgs[^1].Trim('"');
                if (System.IO.File.Exists(p))
                    _ = Load(p);
                else
                    StatusText.Text = "参数文件不存在: " + p;
            }
        }

        async void OnOpen(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "打开 STEP 模型",
                AllowMultiple = false,
                FileTypeFilter = new[] { new FilePickerFileType("STEP") { Patterns = new[] { "*.stp", "*.step" } } }
            });
            if (files.Count == 0) return;
            await Load(files[0].Path.LocalPath);
        }

        static void Log(string s)
        {
            try { System.IO.File.AppendAllText("osv_gui.log", DateTime.Now.ToString("HH:mm:ss") + " " + s + "\n"); } catch { }
        }

        public async Task Load(string path)
        {
            Log("Load 开始 " + path);
            StatusText.Text = "解析中: " + Path.GetFileName(path);
            var sw = System.Diagnostics.Stopwatch.StartNew();

            string err = null;
            Geom.MeshData mesh = null;
            await Task.Run(() =>
            {
                try
                {
                    _model = StepModel.Load(path, s => { });
                    FeatureRecognizer.Recognize(_model.AllFaces, _model.BBoxMin, _model.BBoxMax);
                    // 关节识别：同轴圆柱聚类 → 实体接触图 → 运动链
                    _chain = JointRecognizer.BuildChain(_model, JointRecognizer.DetectAxes(_model));
                    mesh = _tess.Tessellate(_model.AllFaces);
                }
                catch (Exception ex) { err = ex.ToString(); }
            });
            sw.Stop();
            if (err != null) { StatusText.Text = "错误: " + err; Log("Load 错误 " + err); return; }
            // 回到 UI 线程后再触碰 GL 资源
            Log("SetMesh 开始 tri=" + mesh.TriangleCount + " 失败面=" + _tess.Diag.Count);
            GlHost.SetMesh(mesh.Positions, mesh.Normals, mesh.Indices, mesh.BBoxMin, mesh.BBoxMax);
            _lastMesh = mesh;
            Log("SetMesh 完成");

            BuildJointUI();

            var faces = _model.AllFaces;
            StatusText.Text = $"{Path.GetFileName(path)}  实体:{_model.Solids.Count} 面:{faces.Count} " +
                              $"三角形:{_lastMesh.TriangleCount:N0}  关节:{_jointItems.Count}  用时:{sw.ElapsedMilliseconds} ms";
            FillFeatureList();
        }

        // ---------- 关节驱动 ----------

        // 网格按面顺序合并，实体占据连续的索引区间
        void BuildJointUI()
        {
            _solidCount = _model.Solids.Count;
            _solidStart = new int[_solidCount];
            _solidCounts = new int[_solidCount];
            int cur = 0;
            for (int i = 0; i < _solidCount; i++)
            {
                _solidStart[i] = cur;
                int c = 0;
                foreach (var f in _model.Solids[i].Faces) c += f.ICount;
                _solidCounts[i] = c;
                cur += c;
            }
            GlHost.SetParts(_solidStart, _solidCounts);

            _jointItems.Clear();
            if (_chain != null)
                foreach (var j in _chain.Joints)
                    _jointItems.Add(new JointItem
                    {
                        J = j,
                        BaseTitle = $"{j.Name}  轴心({j.Axis.O.X:0},{j.Axis.O.Y:0},{j.Axis.O.Z:0}) " +
                                    $"方向({j.Axis.Dir.X:0.##},{j.Axis.Dir.Y:0.##},{j.Axis.Dir.Z:0.##}) R{j.Axis.Radius:0.#}"
                    });
            JointPanel.ItemsSource = null;
            JointPanel.ItemsSource = _jointItems;

            // 调试用姿势：OSV_POSE="35,-45,0,0,0"
            var pose = System.Environment.GetEnvironmentVariable("OSV_POSE");
            if (!string.IsNullOrWhiteSpace(pose))
            {
                var parts = pose.Split(',');
                for (int i = 0; i < _jointItems.Count && i < parts.Length; i++)
                    if (double.TryParse(parts[i], out var a)) { _jointItems[i].J.AngleDeg = a; _jointItems[i].Angle = a; }
            }
            ApplyJoints();
        }

        void ApplyJoints()
        {
            if (_solidCount == 0) return;
            var chains = new System.Numerics.Matrix4x4[_solidCount][];
            for (int i = 0; i < _solidCount; i++)
            {
                var jc = _chain != null && i < _chain.JointChainOf.Count ? _chain.JointChainOf[i] : null;
                if (jc == null || jc.Count == 0) { chains[i] = System.Array.Empty<System.Numerics.Matrix4x4>(); continue; }
                var arr = new System.Numerics.Matrix4x4[jc.Count];
                for (int k = 0; k < jc.Count; k++) arr[k] = JointMatrix(_chain.Joints[jc[k]]);
                chains[i] = arr;
            }
            GlHost.SetPartChains(chains);
        }

        // 绕关节轴旋转 angleDeg：先平移到轴心、旋转、再平移回去
        static System.Numerics.Matrix4x4 JointMatrix(Joint j)
        {
            if (Math.Abs(j.AngleDeg) < 1e-9) return System.Numerics.Matrix4x4.Identity;
            var d = System.Numerics.Vector3.Normalize(j.Axis.Dir);
            var r = System.Numerics.Matrix4x4.CreateFromAxisAngle(d, (float)(j.AngleDeg * Math.PI / 180));
            return System.Numerics.Matrix4x4.CreateTranslation(-j.Axis.O) * r
                 * System.Numerics.Matrix4x4.CreateTranslation(j.Axis.O);
        }

        void OnJointSlider(object sender, Avalonia.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (sender is Slider sl && sl.DataContext is JointItem it && it.J != null)
            {
                it.J.AngleDeg = e.NewValue;
                it.Angle = e.NewValue;
                ApplyJoints();
                StatusText.Text = $"{it.J.Name} = {e.NewValue:0}°";
            }
        }

        Geom.MeshData _lastMesh;

        void FillFeatureList()
        {
            var items = new List<FeatureItem>();
            int i = 0;
            foreach (var f in _model.AllFaces)
            {
                if (f.Feature == "孔" || f.Feature == "锥孔" || f.Feature == "内圆角" || f.Feature == "自由曲面" && f.Radius > 0)
                    items.Add(new FeatureItem { FaceIndex = i, Type = f.Feature, Info = f.FeatureInfo, IStart = f.IStart, ICount = f.ICount });
                i++;
            }
            FeatureList.ItemsSource = items;
        }

        void OnFeatureSelected(object sender, SelectionChangedEventArgs e)
        {
            if (FeatureList.SelectedItem is FeatureItem it)
                GlHost.SelectFace(it.IStart, it.ICount);
        }

        void OnWire(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            _wire = !_wire;
            GlHost.Wire = _wire;
            ((Button)sender).Content = _wire ? "着色" : "线框";
        }

        void OnFit(object sender, Avalonia.Interactivity.RoutedEventArgs e) { /* 缩放到全图已内置于 SetMesh */ }

        void OnPointerPressed(object sender, PointerPressedEventArgs e)
        {
            _last = e.GetPosition(this);
            e.Pointer.Capture((IInputElement)sender);
        }

        void OnPointerReleased(object sender, PointerReleasedEventArgs e)
        {
            _last = null;
            e.Pointer.Capture(null);
        }

        void OnPointerMoved(object sender, PointerEventArgs e)
        {
            if (!_last.HasValue) return;
            if (e is not PointerDeltaEventArgs de) return;
            var pos = e.GetPosition(this);
            var dx = pos.X - _last.Value.X;
            var dy = pos.Y - _last.Value.Y;
            _last = pos;
            var props = e.GetCurrentPoint(this).Properties;
            if (props.IsRightButtonPressed) GlHost.Pan(dx, dy);
            else GlHost.Orbit(dx, dy);
        }

        void OnWheel(object sender, PointerWheelEventArgs e)
        {
            GlHost.Zoom(e.Delta.Y > 0 ? 0.9 : 1.1);
        }
    }

    // 关节滑块的数据项：Angle 双向绑定，Title 显示当前角度
    public class JointItem : System.ComponentModel.INotifyPropertyChanged
    {
        public Joint J;
        public string BaseTitle = "";
        double _angle;

        public double Angle
        {
            get => _angle;
            set
            {
                if (Math.Abs(_angle - value) < 1e-9) return;
                _angle = value;
                PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(Angle)));
                PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(Title)));
            }
        }

        public string Title => $"{BaseTitle}   {_angle:0}°";

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
    }

    public class FeatureItem
    {
        public int FaceIndex { get; set; }
        public string Type { get; set; }
        public string Info { get; set; }
        public int IStart { get; set; }
        public int ICount { get; set; }
    }
}
