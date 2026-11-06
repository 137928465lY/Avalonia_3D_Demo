using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using V3 = System.Numerics.Vector3;

namespace OpenStepViewer.Geom
{
    public class MeshData
    {
        public float[] Positions;   // xyz
        public float[] Normals;     // xyz
        public int[] Indices;
        public V3 BBoxMin, BBoxMax;
        public double LoopDiag;     // 该面环的 3D 对角线（尺度基准）
        public int TriangleCount => Indices.Length / 3;
    }

    public class Tessellator
    {
        public double Deflection = 0.5;   // 弦高(mm)
        public int MaxGridPerAxis = 320;
        public readonly List<string> Diag = new List<string>(); // 网格化失败面诊断
        public MeshData[] Results;   // 逐面网格（诊断用）
        public readonly List<string> SliverDiag = new List<string>(); // 细长三角形（翼片）诊断
        public long CntSliver, CntFlap, CntPin;
        public long CntRejBox, CntRejDeg, CntRejEdge;
        object _diagGate = new object();

        // --- 性能剖析（Interlocked 累加，避免锁）---
        public static long TicksEval, TicksNormal, TicksScan, TicksSample, TicksTri;
        public static long CntEval, CntNormal, CntGridPts;
        public static void ResetProfiler() { TicksEval = TicksNormal = TicksScan = TicksSample = TicksTri = 0; CntEval = CntNormal = CntGridPts = 0; }
        public static string ProfileReport()
        {
            double Ms(long t) => t * 1000.0 / Stopwatch.Frequency;
            return $"  剖析(累加): 扫描线={Ms(TicksScan):N0}ms 采样={Ms(TicksSample):N0}ms 三角化={Ms(TicksTri):N0}ms " +
                   $"Eval={Ms(TicksEval):N0}ms/{CntEval:N0} Normal={Ms(TicksNormal):N0}ms/{CntNormal:N0} 网格点={CntGridPts:N0}";
        }

        MeshData Fail(int faceIdx, FaceInfo f, string reason)
        {
            lock (_diagGate) Diag.Add($"面#{faceIdx} [{f.S?.GetType().Name}] {reason}");
            return null;
        }

        public MeshData Tessellate(List<FaceInfo> faces)
        {
            var sw = Stopwatch.StartNew();
            var results = new MeshData[faces.Count];
            object gate = new object();
            int totalVerts = 0, totalIdx = 0;
            V3 mn = new V3(float.MaxValue), mx = new V3(float.MinValue);

            Parallel.For(0, faces.Count, i =>
            {
                results[i] = TessellateFace(faces[i], i);
            });

            // 全局环包围盒：所有面的环点都在模型表面上。
            // 参数化异常的采样点会跑到远处形成链式细丝/翼片，全局尺度用于最终过滤
            var gmin = new V3(float.MaxValue); var gmax = new V3(float.MinValue);
            foreach (var f in faces)
                foreach (var loop in f.Loops)
                    foreach (var p3 in loop) { gmin = V3.Min(gmin, p3); gmax = V3.Max(gmax, p3); }
            double gdiag = (gmax - gmin).Length() + Deflection * 2;
            float margin = (float)(gdiag * 0.05); // 盒外扩 5% 对角线为容差
            double gLim2 = (double)margin * margin; // 顶点在 bbox 外扩盒之外即视为异常

            var posList = new List<float>(1 << 22);
            var norList = new List<float>(1 << 22);
            var idxList = new List<int>(1 << 23);

            for (int i = 0; i < faces.Count; i++)
            {
                var md = results[i];
                var f = faces[i];
                if (md == null) { f.VStart = f.IStart = f.VCount = f.ICount = 0; continue; }
                int vBase = posList.Count / 3;
                posList.AddRange(md.Positions);
                norList.AddRange(md.Normals);
                int iBase = idxList.Count;
                int kept = 0;
                for (int k = 0; k < md.Indices.Length; k += 3)
                {
                    // 全局尺度过滤：任一顶点落在模型 bbox 外扩盒之外则丢弃该三角形
                    int i0 = md.Indices[k], i1 = md.Indices[k + 1], i2 = md.Indices[k + 2];
                    bool bad = DistToBox2(md.Positions, i0, gmin, gmax, margin) > gLim2 ||
                               DistToBox2(md.Positions, i1, gmin, gmax, margin) > gLim2 ||
                               DistToBox2(md.Positions, i2, gmin, gmax, margin) > gLim2;
                    if (bad) continue;
                    idxList.Add(i0 + vBase); idxList.Add(i1 + vBase); idxList.Add(i2 + vBase);
                    kept += 3;
                }
                f.VStart = vBase; f.VCount = md.Positions.Length / 3;
                f.IStart = iBase; f.ICount = kept;
                totalVerts += f.VCount; totalIdx += kept;
                int tot = md.Indices.Length / 3;
                if (tot > 0 && kept / 3 < tot)
                    Fail(i, f, $"全局过滤删除 {tot - kept / 3}/{tot} 三角形(参数化异常)");
                if (kept > 0) { mn = V3.Min(mn, md.BBoxMin); mx = V3.Max(mx, md.BBoxMax); }
            }

            sw.Stop();
            Results = results;   // 供诊断：逐面网格（含 bbox）
            return new MeshData
            {
                Positions = posList.ToArray(),
                Normals = norList.ToArray(),
                Indices = idxList.ToArray(),
                BBoxMin = gmin, BBoxMax = gmax   // 环点在表面上，环 bbox 即模型真实 bbox（不受孤儿顶点污染）
            };
        }

        MeshData TessellateFace(FaceInfo face, int faceIdx)
        {
            var s = face.S;
            try
            {
                // 1. 环 → UV（反求 + 接缝展开）
                var uvLoops = new List<List<V2>>(face.Loops.Count);
                int nanCount = 0;
                foreach (var loop in face.Loops)
                {
                    var uv = new List<V2>(loop.Count);
                    double prevU = double.NaN;
                    foreach (var p in loop)
                    {
                        var q = s.Inverse(p);
                        if (double.IsNaN(q.U) || double.IsNaN(q.V)) { nanCount++; continue; } // 反求失败点跳过，避免 NaN 污染包围盒导致整面缺失
                        if (s.UWrapped && !double.IsNaN(prevU))
                        {
                            while (q.U - prevU > Math.PI) q.U -= s.UPeriod;
                            while (q.U - prevU < -Math.PI) q.U += s.UPeriod;
                        }
                        prevU = q.U;
                        uv.Add(q);
                    }
                    if (uv.Count >= 3) uvLoops.Add(uv);
                }
                if (uvLoops.Count == 0) return Fail(faceIdx, face, $"无有效环(原始环点: {string.Join(",", face.Loops.Select(l => l.Count))}, NaN: {nanCount}, 环NaN点: {string.Join(",", face.Loops.Select(l => l.Count(p => float.IsNaN(p.X) || float.IsNaN(p.Y) || float.IsNaN(p.Z))))})");

                // 2. UV 包围盒
                double umin = double.MaxValue, umax = double.MinValue, vmin = double.MaxValue, vmax = double.MinValue;
                foreach (var l in uvLoops)
                    foreach (var q in l)
                    {
                        if (q.U < umin) umin = q.U; if (q.U > umax) umax = q.U;
                        if (q.V < vmin) vmin = q.V; if (q.V > vmax) vmax = q.V;
                    }
                if (umax <= umin || vmax <= vmin) return Fail(faceIdx, face, $"UV包围盒无效 ({umin:0.###},{umax:0.###})x({vmin:0.###},{vmax:0.###})");

                double du = umax - umin, dv = vmax - vmin;
                // 环 3D 尺度上界：环点都在该面上，采样网格点不应远超此尺度
                var lbmin = new V3(float.MaxValue); var lbmax = new V3(float.MinValue);
                foreach (var loop in face.Loops)
                    foreach (var p3 in loop) { lbmin = V3.Min(lbmin, p3); lbmax = V3.Max(lbmax, p3); }
                double loopDiag = (lbmax - lbmin).Length() + Deflection;
                var lbCenter = (lbmin + lbmax) * 0.5f;
                if (s is PlaneS == false)
                {
                    // U 为周期方向（环展开后跨度 ≤ 2π）；球/环面的 V 也是角度量
                    if (du > 4 * Math.PI) return Fail(faceIdx, face, $"U跨度超周期 du={du:0.##}");
                    if ((s is SphereS || s is TorusS) && dv > 4 * Math.PI) return Fail(faceIdx, face, $"V跨度超周期 dv={dv:0.##}");
                }

                // 3. 网格步长：使单元弦高≈Deflection
                s.UVRange(out var ku0, out var ku1, out var kv0, out var kv1);
                if (s is PlaneS)
                { ku0 = umin; ku1 = umax; kv0 = vmin; kv1 = vmax; }

                // 周期/无限方向上 Eval 对任意参数有效，禁止 clamp 到 knots 范围：
                // 解析曲面 Inverse 返回 atan2∈(-π,π]，与 UVRange 的 [0,2π] 错位，
                // 环点整段落在负半区时 clamp 会把采样网格压成一条线，导致整面缺失。
                bool uFree = s.UWrapped || s is PlaneS;
                bool vFree = s is CylinderS || s is ConeS || s is TorusS;

                // 估算世界尺度（|Su|、|Sv| 均值）
                double scaleU = 0, scaleV = 0; int cnt = 0;
                for (int i = 0; i <= 4; i++)
                    for (int j = 0; j <= 4; j++)
                    {
                        double uu = umin + du * i / 4, vv = vmin + dv * j / 4;
                        if (!uFree && (uu < ku0 || uu > ku1)) continue;
                        if (!vFree && (vv < kv0 || vv > kv1)) continue;
                        var (su, sv) = s.Deriv(uu, vv);
                        scaleU += su.Length(); scaleV += sv.Length(); cnt++;
                    }
                if (cnt == 0) return Fail(faceIdx, face, "导数采样全失败");
                scaleU /= cnt; scaleV /= cnt;
                if (scaleU < 1e-9) scaleU = 1e-9;
                if (scaleV < 1e-9) scaleV = 1e-9;

                // 圆柱/圆锥 V 为轴向线性坐标，环覆盖的轴向长度不可能超过环 3D 对角线；
                // 反求异常时 V 跨度会远超面尺度，产生延伸到远处的翼片三角形
                if (s is PlaneS == false && dv * scaleV > 2 * loopDiag)
                    return Fail(faceIdx, face, $"V世界跨度异常 dv={dv:0.##}x{scaleV:0.##} > 2x环尺度{loopDiag:0.##}");

                // U 方向对称钳制：|Su| 局部爆炸时 U 弧长估算会远超面尺度，步长算出的单元数
                // 打满 MaxGridPerAxis，单元宽度远小于 Deflection，渲染成"针状"细条纹并浪费大量三角形。
                // 这里不拒绝该面，只把尺度估算压回 2×环尺度
                if (s is PlaneS == false && du * scaleU > 2 * loopDiag)
                    scaleU = 2 * loopDiag / du;

                // 下限 4：小特征(圆角/倒角/小球面)的环可能小于一个单元，网格过稀会导致
                // 采样点全部落在环外("无内部点")或无法构成三角形
                int nu = (int)Math.Clamp(Math.Ceiling(du * scaleU / Deflection), 4, MaxGridPerAxis);
                int nv = (int)Math.Clamp(Math.Ceiling(dv * scaleV / Deflection), 4, MaxGridPerAxis);
                nu = Math.Min(nu, MaxGridPerAxis); nv = Math.Min(nv, MaxGridPerAxis);

                // 4. 采样网格 + 内外判定
                // 允许节点偏离环盒的容差：边界单元借环外节点的合法偏离 ≤ 1-2 个单元（几 mm），
                // 而参数化异常的链式细丝会偏离几十~几百 mm
                double nodeMargin = Deflection * 6 + loopDiag * 0.05;
                var verts = new V3[(nu + 1) * (nv + 1)];
                var inside = new bool[(nu + 1) * (nv + 1)];
                bool anyInside = false;

                // 4a. UV 扫描线填充 inside 位图（替代逐点 even-odd 射线法）
                // 逐点判定是 O(点数 × 环边数)，扫描线是 O(行数 × 边数 + 点数 log 边数)，实测快 20~50 倍
                long tScan0 = Stopwatch.GetTimestamp();

                // 环形边的扁平数组，节点判定与单元中心判定共用
                int ec = 0;
                foreach (var l in uvLoops) if (l.Count > 2) ec += l.Count;
                var eV0 = new double[ec]; var eV1 = new double[ec];
                var eU0 = new double[ec]; var eU1 = new double[ec];
                int nEdge = 0;
                foreach (var l in uvLoops)
                {
                    if (l.Count < 3) continue;
                    for (int k = 0, m = l.Count - 1; k < l.Count; m = k++)
                    {
                        eV0[nEdge] = l[m].V; eV1[nEdge] = l[k].V;
                        eU0[nEdge] = l[m].U; eU1[nEdge] = l[k].U;
                        nEdge++;
                    }
                }
                ScanlineFill(inside, nv + 1, false, umin, du, nu, vmin, dv, nv,
                    uFree, vFree, ku0, ku1, kv0, kv1, eV0, eV1, eU0, eU1, nEdge);

                Interlocked.Add(ref TicksScan, Stopwatch.GetTimestamp() - tScan0);

                // 单元中心位图（按需构建）：仅小特征面需要，避免对每个空单元做 Eval + 逐点 InsideUV
                bool[] cellInside = null;
                void EnsureCellInside()
                {
                    if (cellInside != null) return;
                    cellInside = new bool[Math.Max(1, nu * nv)];
                    long tc0 = Stopwatch.GetTimestamp();
                    ScanlineFill(cellInside, nv, true, umin, du, nu, vmin, dv, nv,
                        uFree, vFree, ku0, ku1, kv0, kv1, eV0, eV1, eU0, eU1, nEdge);
                    Interlocked.Add(ref TicksScan, Stopwatch.GetTimestamp() - tc0);
                }

                // 4b. 采样位置（法线延迟到顶点真正被引用时再算，可省 ~80% 的导数计算）
                // 同时在节点级预处理"是否在环盒容差内"，三角化时按索引查表，
                // 避免每个三角形做 3 次 DistToBox2（约 3×三角形数次调用）
                double faceMargin = loopDiag * 0.5 + Deflection * 4; // 允许超出环盒的容差（边界单元借环外节点）
                var inBox = new bool[verts.Length];

                // 4b-0. 边界节点吸附：把贴近环折线的网格节点投影到折线上。
                // 相邻面共享同一条环折线（同一 EDGE 的采样点逐点相同），两面边界顶点因此落在
                // 同一条 3D 折线上，逐面独立网格化的台阶缝隙（T 接缝）随之消失。
                long tSnap0 = Stopwatch.GetTimestamp();
                var nodeU = new double[verts.Length];
                var nodeV = new double[verts.Length];
                {
                    double cellU = du / nu, cellV = dv / nv;
                    double tolU = 0.8 * cellU, tolV = 0.8 * cellV;
                    for (int i = 0; i <= nu; i++)
                        for (int j = 0; j <= nv; j++)
                        {
                            int idx = i * (nv + 1) + j;
                            double u = umin + du * i / nu;
                            if (!uFree) u = Clamp(u, ku0, ku1);
                            double v = vmin + dv * j / nv;
                            if (!vFree) v = Clamp(v, kv0, kv1);
                            nodeU[idx] = u; nodeV[idx] = v;

                            // 只处理边界附近：四邻域内外状态不一致，或位于网格外圈
                            bool nearEdge = i == 0 || j == 0 || i == nu || j == nv;
                            if (!nearEdge)
                            {
                                bool st = inside[idx];
                                nearEdge = inside[idx - (nv + 1)] != st || inside[idx + (nv + 1)] != st
                                        || inside[idx - 1] != st || inside[idx + 1] != st;
                            }
                            if (!nearEdge || nEdge == 0) continue;

                            // 吸附到最近的"环点"而非折线上的投影点：环点是 3D 曲线采样点，
                            // 相邻面在该边上共享同一批点，3D 坐标完全一致 → 光栅化不会漏出背景缝
                            double best = 1.0, bu = u, bv = v;
                            for (int e = 0; e < nEdge; e++)
                            {
                                double eu = (u - eU0[e]) / cellU, ev = (v - eV0[e]) / cellV;
                                double d2 = eu * eu + ev * ev;
                                if (d2 < best) { best = d2; bu = eU0[e]; bv = eV0[e]; }
                            }
                            // 吸附半径 1 个单元（best 为按单元尺寸归一化的平方距离）
                            if (best < 1.0) { nodeU[idx] = bu; nodeV[idx] = bv; }
                        }
                }
                Interlocked.Add(ref TicksScan, Stopwatch.GetTimestamp() - tSnap0);

                long tSample0 = Stopwatch.GetTimestamp();
                for (int i = 0; i <= nu; i++)
                {
                    double u = umin + du * i / nu;
                    if (!uFree) u = Clamp(u, ku0, ku1);
                    for (int j = 0; j <= nv; j++)
                    {
                        int idx = i * (nv + 1) + j;
                        double v = vmin + dv * j / nv;
                        if (!vFree) v = Clamp(v, kv0, kv1);
                        // 全部网格点都要采样：边界单元会借用环外节点的 3D 位置贴边
                        long te0 = Stopwatch.GetTimestamp();
                        var p = s.Eval(nodeU[idx], nodeV[idx]);
                        Interlocked.Add(ref TicksEval, Stopwatch.GetTimestamp() - te0);
                        Interlocked.Increment(ref CntEval);
                        Interlocked.Increment(ref CntGridPts);
                        verts[idx] = p;
                        inBox[idx] = DistToBox2(p, lbmin, lbmax, faceMargin) == 0;
                        if (!inside[idx]) continue;
                        // 3D 合理性：参数化异常的面在局部区域 UV 微小变化会导致 3D 位置
                        // 跳到远处（scale 局部爆炸），形成链式细丝。此类节点直接视为 outside，
                        // 从源头切断细丝（整面均值 scale 检查抓不到局部异常）
                        if (DistToBox2(p, lbmin, lbmax, nodeMargin) > 0) inside[idx] = false;
                        else anyInside = true;
                    }
                }

                Interlocked.Add(ref TicksSample, Stopwatch.GetTimestamp() - tSample0);
                tSample0 = Stopwatch.GetTimestamp();   // 复用为三角化起点

                var l0 = uvLoops[0];
                if (!anyInside)
                {
                    double sa = 0;
                    for (int a = 0, b = l0.Count - 1; a < l0.Count; b = a++) sa += l0[b].U * l0[a].V - l0[a].U * l0[b].V;
                    sa /= 2;
                    var pts = string.Join("", l0.Take(30).Select(q => $"({q.U:0.##},{q.V:0.##})"));
                    return Fail(faceIdx, face, $"无内部点(du={du:0.###},dv={dv:0.###},nu={nu},nv={nv}, 面积={sa:0.###}, 环数={uvLoops.Count}, 环0[{l0.Count}]: {pts})");
                }

                // 5. 三角化（网格单元 even-odd）
                var posOut = new List<V3>();
                var norOut = new List<V3>();
                var idxOut = new List<int>();
                var map = new int[verts.Length];
                Array.Fill(map, -1);

                // 法线按需计算：只有真正被三角形引用的顶点才算导数（约占网格点的 20%）
                V3 NormalAt(int gi)
                {
                    var n = s.Normal(nodeU[gi], nodeV[gi]);
                    return face.SameSense ? n : -n;
                }

                int GetVert(int gi)
                {
                    if (map[gi] >= 0) return map[gi];
                    int id = posOut.Count;
                    long t0 = Stopwatch.GetTimestamp();
                    var n = NormalAt(gi);
                    Interlocked.Add(ref TicksNormal, Stopwatch.GetTimestamp() - t0);
                    Interlocked.Increment(ref CntNormal);
                    posOut.Add(verts[gi]);
                    norOut.Add(n);
                    map[gi] = id;
                    return id;
                }

                // 添加三角形，过滤零面积退化三角形与超长边/远翼片
                double maxEdge2 = 400 * Deflection * Deflection; // 正常网格边长约为弦高的 2~3 倍
                int faceSliver = 0, faceFlap = 0, facePin = 0; double faceSliverMax = 0, faceSliverLen = 0; V3 faceSliverPt = default;
                int rejBox = 0, rejDeg = 0, rejEdge = 0;
                double flapEdge2 = 16 * Deflection * Deflection; // 长边 > 4×Deflection 视为异常外伸（翼片）
                void AddTri(int g0, int g1, int g2)
                {
                    // 先做位图/几何过滤，被丢弃的三角形不再创建顶点与法线
                    if (!inBox[g0] || !inBox[g1] || !inBox[g2]) { rejBox++; return; }
                    var p0 = verts[g0]; var p1 = verts[g1]; var p2 = verts[g2];
                    var cr = V3.Cross(p1 - p0, p2 - p0);
                    if (cr.LengthSquared() < 1e-16) { rejDeg++; return; }
                    // 反求/参数化异常时采样点可能落到曲面远端，产生从主体发散的细长翼片
                    double e01 = V3.DistanceSquared(p0, p1), e12 = V3.DistanceSquared(p1, p2), e20 = V3.DistanceSquared(p2, p0);
                    if (e01 > maxEdge2 || e12 > maxEdge2 || e20 > maxEdge2) { rejEdge++; return; }
                    // 翼片诊断：通过过滤但仍明显细长（最长边/最短边 > 10）
                    double eMax = Math.Max(e01, Math.Max(e12, e20));
                    double eMin = Math.Min(e01, Math.Min(e12, e20));
                    if (eMin > 0 && eMax > eMin * 100)
                    {
                        faceSliver++;
                        if (eMax > flapEdge2) faceFlap++;
                        double r = eMax / eMin;
                        if (r > faceSliverMax) { faceSliverMax = r; faceSliverLen = Math.Sqrt(eMax); faceSliverPt = p0; }
                    }
                    // 针状退化三角形：长边超过网格尺寸、最短边却趋近 0（锥尖收敛/参数化退化），
                    // 屏幕上只有孤立像素点，形成断续"细丝"，直接丢弃
                    if (eMax > flapEdge2 && eMax > eMin * 100) { facePin++; return; }
                    int i0 = GetVert(g0), i1 = GetVert(g1), i2 = GetVert(g2);
                    idxOut.Add(i0); idxOut.Add(i1); idxOut.Add(i2);
                }

                for (int i = 0; i < nu; i++)
                    for (int j = 0; j < nv; j++)
                    {
                        int a = i * (nv + 1) + j;
                        int b = (i + 1) * (nv + 1) + j;
                        int c = (i + 1) * (nv + 1) + j + 1;
                        int d = i * (nv + 1) + j + 1;
                        bool ca = inside[a], cb = inside[b], cc = inside[c], cd = inside[d];
                        int sum = (ca ? 1 : 0) + (cb ? 1 : 0) + (cc ? 1 : 0) + (cd ? 1 : 0);
                        if (sum == 0)
                        {
                            // 小特征：环可能完全落在一个单元内，四角节点全 outside。
                            // 单元中心内外判定复用扫描线位图（先做 UV 判定，通过后再 Eval 做 3D 校验）
                            EnsureCellInside();
                            if (!cellInside[i * nv + j]) continue;
                            double uc = umin + du * (i + 0.5) / nu, vc = vmin + dv * (j + 0.5) / nv;
                            if (!uFree) uc = Clamp(uc, ku0, ku1);
                            if (!vFree) vc = Clamp(vc, kv0, kv1);
                            var cp3 = s.Eval(uc, vc);
                            if (DistToBox2(cp3, lbmin, lbmax, nodeMargin) > 0) continue;
                            ca = cb = cc = cd = true;
                        }
                        if (sum == 4 || (ca && cb && cc) || (cb && cc && cd) || (cc && cd && ca) || (cd && ca && cb))
                        {
                            AddTri(a, b, c);
                            AddTri(a, c, d);
                        }
                        else if (sum == 3)
                        {
                            if (ca && cb && cc) AddTri(a, b, c);
                            else if (cb && cc && cd) AddTri(b, c, d);
                            else if (cc && cd && ca) AddTri(c, d, a);
                            else AddTri(d, a, b);
                        }
                        else // sum==2 或 1：对角拆分
                        {
                            if (ca && cc) AddTri(a, b, c);
                            else if (cb && cd) AddTri(b, c, d);
                            if (cb && cc) AddTri(b, c, d);
                            else if (cd && ca) AddTri(d, a, b);
                            if (ca && cb) AddTri(a, b, d);
                            if (cc && cd) AddTri(c, d, a);
                        }
                    }

                Interlocked.Add(ref TicksTri, Stopwatch.GetTimestamp() - tSample0);
                if (idxOut.Count == 0) return Fail(faceIdx, face, "三角化为空(全部退化?)");

                Interlocked.Add(ref CntRejBox, rejBox);
                Interlocked.Add(ref CntRejDeg, rejDeg);
                Interlocked.Add(ref CntRejEdge, rejEdge);
                if (faceSliver > 0)
                {
                    Interlocked.Add(ref CntSliver, faceSliver);
                    Interlocked.Add(ref CntFlap, faceFlap);
                    if (facePin > 0) Interlocked.Add(ref CntPin, facePin);
                    lock (_diagGate)
                        SliverDiag.Add($"面#{faceIdx} [{s?.GetType().Name}] 翼片 {faceFlap} 细长 {faceSliver}/{idxOut.Count / 3} " +
                            $"丢弃针状 {facePin} " +
                            $"最细比={faceSliverMax:0} 最长边={faceSliverLen:0.##} nu={nu} nv={nv} du={du:0.###} dv={dv:0.###} " +
                            $"loopDiag={loopDiag:0.##} 样本=({faceSliverPt.X:0.#},{faceSliverPt.Y:0.#},{faceSliverPt.Z:0.#})");
                }

                var mnp = new V3(float.MaxValue); var mxp = new V3(float.MinValue);
                foreach (var p in posOut) { mnp = V3.Min(mnp, p); mxp = V3.Max(mxp, p); }

                var posF = new float[posOut.Count * 3];
                var norF = new float[norOut.Count * 3];
                for (int i = 0; i < posOut.Count; i++)
                {
                    posF[i * 3] = posOut[i].X; posF[i * 3 + 1] = posOut[i].Y; posF[i * 3 + 2] = posOut[i].Z;
                    norF[i * 3] = norOut[i].X; norF[i * 3 + 1] = norOut[i].Y; norF[i * 3 + 2] = norOut[i].Z;
                }
                return new MeshData { Positions = posF, Normals = norF, Indices = idxOut.ToArray(), BBoxMin = mnp, BBoxMax = mxp, LoopDiag = loopDiag };
            }
            catch (Exception ex)
            {
                return Fail(faceIdx, face, "异常: " + ex.Message);
            }
        }

        static double Clamp(double x, double a, double b) => x < a ? a : (x > b ? b : x);

        // 点到 AABB（外扩 margin）距离的平方；点在盒内返回 0
        static double DistToBox2(V3 p, V3 bmin, V3 bmax, double margin)
        {
            double dx = Math.Max(Math.Max(bmin.X - margin - p.X, p.X - (bmax.X + margin)), 0);
            double dy = Math.Max(Math.Max(bmin.Y - margin - p.Y, p.Y - (bmax.Y + margin)), 0);
            double dz = Math.Max(Math.Max(bmin.Z - margin - p.Z, p.Z - (bmax.Z + margin)), 0);
            return dx * dx + dy * dy + dz * dz;
        }

        // 顶点数组版（vi 为顶点索引）
        static double DistToBox2(float[] pos, int vi, V3 bmin, V3 bmax, float margin)
        {
            float x = pos[vi * 3], y = pos[vi * 3 + 1], z = pos[vi * 3 + 2];
            float dx = MathF.Max(MathF.Max(bmin.X - margin - x, x - (bmax.X + margin)), 0);
            float dy = MathF.Max(MathF.Max(bmin.Y - margin - y, y - (bmax.Y + margin)), 0);
            float dz = MathF.Max(MathF.Max(bmin.Z - margin - z, z - (bmax.Z + margin)), 0);
            return (double)dx * dx + (double)dy * dy + (double)dz * dz;
        }

        // even-odd 内外判定
        static bool InsideUV(double u, double v, List<List<V2>> loops)
        {
            bool inside = false;
            foreach (var loop in loops)
            {
                for (int i = 0, j = loop.Count - 1; i < loop.Count; j = i++)
                {
                    var a = loop[i]; var b = loop[j];
                    if ((a.V > v) != (b.V > v))
                    {
                        double t = (v - a.V) / (b.V - a.V);
                        if (u < a.U + t * (b.U - a.U)) inside = !inside;
                    }
                }
            }
            return inside;
        }

        // 扫描线填充 even-odd 内外位图
        // centers=false: 判定 (nu+1)×(nv+1) 节点，stride=nv+1
        // centers=true : 判定 nu×nv 单元中心，stride=nv
        static void ScanlineFill(bool[] bmp, int stride, bool centers,
            double umin, double du, int nu, double vmin, double dv, int nv,
            bool uFree, bool vFree, double ku0, double ku1, double kv0, double kv1,
            double[] eV0, double[] eV1, double[] eU0, double[] eU1, int ei)
        {
            if (bmp.Length == 0) return;
            double off = centers ? 0.5 : 0.0;
            int nCol = centers ? nu : nu + 1;
            int nRow = centers ? nv : nv + 1;
            var xs = new double[Math.Max(1, ei)];
            double lastV = double.NaN;
            int lastNx = 0;
            for (int j = 0; j < nRow; j++)
            {
                double v = vmin + dv * (j + off) / nv;
                if (!vFree) v = Clamp(v, kv0, kv1);
                if (v == lastV)
                {
                    // 同一行（clamp 压缩时）：复用上一行交点
                    if (lastNx == 0) continue;
                }
                else
                {
                    lastV = v; lastNx = 0;
                    for (int e = 0; e < ei; e++)
                    {
                        if ((eV0[e] > v) != (eV1[e] > v))
                            xs[lastNx++] = eU0[e] + (v - eV0[e]) / (eV1[e] - eV0[e]) * (eU1[e] - eU0[e]);
                    }
                    if (lastNx > 0) Array.Sort(xs, 0, lastNx);
                }
                if (lastNx == 0) continue;
                for (int i = 0; i < nCol; i++)
                {
                    double u = umin + du * (i + off) / nu;
                    if (!uFree) u = Clamp(u, ku0, ku1);
                    int lo = 0, hi = lastNx;
                    while (lo < hi) { int mid = (lo + hi) >> 1; if (xs[mid] < u) lo = mid + 1; else hi = mid; }
                    if ((lo & 1) == 1) bmp[i * stride + j] = true;
                }
            }
        }
    }

    // ---------- 特征识别 ----------
    public static class FeatureRecognizer
    {
        public static void Recognize(List<FaceInfo> faces, V3 bboxMin, V3 bboxMax)
        {
            double diag = (bboxMax - bboxMin).Length();
            foreach (var f in faces)
            {
                if (f.Loops.Count == 0) continue;
                var s = f.S;
                if (s is PlaneS)
                {
                    f.Feature = "平面";
                    var area = EstimateLoopArea(f.Loops[0]);
                    f.FeatureInfo = $"面积≈{area:0.#} mm²";
                }
                else if (s is CylinderS cy)
                {
                    var ang = AngularSpan(f);
                    bool full = ang >= Math.PI * 2 - 0.15;
                    bool concave = IsCylinderConcave(f, cy);
                    f.Radius = cy.R;
                    if (full && concave) { f.Feature = "孔"; f.FeatureInfo = $"Ø{cy.R * 2:0.##} 通/盲孔"; }
                    else if (full) { f.Feature = "圆柱凸台"; f.FeatureInfo = $"Ø{cy.R * 2:0.##}"; }
                    else if (concave) { f.Feature = "内圆角"; f.FeatureInfo = $"R{cy.R:0.##}"; }
                    else { f.Feature = "外圆角"; f.FeatureInfo = $"R{cy.R:0.##}"; }
                }
                else if (s is ConeS co)
                {
                    var ang = AngularSpan(f);
                    bool concave = IsConeConcave(f, co);
                    if (ang >= Math.PI * 2 - 0.15)
                    { f.Feature = concave ? "锥孔" : "锥形凸台"; f.FeatureInfo = $"半角 {Rad2Deg(Math.Atan(co.TanA)):0.#}°"; }
                    else { f.Feature = concave ? "锥面(凹)" : "倒角"; f.FeatureInfo = $"半角 {Rad2Deg(Math.Atan(co.TanA)):0.#}°"; }
                }
                else if (s is TorusS to)
                {
                    f.Feature = "圆角/环面"; f.FeatureInfo = $"R{to.Major:0.##}/r{to.Minor:0.##}";
                }
                else if (s is SphereS sp)
                {
                    f.Feature = "球面"; f.FeatureInfo = $"SR{sp.R:0.##}";
                }
                else if (s is ExtrusionS)
                {
                    f.Feature = "拉伸曲面"; f.FeatureInfo = "";
                }
                else
                {
                    f.Feature = "自由曲面"; f.FeatureInfo = $"NURBS";
                }
            }
        }

        static double Rad2Deg(double r) => r * 180 / Math.PI;

        static double EstimateLoopArea(List<V3> pts)
        {
            double area = 0;
            for (int i = 0, j = pts.Count - 1; i < pts.Count; j = i++)
                area += MathUtil.Cross(pts[j], pts[i]).Length() * 0.5;
            return area * 0.5;
        }

        // 环在 UV 的 u 跨度（近似判断整周）
        static double AngularSpan(FaceInfo f)
        {
            double umin = double.MaxValue, umax = double.MinValue;
            foreach (var loop in f.Loops)
            {
                double prevU = double.NaN, acc = 0;
                double lmin = double.MaxValue, lmax = double.MinValue;
                foreach (var p in loop)
                {
                    var q = f.S.Inverse(p);
                    if (!double.IsNaN(prevU))
                    {
                        while (q.U - prevU > Math.PI) q.U -= f.S.UPeriod;
                        while (q.U - prevU < -Math.PI) q.U += f.S.UPeriod;
                    }
                    prevU = q.U;
                    if (q.U < lmin) lmin = q.U; if (q.U > lmax) lmax = q.U;
                }
                acc = lmax - lmin;
                umin = Math.Min(umin, lmin); umax = Math.Max(umax, lmax);
            }
            return umax - umin;
        }

        static bool IsCylinderConcave(FaceInfo f, CylinderS cy)
        {
            foreach (var loop in f.Loops)
                foreach (var p in loop)
                {
                    var d = p - cy.O;
                    var radial = d - cy.Z * MathUtil.Dot(d, cy.Z);
                    float lr = radial.Length();
                    if (lr < 1e-6f) continue;
                    var n = f.S.Normal(0, 0);
                    // 在实际点处取法线
                    var q = cy.Inverse(p);
                    n = f.S.Normal(q.U, q.V);
                    return MathUtil.Dot(n, radial / lr) < 0;
                }
            return true;
        }

        static bool IsConeConcave(FaceInfo f, ConeS co)
        {
            foreach (var loop in f.Loops)
                foreach (var p in loop)
                {
                    var q = co.Inverse(p);
                    var n = f.S.Normal(q.U, q.V);
                    var d = p - co.O;
                    var radial = d - co.Z * MathUtil.Dot(d, co.Z);
                    float lr = radial.Length();
                    if (lr < 1e-6f) continue;
                    return MathUtil.Dot(n, radial / lr) < 0;
                }
            return true;
        }
    }
}
