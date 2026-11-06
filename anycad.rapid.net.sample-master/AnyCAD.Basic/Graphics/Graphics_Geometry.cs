using AnyCAD.Foundation;

namespace AnyCAD.Demo.Graphics
{
    class Graphics_Geometry : TestCase
    {
        public Graphics_Geometry()
        {
            Title = "基础几何图元";
            Description = "展示带纹理的平面、立方体与球体";
            Tags.Add("显示");
            Tags.Add("几何");
            Level = 1;
        }
        public override void Run(IRenderView render)
        {
            // Create material
            var mMaterial1 = MeshPhongMaterial.Create("phong.texture");
            mMaterial1.SetFaceSide(EnumFaceSide.DoubleSide);
            var texture = ImageTexture2D.Create(GetResourcePath("textures/bricks2.jpg"));
            mMaterial1.SetColorMap(texture);
            mMaterial1.SetColor(new Vector3(1));
            

            var plane = GeometryBuilder.CreatePlane(500, 500);
            var planeNode = new PrimitiveSceneNode(plane, mMaterial1);
            planeNode.SetTransform(Matrix4d.makeTranslation(new Vector3d(0, 0, -100)));
            planeNode.SetPickable(false);
            render.ShowSceneNode(planeNode);

            var box = GeometryBuilder.CreateBox(100, 100, 200);
            var boxNode = new PrimitiveSceneNode(box, mMaterial1);
            render.ShowSceneNode(boxNode);

            var sphere = GeometryBuilder.CreateSphere(50, 32, 32);
            var sphereNode = new PrimitiveSceneNode(sphere, mMaterial1);
            sphereNode.SetTransform(Matrix4d.makeTranslation(new Vector3d(0, 0, 150)));

            render.ShowSceneNode(sphereNode);


            var mMaterial2 = MeshStandardMaterial.Create("std.texture");
            var cylinder = GeometryBuilder.CreateCylinder(10, 15, 300);
            var cylinderNode = new PrimitiveSceneNode(cylinder, mMaterial2);
            cylinderNode.SetTransform(Matrix4d.makeTranslation(new Vector3d(100, 200, 0)));
            render.ShowSceneNode(cylinderNode);

            // 添加阴影
            var lmgr = render.SceneManager.GetLightManager();
            var light = new DirectionalLight();
            light.SetPosition(new Vector3(100, 200, 100));
            light.SetTarget(new Vector3(0));          
            light.SetCastShadow(true);

            lmgr.AddLight(light);

            var settings = render.Viewer.GetRenderSettings();
            settings.SetShadowMapEnabled(true);
        }

    }
}
