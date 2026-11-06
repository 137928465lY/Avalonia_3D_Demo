using AnyCAD.Foundation;
using AnyCAD.Manufacturing;
using System;
using System.Collections.Generic;

namespace AnyCAD.Demo.Graphics
{
    /// <summary>
    /// 纯平面轮廓输入的挖槽刀路示例（真内核版）
    /// 与 Simulation_PocketFillPatterns（手绘刀路）的区别：这里不手工画线，
    /// 而是用 CurveBuilder/SurfaceBuilder 从"直线+圆弧"的平面轮廓直接构造带孔平面面片，
    /// 喂给内核 Pocket2D 工序，由 EnumAreaPocketMode 的 7 种填充方式真实生成刀路。
    ///
    /// 加工区域：80x50 矩形外轮廓 + 中心 r8 圆形岛屿（孔）。
    /// 逐模式 Execute 后取 GetToolPathShape() 展示，4 列 x 2 行并排、按方式着色。
    ///
    /// 已知内核缺陷：Spiral（libarea CurveTree 螺旋）在矩形腔/大尺寸腔上递归偏置树
    /// 提前终止，只出边界两圈 + 中心线，中间留大块漏切（与 stepover 跳变相关，
    /// 只有"圆腔 r20+step4"这类小深宽比场景能出完整螺旋）。带岛屿型腔请优先用
    /// Offset 环切。此条目保留 Spiral 仅作缺陷对照展示。
    /// </summary>
    class Simulation_PocketFillPatternsKernel : TestCase
    {
        public Simulation_PocketFillPatternsKernel()
        {
            Title = "平面轮廓挖槽刀路(内核Pocket2D)";
            Description = "用纯平面轮廓（矩形外环 + r8 圆孔）直接构造面片喂给 Pocket2D，对比 7 种 EnumAreaPocketMode 填充刀路的真实内核输出";
            Tags.Add("仿真");
            Tags.Add("铣削");
            Tags.Add("刀路");
            Tags.Add("填充");
            Level = 2;
        }

        // 型腔边界参数（与手绘版保持一致，便于对照）
        const double PocketXMin = -40.0;
        const double PocketXMax = 40.0;
        const double PocketYMin = -25.0;
        const double PocketYMax = 25.0;
        const double IslandRadius = 8.0;
        const double ToolRadius = 3.0;
        const double Stepover = 4.0;

        public override void Run(IRenderView render)
        {
            // 1. 纯平面轮廓 → 带孔平面面片（不经三维零件布尔）
            var regionFace = CreatePocketRegionFace();
            if (regionFace == null)
                return;

            // 2. 七种填充方式与显示颜色
            var patterns = new (EnumAreaPocketMode Mode, string Name, Vector3 Color)[]
            {
                (EnumAreaPocketMode.ZigZag,       "1. ZigZag",        ColorTable.Blue),
                (EnumAreaPocketMode.Offset,       "2. Offset",        ColorTable.Green),
                (EnumAreaPocketMode.Spiral,       "3. Spiral(缺陷)",  ColorTable.Red),
                (EnumAreaPocketMode.ZigZagOffset, "4. ZigZagOffset",  ColorTable.Orange),
                (EnumAreaPocketMode.Line,         "5. Line",          ColorTable.Purple),
                (EnumAreaPocketMode.Grid,         "6. Grid",          ColorTable.Cyan),
                (EnumAreaPocketMode.Triangle,     "7. Triangle",      ColorTable.Magenta),
            };

            // 3. 网格布局：4 列 x 2 行
            const int cols = 4;
            const double cellW = 110.0;
            const double cellH = 95.0;

            var group = new GroupSceneNode();

            for (int i = 0; i < patterns.Length; i++)
            {
                int col = i % cols;
                int row = i / cols;
                double x = (col - (cols - 1) / 2.0) * cellW;
                double y = (row == 0 ? 1.0 : -1.0) * cellH / 2.0;
                var pattern = patterns[i];

                // 3.1 该填充方式的内核刀路
                var toolpath = GenerateKernelToolpath(pattern.Mode, regionFace);
                if (toolpath == null)
                    return;

                // 3.2 区域轮廓（灰色）
                var boundaryMat = MeshPhongMaterial.Create(string.Format("pocket2d.boundary.{0}", i));
                boundaryMat.SetColor(new Vector3(0.6f));
                var boundaryNode = BrepSceneNode.Create(regionFace, boundaryMat, null);
                boundaryNode.SetTransform(Matrix4d.makeTranslation(x, y, 0.0));
                boundaryNode.SetEdgeMaterialByAll(boundaryMat);
                group.AddNode(boundaryNode);

                // 3.3 刀路（彩色）：刀路在 z=-1，节点抬到面片上方显示，避免被面片遮挡
                var tpMat = MeshPhongMaterial.Create(string.Format("pocket2d.path.{0}", i));
                tpMat.SetColor(pattern.Color);
                var tpNode = BrepSceneNode.Create(toolpath, tpMat, null);
                tpNode.SetTransform(Matrix4d.makeTranslation(x, y, 1.5));
                tpNode.SetEdgeMaterialByAll(tpMat);
                var edgeCount = tpNode.GetEdgeCount();
                for (uint e = 0; e < edgeCount; ++e)
                {
                    tpNode.SetEdgeColor(e, pattern.Color);
                }
                group.AddNode(tpNode);

                // 3.4 模式标签
                var label = TextSceneNode.Create(pattern.Name, pattern.Color, 5.0f, true, 1.0f, 0.0f, 1.0f);
                label.SetTransform(Matrix4d.makeTranslation(x, y + 18.0, 1.0));
                group.AddNode(label);
            }

            render.ShowSceneNode(group);
            render.SetStandardView(EnumStandardView.FacePz, false);
            render.ZoomAll();
        }

        /// <summary>
        /// 用纯平面轮廓构造加工区域：矩形外环 + r8 圆孔 → 带孔平面面片
        /// </summary>
        private static TopoShape CreatePocketRegionFace()
        {
            // 外环：矩形 80 x 50（直线边）
            var pts = new GPntList();
            pts.Add(new GPnt(PocketXMin, PocketYMin, 0.0));
            pts.Add(new GPnt(PocketXMax, PocketYMin, 0.0));
            pts.Add(new GPnt(PocketXMax, PocketYMax, 0.0));
            pts.Add(new GPnt(PocketXMin, PocketYMax, 0.0));
            var outerWire = CurveBuilder.MakePolygonByPoints(pts, true);

            // 内环：圆形岛屿（圆弧边）
            var circleEdge = CurveBuilder.MakeCircle(new GPnt(0.0, 0.0, 0.0), IslandRadius, new GDir(0.0, 0.0, 1.0));
            var islandWire = CurveBuilder.MakeWireByEdge(circleEdge);

            // 外环成面，再挖孔 → 带圆孔的平面面片
            var outerFace = CurveBuilder.MakePlanarFace(outerWire);
            if (outerFace == null)
                return null;
            return SurfaceBuilder.AddHole(outerFace, islandWire);
        }

        /// <summary>
        /// 用内核 Pocket2D 生成指定填充方式的刀路形状
        /// </summary>
        private static TopoShape GenerateKernelToolpath(EnumAreaPocketMode mode, TopoShape regionFace)
        {
            var pocket = new Pocket2D();
            pocket.SetShape(regionFace);
            pocket.SetBaseGeometryList(new TopoShapeList { regionFace });
            pocket.SetToolHorizFeed(500.0);
            pocket.SetToolVertFeed(300.0);

            // 区域运算参数（引用访问器，直接改内核参数）
            var pars = pocket.GetParamsRef();
            pars.SetFill(EnumAreaFill.Empty);
            pars.SetCoplanar(EnumAreaCoplanar.Empty);
            pars.SetSectionCount(-1);
            pars.GetPocket().SetAngle(0.0);
            pars.GetPocket().SetPocketMode(mode);
            pars.GetPocket().SetToolRadius(ToolRadius);
            pars.GetPocket().SetStepover(Stepover);

            // 深度参数：必须有切削余量（StartDepth==FinalDepth 时内核不生成刀路），
            // 平面刀路单层：从 0 切到 -1，Clearance/Safe 抬刀用
            var dp = pocket.GetDepthParams();
            dp.SetClearanceHeight(5.0);
            dp.SetSafeHeight(3.0);
            dp.SetStartDepth(0.0);
            dp.SetStepDown(1.0);
            dp.SetFinalDepth(-1.0);
            pocket.SetDepthParams(dp);

            if (!pocket.Execute())
                return null;
            return pocket.GetToolPathShape();
        }
    }
}
