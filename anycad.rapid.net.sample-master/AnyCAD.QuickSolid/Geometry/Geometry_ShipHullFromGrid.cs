using AnyCAD.Demo;
using AnyCAD.Foundation;
using AnyCAD.QuickSolid;
using System;

namespace AnyCAD.QuickSolid.Geometry
{
    class Geometry_ShipHullFromGrid : TestCase
    {
        public Geometry_ShipHullFromGrid()
        {
            Title = "网格船体曲面";
            Description = "由型值网格点构造Gordon船体曲面";
            Tags.Add("几何");
            Tags.Add("船体");
            Level = 2;
        }
        public override void Run(IRenderView render)
        {
            double[] xStations = { -10, -5, 0, 5, 10 };
            double[] zWaterlines = { 0, 5, 10, 15, 20 };

            double[,] yData = {
                // Z=0   Z=5   Z=10  Z=15  Z=20
                { 0,    2,    3,    2,    0  }, // X=-10
                { 0,    3,    5,    3,    0  }, // X=-5
                { 0,    4,    6,    4,    0  }, // X=0
                { 0,    3,    5,    3,    0  }, // X=5
                { 0,    2,    3,    2,    0  }, // X=10
            };

            int nStations = xStations.Length;
            int nWaterlines = zWaterlines.Length;

            // ========== 方法1: Gordon 曲面 (推荐) ==========
            var profiles = new TopoShapeList();
            var guides = new TopoShapeList();

            for (int i = 0; i < nStations; i++)
            {
                var pts = new GPntList();
                for (int j = 0; j < nWaterlines; j++)
                    pts.Add(new GPnt(xStations[i], yData[i, j], zWaterlines[j]));
                profiles.Add(CurveBuilder.MakeBSplineByPoints(pts, 3, 3, EnumGeometryContinuity.G2, 0.01));
                render.ShowSceneNode(BrepSceneNode.Create(profiles[i], ColorTable.Red, true));
            }

            for (int j = 0; j < nWaterlines; j++)
            {
                var pts = new GPntList();
                for (int i = 0; i < nStations; i++)
                    pts.Add(new GPnt(xStations[i], yData[i, j], zWaterlines[j]));
                guides.Add(CurveBuilder.MakeBSplineByPoints(pts, 3, 3, EnumGeometryContinuity.G2, 0.01));
                render.ShowSceneNode(BrepSceneNode.Create(guides[j], ColorTable.Blue, true));
            }

            TopoShape hull = AdvSurfaceBuilder.MakeNetworkSurface(profiles, guides, 0.1);
            if (hull != null)
            {
                var mat = MeshStandardMaterial.Create("hull");
                mat.SetColor(new Vector3(0.2f, 0.5f, 0.8f));
                mat.SetFaceSide(EnumFaceSide.DoubleSide);
                render.ShowSceneNode(BrepSceneNode.Create(hull, mat, null));
            }

            // ========== 方法2: B 样条曲面直接拟合 (替代方案) ==========
            var allPts = new GPntList();
            for (int i = 0; i < nStations; i++)
                for (int j = 0; j < nWaterlines; j++)
                    allPts.Add(new GPnt(xStations[i], yData[i, j], zWaterlines[j]));

            TopoShape surf2 = SurfaceBuilder.PointsToBSplineSurface(allPts, nWaterlines);
            if (surf2 != null)
            {
                var mat2 = MeshStandardMaterial.Create("hull2");
                mat2.SetColor(new Vector3(0.8f, 0.3f, 0.2f));
                mat2.SetFaceSide(EnumFaceSide.DoubleSide);
                var node2 = BrepSceneNode.Create(surf2, mat2, null);
                node2.SetTransform(Matrix4d.makeTranslation(25, 0, 0));
                render.ShowSceneNode(node2);
            }

            render.ZoomAll();
        }
    }
}
