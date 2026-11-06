using System;
using System.Collections.Generic;
using System.Linq;
using V3 = System.Numerics.Vector3;

namespace OpenStepViewer.Geom
{
    // 一条关节旋转轴：由同轴圆柱面聚类得到
    public class JointAxis
    {
        public V3 O;                 // 轴线上一点（世界坐标）
        public V3 Dir;               // 单位方向
        public double Radius;        // 代表半径（最大圆柱面）
        public double Score;         // 面积加权得分（越大越像关节）
        public int FaceCount;
        public double AxialMin, AxialMax;   // 沿 Dir 相对 O 的覆盖范围
        public V3 Center;            // 参与面中心的均值
        public double RadialDist;

        // 点到轴线的垂直距离
        public double DistToLine(V3 p)
        {
            var w = p - O;
            var perp = w - Dir * MathUtil.Dot(w, Dir);
            return perp.Length();
        }

        // 点沿轴向是否落在圆柱面覆盖区间内（否则这条轴在该处并不存在）
        public bool CoversAlong(V3 p, double tol)
        {
            double t = MathUtil.Dot(p - O, Dir);
            return t >= AxialMin - tol && t <= AxialMax + tol;
        }
    }

    // 一个可驱动的关节：父实体 ↔ 子实体，绕 Axis 旋转，驱动 Child 的整棵子树
    public class Joint
    {
        public JointAxis Axis;
        public int ParentSolid = -1;
        public int ChildSolid = -1;
        public List<int> DrivenSolids = new List<int>();
        public double AngleDeg;
        public string Name;
    }

    public static class JointRecognizer
    {
        // ---------- 1. 同轴圆柱面聚类 → 候选轴 ----------
        public static List<JointAxis> DetectAxes(StepModel model)
        {
            var faces = model.AllFaces;
            var clusters = new List<MutableCluster>();

            for (int i = 0; i < faces.Count; i++)
            {
                var f = faces[i];
                if (f.S is not CylinderS cy || cy.R < 2.0) continue;

                double tmin = double.MaxValue, tmax = double.MinValue;
                var c = V3.Zero; int n = 0;
                foreach (var lp in f.Loops)
                    foreach (var p in lp)
                    {
                        double t = MathUtil.Dot(p - cy.O, cy.Z);
                        if (t < tmin) tmin = t;
                        if (t > tmax) tmax = t;
                        c += p; n++;
                    }
                if (n < 3) continue;
                c /= n;

                double span = Math.Max(1.0, tmax - tmin);
                double area = 2 * Math.PI * cy.R * span;

                MutableCluster hit = null; double best = 1e9;
                foreach (var cl in clusters)
                {
                    var d = Math.Abs(MathUtil.Dot(cl.Dir, cy.Z));
                    if (d < 0.98) continue;
                    if (!AxisDistanceSmall(cl.O, cl.Dir, cy.O, 3.0)) continue;
                    if (d > best) continue;
                    best = d; hit = cl;
                }

                if (hit == null)
                {
                    clusters.Add(new MutableCluster
                    {
                        O = cy.O, Dir = cy.Z, Radius = cy.R, Score = area,
                        FaceCount = 1, TMin = tmin, TMax = tmax, SumC = c, N = 1
                    });
                }
                else
                {
                    hit.Score += area; hit.FaceCount++;
                    hit.SumC += c; hit.N++;
                    if (cy.R > hit.Radius) hit.Radius = cy.R;
                    // 其它圆柱面的 O 与本簇参考点不同，轴向区间必须换算到同一基准再合并
                    double rmin = double.MaxValue, rmax = double.MinValue;
                    foreach (var lp in f.Loops)
                        foreach (var p in lp)
                        {
                            double t = MathUtil.Dot(p - hit.O, hit.Dir);
                            if (t < rmin) rmin = t;
                            if (t > rmax) rmax = t;
                        }
                    if (rmin < hit.TMin) hit.TMin = rmin;
                    if (rmax > hit.TMax) hit.TMax = rmax;
                }
            }

            var rootPt = RootPoint(model);
            var res = new List<JointAxis>();
            foreach (var cl in clusters)
            {
                var dir = cl.Dir;
                if (Math.Abs(dir.Z) > 0.5) { if (dir.Z < 0) dir = -dir; }
                else if (dir.X < 0) dir = -dir;

                var center = cl.SumC / Math.Max(1, cl.N);
                var radial = center - rootPt;
                radial -= dir * MathUtil.Dot(radial, dir);

                res.Add(new JointAxis
                {
                    O = cl.O, Dir = dir, Radius = cl.Radius, Score = cl.Score,
                    FaceCount = cl.FaceCount, AxialMin = cl.TMin, AxialMax = cl.TMax,
                    Center = center, RadialDist = radial.Length()
                });
            }
            res.Sort((a, b) => b.Score.CompareTo(a.Score));
            return res;
        }

        class MutableCluster
        {
            public V3 O, Dir, SumC;
            public double Radius, Score, TMin, TMax;
            public int FaceCount, N;
        }

        public static V3 RootPoint(StepModel m)
        {
            var mid = (m.BBoxMin + m.BBoxMax) * 0.5f;
            return new V3(mid.X, mid.Y, m.BBoxMin.Z);
        }

        static bool AxisDistanceSmall(V3 o1, V3 d1, V3 o2, double tol)
        {
            var w = o2 - o1;
            var perp = w - d1 * MathUtil.Dot(w, d1);
            return perp.LengthSquared() <= tol * tol;
        }

        // ---------- 2. 实体接触图 → 运动链 ----------
        public class ChainResult
        {
            public List<Joint> Joints = new List<Joint>();
            public List<int> Parent = new List<int>();      // 实体树父节点
            public List<List<int>> JointChainOf = new List<List<int>>();  // 每个实体从根到自身的关节序列
            public int Root = -1;
            public List<string> Report = new List<string>();
        }

        public static ChainResult BuildChain(StepModel model, List<JointAxis> axes, float tol = 2.0f)
        {
            int n = model.Solids.Count;
            var r = new ChainResult();
            if (n == 0) return r;

            var bmin = new V3[n]; var bmax = new V3[n];
            for (int i = 0; i < n; i++)
            {
                var mn = new V3(float.MaxValue); var mx = new V3(float.MinValue);
                foreach (var f in model.Solids[i].Faces)
                    foreach (var l in f.Loops)
                        foreach (var p in l) { mn = V3.Min(mn, p); mx = V3.Max(mx, p); }
                bmin[i] = mn; bmax[i] = mx;
            }

            // 根：最低点所在的实体
            int root = 0; float lowest = float.MaxValue;
            for (int i = 0; i < n; i++) if (bmin[i].Z < lowest) { lowest = bmin[i].Z; root = i; }
            r.Root = root;

            var parent = Enumerable.Repeat(-1, n).ToArray();
            var visited = new bool[n];
            var queue = new Queue<int>();
            visited[root] = true; queue.Enqueue(root);
            var bfsOrder = new List<int> { root };
            while (queue.Count > 0)
            {
                int u = queue.Dequeue();
                for (int v = 0; v < n; v++)
                {
                    if (visited[v]) continue;
                    if (!Touches(bmin[u], bmax[u], bmin[v], bmax[v], tol)) continue;
                    visited[v] = true; parent[v] = u; queue.Enqueue(v); bfsOrder.Add(v);
                }
            }
            // 未连通的实体：挂到 bbox 中心最近的已访问实体
            for (int v = 0; v < n; v++)
            {
                if (visited[v]) continue;
                int best = root; float bd = float.MaxValue;
                var cv = (bmin[v] + bmax[v]) * 0.5f;
                for (int u = 0; u < n; u++)
                {
                    if (!visited[u]) continue;
                    var cu = (bmin[u] + bmax[u]) * 0.5f;
                    float d = (cu - cv).LengthSquared();
                    if (d < bd) { bd = d; best = u; }
                }
                parent[v] = best; visited[v] = true;
                r.Report.Add($"(孤立实体 #{v} 挂到 #{best})");
            }

            // 每条树边挑 1~2 条候选轴：轴必须真实覆盖接触点，且一条轴只归属一个关节
            var edgeJoints = new Dictionary<int, List<Joint>>();  // key = child solid
            var usedAxes = new HashSet<JointAxis>();
            foreach (int child in bfsOrder)
            {
                if (child == root) continue;
                int par = parent[child];
                var list = new List<Joint>();
                edgeJoints[child] = list;

                // 螺栓/垫片等小零件跟随父件，不单独成关节
                if (IsSmallSolid(model.Solids[child])) continue;

                var c = ContactCenter(bmin[par], bmax[par], bmin[child], bmax[child]);
                var cands = axes
                    .Where(a => !usedAxes.Contains(a) && a.Score > 1500 && a.Radius >= 8
                             && a.CoversAlong(c, 25)
                             // 轴长必须与半径相称：环形倒角/过渡带(R120 但轴向只有 4mm)会被聚成伪轴，
                             // 真关节的圆柱面沿轴向都有足够长度(>=0.3R)
                             && a.AxialMax - a.AxialMin >= 0.3 * a.Radius
                             // 轴线必须真正穿过子件，否则这条轴只存在于父件内部，不可能是父子间的铰链
                             && AxisMeetsBox(a, bmin[child], bmax[child]))
                    .Select(a => new { a, d = a.DistToLine(c) })
                    .Where(x => x.d < 40)
                    .OrderBy(x => x.d)
                    .Take(4)
                    .ToList();

                var picked = new List<JointAxis>();
                foreach (var x in cands)
                {
                    if (picked.Count >= 2) break;
                    // 同一条边上两条轴必须方向明显不同（如底座回转 + 肩部俯仰）
                    if (picked.Any(p => Math.Abs(MathUtil.Dot(p.Dir, x.a.Dir)) > 0.9)) continue;
                    picked.Add(x.a);
                }
                // 竖直轴（底座回转）排在同接触区的前面
                foreach (var a in picked.OrderByDescending(a => Math.Abs(a.Dir.Z)))
                {
                    usedAxes.Add(a);
                    list.Add(new Joint { Axis = a, ParentSolid = par, ChildSolid = child });
                }
            }

            // 子树 → DrivenSolids
            var children = new List<int>[n];
            for (int i = 0; i < n; i++) children[i] = new List<int>();
            for (int i = 0; i < n; i++) if (parent[i] >= 0) children[parent[i]].Add(i);
            List<int> Subtree(int v)
            {
                var acc = new List<int>(); var st = new Stack<int>(); st.Push(v);
                while (st.Count > 0) { int u = st.Pop(); acc.Add(u); foreach (var w in children[u]) st.Push(w); }
                return acc;
            }

            // 关节序列编排：BFS 顺序 + 边内顺序
            var jointChainOfSolid = new List<int>[n];
            for (int i = 0; i < n; i++) jointChainOfSolid[i] = new List<int>();
            jointChainOfSolid[root] = new List<int>();

            foreach (int child in bfsOrder)
            {
                if (child == root) continue;
                int par = parent[child];
                var chainOfParent = jointChainOfSolid[par];
                var myChain = new List<int>(chainOfParent);
                foreach (var j in edgeJoints[child])
                {
                    j.DrivenSolids = Subtree(child);
                    myChain.Add(r.Joints.Count);
                    r.Joints.Add(j);
                }
                jointChainOfSolid[child] = myChain;
            }

            r.Parent = parent.ToList();
            for (int i = 0; i < n; i++) r.JointChainOf.Add(jointChainOfSolid[i]);

            for (int k = 0; k < r.Joints.Count; k++)
            {
                var j = r.Joints[k];
                j.Name = "J" + (k + 1);
                r.Report.Add($"{j.Name}: 父#{j.ParentSolid}→子#{j.ChildSolid} 轴心=({j.Axis.O.X:0.#},{j.Axis.O.Y:0.#},{j.Axis.O.Z:0.#}) " +
                    $"方向=({j.Axis.Dir.X:0.##},{j.Axis.Dir.Y:0.##},{j.Axis.Dir.Z:0.##}) R={j.Axis.Radius:0.#} " +
                    $"驱动实体={j.DrivenSolids.Count}");
            }
            return r;
        }

        // 螺栓/垫片/小支架：不单独成为旋转件
        static bool IsSmallSolid(SolidInfo s)
        {
            var mn = new V3(float.MaxValue); var mx = new V3(float.MinValue);
            foreach (var f in s.Faces)
                foreach (var l in f.Loops)
                    foreach (var p in l) { mn = V3.Min(mn, p); mx = V3.Max(mx, p); }
            var sz = mx - mn;
            double maxDim = Math.Max(sz.X, Math.Max(sz.Y, sz.Z));
            return maxDim < 30 || s.Faces.Count < 12;
        }

        // 轴线（含圆柱面覆盖区间）是否真的穿过该实体包围盒——父子件之间的铰链必须穿过子件
        static bool AxisMeetsBox(JointAxis a, V3 mn, V3 mx)
        {
            const float pad = 5f;
            const int n = 16;
            for (int k = 0; k <= n; k++)
            {
                double t = a.AxialMin + (a.AxialMax - a.AxialMin) * k / n;
                var p = a.O + a.Dir * (float)t;
                if (p.X >= mn.X - pad && p.X <= mx.X + pad &&
                    p.Y >= mn.Y - pad && p.Y <= mx.Y + pad &&
                    p.Z >= mn.Z - pad && p.Z <= mx.Z + pad) return true;
            }
            return false;
        }

        static bool Touches(V3 amin, V3 amax, V3 bmin, V3 bmax, float tol)
        {
            return amin.X - tol <= bmax.X && bmin.X - tol <= amax.X
                && amin.Y - tol <= bmax.Y && bmin.Y - tol <= amax.Y
                && amin.Z - tol <= bmax.Z && bmin.Z - tol <= amax.Z;
        }

        static V3 ContactCenter(V3 amin, V3 amax, V3 bmin, V3 bmax)
        {
            var lo = V3.Max(amin, bmin);
            var hi = V3.Min(amax, bmax);
            if (lo.X > hi.X || lo.Y > hi.Y || lo.Z > hi.Z)
                return ((amin + amax) * 0.5f + (bmin + bmax) * 0.5f) * 0.5f;
            return (lo + hi) * 0.5f;
        }
    }
}
