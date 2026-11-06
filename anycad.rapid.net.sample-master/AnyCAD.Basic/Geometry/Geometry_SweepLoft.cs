using AnyCAD.Foundation;

namespace AnyCAD.Demo.Geometry
{
    class Geometry_SweepLoft : TestCase
    {
        public Geometry_SweepLoft()
        {
            Title = "扫掠放样";
            Description = "组合多段截面做扫掠放样造型。";
            Tags.Add("几何");
            Tags.Add("放样");
            Level = 1;
        }
        public void SweepBySections1(IRenderView render)
        {
            var path = CurveBuilder.MakeArcOfCircle(new GPnt(0, 0, 0), new GPnt(10, 0, 10), new GPnt(5, 0, 8));
            var baseSketch = CurveBuilder.MakeRectangle(new GAx2(new GPnt(-5, -10, 0), GP.DZ(), GP.DX()), 10, 20, 2, false);
            var topSketch = CurveBuilder.MakeCircle(new GPnt(10, 0, 10), 5, GP.DX());

            var shapeList = new TopoShapeList();
            shapeList.Add(baseSketch);
            shapeList.Add(topSketch);
            var shape = FeatureTool.SweepBySections(shapeList, path, EnumSweepTransitionMode.RoundCorner, true);

            render.ShowSceneNode(BrepSceneNode.Create(shape, ColorTable.Beige, true));
        }

        public void SweepBySections2(IRenderView render)
        {
            var baseWire = CurveBuilder.MakeRectangle(new GAx2(new GPnt(35, -60, 0), GP.DZ()), 30, 20, 9, false);
            var topWire = FeatureTool.OffsetWire(baseWire, 6, 30, EnumGeomJoinType.Arc, false);
            var midWire = FeatureTool.OffsetWire(baseWire, 2, 15, EnumGeomJoinType.Arc, false);

            TopoShapeList topLst = new TopoShapeList();
            topLst.Add(baseWire);
            topLst.Add(midWire);
            topLst.Add(topWire);
            var path2 = CurveBuilder.MakeLine(new GPnt(35 + 15, -60 + 10, 0), new GPnt(35 + 15, -60 + 10, 30));

            render.ShowSceneNode(BrepSceneNode.Create(baseWire, ColorTable.Violet, true));
            render.ShowSceneNode(BrepSceneNode.Create(midWire, ColorTable.Violet, true));
            render.ShowSceneNode(BrepSceneNode.Create(topWire, ColorTable.Violet, true));

            render.ShowSceneNode(BrepSceneNode.Create(path2, ColorTable.Violet, true));

            var sweepFrenet = FeatureTool.SweepBySections(topLst, path2, EnumSweepTransitionMode.Transformed, true);
            render.ShowSceneNode(BrepSceneNode.Create(sweepFrenet, ColorTable.LightYellow, true));
        }

        public override void Run(IRenderView render)
        {
            SweepBySections1(render);
            SweepBySections2(render);
        }
    }
}
