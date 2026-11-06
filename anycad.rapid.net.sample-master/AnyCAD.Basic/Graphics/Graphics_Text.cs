using AnyCAD.Foundation;



namespace AnyCAD.Demo.Graphics
{
    class Graphics_Text : TestCase
    {
        public Graphics_Text()
        {
            Title = "多形式文字渲染";
            Description = "演示字体纹理、公告牌与标签等文字显示";
            Tags.Add("显示");
            Tags.Add("文字");
            Level = 1;
        }
        static bool mLoaded = false;
        public override void Run(IRenderView render)
        {
            if(!mLoaded)
            {
                mLoaded = true;

                FontManager.Instance().AddFont("LS", @"C:\Windows\Fonts\STLITI.TTF");
            }    

            {
                var fontMaterial = FontMaterial.Create("font-texture-1");
                fontMaterial.SetFaceSide(EnumFaceSide.DoubleSide);
                fontMaterial.SetColor(new Vector3(1, 1, 0));
                fontMaterial.SetBackground(new Vector3(0, 0, 1));
                fontMaterial.SetBillboard(true);

                var dim = fontMaterial.SetText("Hello 世界!", 128, "LS");
                var shape = GeometryBuilder.CreatePlane(dim.x * 0.05f, dim.y * 0.05f);

                var node = new PrimitiveSceneNode(shape, fontMaterial);
                node.SetPickable(false);
                node.SetTransform(Matrix4d.makeTranslation(new Vector3d(100, -100, 100)));
                render.ShowSceneNode(node);
            }
            {
                var fixedSizeMaterial = SpriteMaterial.Create("font-mesh-material");
                fixedSizeMaterial.SetSizeAttenuation(false);
                fixedSizeMaterial.SetColor(ColorTable.OrangeRed);

                var mesh = FontManager.Instance().CreateMesh("为中华之崛起而代码！");
                var node = new PrimitiveSceneNode(mesh, fixedSizeMaterial);
                var scale = 1 / 44.0f;
                node.SetTransform(Matrix4d.makeTranslation(10, 10, 10) * Matrix4d.makeScale(scale, scale, scale));
                node.SetPickable(false);

                render.ShowSceneNode(node);
                
            }

            {
                var text = new TextSceneNode("33.0℃", 32, ColorTable.White, ColorTable.Green, true, new Vector4(13,1,10,1), 2, ColorTable.Red);
                text.SetTransform(Matrix4d.makeTranslate(100, 100, 100));
                render.ShowSceneNode(text);
            }

            {
                var text = TextSceneNode.Create("不缩放大小", ColorTable.Green, 32, true);
                var tag = TagNode2D.Create(text, new Vector3(-10, -10, -10));               
                render.ShowSceneNode(tag);

                var pt = GeometryBuilder.CreatePoint(new Vector3(-10, -10, -10));
                var node = PrimitiveSceneNode.Create(pt, null);
                render.ShowSceneNode(node); 
            }

        }
    }
}
