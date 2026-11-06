using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using OpenStepViewer.Step;
using V3 = System.Numerics.Vector3;

namespace OpenStepViewer.Geom
{
    public class FaceInfo
    {
        public Surface S;
        public int StepId;      // 源 STEP 实体编号（诊断用）
        public bool SameSense = true;
        public List<List<V3>> Loops = new List<List<V3>>();   // 裁剪环（3D 采样点，首尾不重复）
        public int SolidIndex;
        public int FaceIndex;
        public string Feature;     // 特征分类
        public string FeatureInfo; // 附加信息
        public int VStart, VCount, IStart, ICount;  // 网格范围
        public V3 Center;
        public double Radius;
    }

    public class SolidInfo
    {
        public string Name;
        public List<FaceInfo> Faces = new List<FaceInfo>();
    }

    public class StepModel
    {
        public StepFile File;
        public List<SolidInfo> Solids = new List<SolidInfo>();
        public List<FaceInfo> AllFaces => Solids.SelectMany(s => s.Faces).ToList();
        public V3 BBoxMin, BBoxMax;
        public double Scale = 1.0; // 单位→mm
        public int UnsupportedFaces;
        public readonly List<string> Warnings = new List<string>();

        public static StepModel Load(string path, Action<string> log = null)
        {
            log?.Invoke("读取文件...");
            var text = System.IO.File.ReadAllText(path);
            log?.Invoke("词法解析...");
            var sf = StepParser.Parse(text);
            log?.Invoke($"实体图构建完成: {sf.Entities.Count} 条目");

            // 单位（SI_UNIT (.MILLI., .METRE.) 等）
            double scale = 1.0;
            var su = sf.ByType("SI_UNIT").FirstOrDefault();
            if (su != null)
            {
                var enums = new List<string>();
                CollectEnums(su.Args, enums);
                if (enums.Contains("MILLI")) scale = 1.0;      // 内部统一用文件坐标，仅报告
                else if (enums.Contains("CENTI")) scale = 1.0;
                log?.Invoke("长度单位: " + string.Join(".", enums));
            }

            var m = new StepModel { File = sf, Scale = scale };
            var reader = new Reader(sf, m, log);
            reader.BuildSolids();
            m.ComputeBBox();
            return m;
        }

        static void CollectEnums(Arg a, List<string> outp)
        {
            if (a == null) return;
            if (a.Kind == ArgKind.Enum) outp.Add(a.Str);
            foreach (var it in a.Items) CollectEnums(it, outp);
        }

        void ComputeBBox()
        {
            var mn = new V3(float.MaxValue); var mx = new V3(float.MinValue);
            foreach (var f in AllFaces)
                foreach (var loop in f.Loops)
                    foreach (var p in loop) { mn = V3.Min(mn, p); mx = V3.Max(mx, p); }
            BBoxMin = mn; BBoxMax = mx;
        }
    }

    internal class Reader
    {
        readonly StepFile _f;
        readonly StepModel _m;
        readonly Action<string> _log;
        readonly Dictionary<int, Surface> _surfCache = new();
        readonly Dictionary<int, Curve3> _curveCache = new();
        readonly HashSet<int> _buildingSurf = new();
        internal static bool _debug = true;
        static int _originHits;

        public Reader(StepFile f, StepModel m, Action<string> log) { _f = f; _m = m; _log = log; }

        public void BuildSolids()
        {
            var seen = new HashSet<int>();
            foreach (var e in _f.ByType("MANIFOLD_SOLID_BREP", "BREP_WITH_VOIDS"))
            {
                if (!seen.Add(e.Id)) continue;
                var solid = new SolidInfo { Name = e.Args[0].Str ?? ("Solid#" + e.Id) };
                // (name, closed_shell) 或 (name, outer, voids[])
                var shells = new List<Arg>();
                if (e.Type == "BREP_WITH_VOIDS") { shells.Add(e.Args[1]); for (int i = 0; i < e.Args[2].Count; i++) shells.Add(e.Args[2][i]); }
                else shells.Add(e.Args[1]);
                foreach (var shRef in shells)
                {
                    var sh = _f.Get(StepParser.AsRef(shRef));
                    if (sh == null || sh.Type != "CLOSED_SHELL" && sh.Type != "OPEN_SHELL" && sh.Type != "ORIENTED_CLOSED_SHELL")
                    { _m.Warnings.Add($"壳引用无效: {shRef}"); continue; }
                    var facesArg = sh.Type == "ORIENTED_CLOSED_SHELL" ? sh.Args[1] : sh.Args[1];
                    // CLOSED_SHELL(name, faces[]) 
                    facesArg = sh.Args.Count >= 2 ? sh.Args[1] : sh.Args[0];
                    foreach (var fref in facesArg.Items)
                    {
                        var fe = _f.Get(StepParser.AsRef(fref));
                        if (fe == null) continue;
                        if (fe.Type == "ORIENTED_FACE") fe = _f.Get(StepParser.AsRef(fe.Args[1])) ?? fe;
                        if (fe.Type != "ADVANCED_FACE" && fe.Type != "FACE_SURFACE") continue;
                        try
                        {
                            var fi = BuildFace(fe);
                            if (fi != null) { fi.SolidIndex = _m.Solids.Count; solid.Faces.Add(fi); }
                            else _m.Warnings.Add($"面 #{fe.Id}: 构建失败(空)");
                        }
                        catch (Exception ex)
                        {
                            var st = (ex.StackTrace ?? "").Replace("\r", "").Split('\n');
                            var frames = string.Join(" <- ", st.Take(5).Select(s => s.Trim()
                                .Replace("OpenStepViewer.", "").Replace(" in D:\\科能慧智\\RAE逆向\\OpenStepViewer\\", " ")));
                            var surfRef = StepParser.AsRef(fe.Args.Count > 2 ? fe.Args[2] : Arg.Null);
                            _m.Warnings.Add($"面 #{fe.Id}: {ex.GetType().Name} {ex.Message} @ {frames}");
                        }
                    }
                }
                if (solid.Faces.Count > 0) _m.Solids.Add(solid);
            }
            _m.UnsupportedFaces = _m.Warnings.Count;
            _log?.Invoke($"实体 {_m.Solids.Count}，面 {_m.AllFaces.Count}，警告 {_m.Warnings.Count}");
        }

        FaceInfo BuildFace(StepEntity e)
        {
            var args = e.Args;
            if (args.Count < 4) return null;
            var face = new FaceInfo { SameSense = StepParser.AsBool(args[3]), StepId = e.Id };
            face.S = BuildSurface(StepParser.AsRef(args[2]));
            if (face.S == null) { _m.Warnings.Add($"#{e.Id}: 不支持的曲面"); return null; }

            foreach (var bRef in args[1].Items)
            {
                var b = _f.Get(StepParser.AsRef(bRef));
                if (b == null) continue;
                if (b.Type != "FACE_OUTER_BOUND" && b.Type != "FACE_BOUND") continue;
                bool boundOrient = StepParser.AsBool(b.Args[2]);
                var loopE = _f.Get(StepParser.AsRef(b.Args[1]));
                if (loopE == null) continue;
                List<V3> pts = null;
                if (loopE.Type == "EDGE_LOOP") pts = SampleEdgeLoop(loopE, face.S);
                else if (loopE.Type == "POLY_LOOP") pts = loopE.Args[1].Items.Select(x => MathUtil.V(x, _f)).ToList();
                else if (loopE.Type == "VERTEX_LOOP") { _m.Warnings.Add($"#{e.Id}: VERTEX_LOOP"); return null; }
                if (pts == null || pts.Count < 3) continue;
                if (!boundOrient) pts.Reverse();
                if ((pts[0] - pts[pts.Count - 1]).Length() < 1e-6f) pts.RemoveAt(pts.Count - 1);
                if (pts.Count >= 3) face.Loops.Add(pts);
            }
            if (face.Loops.Count == 0)
            {
                _m.Warnings.Add($"面 #{e.Id}: 无有效裁剪环 (bounds={args[1].Items.Count})");
                return null;
            }
            face.Center = face.Loops[0].Aggregate(V3.Zero, (a, p) => a + p) / face.Loops[0].Count;
            return face;
        }

        List<V3> SampleEdgeLoop(StepEntity loopE, Surface s)
        {
            var pts = new List<V3>();
            foreach (var oeRef in loopE.Args[1].Items)
            {
                var oe = _f.Get(StepParser.AsRef(oeRef));
                if (oe == null || oe.Type != "ORIENTED_EDGE") continue;
                // ORIENTED_EDGE(name, edge_start, edge_end, edge_element, orientation)
                int edgeIdx = oe.Args.Count >= 5 ? 3 : 1;
                int orientIdx = oe.Args.Count >= 5 ? 4 : 2;
                bool orient = StepParser.AsBool(oe.Args[orientIdx]);
                var ec = _f.Get(StepParser.AsRef(oe.Args[edgeIdx]));
                if (ec == null || ec.Type != "EDGE_CURVE") continue;
                var segs = SampleEdgeCurve(ec, s);
                if (segs == null || segs.Count == 0) continue;
                if (!orient) segs.Reverse();
                foreach (var p in segs)
                {
                    if (pts.Count > 0 && (p - pts[pts.Count - 1]).LengthSquared() < 1e-12f) continue;
                    pts.Add(p);
                }
            }
            if (pts.Count < 3 && _debug)
            {
                var first = loopE.Args[1].Items.Count > 0 ? _f.Get(StepParser.AsRef(loopE.Args[1].Items[0])) : null;
                var ec0 = first != null ? _f.Get(StepParser.AsRef(first.Args[first.Args.Count >= 5 ? 3 : 1])) : null;
                var curve0 = ec0 != null ? BuildCurve(StepParser.AsRef(ec0.Args[3])) : null;
                _m.Warnings.Add($"环 #{loopE.Id}: 采样点={pts.Count}, 边数={loopE.Args[1].Items.Count}, 首边={ec0?.Id}/{ec0?.Type}, 曲线={curve0?.GetType().Name ?? "null"}");
            }
            return pts;
        }

        V3 GetVertexPoint(Arg refArg)
        {
            var ve = _f.Get(StepParser.AsRef(refArg));   // VERTEX_POINT(name, point_ref)
            if (ve == null || ve.Args.Count < 2) return default;
            return MathUtil.V(ve.Args[1], _f);           // 解引用 CARTESIAN_POINT
        }

        List<V3> SampleEdgeCurve(StepEntity ec, Surface s)
        {
            // EDGE_CURVE(name, v1, v2, curve, sense)
            var v1 = GetVertexPoint(ec.Args[1]);
            var v2 = GetVertexPoint(ec.Args[2]);
            var curve = BuildCurve(StepParser.AsRef(ec.Args[3]));
            if (curve == null) return null;

            bool sense = ec.Args.Count >= 5 ? StepParser.AsBool(ec.Args[4]) : true;
            double t1 = curve.ParamFromPoint(v1, 0), t2 = curve.ParamFromPoint(v2, 0);
            if (curve is TrimmedCurve3 tc) { t1 = tc.T1; t2 = tc.T2; }
            else
            {
                // 周期曲线参数歧义修正：ParamFromPoint 用 atan2 ∈ (-π,π]，闭合边端点
                // 需按 sense 平移 ±2π，否则采样会走错半圈（环来回重叠、面积归零）
                double period = curve is Circle3 || curve is Ellipse3 ? 2 * Math.PI : 0;
                if (period > 0)
                {
                    if ((v1 - v2).LengthSquared() < 1e-12f)
                        t2 = sense ? t1 + period : t1 - period; // 闭合边（完整圆/椭圆）：整周
                    else if (sense)
                    {
                        while (t2 < t1) t2 += period;
                        while (t2 - t1 > period) t2 -= period;
                    }
                    else
                    {
                        while (t2 > t1) t2 -= period;
                        while (t1 - t2 > period) t2 += period;
                    }
                }
            }

            // 采样数按曲线长度（NaN/负值防御）
            if (double.IsNaN(t1) || double.IsInfinity(t1)) t1 = 0;
            if (double.IsNaN(t2) || double.IsInfinity(t2)) t2 = t1;
            double len = EstimateLength(curve, t1, t2);
            if (double.IsNaN(len) || len < 0) len = 1;
            int n = (int)Math.Min(240, Math.Max(2, Math.Ceiling(len / 0.6)));
            if (_debug && ec.Id == 21)
                _m.Warnings.Add($"边#21: v1={v1} v2={v2} curve={curve.GetType().Name} t1={t1:0.###} t2={t2:0.###} len={len:0.###} n={n}");
            var pts = new List<V3>(n + 1);
            for (int i = 0; i <= n; i++)
            {
                double t = t1 + (t2 - t1) * i / n;
                pts.Add(curve.Eval(t));
            }
            if ((pts[0] - v1).Length() > (pts[0] - v2).Length()) pts.Reverse(); // 对齐起点
            if (_originHits < 12 && pts.Any(p => p.LengthSquared() < 1e-12f))
            {
                _originHits++;
                _m.Warnings.Add($"边#{ec.Id} 采样落到原点: curve={curve.GetType().Name} t1={t1:0.###} t2={t2:0.###} " +
                    $"v1=({v1.X:0.#},{v1.Y:0.#},{v1.Z:0.#}) v2=({v2.X:0.#},{v2.Y:0.#},{v2.Z:0.#}) 采样点={pts.Count}");
            }
            return pts;
        }

        static double EstimateLength(Curve3 c, double t1, double t2)
        {
            int n = 24; double len = 0;
            var prev = c.Eval(t1);
            for (int i = 1; i <= n; i++)
            {
                var p = c.Eval(t1 + (t2 - t1) * i / n);
                len += (p - prev).Length(); prev = p;
            }
            return len;
        }

        public Curve3 BuildCurve(int id)
        {
            if (id < 0) return null;
            if (_curveCache.TryGetValue(id, out var c)) return c;
            var e = _f.Get(id);
            if (e == null) return null;
            var a = e.Args;
            Curve3 result = null;
            if (e.Type != null && e.Type.Contains("B_SPLINE_CURVE"))
                return _curveCache[id] = BuildBSplineCurve(e);
            switch (e.Type)
            {
                case "LINE":
                    result = new Line3 { P = MathUtil.V(a[1], _f), D = MathUtil.Normalized(MathUtil.V(a[2], _f)) };
                    break;
                case "CIRCLE":
                    {
                        var fm = MathUtil.Frame(a[1], _f);
                        result = new Circle3 { O = fm[0], X = fm[1], Y = fm[2], Z = fm[3], R = a[2].Num };
                    }
                    break;
                case "ELLIPSE":
                    {
                        var fm = MathUtil.Frame(a[1], _f);
                        result = new Ellipse3 { O = fm[0], X = fm[1], Y = fm[2], R1 = a[2].Num, R2 = a[3].Num };
                    }
                    break;
                case "B_SPLINE_CURVE_WITH_KNOTS":
                case "RATIONAL_B_SPLINE_CURVE":
                    result = BuildBSplineCurve(e);
                    break;
                case "TRIMMED_CURVE":
                    {
                        var basis = BuildCurve(StepParser.AsRef(a[1]));
                        if (basis == null) break;
                        double t1 = 0, t2 = 0;
                        if (a[2].Kind == ArgKind.Typed && a[2].Type == "PARAMETER_VALUE") t1 = a[2][1].Num;
                        else if (a[2].Kind == ArgKind.Ref) { var p = MathUtil.V(a[2], _f); t1 = basis.ParamFromPoint(p, 0); }
                        if (a[3].Kind == ArgKind.Typed && a[3].Type == "PARAMETER_VALUE") t2 = a[3][1].Num;
                        else if (a[3].Kind == ArgKind.Ref) { var p = MathUtil.V(a[3], _f); t2 = basis.ParamFromPoint(p, t1); }
                        result = new TrimmedCurve3 { Basis = basis, T1 = t1, T2 = t2, Sense = a[4] != null && StepParser.AsBool(a[4]) };
                    }
                    break;
                case "SEAM_CURVE":
                case "SURFACE_CURVE":
                case "INTERSECTION_CURVE":
                case "SEAM_CURVE_SINGULAR":
                    // SURFACE_CURVE(name, basis_curve, pcurves[], master) — 3D 曲线在 args[1]
                    result = BuildCurve(StepParser.AsRef(a[1]));
                    break;
                case "COMPOSITE_CURVE":
                    _m.Warnings.Add("COMPOSITE_CURVE 未支持");
                    break;
            }
            _curveCache[id] = result;
            return result;
        }

        Curve3 BuildBSplineCurve(StepEntity e)
        {
            int deg;
            V3[] pts;
            List<int> mult;
            List<double> knots;
            float[] w = null;

            if (e.Type != null && e.Type.StartsWith("|"))
            {
                // 复杂实体：B_SPLINE_CURVE(deg,pts,form,closed,self) + WITH_KNOTS(mult,knots,spec) + RATIONAL(wts)
                var bs = e.Args.Items.FirstOrDefault(x => x.Kind == ArgKind.Typed && x.Type != null && x.Type.Contains("B_SPLINE_CURVE") && !x.Type.Contains("WITH_KNOTS"));
                var kn = e.Args.Items.FirstOrDefault(x => x.Kind == ArgKind.Typed && x.Type != null && x.Type.Contains("WITH_KNOTS"));
                var ra = e.Args.Items.FirstOrDefault(x => x.Kind == ArgKind.Typed && x.Type != null && x.Type.Contains("RATIONAL"));
                if (bs == null || kn == null) { _m.Warnings.Add($"复杂 B 样条曲线 #{e.Id}: 缺少子类型节点"); return null; }

                int off = bs.Count > 0 && bs[0].Kind == ArgKind.Num ? 0 : 1;
                deg = (int)bs[off].Num;
                pts = bs[off + 1].Items.Select(x => MathUtil.V(x, _f)).ToArray();

                var lists = kn.Items.Where(x => x.Kind == ArgKind.List).ToList();
                if (lists.Count < 2) { _m.Warnings.Add($"复杂 B 样条曲线 #{e.Id}: 节点数据不足"); return null; }
                mult = lists[0].Items.Select(x => (int)x.Num).ToList();
                knots = lists[1].Items.Select(x => x.Num).ToList();

                var wArg = FindWeightsArg(ra);
                if (wArg != null) w = wArg.Items.Select(x => (float)x.Num).ToArray();
            }
            else
            {
                var a = e.Args;
                bool rational = e.Type != null && e.Type.Contains("RATIONAL");
                int off = a.Count > 0 && a[0].Kind == ArgKind.Num ? 0 : 1;
                deg = (int)a[off].Num;
                pts = a[off + 1].Items.Select(x => MathUtil.V(x, _f)).ToArray();
                int k = off + 2;
                if (rational)
                {
                    k = NextList(a, k);
                    if (k >= a.Count) { _m.Warnings.Add($"B 样条曲线 #{e.Id}: 权重缺失"); return null; }
                    w = a[k].Items.Select(x => (float)x.Num).ToArray();
                    k++;
                }
                k = NextList(a, k);
                mult = a[k].Items.Select(x => (int)x.Num).ToList(); k++;
                knots = a[k].Items.Select(x => x.Num).ToList(); k++;
            }
            var K = ExpandKnots(knots, mult);
            if (K.Length < pts.Length + deg + 1) return null;
            return new BSplineCurve3 { Degree = deg, CP = pts, W = w, Knots = K };
        }

        // 跳过标量标志（curve_form/closed/self_intersect 等），定位下一个 List 参数
        static int NextList(Arg a, int k) { while (k < a.Count && a[k].Kind != ArgKind.List) k++; return k; }
        static int NextList2(Arg a, int k) { while (k < a.Count && a[k].Kind != ArgKind.List) k++; return k; }

        // RATIONAL 子节点里找权重列表（list of list of number），兼容有无 name
        static Arg FindWeightsArg(Arg ra)
        {
            if (ra == null) return null;
            foreach (var it in ra.Items)
                if (it.Kind == ArgKind.List && it.Items.Count > 0 && it.Items[0].Kind == ArgKind.List)
                    return it;
            if (ra.Count >= 2 && ra[1].Kind == ArgKind.List) return ra[1];
            return null;
        }

        static double[] ExpandKnots(List<double> knots, List<int> mult)
        {
            var list = new List<double>();
            for (int i = 0; i < knots.Count && i < mult.Count; i++)
                for (int j = 0; j < mult[i]; j++) list.Add(knots[i]);
            return list.ToArray();
        }

        public Surface BuildSurface(int id)
        {
            if (id < 0) return null;
            if (_surfCache.TryGetValue(id, out var s)) return s;
            if (!_buildingSurf.Add(id)) return null;
            try
            {
                var e = _f.Get(id);
                if (e == null) return null;
                var a = e.Args;
                Surface result = null;
                // AP203 复杂实体：|BOUNDED_SURFACE|B_SPLINE_SURFACE|...|RATIONAL_B_SPLINE_SURFACE|...
                if (e.Type != null && e.Type.Contains("B_SPLINE_SURFACE"))
                    return _surfCache[id] = BuildBSplineSurface(e);
                switch (e.Type)
                {
                    case "PLANE":
                        {
                            var fm = MathUtil.Frame(a[1], _f);
                            result = new PlaneS { O = fm[0], X = fm[1], Y = fm[2], Z = fm[3] };
                        }
                        break;
                    case "CYLINDRICAL_SURFACE":
                        {
                            var fm = MathUtil.Frame(a[1], _f);
                            result = new CylinderS { O = fm[0], X = fm[1], Y = fm[2], Z = fm[3], R = a[2].Num };
                        }
                        break;
                    case "CONICAL_SURFACE":
                        {
                            var fm = MathUtil.Frame(a[1], _f);
                            double ang = a[3].Num; // 半角(rad)
                            result = new ConeS { O = fm[0], X = fm[1], Y = fm[2], Z = fm[3], R0 = a[2].Num, TanA = Math.Tan(ang) };
                        }
                        break;
                    case "TOROIDAL_SURFACE":
                        {
                            var fm = MathUtil.Frame(a[1], _f);
                            result = new TorusS { O = fm[0], X = fm[1], Y = fm[2], Z = fm[3], Major = a[2].Num, Minor = a[3].Num };
                        }
                        break;
                    case "DEGENERATE_TOROIDAL_SURFACE":
                        {
                            var fm = MathUtil.Frame(a[1], _f);
                            result = new TorusS { O = fm[0], X = fm[1], Y = fm[2], Z = fm[3], Major = a[2].Num, Minor = a[3].Num };
                        }
                        break;
                    case "SPHERICAL_SURFACE":
                        {
                            var fm = MathUtil.Frame(a[1], _f);
                            result = new SphereS { O = fm[0], X = fm[1], Y = fm[2], Z = fm[3], R = a[2].Num };
                        }
                        break;
                    case "B_SPLINE_SURFACE_WITH_KNOTS":
                    case "RATIONAL_B_SPLINE_SURFACE":
                        result = BuildBSplineSurface(e);
                        break;
                    case "SURFACE_OF_LINEAR_EXTRUSION":
                        {
                            var c = BuildCurve(StepParser.AsRef(a[1]));
                            if (c != null)
                                result = new ExtrusionS { C = c, D = MathUtil.Normalized(MathUtil.V(a[2], _f)) };
                        }
                        break;
                    case "SURFACE_OF_REVOLUTION":
                        // 轴 + 基曲线；用数值网格化近似：转换成旋转采样曲面暂不支持
                        _m.Warnings.Add("SURFACE_OF_REVOLUTION 未支持");
                        break;
                    case "BOUNDED_SURFACE":
                        _m.Warnings.Add("BOUNDED_SURFACE 复杂实体未支持");
                        break;
                }
                if (result == null)
                    _m.Warnings.Add($"曲面 #{id}: {e.Type} 未支持/解析失败");
                _surfCache[id] = result;
                return result;
            }
            finally { _buildingSurf.Remove(id); }
        }

        static bool IsSane(V3 p) => !float.IsNaN(p.X) && !float.IsNaN(p.Y) && !float.IsNaN(p.Z)
                                    && !float.IsInfinity(p.X) && !float.IsInfinity(p.Y) && !float.IsInfinity(p.Z)
                                    && MathF.Abs(p.X) < 1e8f && MathF.Abs(p.Y) < 1e8f && MathF.Abs(p.Z) < 1e8f;
        static bool IsSaneD(double x) => !double.IsNaN(x) && !double.IsInfinity(x);

        Surface BuildBSplineSurface(StepEntity e)
        {
            // 统一提取：次数、控制点、重数、节点、权重
            int pu, pv;
            List<Arg> ptsList;
            List<int> uMult, vMult;
            List<double> uKnots, vKnots;
            float[][] w = null;

            if (e.Type != null && e.Type.StartsWith("|"))
            {
                // 复杂实体：B_SPLINE_SURFACE(deg,deg,pts,form,closed...) + WITH_KNOTS(uMult,vMult,uKnots,vKnots,spec) + RATIONAL(wts)
                var bs = e.Args.Items.FirstOrDefault(x => x.Kind == ArgKind.Typed && x.Type != null && x.Type.Contains("B_SPLINE_SURFACE") && !x.Type.Contains("WITH_KNOTS"));
                var kn = e.Args.Items.FirstOrDefault(x => x.Kind == ArgKind.Typed && x.Type != null && x.Type.Contains("WITH_KNOTS"));
                var ra = e.Args.Items.FirstOrDefault(x => x.Kind == ArgKind.Typed && x.Type != null && x.Type.Contains("RATIONAL"));
                if (bs == null || kn == null) { _m.Warnings.Add($"复杂 B 样条曲面 #{e.Id}: 缺少 B_SPLINE_SURFACE/WITH_KNOTS 子类型"); return null; }

                int off = bs.Count > 0 && bs[0].Kind == ArgKind.Num ? 0 : 1;
                if (bs.Count < off + 3) { _m.Warnings.Add($"复杂 B 样条曲面 #{e.Id}: 次数/点不足"); return null; }
                pu = (int)bs[off].Num; pv = (int)bs[off + 1].Num;
                ptsList = bs[off + 2].Items;

                var lists = kn.Items.Where(x => x.Kind == ArgKind.List).ToList();
                if (lists.Count < 4)
                {
                    var kinds = string.Join(",", kn.Items.Select(x => x.Kind + (x.Kind == ArgKind.Typed ? ":" + x.Type : "")));
                    _m.Warnings.Add($"复杂 B 样条曲面 #{e.Id}: 节点数据不足 ({lists.Count}) kn[{kn.Count}] kinds={kinds}");
                    return null;
                }
                uMult = lists[0].Items.Select(x => (int)x.Num).ToList();
                vMult = lists[1].Items.Select(x => (int)x.Num).ToList();
                uKnots = lists[2].Items.Select(x => x.Num).ToList();
                vKnots = lists[3].Items.Select(x => x.Num).ToList();

                var wArg = FindWeightsArg(ra);
                if (wArg != null)
                {
                    w = new float[ptsList.Count][];
                    for (int i = 0; i < ptsList.Count; i++)
                    {
                        var row = wArg.Items[Math.Min(i, wArg.Items.Count - 1)];
                        w[i] = row.Items.Select(x => (float)x.Num).ToArray();
                    }
                }
            }
            else
            {
                var a = e.Args;
                bool rational = e.Type != null && e.Type.Contains("RATIONAL");
                int off = a.Count > 0 && a[0].Kind == ArgKind.Num ? 0 : 1;
                pu = (int)a[off].Num; pv = (int)a[off + 1].Num;
                ptsList = a[off + 2].Items;
                int k = off + 3;
                if (rational)
                {
                    k = NextList2(a, k);
                    if (k >= a.Count) { _m.Warnings.Add($"B 样条曲面 #{e.Id}: 权重缺失"); return null; }
                    w = new float[ptsList.Count][];
                    for (int i = 0; i < ptsList.Count; i++)
                    {
                        var row = a[k].Items[Math.Min(i, a[k].Items.Count - 1)];
                        w[i] = row.Items.Select(x => (float)x.Num).ToArray();
                    }
                    k++;
                }
                k = NextList2(a, k);
                uMult = a[k].Items.Select(x => (int)x.Num).ToList(); k++;
                vMult = a[k].Items.Select(x => (int)x.Num).ToList(); k++;
                uKnots = a[k].Items.Select(x => x.Num).ToList(); k++;
                vKnots = a[k].Items.Select(x => x.Num).ToList();
            }

            int nu = ptsList.Count, nv = ptsList[0].Count;
            var cp = new V3[nu][];
            for (int i = 0; i < nu; i++)
            {
                cp[i] = new V3[nv];
                var row = ptsList[i];
                if (row.Count != nv) return null;
                for (int j = 0; j < nv; j++)
                {
                    var pt = row[j];
                    if (pt.Kind == ArgKind.Typed && pt.Type == "CARTESIAN_POINT")
                        cp[i][j] = MathUtil.V(pt, _f);
                    else cp[i][j] = MathUtil.V(pt, _f);
                }
            }
            var ku = ExpandKnots(uKnots, uMult);
            var kv = ExpandKnots(vKnots, vMult);
            if (ku.Length < nu + pu + 1 || kv.Length < nv + pv + 1)
            {
                _m.Warnings.Add("B 样条节点数不匹配");
                return null;
            }
            var bss = new BSplineS(pu, pv, cp, w, ku, kv);
            bss.UVRange(out double bu0, out double bu1, out double bv0, out double bv1);
            bool bad = double.IsNaN(bu0) || double.IsNaN(bu1) || double.IsNaN(bv0) || double.IsNaN(bv1)
                       || cp.Any(row => row.Any(p => !IsSane(p)));
            if (bad)
            {
                var badKu = ku.Select((x, i) => (x, i)).Where(t => !IsSaneD(t.x)).Take(3).Select(t => $"[{t.i}]={t.x:0.##e+0}").ToArray();
                var badKv = kv.Select((x, i) => (x, i)).Where(t => !IsSaneD(t.x)).Take(3).Select(t => $"[{t.i}]={t.x:0.##e+0}").ToArray();
                var badCp = cp.SelectMany((row, i) => row.Select((p, j) => (i, j, p))).Where(t => !IsSane(t.p)).Take(3).Select(t => $"[{t.i},{t.j}]={t.p}").ToArray();
                _m.Warnings.Add($"B 样条曲面 #{e.Id}: 数据异常 uv=({bu0:0.##},{bu1:0.##})x({bv0:0.##},{bv1:0.##}) badCp={string.Join(" ", badCp)} nanKu={string.Join(",", badKu)} nanKv={string.Join(",", badKv)}");
            }
            return bss;
        }
    }
}
