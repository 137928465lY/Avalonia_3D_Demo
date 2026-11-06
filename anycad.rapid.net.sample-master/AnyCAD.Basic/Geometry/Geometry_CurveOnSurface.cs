using AnyCAD.Foundation;
using System;

namespace AnyCAD.Demo.Geometry
{
    class Geometry_CurveOnSurface : TestCase
    {
        public Geometry_CurveOnSurface()
        {
            Title = "曲面曲线";
            Description = "将参数曲线映射到锥面与柱面上。";
            Tags.Add("几何");
            Tags.Add("投影");
            Level = 1;
        }
        public override void Run(IRenderView render)
        {


            var paramCurve = Sketch2dBuilder.MakeLine(new GPnt2d(0, 0), new GPnt2d(Math.PI*20, 20));

            // curve one cone
            {
                var cone = ShapeBuilder.MakeCone(GP.XOY(), 1, 10, 20, 0);
                var coneFace = cone.FindChild(EnumTopoShapeType.Topo_FACE, 0);
                var curveOnConeSurface = CurveBuilder.MakeCurveOnSurface(paramCurve, coneFace);

                var material = LineDashedMaterial.Create("my.dashed.material");
                material.SetColor(ColorTable.Hex(0x0FFAA));
                var node = BrepSceneNode.Create(curveOnConeSurface, null, material);

                render.ShowSceneNode(node);
            }
            // curve one cylinder
            {
                var cylinder = ShapeBuilder.MakeCylinder(GP.XOY(), 2, 20, 0);
                var cylinderFace = cylinder.FindChild(EnumTopoShapeType.Topo_FACE, 0);

                var curveOnCylinderSurface = CurveBuilder.MakeCurveOnSurface(paramCurve, cylinderFace);

                var node2 = BrepSceneNode.Create(curveOnCylinderSurface, null, null);
                render.ShowSceneNode(node2);

            }

        }
    }
}
