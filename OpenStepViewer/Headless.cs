using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using OpenStepViewer.Geom;
using OpenStepViewer.Step;
using V3 = System.Numerics.Vector3;

namespace OpenStepViewer
{
    // 无头验证：--stats <file> / --mesh <file>
    public static class Headless
    {
        // 面几何指纹：曲面类型 + 环点数 + 环点质心（同一面被重复引用时指纹相同）
        static string Finger(Geom.FaceInfo f)
        {
            var c = V3.Zero; int n = 0;
            foreach (var l in f.Loops) foreach (var p in l) { c += p; n++; }
            if (n > 0) c /= n;
            return $"{f.S?.GetType().Name}|{f.Loops.Count}|{n}|{c.X:0.0},{c.Y:0.0},{c.Z:0.0}";
        }

        public static void Dump(string path, int id)
        {
            var text = File.ReadAllText(path);
            var sf = StepParser.Parse(text);
            var e = sf.Get(id);
            Console.WriteLine($"#{id} {e?.Type}");
            var args = e?.Args;
            if (args != null)
                Console.WriteLine("子项: " + string.Join(" | ", args.Items.Select(x => x.Kind + (x.Type != null ? ":" + x.Type : "") + $"({x.Count})")));
            if (e != null && e.RawArgs != null)
                Console.WriteLine(e.RawArgs.Substring(0, Math.Min(400, e.RawArgs.Length)));
        }

        public static void Types(string path)
        {
            var text = File.ReadAllText(path);
            var sf = StepParser.Parse(text);
            foreach (var g in sf.Entities.Values.GroupBy(e => e.Type).OrderByDescending(g => g.Count()).Take(30))
                Console.WriteLine($"{g.Key,-36} {g.Count()}");
            Console.WriteLine("--- 空类型样本 ---");
            foreach (var e in sf.Entities.Values.Where(e => string.IsNullOrEmpty(e.Type)).Take(5))
                Console.WriteLine($"#{e.Id}: raw={e.RawArgs.Substring(0, Math.Min(100, e.RawArgs.Length))}");
            var brep = sf.Entities.Values.Where(e => e.Type.Contains("SHELL") || e.Type.Contains("BREP")).Take(5);
            Console.WriteLine("--- SHELL/BREP ---");
            foreach (var e in brep) Console.WriteLine($"#{e.Id} {e.Type}: {e.RawArgs.Substring(0, Math.Min(80, e.RawArgs.Length))}");
        }

        // 逐面归属诊断：每个实体的轴向圆柱面分组，用于核对关节轴是否选在了正确的父子件接触区
        public static void Axes(string path)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            var model = StepModel.Load(path, null);
            var faces = model.AllFaces;
            var axes = JointRecognizer.DetectAxes(model);
            Console.WriteLine($"实体 {model.Solids.Count}  面 {faces.Count}  轴候选 {axes.Count}");
            Console.WriteLine("== 全部候选轴(按得分) ==");
            for (int i = 0; i < axes.Count; i++)
            {
                var a = axes[i];
                if (a.Score < 8000) continue;
                Console.WriteLine($"  #{i,2} R={a.Radius,7:0.##} 面={a.FaceCount,4} 得分={a.Score,9:0} " +
                    $"O=({a.O.X:0.#},{a.O.Y:0.#},{a.O.Z:0.#}) Dir=({a.Dir.X:0.##},{a.Dir.Y:0.##},{a.Dir.Z:0.##}) " +
                    $"轴区间=[{a.AxialMin:0.#},{a.AxialMax:0.#}] 离心={a.RadialDist:0.#}");
            }
            Console.WriteLine("== 实体内的大圆柱面 ==");
            for (int si = 0; si < model.Solids.Count; si++)
            {
                var s = model.Solids[si];
                var mn = new V3(float.MaxValue); var mx = new V3(float.MinValue);
                foreach (var f0 in s.Faces) foreach (var l in f0.Loops) foreach (var p in l) { mn = V3.Min(mn, p); mx = V3.Max(mx, p); }
                var sz = mx - mn;
                if (s.Faces.Count < 12) continue;
                Console.WriteLine($"  实体#{si} {s.Name} 面={s.Faces.Count} 尺寸=({sz.X:0.#},{sz.Y:0.#},{sz.Z:0.#}) 范围 z=[{mn.Z:0.#},{mx.Z:0.#}]");
                foreach (var f in s.Faces)
                {
                    if (f.S is not CylinderS cy || cy.R < 8) continue;
                    double tmin = double.MaxValue, tmax = double.MinValue;
                    foreach (var lp in f.Loops) foreach (var p in lp)
                    {
                        double t = MathUtil.Dot(p - cy.O, cy.Z);
                        if (t < tmin) tmin = t;
                        if (t > tmax) tmax = t;
                    }
                    if (tmin > tmax) continue;
                    double area = 2 * Math.PI * cy.R * Math.Max(1.0, tmax - tmin);
                    if (area < 3000) continue;
                    Console.WriteLine($"      面#{f.FaceIndex,4} STEP#{f.StepId,6} R={cy.R,7:0.##} " +
                        $"Dir=({cy.Z.X:0.##},{cy.Z.Y:0.##},{cy.Z.Z:0.##}) O=({cy.O.X:0.#},{cy.O.Y:0.#},{cy.O.Z:0.#}) " +
                        $"轴向长={tmax - tmin:0.#} 面积={area:0}");
                }
            }
            var chain = JointRecognizer.BuildChain(model, axes);
            Console.WriteLine($"== 运动链: 根 #{chain.Root} ==");
            foreach (var line in chain.Report) Console.WriteLine("  " + line);
        }

        public static void Run(bool withMesh, string path)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            var sw = Stopwatch.StartNew();
            var model = StepModel.Load(path, s => Console.WriteLine("[load] " + s));
            Console.WriteLine($"解析: {sw.ElapsedMilliseconds} ms, 实体条目 {model.File.Entities.Count}");
            Console.WriteLine($"实体数: {model.Solids.Count}");
            Console.WriteLine($"面数:   {model.AllFaces.Count}");
            foreach (var g in model.AllFaces.GroupBy(f => f.S?.GetType().Name ?? "null").OrderByDescending(g => g.Count()))
                Console.WriteLine($"  曲面 {g.Key,-14} x {g.Count()}");
            if (model.Warnings.Count > 0)
            {
                Console.WriteLine($"警告: {model.Warnings.Count} 条");
                foreach (var g in model.Warnings.GroupBy(w =>
                {
                    var s = w;
                    int p = s.IndexOf(':');
                    return p > 0 ? s.Substring(0, p) : s;
                }).OrderByDescending(g => g.Count()).Take(6))
                    Console.WriteLine($"  [{g.Count()}] {g.Key}");
                foreach (var w in model.Warnings.Take(2)) Console.WriteLine("  样本: " + w.Substring(0, Math.Min(150, w.Length)));
                foreach (var w in model.Warnings.Where(x => x.Contains("原点")).Take(12))
                    Console.WriteLine("  " + w.Substring(0, Math.Min(220, w.Length)));
            }

            {
                Console.WriteLine($"实体包围盒: {model.Solids.Count}");
                for (int i = 0; i < model.Solids.Count; i++)
                {
                    var s = model.Solids[i];
                    var mn = new V3(float.MaxValue); var mx = new V3(float.MinValue);
                    foreach (var f in s.Faces) foreach (var l in f.Loops) foreach (var p in l) { mn = V3.Min(mn, p); mx = V3.Max(mx, p); }
                    var sz = mx - mn;
                    Console.WriteLine($"  #{i,2} {s.Name,-28} 面={s.Faces.Count,4} 尺寸=({sz.X:0.#},{sz.Y:0.#},{sz.Z:0.#}) " +
                        $"范围=({mn.X:0.#},{mn.Y:0.#},{mn.Z:0.#})~({mx.X:0.#},{mx.Y:0.#},{mx.Z:0.#})");
                }
            }

            {
                var axes = JointRecognizer.DetectAxes(model);
                Console.WriteLine($"关节轴候选: {axes.Count}");
                foreach (var j in axes.Take(8))
                    Console.WriteLine($"  R={j.Radius,7:0.##} 面={j.FaceCount,4} 得分={j.Score,10:0} " +
                        $"轴心=({j.O.X:0.#},{j.O.Y:0.#},{j.O.Z:0.#}) 方向=({j.Dir.X:0.##},{j.Dir.Y:0.##},{j.Dir.Z:0.##})");
                var chain = JointRecognizer.BuildChain(model, axes);
                Console.WriteLine($"运动链: 根实体 #{chain.Root}, 关节 {chain.Joints.Count}");
                foreach (var line in chain.Report) Console.WriteLine("  " + line);
            }

            // 重复面指纹：同一几何面被拓扑重复引用时，两套网格会近共面重叠（z-fighting → 斑点/条纹）
            {
                var dup = model.AllFaces.Select((f, i) => new { i, k = Finger(f) })
                    .GroupBy(x => x.k).Where(g => g.Count() > 1).OrderByDescending(g => g.Count()).ToList();
                Console.WriteLine($"重复面指纹组: {dup.Count}, 涉及面 {dup.Sum(g => g.Count())} / {model.AllFaces.Count}");
                foreach (var g in dup.Take(5))
                    Console.WriteLine($"  x{g.Count()} 面#{string.Join(",", g.Take(6).Select(x => x.i))} {g.Key}");
            }

            if (withMesh)
            {
                sw.Restart();
                Tessellator.ResetProfiler();
                var tess = new Tessellator { Deflection = 0.8 };
                var mesh = tess.Tessellate(model.AllFaces);
                Console.WriteLine($"网格化: {sw.ElapsedMilliseconds} ms");
                Console.WriteLine(Tessellator.ProfileReport());
                Console.WriteLine($"三角形: {mesh.TriangleCount:N0}, 顶点: {mesh.Positions.Length / 3:N0}");
                Console.WriteLine($"包围盒: {mesh.BBoxMin} ~ {mesh.BBoxMax}");
                Console.WriteLine($"空/失败面: {tess.Diag.Count} / {model.AllFaces.Count}");
                foreach (var g in tess.Diag.GroupBy(d =>
                {
                    int i = d.IndexOf('['), j = d.IndexOf(']');
                    string t = i >= 0 && j > i ? d.Substring(i, j - i + 1) : "?";
                    int k = d.IndexOf(']', i);
                    string r = k >= 0 && k + 1 < d.Length ? d.Substring(k + 1).Trim() : "?";
                    return t + " " + r;
                }).OrderByDescending(g => g.Count()))
                    Console.WriteLine($"  {g.Key} x{g.Count()}");
                foreach (var g in tess.Diag.GroupBy(d => d.Substring(d.IndexOf(']') + 1)).OrderByDescending(g => g.Count()))
                    Console.WriteLine($"  {g.Count()}x {g.Key.Trim()}");
                foreach (var d in tess.Diag.Take(20)) Console.WriteLine("  " + d);

                Console.WriteLine($"细长三角形: {tess.CntSliver:N0} 个, 其中翼片(长边>4×Deflection) {tess.CntFlap:N0} 个 (已丢弃针状 {tess.CntPin:N0}), 涉及面 {tess.SliverDiag.Count} / {model.AllFaces.Count}");
                Console.WriteLine($"丢弃三角形: 盒外={tess.CntRejBox:N0} 零面积={tess.CntRejDeg:N0} 超长边={tess.CntRejEdge:N0} 针状={tess.CntPin:N0}");
                int FlapOf(string d)
                {
                    int k = d.IndexOf("翼片 ");
                    if (k < 0) return 0;
                    int sp = d.IndexOf(' ', k + 3);
                    return int.TryParse(d.Substring(k + 3, sp - k - 3), out var v) ? v : 0;
                }
                foreach (var g in tess.SliverDiag.GroupBy(d => FlapOf(d) > 0))
                    Console.WriteLine($"  {(g.Key ? "含翼片面" : "仅宽高比细长面")} x{g.Count()}");
                foreach (var d in tess.SliverDiag.OrderByDescending(FlapOf).Take(15)) Console.WriteLine("  " + d);

                // 刀片状面：整体 3D 尺寸一维极薄（<1mm）而另一维很长（>10mm），
                // 屏幕上呈细丝/条纹，且与网格密度无关
                if (tess.Results != null)
                {
                    // 等效宽度 = 2×环面积/环周长：区分"细长条带"(窄) 与"轴对齐平面"(某轴尺寸 0 但很宽)
                    var blades = new List<(int i, double wid, double len, double area)>();
                    for (int i = 0; i < tess.Results.Length; i++)
                    {
                        var md = tess.Results[i];
                        if (md == null || md.TriangleCount == 0) continue;
                        var sz = md.BBoxMax - md.BBoxMin;
                        double hi = Math.Max(sz.X, Math.Max(sz.Y, sz.Z));
                        double area = 0, perim = 0;
                        foreach (var l in model.AllFaces[i].Loops)
                        {
                            int n = l.Count;
                            if (n < 3) continue;
                            var c = V3.Zero; foreach (var q in l) c += q; c /= n;
                            var acc = V3.Zero;
                            for (int k = 0; k < n; k++)
                            {
                                acc += V3.Cross(l[k] - c, l[(k + 1) % n] - c);
                                perim += (l[(k + 1) % n] - l[k]).Length();
                            }
                            area += acc.Length() * 0.5;
                        }
                        double wid = perim > 0 ? 2 * area / perim : 0;
                        if (hi > 10 && wid < 1.0) blades.Add((i, wid, hi, area));
                    }
                    Console.WriteLine($"细长条带面(最长>10mm 且 等效宽度<1mm): {blades.Count}");
                    Console.WriteLine($"  宽度分档: <0.05mm={blades.Count(x => x.wid < 0.05)} " +
                        $"<0.1mm={blades.Count(x => x.wid < 0.1)} <0.5mm={blades.Count(x => x.wid < 0.5)} <1mm={blades.Count}");
                    foreach (var b in blades.OrderByDescending(x => x.len).Take(6))
                    {
                        var f = model.AllFaces[b.i];
                        var lo = new V3(float.MaxValue); var hi2 = new V3(float.MinValue);
                        foreach (var l in f.Loops) foreach (var q in l) { lo = V3.Min(lo, q); hi2 = V3.Max(hi2, q); }
                        Console.WriteLine($"  面#{b.i}/STEP#{f.StepId} [{f.S?.GetType().Name}] 宽={b.wid:0.###}mm 长={b.len:0.##}mm 面积={b.area:0.##}mm² " +
                            $"环=({lo.X:0.#},{lo.Y:0.#},{lo.Z:0.#})~({hi2.X:0.#},{hi2.Y:0.#},{hi2.Z:0.#})");
                    }
                    Console.WriteLine($"  模型包围盒: {mesh.BBoxMin} ~ {mesh.BBoxMax}");
                }

                FeatureRecognizer.Recognize(model.AllFaces, mesh.BBoxMin, mesh.BBoxMax);
                foreach (var g in model.AllFaces.GroupBy(f => f.Feature).OrderByDescending(g => g.Count()))
                    Console.WriteLine($"  特征 {g.Key,-10} x {g.Count()}");
                var holes = model.AllFaces.Where(f => f.Feature == "孔").OrderByDescending(f => f.Radius).Take(8).ToList();
                Console.WriteLine("最大孔径 TOP8:");
                foreach (var h in holes) Console.WriteLine($"  Ø{h.Radius * 2:0.##} mm");
            }
        }
    }
}
