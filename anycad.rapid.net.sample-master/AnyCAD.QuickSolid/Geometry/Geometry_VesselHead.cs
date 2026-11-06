using AnyCAD.Foundation;
using AnyCAD.QuickSolid;
using System;


namespace AnyCAD.Demo.Geometry
{
    class Geometry_VesselHead : TestCase
    {
        public Geometry_VesselHead()
        {
            Title = "封头开孔";
            Description = "在椭圆封头上开孔并生成接管";
            Tags.Add("几何");
            Tags.Add("封头");
            Level = 2;
        }
        public VesselHead myVessel;
        public override void Run(IRenderView render)
        {
            myVessel = new VesselHead(500, 10, 150, VesselHead.HeadSetType.EHA);
            TopoShape origin = myVessel.CreateTopo();

            double centerX = Math.Cos(270 * Math.PI / 180) * 90;
            double centerY = Math.Sin(270 * Math.PI / 180) * 90;
            GPnt center = new GPnt(centerX, centerY, 0);
            GAx2 center2 = new GAx2(center, GP.DZ());
            TopoShape nozzelInner = ShapeBuilder.MakeCylinder(center2, 30, 435, 0);
            TopoShape nozzelOuter = ShapeBuilder.MakeCylinder(center2, 40, 335, 0);
            TopoShape downWire = BooleanTool.Section(nozzelInner, myVessel.InnerFace);
            downWire  = CurveBuilder.MakeBSplineByWire(downWire, 1, EnumConvertParameterisationType.RationalC1);
            TopoShape upWire = BooleanTool.Section(nozzelOuter, myVessel.OuterFace);
            upWire = CurveBuilder.MakeBSplineByWire(upWire, 1, EnumConvertParameterisationType.RationalC1);
            TopoShape cutFace = FeatureTool.Loft(downWire, upWire, false);
            render.ShowSceneNode(BrepSceneNode.Create(cutFace, ColorTable.SaddleBrown, true));
        }
    }
}