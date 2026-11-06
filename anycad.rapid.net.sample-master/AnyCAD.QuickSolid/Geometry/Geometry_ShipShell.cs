using AnyCAD.Demo;
using AnyCAD.Foundation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnyCAD.QuickSolid.Geometry
{
    internal class Geometry_ShipShell : TestCase
    {
        public Geometry_ShipShell()
        {
            Title = "船体放样";
            Description = "放样横剖曲线缝合船体并生成实体";
            Tags.Add("几何");
            Tags.Add("船体");
            Level = 2;
        }
        public override void Run(IRenderView mRenderControl)
        {

            int pointCount = 20;

            // ========== 1. 获取三根横剖曲线 ==========
            // 船尾曲线 (x=0)
            var sternCurvePoints = new GPntList();
            for (int i = 0; i <= pointCount; i++)
            {
                double angle = Math.PI * i / pointCount;
                double y = 20 * Math.Cos(angle);
                double z = 20 * Math.Sin(angle);
                sternCurvePoints.Add(new GPnt(0, y, z));
            }
            TopoShape sternCurve = CurveBuilder.MakeBSplineByPoints(sternCurvePoints, 3, 3, EnumGeometryContinuity.G2, 0.01);

            // 船中曲线 (x=50)
            var midCurvePoints = new GPntList();
            for (int i = 0; i <= pointCount; i++)
            {
                double angle = Math.PI * i / pointCount;
                double y = 35 * Math.Cos(angle);
                double z = 35 * Math.Sin(angle);
                midCurvePoints.Add(new GPnt(50, y, z));
            }
            TopoShape midCurve = CurveBuilder.MakeBSplineByPoints(midCurvePoints, 3, 3, EnumGeometryContinuity.G2, 0.01);

            // 船首曲线 (x=100)
            var bowCurvePoints = new GPntList();
            for (int i = 0; i <= pointCount; i++)
            {
                double angle = Math.PI * i / pointCount;
                double y = 8 * Math.Cos(angle);
                double z = 8 * Math.Sin(angle);
                bowCurvePoints.Add(new GPnt(100, y, z));
            }
            TopoShape bowCurve = CurveBuilder.MakeBSplineByPoints(bowCurvePoints, 3, 3, EnumGeometryContinuity.G2, 0.01);

            // 显示三条曲线（调试用）
            // mRenderControl.ShowShape(sternCurve, ColorTable.Red);
            // mRenderControl.ShowShape(midCurve, ColorTable.Green);
            // mRenderControl.ShowShape(bowCurve, ColorTable.Blue);

            // ========== 2. 创建三条直线（连接首尾端点） ==========
            // 船尾直线（连接船尾曲线的左右端点）
            GPnt sternLeft = sternCurvePoints[0];           // 左端点 (-20, 0)
            GPnt sternRight = sternCurvePoints[pointCount]; // 右端点 (20, 0)
            TopoShape sternLine = CurveBuilder.MakeLine(sternRight, sternLeft);

            // 船中直线
            GPnt midLeft = midCurvePoints[0];               // 左端点 (-35, 0)
            GPnt midRight = midCurvePoints[pointCount];     // 右端点 (35, 0)
            TopoShape midLine = CurveBuilder.MakeLine(midRight, midLeft);

            // 船首直线
            GPnt bowLeft = bowCurvePoints[0];               // 左端点 (-8, 0)
            GPnt bowRight = bowCurvePoints[pointCount];     // 右端点 (8, 0)
            TopoShape bowLine = CurveBuilder.MakeLine(bowRight, bowLeft);

            // ========== 3. 用曲线放样创建外板面 ==========
            var hullCurves = new TopoShapeList();
            hullCurves.Add(sternCurve);
            hullCurves.Add(midCurve);
            hullCurves.Add(bowCurve);
            TopoShape hullSurface = FeatureTool.Loft(hullCurves, false, false);
            mRenderControl.ShowSceneNode(BrepSceneNode.Create(hullSurface, new Vector3(0.2f, 0.4f, 0.8f), true));
            System.Diagnostics.Debug.WriteLine("外板曲面创建成功");

            // ========== 4. 用直线放样创建甲板面 ==========
            var deckCurves = new TopoShapeList();
            deckCurves.Add(sternLine);
            deckCurves.Add(midLine);
            deckCurves.Add(bowLine);
            TopoShape deckSurface = FeatureTool.Loft(deckCurves, false, false);
            //mRenderControl.ShowShape(deckSurface, mRenderControl.GetMaterial(new Vector3(0.3f, 0.8f, 0.3f), 0.5f), null);

            mRenderControl.ShowSceneNode(BrepSceneNode.Create(deckSurface, new Vector3(0.3f, 0.8f, 0.3f), true));

            System.Diagnostics.Debug.WriteLine("甲板曲面创建成功");

            // ========== 5. 创建首封板面（船首曲线 + 船首直线） ==========
            var bowFaces = new TopoShapeList();
            bowFaces.Add(bowCurve);
            bowFaces.Add(bowLine);
            TopoShape bowSurface = FeatureTool.Loft(bowFaces, false, false);
            mRenderControl.ShowSceneNode(BrepSceneNode.Create(bowSurface, new Vector3(1.0f, 0.6f, 0.2f), true));
            System.Diagnostics.Debug.WriteLine("船首封板创建成功");

            // ========== 6. 创建尾封板面（船尾曲线 + 船尾直线） ==========
            var sternFaces = new TopoShapeList();
            sternFaces.Add(sternCurve);
            sternFaces.Add(sternLine);
            TopoShape sternSurface = FeatureTool.Loft(sternFaces, false, false);
            mRenderControl.ShowSceneNode(BrepSceneNode.Create(sternSurface, new Vector3(0.9f, 0.2f, 0.2f), true));
            System.Diagnostics.Debug.WriteLine("船尾封板创建成功");

            // ========== 7. 收集所有面 ==========
            var allFaces = new TopoShapeList();

            // 获取外板的所有面
            var hullFaces = hullSurface.GetChildren(EnumTopoShapeType.Topo_FACE);
            foreach (var f in hullFaces) allFaces.Add(f);

            // 获取甲板的所有面
            var deckFacesList = deckSurface.GetChildren(EnumTopoShapeType.Topo_FACE);
            foreach (var f in deckFacesList) allFaces.Add(f);

            // 获取封板的面
            var bowFacesList = bowSurface.GetChildren(EnumTopoShapeType.Topo_FACE);
            foreach (var f in bowFacesList) allFaces.Add(f);

            var sternFacesList = sternSurface.GetChildren(EnumTopoShapeType.Topo_FACE);
            foreach (var f in sternFacesList) allFaces.Add(f);

            // ========== 8. 缝合成实体 ==========
           TopoShape shell = ShapeBuilder.MakeShell(allFaces);
           // TopoShape solid = ShapeBuilder.MakeSolid(shell);
           TopoShape solid = AdvShapeBuilder.MakeSolid(allFaces, 0.05);
            if (solid != null)
            {
                TopoShapeProperty prop = new TopoShapeProperty();
                prop.Initialize(solid, false);
                if (true)
                {
                    double volume = prop.SolidVolume();
                    System.Diagnostics.Debug.WriteLine($"实体体积: {volume}");

                    if (volume > 0)
                    {
                        // 隐藏曲面，显示实体
                        mRenderControl.ClearScene();
                        mRenderControl.ShowSceneNode(BrepSceneNode.Create(solid, new Vector3(0.2f, 0.5f, 0.8f), true));
                        System.Diagnostics.Debug.WriteLine("封闭实体创建成功！");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("体积为0，显示各组成面");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("实体存在不正确的内容");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("实体创建失败，显示各组成面");
            }

            mRenderControl.ZoomAll();
        }

    }
}
