using AnyCAD.Foundation;
using System;
using System.Xml.Linq;

namespace AnyCAD.Demo.Geometry
{
    class Geometry_ArcBoard : TestCase
    {
        public Geometry_ArcBoard()
        {
            Title = "弧形板造型";
            Description = "通过多段截面轮廓扫掠生成弧形板实体。";
            Tags.Add("几何");
            Tags.Add("扫掠");
            Level = 1;
        }
        public override void Run(IRenderView render)
        {
            //创建实体

            TopoShape aa1 = CreateArcBoard2(render, 300, 500, 30, 10, 10, 45, 2000);
            var node = BrepSceneNode.Create(aa1, ColorTable.LightBlue, true);
            render.ShowSceneNode(node);
            render.ViewContext.GetSceneManager().SelectSubTopo(node.GetUuid(), EnumTopoShapeType.Topo_FACE, 1);

        }


        public virtual TopoShape CreateArcBoard2(IRenderView render, double width, double length, double thickness, double root, double rootOut, double bevelAngle, double radius)
        {
            double arcAngle = width / radius;
            double rInner = radius - thickness / 2;
            double rOuter = radius + thickness / 2;
            var points = new GPntList();
            double a = 0;
            double b = 0;
            double c;
            GPnt p1 = new GPnt(a, b, 0);         //1
            b = root / 2;
            GPnt p2 = new GPnt(a, b, 0);      //2
            a = 0.0 - rootOut;
            GPnt p3 = new GPnt(a, b, 0);      //3
            b = thickness / 2;
            c = a - (thickness - root) * Math.Tan(Math.PI * bevelAngle / 180);
            GPnt p4 = new GPnt(c, b, 0);      //4
            b = 0.0 - b;
            GPnt p9 = new GPnt(c, b, 0);      //9
            a = 0.0 - rootOut;
            b = 0.0 - root / 2;
            GPnt p10 = new GPnt(a, b, 0);      //10
            a = 0;
            GPnt p11 = new GPnt(a, b, 0);      //11
            GPnt p12 = new GPnt(0, 0, 0);      //12
            GPnt p5 = GetValue(arcAngle / 2, rInner, thickness / 2);
            GPnt p6 = GetValue(arcAngle, rInner, thickness / 2);
            GPnt p7 = GetValue(arcAngle, rOuter, -thickness / 2);
            GPnt p8 = GetValue(arcAngle / 2, rOuter, -thickness / 2);

            var line1 = CurveBuilder.MakeLine(p11, p2);
            var line2 = CurveBuilder.MakeLine(p2, p3);
            var line3 = CurveBuilder.MakeLine(p3, p4);
            var line4 = CurveBuilder.MakeArcOfCircle(p4, p6, p5);
            var line5 = CurveBuilder.MakeLine(p6, p7);
            var line6 = CurveBuilder.MakeArcOfCircle(p7, p9, p8);
            var line7 = CurveBuilder.MakeLine(p9, p10);
            var line8 = CurveBuilder.MakeLine(p10, p11);

            var shapeList = new TopoShapeList {
                line1,
                line2,
                line3,
                line4,
                line5,
                line6,
                line7,
                line8
            };
            var wires = CurveBuilder.MakeWire(shapeList);
            var tt2 = CurveBuilder.MakePlanarFace(wires);
            TopoShape tt3 = FeatureTool.Extrude(tt2, length, GP.DZ());

            foreach(var e  in shapeList)
                render.ShowSceneNode(BrepSceneNode.Create(e, ColorTable.Red, true));

            return tt3;
        }
        GPnt GetValue(double angle, double radius, double offset)
        {
            GPnt gPnt = new GPnt(0, 0, 0);
            double xx = 0.0 - radius * Math.Sin(angle);
            double yy = radius * (1 - Math.Cos(angle)) + offset;
            gPnt.x = xx;
            gPnt.y = yy;
            return gPnt;
        }
    }
}
