using AnyCAD.Demo;
using AnyCAD.Foundation;
namespace AnyCAD.Test
{
    class Interaction_PointOnSurface : TestCase
    {
        public Interaction_PointOnSurface()
        {
            Title = "曲面点法向";
            Description = "拾取圆柱面并显示点击处的法向箭头";
            Tags.Add("交互");
            Tags.Add("曲面拾取");
            Level = 2;
        }
        public override void Run(IRenderView render)
        {
            var cylinder = ShapeBuilder.MakeCylinder(GP.XOY(), 10, 10, 0);
            render.ShowSceneNode(BrepSceneNode.Create(cylinder, ColorTable.Blue, true));

        }

        public override void OnSelectionChanged(IRenderView render, PickedResult result)
        {
            if (result.IsEmpty())
                return;
            var hit = result.GetItem();
            var node = BrepSceneNode.Cast(hit.GetNode());
            if (node == null)
                return;


            var shape = node.GetTopoShape();

            if (hit.GetShapeFilter() != EnumShapeFilter.Face)
                return;

            var face = shape.FindChild(EnumTopoShapeType.Topo_FACE, hit.GetTopoShapeId());
            if (face == null)
                return;

            ParametricSurface surface = new ParametricSurface(face);
            if (!surface.IsValidGeometry())
                return;
            var uv = surface.GetUV(hit.GetPositiond().ToPnt());
            var normal = surface.GetNormal(uv);


            ArrowWidget arrow = ArrowWidget.Create(0.2f, 1.0f, null);
            arrow.SetLocation(hit.GetPosition(), Vector3.From(hit.GetPoint().GetNormal()));

            render.ShowSceneNode(arrow);
        }
    }
}
