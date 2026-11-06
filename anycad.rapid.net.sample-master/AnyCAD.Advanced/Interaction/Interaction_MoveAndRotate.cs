using AnyCAD.Foundation;

namespace AnyCAD.Demo.Graphics
{
    class Interaction_MoveAndRotate : TestCase
    {
        public Interaction_MoveAndRotate()
        {
            Title = "移动旋转节点";
            Description = "用变换手柄编辑器拖动旋转场景节点";
            Tags.Add("交互");
            Tags.Add("变换手柄");
            Level = 2;
        }
        public override void Run(IRenderView render)
        {
            var shape = GeometryBuilder.CreateBox(10, 100, 200);
            var node = PrimitiveSceneNode.Create(shape, null);
            render.ShowSceneNode(node);

            var editor = new NodeFrameEditor(node, TransformWidget.Create(20), "MyNodeEditor");
            render.SetEditor(editor);
        }

        public override void Exit(IRenderView render)
        {
            render.SetEditor(null);
        }
    }
}
