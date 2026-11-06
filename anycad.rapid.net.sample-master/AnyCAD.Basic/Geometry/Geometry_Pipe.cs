using AnyCAD.Foundation;

namespace AnyCAD.Demo.Geometry
{
    class Geometry_Pipe : TestCase
    {
        public Geometry_Pipe()
        {
            Title = "金属管道";
            Description = "沿多段路径扫掠圆截面生成金属管道。";
            Tags.Add("几何");
            Tags.Add("扫掠");
            Level = 1;
        }

        public override void Run(IRenderView render)
        {
            var material = MeshStandardMaterial.Create("metal-double");
            material.SetColor(ColorTable.金);
            material.SetMetalness(0.4f);
            material.SetRoughness(0.5f);
            material.SetFaceSide(EnumFaceSide.DoubleSide);


            var startPt = new GVec(0, 100, 0);
            var points = new GPntList();
            points.Add(new GPnt(startPt.XYZ()));
            points.Add(new GPnt(startPt.Added(new GVec(0, 0, 150)).XYZ()));
            points.Add(new GPnt(startPt.Added(new GVec(0, 100, 150)).XYZ()));
            points.Add(new GPnt(startPt.Added(new GVec(-100, 100, 150)).XYZ()));
            points.Add(new GPnt(startPt.Added(new GVec(-100, 300, 150)).XYZ()));
            points.Add(new GPnt(startPt.Added(new GVec(100, 300, 150)).XYZ()));
            TopoShape path = CurveBuilder.MakePolygon(points, false);

            var sectionList = new TopoShapeList();

            GAx2 coord1 = new GAx2(new GPnt(startPt.Added(new GVec(- 25, - 25, 0)).XYZ()), GP.DY());
            TopoShape section1 = CurveBuilder.MakeRectangle(coord1, 50, 50, 10, false);

            render.ShowSceneNode(BrepSceneNode.Create(section1, ColorTable.Red, true));
            render.ShowSceneNode(BrepSceneNode.Create(path, ColorTable.Green, true));

            TopoShape pipe = FeatureTool.SweepByFrenet(section1, path, EnumSweepTransitionMode.RoundCorner,
                false);

            var node = BrepSceneNode.Create(pipe, material, null);

            render.ShowSceneNode(node);
        }
    }
}
