using AnyCAD.Foundation;

namespace AnyCAD.Demo.Graphics
{
    class Graphics_Material : TestCase
    {
        public Graphics_Material()
        {
            Title = "镜像法线修正";
            Description = "对比镜像几何体法线反向与双面材质修正";
            Tags.Add("显示");
            Tags.Add("材质");
            Level = 1;
        }       
        public void WrongNormal(IRenderView render)
        {
            var shape = ShapeBuilder.MakeCylinder(GP.XOY(), 4, 4, 0);
            MeshPhongMaterial mMaterial2 = MeshPhongMaterial.Create("phton.1.flip");
            mMaterial2.SetColor(new Vector3(1, 0, 1));
            var node2 = BrepSceneNode.Create(shape, mMaterial2, null);

            // 构造一个镜像矩阵
            node2.SetTransform(Matrix4d.makeTranslate(10, 0, 0) * Matrix4d.makeScale(new Vector3d(-1)));
            render.ShowSceneNode(node2);
        }

        /// <summary>
        ///  修正由于镜像矩阵导致的法线反向
        /// </summary>
        /// <param name="render"></param>
        public void CorrectNormal(IRenderView render)
        {
            MeshPhongMaterial mMaterial1;
            mMaterial1 = MeshPhongMaterial.Create("phong.1");
            mMaterial1.SetColor(new Vector3(1, 0, 1));
            mMaterial1.GetTemplate().SetFlipSided(true);  // 面取反
            mMaterial1.SetFaceSide(EnumFaceSide.DoubleSide);

            var shape = ShapeBuilder.MakeCylinder(GP.XOY(), 4, 4, 0);
            var node1 = BrepSceneNode.Create(shape, mMaterial1, null);
            // 构造一个镜像矩阵
            node1.SetTransform(Matrix4d.makeScale(new Vector3d(-1)));
            render.ShowSceneNode(node1);
        }

        public override void Run(IRenderView render)
        {
            CorrectNormal(render);
            WrongNormal(render);
        }
    }
}
