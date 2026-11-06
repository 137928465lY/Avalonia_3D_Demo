using System;
using System.Collections.Generic;
using OpenStepViewer.Step;
using V3 = System.Numerics.Vector3;

namespace OpenStepViewer.Geom
{
    public static class MathUtil
    {
        public static V3 V(Arg a, StepFile f)
        {
            if (a == null) return default;
            Arg pa = a;
            if (a.Kind == ArgKind.Ref) pa = f.Get(a.Ref)?.Args;
            if (pa == null || pa.Kind != ArgKind.Typed) return default;
            if (pa.Type == "VECTOR")
            {
                var d = Normalized(V(pa[1], f));
                float mag = pa.Count >= 3 ? (float)pa[2].Num : 1f;
                return d * mag;
            }
            if (pa.Type == "DIRECTION" || pa.Type == "CARTESIAN_POINT")
            {
                var list = pa.Count >= 2 ? pa[1] : pa[0];
                if (list == null || list.Kind != ArgKind.List || list.Count < 3) return default;
                return new V3((float)list[0].Num, (float)list[1].Num, (float)list[2].Num);
            }
            return default;
        }

        public static float Dot(V3 a, V3 b) => Vector3Ext.Dot(a, b);
        public static V3 Cross(V3 a, V3 b) => Vector3Ext.Cross(a, b);
        public static V3 Normalized(V3 a) { float l = a.Length(); return l > 1e-12f ? a / l : new V3(0, 0, 1); }

        public static V3[] Frame(Arg axis, StepFile f)
        {
            // AXIS2_PLACEMENT_3D(name, location, axis(z), refDirection(x))
            var a = axis.Kind == ArgKind.Ref ? f.Get(axis.Ref)?.Args : axis;
            var loc = V(a[1], f);
            var z = a.Count < 3 || a[2].Kind == ArgKind.Null ? new V3(0, 0, 1) : Normalized(V(a[2], f));
            var xref = a.Count < 4 || a[3].Kind == ArgKind.Null ? new V3(1, 0, 0) : V(a[3], f);
            // x = ref - (ref·z)z；退化时任取
            xref -= z * Dot(xref, z);
            if (xref.LengthSquared() < 1e-10f)
                xref = Math.Abs(z.X) < 0.9f ? V3.UnitX - z * z.X : V3.UnitY - z * z.Y;
            xref = Normalized(xref);
            var y = Cross(z, xref);
            return new[] { loc, xref, y, z };
        }
    }

    public static class Vector3Ext
    {
        public static float Dot(this V3 a, V3 b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;
        public static V3 Cross(this V3 a, V3 b) => new V3(a.Y * b.Z - a.Z * b.Y, a.Z * b.X - a.X * b.Z, a.X * b.Y - a.Y * b.X);
    }

    // ---------- 曲线 ----------
    public abstract class Curve3
    {
        public abstract V3 Eval(double t);
        public virtual double ParamFromPoint(V3 p, double seed) => seed;
    }

    public class Line3 : Curve3
    {
        public V3 P, D;
        public override V3 Eval(double t) => P + D * (float)t;
        public override double ParamFromPoint(V3 p, double seed) => MathUtil.Dot(p - P, D);
    }

    public class Circle3 : Curve3
    {
        public V3 O, X, Y, Z; public double R;
        public override V3 Eval(double t) => O + (X * (float)(Math.Cos(t) * R)) + (Y * (float)(Math.Sin(t) * R));
        public override double ParamFromPoint(V3 p, double seed) => Math.Atan2(MathUtil.Dot(p - O, Y), MathUtil.Dot(p - O, X));
    }

    public class Ellipse3 : Curve3
    {
        public V3 O, X, Y; public double R1, R2;
        public override V3 Eval(double t) => O + (X * (float)(Math.Cos(t) * R1)) + (Y * (float)(Math.Sin(t) * R2));
        public override double ParamFromPoint(V3 p, double seed) => Math.Atan2(MathUtil.Dot(p - O, Y) / R2, MathUtil.Dot(p - O, X) / R1);
    }

    public class BSplineCurve3 : Curve3
    {
        public int Degree;
        public V3[] CP;          // 控制点
        public float[] W;        // 权重（非有理为 1）
        public double[] Knots;   // 展开后的节点向量
        public double TMin => Knots[Degree];
        public double TMax => Knots[Knots.Length - Degree - 1];

        public override V3 Eval(double t)
        {
            int n = CP.Length;
            int span = FindSpan(t);
            var N = Basis(span, t, Degree, Knots);
            float wsum = 0;
            var p = V3.Zero;
            for (int i = 0; i <= Degree; i++)
            {
                int idx = span - Degree + i;
                if (idx < 0 || idx >= n) continue;
                float w = W == null ? 1f : W[idx];
                float b = N[i] * w;
                p += CP[idx] * b;
                wsum += b;
            }
            // wsum 退化（基函数全 0，通常由非法参数或节点不匹配引起）时返回最近控制点，
            // 绝不能返回 p=(0,0,0)：那会让环采样点落到世界原点，生成横跨整个模型的刀刃面
            return wsum > 1e-12f ? p / wsum : CP[Math.Clamp(span, 0, n - 1)];
        }

        // The NURBS Book 算法 A2.1：保证 Knots[span] <= t < Knots[span+1]
        // 旧实现上界用 n-1，遇到多重内部节点（如 mult=(4,2,4)）会把 t 归到前一段，
        // 造成 Basis 里 dnm=0、基函数全 0、Eval 返回原点
        public int FindSpan(double t)
        {
            int n = CP.Length - 1;
            if (t >= Knots[Math.Min(n + 1, Knots.Length - 1)]) return n;
            if (t <= Knots[Degree]) return Degree;
            int lo = Degree, hi = n + 1;
            int mid = (lo + hi) >> 1;
            while (t < Knots[mid] || t >= Knots[mid + 1])
            {
                if (t < Knots[mid]) hi = mid; else lo = mid;
                mid = (lo + hi) >> 1;
            }
            return mid;
        }

        public static float[] Basis(int span, double t, int p, double[] K)
        {
            var N = new float[p + 1];
            var left = new double[p + 1];
            var right = new double[p + 1];
            N[0] = 1f;
            for (int j = 1; j <= p; j++)
            {
                left[j] = t - K[span + 1 - j];
                right[j] = K[span + j] - t;
                float saved = 0;
                for (int r = 0; r < j; r++)
                {
                    // 重复节点处分母为 0（K[span+j]==K[span+1-j]），约定基函数项为 0，
                    // 否则 0/0=NaN 会经 Eval/Deriv 传播到网格化（nu=(int)NaN 溢出）
                    double dnm = right[r + 1] + left[j - r];
                    float temp = dnm != 0 ? N[r] / (float)dnm : 0f;
                    N[r] = saved + temp * (float)right[r + 1];
                    saved = temp * (float)left[j - r];
                }
                N[j] = saved;
            }
            return N;
        }

        public override double ParamFromPoint(V3 p, double seed)
        {
            // 粗采样 + 局部细化
            int n = 64; double best = TMin, bd = float.MaxValue;
            for (int i = 0; i <= n; i++)
            {
                double t = TMin + (TMax - TMin) * i / n;
                double d = (Eval(t) - p).LengthSquared();
                if (d < bd) { bd = d; best = t; }
            }
            double step = (TMax - TMin) / n;
            for (int k = 0; k < 3; k++)
            {
                double t0 = Math.Max(TMin, best - step), t1 = Math.Min(TMax, best + step);
                for (int i = 0; i <= 20; i++)
                {
                    double t = t0 + (t1 - t0) * i / 20;
                    double d = (Eval(t) - p).LengthSquared();
                    if (d < bd) { bd = d; best = t; }
                }
                step /= 8;
            }
            return best;
        }
    }

    public class TrimmedCurve3 : Curve3
    {
        public Curve3 Basis;
        public double T1, T2;
        public bool Sense = true;
        // 注意：sense=false 表示从 T1 沿参数减小方向走到 T2（采样时 T2<T1 自然递减），
        // 不能用 -t 翻转采样参数——那会把采样点映射到曲线的负参数区间，环点全部错位。
        public override V3 Eval(double t) => Basis.Eval(t);
        public override double ParamFromPoint(V3 p, double seed) => Basis.ParamFromPoint(p, seed);
    }

    // ---------- 曲面 ----------
    public abstract class Surface
    {
        public abstract V3 Eval(double u, double v);
        public abstract V3 Normal(double u, double v);       // 未按 same_sense 翻转
        public abstract void UVRange(out double umin, out double umax, out double vmin, out double vmax);
        public virtual bool UWrapped => false;               // u 是否为周期 2π
        public abstract V2 Inverse(V3 p);                    // 世界点 → UV
        public double UPeriod => Math.PI * 2;

        public (V3 Su, V3 Sv) Deriv(double u, double v)
        {
            // 差分步长必须跟随参数域尺度：固定 1e-4 在 UV 跨度 ~800 的曲面上，
            // 位移量只有 ~1e-4 mm，被 float 坐标精度（~1e-5 mm）噪声淹没；
            // 在 UV 跨度 ~1e-3 的曲面上又会跨越跨度的 10%，两者都会让法线抖动（暗斑/条纹）
            UVRange(out double u0, out double u1, out double v0, out double v1);
            float hu = (float)DiffStep(u, u0, u1), hv = (float)DiffStep(v, v0, v1);
            var a = Eval(u - hu, v); var b = Eval(u + hu, v);
            var c = Eval(u, v - hv); var d = Eval(u, v + hv);
            return ((b - a) / (2 * hu), (d - c) / (2 * hv));
        }

        static double DiffStep(double t, double lo, double hi)
        {
            if (!double.IsInfinity(lo) && !double.IsInfinity(hi) && hi > lo)
                return Math.Max((hi - lo) * 1e-3, 1e-9);
            return Math.Max(1e-3 * Math.Max(1.0, Math.Abs(t)), 1e-9);
        }

        // 供 NURBS 等使用的数值反求
        protected V2 InverseNumeric(V3 p, double uGuess, double vGuess, double umin, double umax, double vmin, double vmax)
        {
            // NaN 比较恒为 false 的陷阱：det=NaN 时不 break、nd=NaN 时不拒绝，
            // 会把 NaN 步长接受进 u/v 并传播到返回值。所有比较用"拒绝非法值"写法。
            double u = uGuess, v = vGuess;
            double bd = (Eval(u, v) - p).LengthSquared();
            if (double.IsNaN(bd)) bd = double.MaxValue;
            // 粗网格搜索
            int N = 24;
            for (int i = 0; i <= N; i++)
                for (int j = 0; j <= N; j++)
                {
                    double uu = umin + (umax - umin) * i / N;
                    double vv = vmin + (vmax - vmin) * j / N;
                    double d = (Eval(uu, vv) - p).LengthSquared();
                    if (d < bd) { bd = d; u = uu; v = vv; }
                }
            // Newton 细化（有限差分）
            for (int k = 0; k < 6; k++)
            {
                var e = Eval(u, v) - p;
                var (su, sv) = Deriv(u, v);
                float a11 = MathUtil.Dot(su, su), a12 = MathUtil.Dot(su, sv), a22 = MathUtil.Dot(sv, sv);
                float b1 = -MathUtil.Dot(su, e), b2 = -MathUtil.Dot(sv, e);
                float det = a11 * a22 - a12 * a12;
                if (!(Math.Abs(det) >= 1e-20f)) break;   // det 为 NaN 或过小时终止
                double du = (b1 * a22 - b2 * a12) / det;
                double dv = (a11 * b2 - a12 * b1) / det;
                double nu = Clamp(u + du, umin, umax), nv = Clamp(v + dv, vmin, vmax);
                if (double.IsNaN(nu) || double.IsNaN(nv)) break;
                double nd = (Eval(nu, nv) - p).LengthSquared();
                if (double.IsNaN(nd) || nd >= bd) break;
                u = nu; v = nv;
                bd = nd;
            }
            if (double.IsNaN(u) || double.IsNaN(v)) return new V2(umin, vmin);
            return new V2((float)u, (float)v);
        }

        protected static double Clamp(double x, double a, double b) => x < a ? a : (x > b ? b : x);
    }

    public struct V2 { public double U, V; public V2(double u, double v) { U = u; V = v; } }

    public class PlaneS : Surface
    {
        public V3 O, X, Y, Z;
        public override V3 Eval(double u, double v) => O + X * (float)u + Y * (float)v;
        public override V3 Normal(double u, double v) => Z;
        public override void UVRange(out double a, out double b, out double c, out double d) { a = b = c = d = 0; } // 平面由裁剪环决定
        public override bool UWrapped => false;
        public override V2 Inverse(V3 p) => new V2(MathUtil.Dot(p - O, X), MathUtil.Dot(p - O, Y));
    }

    public class CylinderS : Surface
    {
        public V3 O, X, Y, Z; public double R;
        public override V3 Eval(double u, double v) => O + (X * (float)(Math.Cos(u) * R)) + (Y * (float)(Math.Sin(u) * R)) + Z * (float)v;
        public override V3 Normal(double u, double v) => X * (float)Math.Cos(u) + Y * (float)Math.Sin(u);
        public override bool UWrapped => true;
        public override void UVRange(out double a, out double b, out double c, out double d) { a = 0; b = Math.PI * 2; c = double.NegativeInfinity; d = double.PositiveInfinity; }
        public override V2 Inverse(V3 p)
        {
            var d = p - O;
            return new V2(Math.Atan2(MathUtil.Dot(d, Y), MathUtil.Dot(d, X)), MathUtil.Dot(d, Z));
        }
    }

    public class ConeS : Surface
    {
        public V3 O, X, Y, Z; public double R0, TanA;
        // r(v) = R0 + v * TanA
        public override V3 Eval(double u, double v)
        {
            double r = R0 + v * TanA;
            return O + (X * (float)(Math.Cos(u) * r)) + (Y * (float)(Math.Sin(u) * r)) + Z * (float)v;
        }
        public override V3 Normal(double u, double v)
        {
            var radial = X * (float)Math.Cos(u) + Y * (float)Math.Sin(u);
            return MathUtil.Normalized(radial - Z * (float)TanA);
        }
        public override bool UWrapped => true;
        public override void UVRange(out double a, out double b, out double c, out double d) { a = 0; b = Math.PI * 2; c = double.NegativeInfinity; d = double.PositiveInfinity; }
        public override V2 Inverse(V3 p)
        {
            var d = p - O;
            double u = Math.Atan2(MathUtil.Dot(d, Y), MathUtil.Dot(d, X));
            double v = MathUtil.Dot(d, Z);
            return new V2(u, v);
        }
    }

    public class TorusS : Surface
    {
        public V3 O, X, Y, Z; public double Major, Minor;
        public override V3 Eval(double u, double v)
        {
            double rr = Major + Minor * Math.Cos(v);
            return O + (X * (float)(Math.Cos(u) * rr)) + (Y * (float)(Math.Sin(u) * rr)) + Z * (float)(Minor * Math.Sin(v));
        }
        public override V3 Normal(double u, double v)
        {
            return MathUtil.Normalized((X * (float)Math.Cos(u) + Y * (float)Math.Sin(u)) * (float)Math.Cos(v) + Z * (float)Math.Sin(v));
        }
        public override bool UWrapped => true;
        public override void UVRange(out double a, out double b, out double c, out double d) { a = 0; b = Math.PI * 2; c = 0; d = Math.PI * 2; }
        public override V2 Inverse(V3 p)
        {
            var d = p - O;
            double u = Math.Atan2(MathUtil.Dot(d, Y), MathUtil.Dot(d, X));
            double rad = Math.Sqrt(d.X * d.X + d.Y * d.Y) - Major;
            double v = Math.Atan2(d.Z, rad);
            return new V2(u, v);
        }
    }

    public class SphereS : Surface
    {
        public V3 O, X, Y, Z; public double R;
        public override V3 Eval(double u, double v)
        {
            double sv = Math.Sin(v);
            return O + (X * (float)(Math.Cos(u) * R * sv)) + (Y * (float)(Math.Sin(u) * R * sv)) + Z * (float)(R * Math.Cos(v));
        }
        public override V3 Normal(double u, double v)
        {
            var d = Eval(u, v) - O;
            return d / (float)R;
        }
        public override bool UWrapped => true;
        public override void UVRange(out double a, out double b, out double c, out double d) { a = 0; b = Math.PI * 2; c = 0; d = Math.PI; }
        public override V2 Inverse(V3 p)
        {
            var d = p - O;
            return new V2(Math.Atan2(MathUtil.Dot(d, Y), MathUtil.Dot(d, X)), Math.Acos(MathUtil.Dot(d, Z) / R));
        }
    }

    public class BSplineS : Surface
    {
        public int Pu, Pv;
        public V3[][] CP;      // [i][j] 控制点网格 (u 方向 i, v 方向 j)
        public float[][] W;
        public double[] Ku, Kv; // 展开节点
        double _umin, _umax, _vmin, _vmax;

        public BSplineS(int pu, int pv, V3[][] cp, float[][] w, double[] ku, double[] kv)
        {
            Pu = pu; Pv = pv; CP = cp; W = w; Ku = ku; Kv = kv;
            _umin = Ku[pu]; _umax = Ku[Ku.Length - pu - 1];
            _vmin = Kv[pv]; _vmax = Kv[Kv.Length - pv - 1];
        }

        public override void UVRange(out double a, out double b, out double c, out double d) { a = _umin; b = _umax; c = _vmin; d = _vmax; }

        public override V3 Eval(double u, double v)
        {
            u = Clamp(u, _umin, _umax); v = Clamp(v, _vmin, _vmax);
            int iu = FindSpan(Ku, CP.Length, Pu, u), iv = FindSpan(Kv, CP[0].Length, Pv, v);
            var bu = BSplineCurve3.Basis(iu, u, Pu, Ku);
            var bv = BSplineCurve3.Basis(iv, v, Pv, Kv);
            float ws = 0; var p = V3.Zero;
            for (int i = 0; i <= Pu; i++)
            {
                int ci = iu - Pu + i;
                if (ci < 0 || ci >= CP.Length) continue;
                for (int j = 0; j <= Pv; j++)
                {
                    int cj = iv - Pv + j;
                    if (cj < 0 || cj >= CP[ci].Length) continue;
                    float w = W?[ci][cj] ?? 1f;
                    float b = bu[i] * bv[j] * w;
                    p += CP[ci][cj] * b;
                    ws += b;
                }
            }
            if (ws > 1e-12f) return p / ws;
            // 基函数全 0 时返回最近控制点，避免采样点落到世界原点（会产生横跨模型的翼片）
            int fi = Math.Clamp(iu - Pu, 0, CP.Length - 1);
            int fj = Math.Clamp(iv - Pv, 0, CP[fi].Length - 1);
            return CP[fi][fj];
        }

        public override V3 Normal(double u, double v)
        {
            var (su, sv) = Deriv(u, v);
            var n = MathUtil.Cross(su, sv);
            float l = n.Length();
            return l > 1e-10f ? n / l : new V3(0, 0, 1);
        }

        public override bool UWrapped => false;
        public override V2 Inverse(V3 p) => InverseNumeric(p, (_umin + _umax) / 2, (_vmin + _vmax) / 2, _umin, _umax, _vmin, _vmax);

        // The NURBS Book A2.1（n = 该方向控制点数），保证 K[span] <= t < K[span+1]
        static int FindSpan(double[] K, int n, int p, double t)
        {
            if (t >= K[Math.Min(n, K.Length - 1)]) return n - 1;
            if (t <= K[p]) return p;
            int lo = p, hi = n;
            int mid = (lo + hi) >> 1;
            while (t < K[mid] || t >= K[mid + 1])
            {
                if (t < K[mid]) hi = mid; else lo = mid;
                mid = (lo + hi) >> 1;
            }
            return mid;
        }
    }

    public class ExtrusionS : Surface
    {
        public Curve3 C;
        public V3 D;
        public override V3 Eval(double u, double v) => C.Eval(u) + D * (float)v;
        public override V3 Normal(double u, double v)
        {
            float h = 1e-4f;
            var su = (C.Eval(u + h) - C.Eval(u - h)) / (2 * h);
            return MathUtil.Normalized(MathUtil.Cross(su, D));
        }
        public override void UVRange(out double a, out double b, out double c, out double d)
        {
            a = b = 0; c = double.NegativeInfinity; d = double.PositiveInfinity;
            if (C is BSplineCurve3 b3) { a = b3.TMin; b = b3.TMax; }
        }
        public override V2 Inverse(V3 p)
        {
            // 沿曲线采样找 u（最小化到直线族 C(u)+tD 的距离），再算 v
            double best = 0, bd = double.MaxValue;
            int N = 128;
            double tmin = 0, tmax = 100;
            if (C is Line3 ln) { tmin = -1e4; tmax = 1e4; }
            else if (C is Circle3 ci) { tmin = 0; tmax = Math.PI * 2; }
            else if (C is BSplineCurve3 b3) { tmin = b3.TMin; tmax = b3.TMax; }
            for (int i = 0; i <= N; i++)
            {
                double t = tmin + (tmax - tmin) * i / N;
                var q = C.Eval(t);
                var w = p - q;
                var tangential = D * MathUtil.Dot(w, D);
                double d = (w - tangential).LengthSquared();
                if (d < bd) { bd = d; best = t; }
            }
            double step = (tmax - tmin) / N;
            for (int k = 0; k < 4; k++)
            {
                double t0 = Math.Max(tmin, best - step), t1 = Math.Min(tmax, best + step);
                for (int i = 0; i <= 20; i++)
                {
                    double t = t0 + (t1 - t0) * i / 20;
                    var q = C.Eval(t);
                    var w = p - q;
                    var tangential = D * MathUtil.Dot(w, D);
                    double d = (w - tangential).LengthSquared();
                    if (d < bd) { bd = d; best = t; }
                }
                step /= 8;
            }
            double v = MathUtil.Dot(p - C.Eval(best), D);
            return new V2(best, v);
        }
    }
}
