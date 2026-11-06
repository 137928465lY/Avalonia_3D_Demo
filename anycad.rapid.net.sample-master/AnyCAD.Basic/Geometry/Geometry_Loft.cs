using AnyCAD.Foundation;

namespace AnyCAD.Demo.Geometry
{
    class Geometry_Loft : TestCase
    {
        public Geometry_Loft()
        {
            Title = "双截面放样";
            Description = "在两个倾斜截面间放样成实体。";
            Tags.Add("几何");
            Tags.Add("放样");
            Level = 1;
        }
        public override void Run(IRenderView render)
        {
            var baseSketch = CurveBuilder.MakeRectangle(GP.XOY(), 10, 20, 2, false);
            
            // project to plane
            var topSketch = ProjectionTool.ProjectOnPlane(baseSketch, new GPnt(0, 0, 30), new GDir(0,-0.5,  0.5), GP.DZ());

            // make loft
            var shape =  FeatureTool.Loft(baseSketch, topSketch, true);

            render.ShowSceneNode(BrepSceneNode.Create(shape, ColorTable.Green, true));
        }
    }
}
