using AnyCAD.Foundation;
using System.Xml.Linq;

namespace AnyCAD.Demo.Geometry
{
    class Geometry_SweepGear : TestCase
    {
        public Geometry_SweepGear()
        {
            Title = "齿轮扫掠";
            Description = "沿螺旋路径扫掠齿轮截面。";
            Tags.Add("几何");
            Tags.Add("扫掠");
            Level = 1;
        }

        void Run1(IRenderView render)
        {
            var face = ShapeIO.Open(GetResourcePath("sweep/profile.brep"));
            var profile = face.GetChildren(EnumTopoShapeType.Topo_WIRE)[0];

            var trf = new GTrsf();
            trf.SetScale(GP.Origin(), 0.8);
            trf.SetTranslationPart(new GVec(0, 0, -6));
            var quat = new GQuaternion(new GVec(0, 0, 1), System.Math.PI / 12);
            trf.SetRotationPart(quat);
            var top = TransformTool.Transform(profile, trf);

 
            var section = new TopoShapeList();
            section.Add(profile);
            section.Add(top);
            var solid = FeatureTool.Loft(section, true, false);
            render.ShowSceneNode(BrepSceneNode.Create(solid, ColorTable.BrulyWood, true));
        }

        void Run2(IRenderView render)
        {
            var profile = ShapeIO.Open(GetResourcePath("sweep/profile2.brep"));
            var path = ShapeIO.Open(GetResourcePath("sweep/path2.brep"));

            render.ShowSceneNode(BrepSceneNode.Create(profile, ColorTable.Purple, true));
            render.ShowSceneNode(BrepSceneNode.Create(path, ColorTable.Red, true));

            var solid = FeatureTool.SweepByFrenet(profile, path, EnumSweepTransitionMode.RoundCorner, true);
            render.ShowSceneNode(BrepSceneNode.Create(solid, ColorTable.Gold, true));
        }

        void Run3(IRenderView render)
        {
            var path = ShapeIO.Open(GetResourcePath("sweep/path2.brep"));
            render.ShowSceneNode(BrepSceneNode.Create(path, ColorTable.Green, true));

            TopoShape wire = ShapeIO.Open(GetResourcePath("sweep/AxialPro.brep"));
            ParametricCurve pc = new ParametricCurve(wire);

            GPnt p0 = pc.GetStartPoint();
            GPnt p1 = p0.Translated(new GVec(0, 10, 0));
            GPnt p3 = pc.GetEndPoint();
            GPnt p2 = p3.Translated(new GVec(0, 10, 0));

            TopoShape line0 = CurveBuilder.MakeLine(p1, p0);
            TopoShape line1 = CurveBuilder.MakeLine(p2, p1);
            TopoShape line2 = CurveBuilder.MakeLine(p3, p2);

            TopoShapeList listlines = new TopoShapeList();
            listlines.Add(wire);
            listlines.Add(line0);
            listlines.Add(line1);
            listlines.Add(line2);
            var FaceTeeth = CurveBuilder.MakeWire(listlines);

            render.ShowSceneNode(BrepSceneNode.Create(FaceTeeth, ColorTable.Red, true));

            var TeethSolid = FeatureTool.SweepByFrenet(FaceTeeth, path, EnumSweepTransitionMode.RightCorner, true);

            render.ShowSceneNode(BrepSceneNode.Create(TeethSolid, ColorTable.Gold, true));
        }
        public override void Run(IRenderView render)
        {
            Run3(render);
        }
    }
}
