using AnyCAD.Foundation;
using AnyCAD.QuickSolid;
namespace AnyCAD.Demo.Geometry
{
    class Geometry_HLR : TestCase
    {
        public Geometry_HLR()
        {
            Title = "消隐轮廓";
            Description = "计算模型在三向投影视图下的可见轮廓";
            Tags.Add("几何");
            Tags.Add("消隐");
            Level = 2;
        }
        public override void Run(IRenderView render)
        {
            var fileName = GetResourcePath("ST038.stp");
            var model = ShapeIO.Open(fileName);
            if(model == null) return;

            ShapeHLRBuilder builder = new ShapeHLRBuilder();
            builder.AddShape(model);
            var bbox = model.GetBBox();

            {
                var ax = new GAx2(bbox.CornerMax().Translated(new GVec(10, 0, 0)), new GDir(new GVec(-1, 0, 0)));
                if (builder.Compute(ax))
                {
                    var edges = builder.GetVisibleOutlines();
                    var group = ShapeBuilder.MakeCompound(edges);
                    var node = BrepSceneNode.Create(group, ColorTable.LightGreen, true);
                    render.ShowSceneNode(node);
                    var trf = TransformTool.ToMatrix4d(ax);
                    node.SetTransform(trf);
                }
            }
            {
                var ax = new GAx2(bbox.CornerMax().Translated(new GVec(0, 10, 0)), new GDir(new GVec(0, -1, 0)));
                if (builder.Compute(ax))
                {
                    var edges = builder.GetVisibleOutlines();
                    var group = ShapeBuilder.MakeCompound(edges);
                    var node = BrepSceneNode.Create(group, ColorTable.LightPink, true);
                    render.ShowSceneNode(node);
                    var trf = TransformTool.ToMatrix4d(ax);
                    node.SetTransform(trf);
                }
            }
            {
                var ax = new GAx2(bbox.CornerMax().Translated(new GVec(0, 0, 10)), new GDir(new GVec(0, 0, -1)));
                if (builder.Compute(ax))
                {
                    var edges = builder.GetVisibleOutlines();
                    var group = ShapeBuilder.MakeCompound(edges);
                    var node = BrepSceneNode.Create(group, ColorTable.GreenYellow, true);
                    render.ShowSceneNode(node);
                    var trf = TransformTool.ToMatrix4d(ax);
                    node.SetTransform(trf);
                }
            }

            render.ShowSceneNode(BrepSceneNode.Create(model, ColorTable.LightGray, true));
        }
    }
}
