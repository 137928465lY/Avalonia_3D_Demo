using AnyCAD.Foundation;
using System.IO;
using System.Numerics;

namespace AnyCAD.Demo.Geometry
{
    class Geometry_SweepByFrenet : TestCase
    {
        public Geometry_SweepByFrenet()
        {
            Title = "Frenet扫掠";
            Description = "基于Frenet标架沿路径扫掠截面。";
            Tags.Add("几何");
            Tags.Add("扫掠");
            Level = 1;
        }
        public void Sweep(IRenderView render)
        {
            var path2 = ShapeIO.Open(GetResourcePath("sweep/path_pl.igs"));
            render.ShowSceneNode(BrepSceneNode.Create(path2, ColorTable.Red, true));
            var rectShp = CurveBuilder.MakeRectangle(GP.YOZ(), 0.2, 0.1, 0, false);
            rectShp = TransformTool.Translate(rectShp, new GVec(0.0, 5.0, 0.0));
            render.ShowSceneNode(BrepSceneNode.Create(rectShp, ColorTable.Blue, true));
            //var topshp3 = FeatureTool.SweepByFrenet(rectShp, path2, EnumSweepTransitionMode.RoundCorner, true, false, false);
            //render.ShowShape(topshp3, ColorTable.Red);
        }

        public void SweepWire(IRenderView render)
        {
            var file = OpenModelFile();
            if (file.IsEmpty())
                return;

            var wire = ShapeIO.Open(file);

            var line = CurveBuilder.MakeLine(new GPnt(), new GPnt(0, 100, 0));
            var shape = FeatureTool.SweepByFrenet(wire, line, EnumSweepTransitionMode.Transformed, true);

            render.ShowSceneNode(BrepSceneNode.Create(shape, ColorTable.Azure, true));

        }

        public  void Sweep2(IRenderView render)
        {
            var path = ShapeIO.Open(GetResourcePath("sweep/path.igs"));
            var spline = ShapeIO.Open(GetResourcePath("sweep/spline.igs"));

            var shape = FeatureTool.SweepByFrenet(spline, path, EnumSweepTransitionMode.Transformed, false, false, false);

            render.ShowSceneNode(BrepSceneNode.Create(shape, ColorTable.Red, true));

            var cylinder = ShapeIO.Open(GetResourcePath("sweep/Cylinder.igs"));

            render.ShowSceneNode(BrepSceneNode.Create(cylinder, ColorTable.Blue, true));
        }
        public override void Run(IRenderView render)
        {
            Sweep(render);
        }
    }
}
