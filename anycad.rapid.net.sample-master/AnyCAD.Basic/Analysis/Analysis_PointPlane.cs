using AnyCAD.Foundation;

namespace AnyCAD.Demo.Geometry
{
    class Analysis_PointPlane : TestCase
    {
        public Analysis_PointPlane()
        {
            Title = "点面距离";
            Description = "计算点和面之间的距离";
            Tags.Add("几何");
            Tags.Add("分析");
            Level = 1;
        }
        public override void Run(IRenderView renderer)
        {
            var pt = new GPnt(10,10,10);
            var plane = CurveBuilder.MakePlanarFace(new GPln(new GPnt(200, 0, 0), new GDir(1, 0, 0)), -100, 100, -100, 100);

            var etrema = new ExtremaPointSurface();
            if(etrema.Initialize(plane, pt))
            {               
                var dist = System.Math.Sqrt(etrema.GetSquareDistance(0));

                var text = TextSceneNode.Create(dist.ToString(), ColorTable.Blue, 30, true);

                renderer.ShowSceneNode(text);

                var uv =  etrema.GetParameter(0);
                var pos = etrema.GetPoint(0);

                var pLine = GeometryBuilder.CreateLine(new Vector3(pt.x, pt.y, pt.z), new Vector3(pos.x, pos.y, pos.z));
                var pLineNode = PrimitiveSceneNode.Create(pLine, null);
                pLineNode.SetColor(ColorTable.Red);
                renderer.ShowSceneNode(pLineNode);
            }


            renderer.ShowSceneNode(BrepSceneNode.Create(plane, ColorTable.Bisque, true));

          
        }
    }
}
