using AnyCAD.Foundation;

namespace AnyCAD.Demo.Graphics
{

    /// <summary>
    /// 自定义拾取编辑器
    /// </summary>
    class MySnapEditor : Editor
    {
        public MySnapEditor()
        {

        }
        EnumShapeFilter _Filter = EnumShapeFilter.Zero;
        public override EnumEditorCode Start(ViewContext ctx)
        {
            _Filter = ctx.GetPickFilter();
            ctx.SetPickFilter(EnumShapeFilter.Vertex);
            return base.Start(ctx);
        }

        public override void Finish(ViewContext ctx)
        {
            ctx.SetPickFilter(_Filter);
            base.Finish(ctx);
        }

        public override EnumEditorCode OnMouseDown(ViewContext ctx, InputEvent evt)
        {
            if (evt.GetButtons() == EnumMouseButton.Left)
            {

                var item = ctx.SnapPoint(evt.GetX(), evt.GetY());
                var smgr = ctx.GetSelectionManager();
                smgr.ClearHilight();
                smgr.Select(item, false, evt.GetX(), evt.GetY());

                ctx.RequestUpdate(EnumUpdateFlags.Selection);

                return EnumEditorCode.Processed;
            }

            return base.OnMouseDown(ctx, evt);
        }

        public override EnumEditorCode OnMouseMove(ViewContext ctx, InputEvent evt)
        {
            if (evt.GetButtons() == EnumMouseButton.Zero)
            {
                var item = ctx.SnapPoint(evt.GetX(), evt.GetY());
                if (item.GetShapeFilter() != EnumShapeFilter.Edge)
                {
                    var smgr = ctx.GetSelectionManager();
                    smgr.Hilight(item, evt.GetX(), evt.GetY());
                }
                ctx.RequestUpdate(EnumUpdateFlags.Selection);

                return EnumEditorCode.Processed;
            }


            return base.OnMouseMove(ctx, evt);
        }
    }


    class Interaction_Snap : TestCase
    {
        public Interaction_Snap()
        {
            Title = "顶点捕捉";
            Description = "演示捕捉编辑器吸附拾取顶点";
            Tags.Add("交互");
            Tags.Add("捕捉");
            Level = 1;
        }
        public override void Run(IRenderView render)
        {

            var shape = ShapeBuilder.MakeBox(new GAx2(), 10, 10, 10);
            if (shape == null)
                return;

            var node = BrepSceneNode.Create(shape, null, null);
            render.ShowSceneNode(node);

            render.SetEditor(new MySnapEditor());

        }


        public override void OnSelectionChanged(IRenderView render, PickedResult result)
        {
            var item = result.GetItem();
            if (item.GetNode() == null)
                return;
        }

        public override void Exit(IRenderView render)
        {
            render.SetEditor(null);
            base.Exit(render);
        }
    }
}
