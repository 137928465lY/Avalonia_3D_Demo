using System;
using System.Collections.Generic;
using System.IO;
using Path = System.IO.Path;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using AnyCAD.Demo;
using AnyCAD.Foundation;

namespace AnyCADShowcaseRunner
{
    /// <summary>
    /// 遍历 AnyCAD 示例工程中的所有 TestCase，逐个运行并截图保存到 showcase 目录。
    /// 必须从 bin\Release\net8.0-windows 目录运行（GetResourcePath 依赖 ..\..\..\data）。
    /// </summary>
    internal class Program
    {
        static IRenderView _view;
        static string _showcaseDir;
        static StreamWriter _log;
        static string _logPath;
        static int _maxCases = -1;
        static string _onlyName = null;
        static bool _viewerReady = false;
        static bool _resume = false;
        static HashSet<string> _skip = new HashSet<string>();
        static string _viewProbe = null;

        [STAThread]
        static int Main(string[] args)
        {
            int maxCases = -1;
            string onlyName = null;
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--max" && i + 1 < args.Length) maxCases = int.Parse(args[i + 1]);
                if (args[i] == "--only" && i + 1 < args.Length) onlyName = args[i + 1];
                if (args[i] == "--resume") _resume = true;
                if (args[i] == "--skip" && i + 1 < args.Length)
                    _skip.Add(NormalizeCaseKey(args[i + 1]));
                if (args[i] == "--viewprobe" && i + 1 < args.Length) _viewProbe = args[i + 1];
            }
            _maxCases = maxCases;
            _onlyName = onlyName;
            string appBase = AppContext.BaseDirectory;
            string repo = Path.GetFullPath(Path.Combine(appBase, "..", "..", ".."));
            _showcaseDir = Path.Combine(repo, "showcase");
            Directory.CreateDirectory(_showcaseDir);
            _logPath = Path.Combine(repo, "showcase-capture.log");
            _log = new StreamWriter(_logPath, true, System.Text.Encoding.UTF8) { AutoFlush = true };
            Log($"=== run start (resume={_resume}, only={_onlyName ?? "-"}, skip={string.Join(",", _skip)}) ===");
            Log($"appBase={appBase}");
            Log($"repo={repo}");
            Log($"showcase={_showcaseDir}");

            var app = new Application();
            app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            Log("step: app created");

            GlobalInstance.LogFileName = Path.Combine(appBase, "anycad.log");
            GlobalInstance.Initialize();
            Log("step: GlobalInstance.Initialize ok");
            ModelingEngine.EnableSnapShape();
            Log("step: EnableSnapShape ok");

            var win = new Window
            {
                Width = 1600,
                Height = 1040,
                WindowStyle = WindowStyle.None,
                ResizeMode = ResizeMode.NoResize,
                ShowInTaskbar = false,
                WindowStartupLocation = WindowStartupLocation.Manual,
                Left = 0,
                Top = 0,
                Title = "AnyCAD Showcase Capture",
            };

            var ctrl = new AnyCAD.WPF.RenderControl();
            Log("step: RenderControl created");
            _view = ctrl;
            ctrl.ViewerReady += OnViewerReady;
            win.Content = ctrl;

            // 等窗口完成首次布局与渲染后再开始截图，否则渲染表面尚未就绪。
            win.ContentRendered += (s, e) =>
            {
                Log("step: window content rendered");
                ScaleWindowToTargetDpi(win, 1600, 1040);
                Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(StartCapture));
            };

            win.Show();
            Log("step: window shown");
            var mainHwnd = new WindowInteropHelper(win).Handle;
            Log("step: hwnd=" + mainHwnd);
            DialogGuard.SetMain(mainHwnd);
            DialogGuard.Start();
            app.Run();
            Log("step: app run exited");

            DialogGuard.Stop();
            GlobalInstance.Destroy();
            _log.Close();
            return 0;
        }

        /// <summary>按系统 DPI 缩放窗口 DIP 尺寸，使截图像素接近目标物理分辨率。</summary>
        static void ScaleWindowToTargetDpi(Window win, int targetPhysW, int targetPhysH)
        {
            var src = PresentationSource.FromVisual(win);
            if (src?.CompositionTarget != null)
            {
                double scale = src.CompositionTarget.TransformToDevice.M11;
                if (scale > 0)
                {
                    win.Width = targetPhysW / scale;
                    win.Height = targetPhysH / scale;
                    Log($"step: dpi scale={scale:F3}, window DIP={win.Width:F0}x{win.Height:F0}, target phys={targetPhysW}x{targetPhysH}");
                }
            }
        }

        static void OnViewerReady()
        {
            _viewerReady = true;
            Log("viewer ready");

            _view.SetViewCube(EnumViewCoordinateType.Sphere);
        }

        static void StartCapture()
        {
            if (!_viewerReady)
            {
                Log("viewer not ready yet, wait 500ms");
                Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle,
                    new Action(() => { Pump(500); StartCapture(); }));
                return;
            }

            try
            {
                var cases = CollectCases();
                Log($"total cases: {cases.Count}");
                int startIndex = _resume ? GetLastCompletedIndex() + 1 : 0;
                int ok = 0, fail = 0;

                for (int i = startIndex; i < cases.Count; i++)
                {
                    if (_maxCases > 0 && i >= _maxCases) break;
                    var c = cases[i];
                    string key = NormalizeCaseKey(c.group + "." + c.name);
                    if (!string.IsNullOrEmpty(_onlyName))
                    {
                        string onlyKey = NormalizeCaseKey(_onlyName);
                        if (onlyKey != key && onlyKey != NormalizeCaseKey(c.name))
                            continue;
                    }
                    if (!string.IsNullOrEmpty(_viewProbe))
                    {
                        string probeKey = NormalizeCaseKey(_viewProbe);
                        if (probeKey != key && probeKey != NormalizeCaseKey(c.name))
                            continue;
                    }
                    if (_skip.Contains(key) || _skip.Contains(NormalizeCaseKey(c.name)))
                        continue;
                    string safe = key;
                    foreach (var ch in Path.GetInvalidFileNameChars())
                        safe = safe.Replace(ch, '_');
                    string outFile = Path.Combine(_showcaseDir, safe + ".png");

                    Log($"[STATE {i + 1}/{cases.Count}] {c.group}/{c.name}");
                    try
                    {
                        TestCase.CreateTest(c.type, _view);
                        Pump(250); // 等待 Run 产生的首帧渲染完成

                        if (HasAnimationOverride(c.type))
                        {
                            for (int t = 100; t <= 1500; t += 100)
                            {
                                TestCase.RunAnimation(_view, t);
                                Pump(20);
                            }
                            Pump(150);
                        }

                        _view.ZoomAll();
                        Pump(300);

                        if (!string.IsNullOrEmpty(_viewProbe))
                        {
                            LogViewProbe(c, outFile);
                            continue;
                        }

                        ApplySmartView(c);
                        Pump(2000); // 截屏前等待场景完整渲染（部分场景纹理/大场景渲染较慢）
                        bool saved = SaveShot(outFile);
                        if (saved)
                        {
                            ok++;
                            Log($"[OK {i + 1}/{cases.Count}] {c.group}/{c.name} -> {safe}.png");
                        }
                        else
                        {
                            fail++;
                            Log($"[FAIL {i + 1}/{cases.Count}] {c.group}/{c.name}: save failed");
                        }
                    }
                    catch (Exception ex)
                    {
                        fail++;
                        Log($"[FAIL {i + 1}/{cases.Count}] {c.group}/{c.name}: {ex.Message}");
                        Log("    " + ex.StackTrace?.Replace("\n", "\n    "));
                    }
                }

                Log($"DONE ok={ok} fail={fail}");
            }
            catch (Exception ex)
            {
                Log("FATAL: " + ex);
            }
            finally
            {
                Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() => Application.Current.Shutdown()));
            }
        }

        /// <summary>归一化用例标识：小写，/ 与 \ 统一为 .（如 Geometry/SweepCable -> geometry.sweepcable）</summary>
        static string NormalizeCaseKey(string s) =>
            s.Replace('/', '.').Replace('\\', '.').ToLowerInvariant();

        /// <summary>截图（CreateMemoryImage 取字节直写；该构建的 SaveFile 有缺陷返回 false）</summary>
        static bool SaveShot(string outFile)
        {
            bool saved = false;
            var ss = _view.CreateScreenShot();
            for (int attempt = 0; attempt < 3 && !saved; attempt++)
            {
                try
                {
                    var mem = ss.CreateMemoryImage();
                    if (mem != null && mem.Count > 0)
                    {
                        File.WriteAllBytes(outFile, mem.ToArray());
                        saved = true;
                    }
                }
                catch (Exception ex)
                {
                    Log($"  save error {attempt + 1}: {ex.Message}");
                }
                if (!saved)
                {
                    ss.Dispose();
                    Pump(400);
                    ss = _view.CreateScreenShot();
                }
            }
            return saved;
        }

        /// <summary>记录当前模型包围盒与相机方向（诊断用）</summary>
        static void LogBBoxCamera(string tag)
        {
            try
            {
                var box = _view.SceneManager.GetSceneBBox();
                if (box != null)
                {
                    var mn = box.getMinimum();
                    var mx = box.getMaximum();
                    Log($"  [{tag}] bbox min=({mn.x:F3},{mn.y:F3},{mn.z:F3}) max=({mx.x:F3},{mx.y:F3},{mx.z:F3})");
                }
                var cam = _view.Camera;
                if (cam != null)
                {
                    var d = cam.GetDirection();
                    Log($"  [{tag}] camera dir=({d.x:F3},{d.y:F3},{d.z:F3}) pos=({cam.GetPosition().x:F3},{cam.GetPosition().y:F3},{cam.GetPosition().z:F3})");
                }
            }
            catch (Exception ex) { Log("  bbox/camera error: " + ex.Message); }
        }

        /// <summary>按用例强制指定视角：细长管材激光切割整体是扁平包围盒，俯视只剩一条线，强制等轴测展示 3D 管件与切割轨迹。</summary>
        static readonly Dictionary<string, EnumStandardView> ViewOverrides = new(StringComparer.OrdinalIgnoreCase)
        {
            { "simulation.tubelasercut", EnumStandardView.VertexNxPyPz },
            { "simulation.tubelasercutround", EnumStandardView.VertexNxPyPz },
        };

        /// <summary>智能视角：3D 体积模型切等轴测视角；扁平（近似 2D）模型保持默认俯视；ViewOverrides 优先。</summary>
        static void ApplySmartView((string group, string name, Type type) c)
        {
            try
            {
                var viewKey = NormalizeCaseKey(c.group + "." + c.name);
                if (ViewOverrides.TryGetValue(viewKey, out var viewOverride))
                {
                    Log($"  smart view: override {viewKey} -> {viewOverride} [{c.group}/{c.name}]");
                    _view.SetStandardView(viewOverride, false);
                    Pump(200);
                    _view.ZoomAll();
                    Pump(250);
                    return;
                }

                var box = _view.SceneManager.GetSceneBBox();
                double flatThreshold = -1;
                if (box != null)
                {
                    var mn = box.getMinimum();
                    var mx = box.getMaximum();
                    double dx = mx.x - mn.x, dy = mx.y - mn.y, dz = mx.z - mn.z;
                    double span2d = Math.Max(dx, dy);
                    flatThreshold = (span2d > 1e-9) ? dz / span2d : -1;
                }
                bool flat = flatThreshold >= 0 && flatThreshold <= 0.15;
                Log($"  smart view: flatRatio={flatThreshold:F4} -> {(flat ? "俯视(Top)" : "等轴测(VertexNxPyPz)")} [{c.group}/{c.name}]");
                // 注意：渲染视图跨用例复用，相机状态会继承，必须显式设置视角
                if (flat)
                {
                    _view.SetStandardView(EnumStandardView.Top, false);
                    Pump(150);
                    _view.ZoomAll();
                    Pump(250);
                }
                else
                {
                    _view.SetStandardView(EnumStandardView.VertexNxPyPz, false);
                    Pump(200);
                    _view.ZoomAll();
                    Pump(250);
                }
            }
            catch (Exception ex)
            {
                Log($"  smart view error: {ex.Message}");
            }
        }

        /// <summary>视角对比调试：对同一用例输出默认与多个候选等轴测视角的截图与包围盒数据。</summary>
        static void LogViewProbe((string group, string name, Type type) c, string outFile)
        {
            LogBBoxCamera("default");
            SaveShot(Path.ChangeExtension(outFile, ".default.png"));
            foreach (var v in new[] {
                EnumStandardView.VertexNxPyPz, EnumStandardView.VertexPxPyNz,
                EnumStandardView.VertexNxPyNz, EnumStandardView.VertexPxPyPz,
                EnumStandardView.Front })
            {
                _view.SetStandardView(v, false);
                Pump(150);
                _view.ZoomAll();
                Pump(200);
                LogBBoxCamera(v.ToString());
                SaveShot(Path.ChangeExtension(outFile, "." + v.ToString().ToLowerInvariant() + ".png"));
            }
            Log($"[VIEWPROBE] {c.group}/{c.name} done");
        }

        /// <summary>从日志解析最后一个已处理（OK 或 FAIL）的用例序号（0 基）。无则返回 -1。</summary>
        static int GetLastCompletedIndex()
        {
            int last = -1;
            try
            {
                if (File.Exists(_logPath))
                {
                    // 本进程的 StreamWriter 持有该文件写句柄，需显式 ReadWrite 共享读
                    using var fs = new FileStream(_logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using var sr = new StreamReader(fs, System.Text.Encoding.UTF8);
                    string line;
                    var rx = new System.Text.RegularExpressions.Regex(@"\[(OK|FAIL) (\d+)/(\d+)\]");
                    while ((line = sr.ReadLine()) != null)
                    {
                        var m = rx.Match(line);
                        if (m.Success)
                            last = int.Parse(m.Groups[2].Value) - 1;
                    }
                }
            }
            catch (Exception ex) { Log($"resume: read error: {ex.Message}"); }
            Log($"resume: last completed index = {last}");
            return last;
        }

        static bool HasAnimationOverride(Type t)
        {
            return t.GetMethod("Animation", BindingFlags.Public | BindingFlags.Instance).DeclaringType
                != typeof(AnyCAD.Demo.TestCase);
        }

        static List<(string group, string name, Type type)> CollectCases()
        {
            var list = new List<(string group, string name, Type type)>();
            var handler = new AnyCAD.Demo.TestCase.TestCaseHandler(
                (Type type, string name, string group) => list.Add((group, name, type)));

            TryLoader(typeof(AnyCAD.Demo.TestCaseLoaderBase), "Basic", handler);
            TryLoader(Type.GetType("AnyCAD.Demo.TestCaseLoaderSimulate, AnyCAD.Advanced"), "Adv", handler);
            TryLoader(Type.GetType("AnyCAD.Demo.TestCaseLoaderMfg, AnyCAD.MFG"), "Mfg", handler);
            TryLoader(Type.GetType("AnyCAD.Demo.TestCaseLoaderPtc, AnyCAD.PTC"), "Ptc", handler);
            TryLoader(typeof(AnyCAD.Demo.TestCaseLoaderQuickSolid), "QuickSolid", handler);

            // 去重（同 group 内 name 唯一；跨 group 的 name 因文件名带 group 前缀不会冲突）
            return list;
        }

        static void TryLoader(Type loaderType, string label, AnyCAD.Demo.TestCase.TestCaseHandler handler)
        {
            if (loaderType == null)
            {
                Log($"loader {label}: type not found");
                return;
            }
            try
            {
                loaderType.GetMethod("ForEachCase").Invoke(null, new object[] { handler });
            }
            catch (Exception ex)
            {
                Log($"loader {label} error: " + (ex.InnerException ?? ex).Message);
            }
        }

        /// <summary>泵动 WPF 调度器，让渲染线程处理绘制与布局。</summary>
        static void Pump(int ms)
        {
            var end = DateTime.UtcNow.AddMilliseconds(ms);
            var frame = new DispatcherFrame();
            var timer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromMilliseconds(16)
            };
            timer.Tick += (s, e) =>
            {
                if (DateTime.UtcNow >= end)
                {
                    timer.Stop();
                    frame.Continue = false;
                }
            };
            timer.Start();
            Dispatcher.PushFrame(frame);
        }

        static void Log(string msg)
        {
            string line = $"[{DateTime.Now:HH:mm:ss}] {msg}";
            Console.WriteLine(line);
            _log?.WriteLine(line);
        }
    }

    /// <summary>
    /// 看门狗：运行用例期间自动关闭同进程内冒出的原生消息框（如 Analysis_PointPlane 的距离提示），
    /// 避免截图流程被模态对话框阻塞。
    /// </summary>
    internal static class DialogGuard
    {
        delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
        [DllImport("user32.dll")]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
        [DllImport("user32.dll")]
        static extern bool IsWindowVisible(IntPtr hWnd);
        [DllImport("user32.dll")]
        static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        const uint WM_CLOSE = 0x0010;

        static int _pid = Environment.ProcessId;
        static IntPtr _mainHwnd;
        static Timer _timer;

        public static void SetMain(IntPtr hwnd) => _mainHwnd = hwnd;

        public static void Start()
        {
            _timer = new Timer(_ => CloseForeignWindows(), null, 200, 250);
        }

        public static void Stop()
        {
            _timer?.Dispose();
            _timer = null;
        }

        static void CloseForeignWindows()
        {
            EnumWindows((hWnd, lParam) =>
            {
                GetWindowThreadProcessId(hWnd, out uint pid);
                if (pid == (uint)_pid && hWnd != _mainHwnd && IsWindowVisible(hWnd))
                    PostMessage(hWnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                return true;
            }, IntPtr.Zero);
        }
    }
}
