using AnyCAD.Foundation;

namespace AnyCAD.Demo.Geometry
{
    class Geometry_RectPipeStep : TestCase
    {
        public Geometry_RectPipeStep()
        {
            Title = "台阶方管";
            Description = "矩形轮廓差集后拉伸成台阶方管。";
            Tags.Add("几何");
            Tags.Add("拉伸");
            Level = 1;
        }
        public override void Run(IRenderView render)
        {
           var rect1 =  CurveBuilder.MakeRectangle(GP.XOY(), 10, 5, 1, true);
           var rect2 = CurveBuilder.MakeRectangle(new GAx2(new GPnt(1,1,0), GP.DZ(), GP.DX()), 8, 3, 1, true);

            var cut = BooleanTool.Cut(rect1, rect2);

            var extrude = FeatureTool.Extrude(cut, 100, GP.DZ());

            render.ShowSceneNode(BrepSceneNode.Create(extrude, ColorTable.Blue, true));
        }
    }
}
