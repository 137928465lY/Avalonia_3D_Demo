using AnyCAD.Foundation;

namespace AnyCAD.Demo.Graphics
{

    /// <summary>
    /// 自定义拾取编辑器
    /// </summary>
    class MyPickEditor : Editor
    {
        public MyPickEditor()
        {

        }
        Raycaster m_RayCaster = new Raycaster();
        public override EnumEditorCode OnMouseDown(ViewContext ctx, InputEvent evt)
        {
            var smgr = ctx.GetSelectionManager();
            m_RayCaster.Initialize(ctx.GetCamera(), EnumShapeFilter.VertexEdgeFace, evt.GetX(), evt.GetY());
            m_RayCaster.HitTest(ctx.GetSceneManager());
            var item = m_RayCaster.GetTopItem();

            // 第一次选择，整体选择，第二次选择，子形状选择
            if (smgr.GetSelection().GetCount() == 0)
            {
                item.SetShapeFilter(EnumShapeFilter.Zero);
            }

            smgr.Select(item, false, evt.GetX(), evt.GetY());

            ctx.RequestUpdate(EnumUpdateFlags.Selection);
            return base.OnMouseDown(ctx, evt);
        }

        public override EnumEditorCode OnMouseMove(ViewContext ctx, InputEvent evt)
        {
            m_RayCaster.Initialize(ctx.GetCamera(), EnumShapeFilter.VertexEdgeFace, evt.GetX(), evt.GetY());
            m_RayCaster.HitTest(ctx.GetSceneManager());
            ctx.GetSelectionManager().Hilight(m_RayCaster.GetTopItem(), evt.GetX(), evt.GetY());
            ctx.RequestUpdate(EnumUpdateFlags.Selection);

            return base.OnMouseMove(ctx, evt);
        }
    }


    class Interaction_CustomizedPick : TestCase
    {
        public Interaction_CustomizedPick()
        {
            Title = "自定义拾取";
            Description = "演示自定义拾取编辑器，先选整体再选子形状";
            Tags.Add("交互");
            Tags.Add("拾取");
            Level = 1;
        }
        public override void Run(IRenderView render)
        {
            string fileName = GetResourcePath("ST038.stp");
            var shape = ShapeIO.Open(fileName);
            if (shape == null)
                return;

            var node = BrepSceneNode.Create(shape, null, null);
            render.ShowSceneNode(node);

            render.SetEditor(new MyPickEditor());
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
