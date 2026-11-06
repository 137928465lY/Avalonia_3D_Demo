using AnyCAD.Foundation;
using AnyCAD.Manufacturing;
using System;
using System.Collections.Generic;

namespace AnyCAD.Demo.Graphics
{
    /// <summary>
    /// 平面加工路径填充方式示例
    /// 在一个带圆形岛屿的矩形型腔上，对比展示 7 种平面铣削填充刀路：
    /// 1. ZigZag 行切法 - 基础的双向平行直线切削
    /// 2. Offset 环切法 - 沿轮廓的等距偏置环切削，适合带岛屿的复杂型腔
    /// 3. Spiral 螺旋法 - 连续的螺旋路径，适合圆形或回转体型腔
    /// 4. ZigZagOffset 行切+环切混合 - 结合行切和环切特点的混合策略
    /// 5. Line 单向直线 - 单向的平行直线填充
    /// 6. Grid 网格 - 网格状的填充路径
    /// 7. Triangle 三角形 - 三角形网格的填充路径
    /// </summary>
    class Simulation_PocketFillPatterns : TestCase
    {
        public Simulation_PocketFillPatterns()
        {
            Title = "平面加工路径填充方式";
            Description = "在带岛屿的矩形型腔上对比展示 7 种平面铣削填充刀路：行切法、环切法、螺旋法、行切+环切混合、单向直线、网格、三角形";
            Tags.Add("仿真");
            Tags.Add("铣削");
            Tags.Add("刀路");
            Tags.Add("填充");
            Level = 2;
        }

        public override void Run(IRenderView render)
        {
            // 1. 创建加工区域：带圆形岛屿的矩形型腔
            var outerWire = CreateOuterRectangleWire();
            var islandWire = CreateIslandCircleWire();

            // 2. 定义 7 种填充方式及其显示颜色
            var patterns = new (EnumAreaPocketMode Mode, string Name, Vector3 Color)[]
            {
                (EnumAreaPocketMode.ZigZag,       "1. 行切法 ZigZag",            ColorTable.Blue),
                (EnumAreaPocketMode.Offset,       "2. 环切法 Offset",            ColorTable.Green),
                (EnumAreaPocketMode.Spiral,       "3. 螺旋法 Spiral",            ColorTable.Red),
                (EnumAreaPocketMode.ZigZagOffset, "4. 行切+环切 ZigZagOffset",   ColorTable.Orange),
                (EnumAreaPocketMode.Line,         "5. 单向直线 Line",            ColorTable.Purple),
                (EnumAreaPocketMode.Grid,         "6. 网格 Grid",                ColorTable.Cyan),
                (EnumAreaPocketMode.Triangle,     "7. 三角形 Triangle",          ColorTable.Magenta),
            };

            // 3. 网格布局：4 列 x 2 行
            const int cols = 4;
            const double cellW = 120.0;
            const double cellH = 105.0;
            const double toolRadius = 2.0;
            const double stepover = 4.0;

            var group = new GroupSceneNode();

            for (int i = 0; i < patterns.Length; i++)
            {
                int col = i % cols;
                int row = i / cols;
                double x = (col - (cols - 1) / 2.0) * cellW;
                double y = (row == 0 ? 1.0 : -1.0) * cellH / 2.0;

                var pattern = patterns[i];

                // 3.1 生成该填充方式的刀路
                var segments = GeneratePocketToolpath(pattern.Mode, toolRadius, stepover);

                // 3.2 显示型腔外轮廓（灰色细线）
                var boundaryMat = MeshPhongMaterial.Create($"pocket.boundary.{i}");
                boundaryMat.SetColor(new Vector3(0.6f));
                var boundaryNode = BrepSceneNode.Create(outerWire, boundaryMat, null);
                boundaryNode.SetTransform(Matrix4d.makeTranslation(x, y, 0.0));
                group.AddNode(boundaryNode);

                // 3.3 显示岛屿（深灰色细线）
                var islandMat = MeshPhongMaterial.Create($"pocket.island.{i}");
                islandMat.SetColor(new Vector3(0.25f));
                var islandNode = BrepSceneNode.Create(islandWire, islandMat, null);
                islandNode.SetTransform(Matrix4d.makeTranslation(x, y, 0.05));
                group.AddNode(islandNode);

                // 3.4 显示刀路段（彩色）
                var tpMat = MeshPhongMaterial.Create($"pocket.toolpath.{i}");
                tpMat.SetColor(pattern.Color);
                var segList = new List<TopoShape>();
                foreach (var seg in segments)
                {
                    if (seg != null)
                        segList.Add(seg);
                }
                foreach (var seg in segList)
                {
                    var node = BrepSceneNode.Create(seg, tpMat, null);
                    node.SetTransform(Matrix4d.makeTranslation(x, y, 0.1));
                    group.AddNode(node);
                }

                // 3.5 显示模式名称标签
                var label = TextSceneNode.Create(pattern.Name, pattern.Color, 5.0f, true, 1.0f, 0.0f, 1.0f);
                label.SetTransform(Matrix4d.makeTranslation(x, y + cellH / 2.0 - 12.0, 10.0));
                group.AddNode(label);
            }

            render.ShowSceneNode(group);
            render.ZoomAll();
        }

        // 型腔边界参数
        const double PocketXMin = -40.0;
        const double PocketXMax = 40.0;
        const double PocketYMin = -25.0;
        const double PocketYMax = 25.0;
        const double IslandRadius = 8.0;

        /// <summary>
        /// 创建型腔外轮廓：矩形 80 x 50
        /// </summary>
        private static TopoShape CreateOuterRectangleWire()
        {
            var pts = new GPntList();
            pts.Add(new GPnt(PocketXMin, PocketYMin, 0.0));
            pts.Add(new GPnt(PocketXMax, PocketYMin, 0.0));
            pts.Add(new GPnt(PocketXMax, PocketYMax, 0.0));
            pts.Add(new GPnt(PocketXMin, PocketYMax, 0.0));
            return CurveBuilder.MakePolygonByPoints(pts, true);
        }

        /// <summary>
        /// 创建岛屿轮廓：圆形（半径 8）
        /// </summary>
        private static TopoShape CreateIslandCircleWire()
        {
            var circleEdge = CurveBuilder.MakeCircle(new GPnt(0.0, 0.0, 0.0), IslandRadius, new GDir(0.0, 0.0, 1.0));
            return CurveBuilder.MakeWireByEdge(circleEdge);
        }

        /// <summary>
        /// 生成指定填充方式的刀路段（手动生成，用于可视化对比）
        /// </summary>
        private static List<TopoShape> GeneratePocketToolpath(EnumAreaPocketMode mode, double toolRadius, double stepover)
        {
            var segments = new List<TopoShape>();
            double margin = toolRadius;

            switch (mode)
            {
                case EnumAreaPocketMode.ZigZag:
                    GenerateZigZag(segments, margin, stepover, true);
                    break;
                case EnumAreaPocketMode.Line:
                    GenerateZigZag(segments, margin, stepover, false);
                    break;
                case EnumAreaPocketMode.Offset:
                    GenerateOffset(segments, margin, stepover);
                    break;
                case EnumAreaPocketMode.Spiral:
                    GenerateSpiral(segments, margin, stepover);
                    break;
                case EnumAreaPocketMode.ZigZagOffset:
                    GenerateZigZagOffset(segments, margin, stepover);
                    break;
                case EnumAreaPocketMode.Grid:
                    GenerateGrid(segments, margin, stepover);
                    break;
                case EnumAreaPocketMode.Triangle:
                    GenerateTriangle(segments, margin, stepover);
                    break;
            }

            return segments;
        }

        /// <summary>
        /// 判断点是否在岛屿外（用于避让岛屿）
        /// </summary>
        private static bool IsOutsideIsland(double x, double y, double margin)
        {
            double dx = x;
            double dy = y;
            return Math.Sqrt(dx * dx + dy * dy) > IslandRadius + margin;
        }

        /// <summary>
        /// 添加一条线段到结果集中（自动裁剪岛屿区域）
        /// </summary>
        private static void AddClippedLine(List<TopoShape> segments, GPnt p1, GPnt p2, double margin)
        {
            // 简单处理：如果线段跨越岛屿区域，则分段
            var inside1 = !IsOutsideIsland(p1.x, p1.y, margin);
            var inside2 = !IsOutsideIsland(p2.x, p2.y, margin);

            if (!inside1 && !inside2)
            {
                // 检查线段是否穿过岛屿
                if (LineIntersectsCircle(p1, p2, IslandRadius + margin))
                {
                    // 裁剪为两段
                    ClipLineAgainstCircle(segments, p1, p2, IslandRadius + margin);
                }
                else
                {
                    segments.Add(SketchBuilder.MakeLine(p1, p2));
                }
            }
            else if (!inside1 && inside2)
            {
                var pt = IntersectCircle(p1, p2, IslandRadius + margin);
                if (pt != null)
                    segments.Add(SketchBuilder.MakeLine(p1, pt));
            }
            else if (inside1 && !inside2)
            {
                var pt = IntersectCircle(p1, p2, IslandRadius + margin);
                if (pt != null)
                    segments.Add(SketchBuilder.MakeLine(pt, p2));
            }
        }

        private static bool LineIntersectsCircle(GPnt p1, GPnt p2, double r)
        {
            double dx = p2.x - p1.x;
            double dy = p2.y - p1.y;
            double fx = p1.x;
            double fy = p1.y;
            double a = dx * dx + dy * dy;
            double b = 2 * (fx * dx + fy * dy);
            double c = fx * fx + fy * fy - r * r;
            double disc = b * b - 4 * a * c;
            if (disc < 0) return false;
            disc = Math.Sqrt(disc);
            double t1 = (-b - disc) / (2 * a);
            double t2 = (-b + disc) / (2 * a);
            return (t1 >= 0 && t1 <= 1) || (t2 >= 0 && t2 <= 1);
        }

        private static GPnt IntersectCircle(GPnt p1, GPnt p2, double r)
        {
            double dx = p2.x - p1.x;
            double dy = p2.y - p1.y;
            double fx = p1.x;
            double fy = p1.y;
            double a = dx * dx + dy * dy;
            double b = 2 * (fx * dx + fy * dy);
            double c = fx * fx + fy * fy - r * r;
            double disc = b * b - 4 * a * c;
            if (disc < 0) return null;
            disc = Math.Sqrt(disc);
            double t1 = (-b - disc) / (2 * a);
            double t2 = (-b + disc) / (2 * a);
            double t = (t1 >= 0 && t1 <= 1) ? t1 : (t2 >= 0 && t2 <= 1) ? t2 : -1;
            if (t < 0) return null;
            return new GPnt(p1.x + t * dx, p1.y + t * dy, 0);
        }

        private static void ClipLineAgainstCircle(List<TopoShape> segments, GPnt p1, GPnt p2, double r)
        {
            double dx = p2.x - p1.x;
            double dy = p2.y - p1.y;
            double fx = p1.x;
            double fy = p1.y;
            double a = dx * dx + dy * dy;
            double b = 2 * (fx * dx + fy * dy);
            double c = fx * fx + fy * fy - r * r;
            double disc = b * b - 4 * a * c;
            if (disc < 0) return;
            disc = Math.Sqrt(disc);
            double t1 = (-b - disc) / (2 * a);
            double t2 = (-b + disc) / (2 * a);
            var pt1 = new GPnt(p1.x + t1 * dx, p1.y + t1 * dy, 0);
            var pt2 = new GPnt(p1.x + t2 * dx, p1.y + t2 * dy, 0);
            if (t1 > 0) segments.Add(SketchBuilder.MakeLine(p1, pt1));
            if (t2 < 1) segments.Add(SketchBuilder.MakeLine(pt2, p2));
        }

        /// <summary>
        /// 行切法/单向直线：平行直线填充
        /// </summary>
        private static void GenerateZigZag(List<TopoShape> segments, double margin, double stepover, bool zigzag)
        {
            double xMin = PocketXMin + margin;
            double xMax = PocketXMax - margin;
            double yMin = PocketYMin + margin;
            double yMax = PocketYMax - margin;

            int count = (int)((yMax - yMin) / stepover) + 1;
            for (int i = 0; i < count; i++)
            {
                double y = yMin + i * stepover;
                if (y > yMax) break;

                if (zigzag && i % 2 == 1)
                {
                    AddClippedLine(segments, new GPnt(xMax, y, 0), new GPnt(xMin, y, 0), margin);
                }
                else
                {
                    AddClippedLine(segments, new GPnt(xMin, y, 0), new GPnt(xMax, y, 0), margin);
                }
            }
        }

        /// <summary>
        /// 环切法：同心矩形环
        /// </summary>
        private static void GenerateOffset(List<TopoShape> segments, double margin, double stepover)
        {
            double xMin = PocketXMin + margin;
            double xMax = PocketXMax - margin;
            double yMin = PocketYMin + margin;
            double yMax = PocketYMax - margin;

            int layers = (int)(Math.Min(xMax - xMin, yMax - yMin) / (2 * stepover));
            for (int layer = 0; layer < layers; layer++)
            {
                double inset = layer * stepover;
                double x1 = xMin + inset;
                double x2 = xMax - inset;
                double y1 = yMin + inset;
                double y2 = yMax - inset;

                if (x2 - x1 < stepover || y2 - y1 < stepover) break;

                // 矩形环的四条边
                AddClippedLine(segments, new GPnt(x1, y1, 0), new GPnt(x2, y1, 0), margin);
                AddClippedLine(segments, new GPnt(x2, y1, 0), new GPnt(x2, y2, 0), margin);
                AddClippedLine(segments, new GPnt(x2, y2, 0), new GPnt(x1, y2, 0), margin);
                AddClippedLine(segments, new GPnt(x1, y2, 0), new GPnt(x1, y1, 0), margin);
            }
        }

        /// <summary>
        /// 螺旋法：连续螺旋路径
        /// </summary>
        private static void GenerateSpiral(List<TopoShape> segments, double margin, double stepover)
        {
            double xMin = PocketXMin + margin;
            double xMax = PocketXMax - margin;
            double yMin = PocketYMin + margin;
            double yMax = PocketYMax - margin;

            double left = xMin, right = xMax, bottom = yMin, top = yMax;
            var prev = new GPnt(left, bottom, 0);

            while (left < right && bottom < top)
            {
                // 向右
                var p = new GPnt(right, bottom, 0);
                AddClippedLine(segments, prev, p, margin);
                prev = p;
                bottom += stepover;
                if (bottom > top) break;

                // 向上
                p = new GPnt(right, top, 0);
                AddClippedLine(segments, prev, p, margin);
                prev = p;
                right -= stepover;
                if (left > right) break;

                // 向左
                p = new GPnt(left, top, 0);
                AddClippedLine(segments, prev, p, margin);
                prev = p;
                top -= stepover;
                if (bottom > top) break;

                // 向下
                p = new GPnt(left, bottom, 0);
                AddClippedLine(segments, prev, p, margin);
                prev = p;
                left += stepover;
            }
        }

        /// <summary>
        /// 行切+环切混合：外圈环切，内部行切
        /// </summary>
        private static void GenerateZigZagOffset(List<TopoShape> segments, double margin, double stepover)
        {
            // 外圈 3 层环切
            double xMin = PocketXMin + margin;
            double xMax = PocketXMax - margin;
            double yMin = PocketYMin + margin;
            double yMax = PocketYMax - margin;

            int offsetLayers = 3;
            for (int layer = 0; layer < offsetLayers; layer++)
            {
                double inset = layer * stepover;
                double x1 = xMin + inset;
                double x2 = xMax - inset;
                double y1 = yMin + inset;
                double y2 = yMax - inset;

                if (x2 - x1 < stepover || y2 - y1 < stepover) break;

                AddClippedLine(segments, new GPnt(x1, y1, 0), new GPnt(x2, y1, 0), margin);
                AddClippedLine(segments, new GPnt(x2, y1, 0), new GPnt(x2, y2, 0), margin);
                AddClippedLine(segments, new GPnt(x2, y2, 0), new GPnt(x1, y2, 0), margin);
                AddClippedLine(segments, new GPnt(x1, y2, 0), new GPnt(x1, y1, 0), margin);
            }

            // 内部行切
            double innerInset = offsetLayers * stepover;
            double ix1 = xMin + innerInset;
            double ix2 = xMax - innerInset;
            double iy1 = yMin + innerInset;
            double iy2 = yMax - innerInset;

            int count = (int)((iy2 - iy1) / stepover) + 1;
            for (int i = 0; i < count; i++)
            {
                double y = iy1 + i * stepover;
                if (y > iy2) break;

                if (i % 2 == 1)
                    AddClippedLine(segments, new GPnt(ix2, y, 0), new GPnt(ix1, y, 0), margin);
                else
                    AddClippedLine(segments, new GPnt(ix1, y, 0), new GPnt(ix2, y, 0), margin);
            }
        }

        /// <summary>
        /// 网格：两组垂直的平行直线
        /// </summary>
        private static void GenerateGrid(List<TopoShape> segments, double margin, double stepover)
        {
            double xMin = PocketXMin + margin;
            double xMax = PocketXMax - margin;
            double yMin = PocketYMin + margin;
            double yMax = PocketYMax - margin;

            // 水平线
            int hCount = (int)((yMax - yMin) / stepover) + 1;
            for (int i = 0; i < hCount; i++)
            {
                double y = yMin + i * stepover;
                if (y > yMax) break;
                AddClippedLine(segments, new GPnt(xMin, y, 0), new GPnt(xMax, y, 0), margin);
            }

            // 垂直线
            int vCount = (int)((xMax - xMin) / stepover) + 1;
            for (int i = 0; i < vCount; i++)
            {
                double x = xMin + i * stepover;
                if (x > xMax) break;
                AddClippedLine(segments, new GPnt(x, yMin, 0), new GPnt(x, yMax, 0), margin);
            }
        }

        /// <summary>
        /// 三角形网格：三组方向（0°, 60°, 120°）的平行直线
        /// </summary>
        private static void GenerateTriangle(List<TopoShape> segments, double margin, double stepover)
        {
            double xMin = PocketXMin + margin;
            double xMax = PocketXMax - margin;
            double yMin = PocketYMin + margin;
            double yMax = PocketYMax - margin;

            // 方向 1：水平线 (0°)
            int hCount = (int)((yMax - yMin) / stepover) + 1;
            for (int i = 0; i < hCount; i++)
            {
                double y = yMin + i * stepover;
                if (y > yMax) break;
                AddClippedLine(segments, new GPnt(xMin, y, 0), new GPnt(xMax, y, 0), margin);
            }

            // 方向 2：60° 斜线
            double diag = Math.Sqrt((xMax - xMin) * (xMax - xMin) + (yMax - yMin) * (yMax - yMin));
            int dCount = (int)(diag / stepover) + 1;
            double offset60 = stepover * Math.Sin(Math.PI / 3);
            for (int i = -dCount; i < dCount; i++)
            {
                double intercept = yMin + i * offset60;
                // y = tan(60°)*x + intercept => y = sqrt(3)*x + intercept
                double slope = Math.Tan(Math.PI / 3);
                var pts = IntersectLineWithRect(slope, intercept, xMin, xMax, yMin, yMax);
                if (pts.Count == 2)
                    AddClippedLine(segments, pts[0], pts[1], margin);
            }

            // 方向 3：120° 斜线
            double offset120 = stepover * Math.Sin(Math.PI / 3);
            for (int i = -dCount; i < dCount; i++)
            {
                double intercept = yMin + i * offset120;
                double slope = Math.Tan(2 * Math.PI / 3); // -sqrt(3)
                var pts = IntersectLineWithRect(slope, intercept, xMin, xMax, yMin, yMax);
                if (pts.Count == 2)
                    AddClippedLine(segments, pts[0], pts[1], margin);
            }
        }

        /// <summary>
        /// 计算直线 y = slope*x + intercept 与矩形的交点
        /// </summary>
        private static List<GPnt> IntersectLineWithRect(double slope, double intercept, double xMin, double xMax, double yMin, double yMax)
        {
            var pts = new List<GPnt>();
            // 左边 x = xMin
            double y = slope * xMin + intercept;
            if (y >= yMin && y <= yMax) pts.Add(new GPnt(xMin, y, 0));
            // 右边 x = xMax
            y = slope * xMax + intercept;
            if (y >= yMin && y <= yMax) pts.Add(new GPnt(xMax, y, 0));
            // 下边 y = yMin
            if (Math.Abs(slope) > 1e-10)
            {
                double x = (yMin - intercept) / slope;
                if (x >= xMin && x <= xMax) pts.Add(new GPnt(x, yMin, 0));
            }
            // 上边 y = yMax
            if (Math.Abs(slope) > 1e-10)
            {
                double x = (yMax - intercept) / slope;
                if (x >= xMin && x <= xMax) pts.Add(new GPnt(x, yMax, 0));
            }
            return pts;
        }
    }
}
