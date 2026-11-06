using AnyCAD.Foundation;
using System;
using System.Collections.Generic;

namespace AnyCAD.Demo.Graphics
{
    class Graphics_100000Sphere : TestCase
    {
        public Graphics_100000Sphere()
        {
            Title = "十万球体渲染";
            Description = "批量渲染十万个随机着色球体验证渲染性能";
            Tags.Add("显示");
            Tags.Add("性能");
            Level = 1;
        }
        public override void Run(IRenderView renderer)
        {
            var shape = GeometryBuilder.CreateSphere(1);
            Random random= new Random();
            List<MeshPhongMaterial> materials = new List<MeshPhongMaterial>();
            for(int ii=0; ii<10; ++ii)
            {
                var color = Vector3.From(random.NextDouble(), random.NextDouble(), random.NextDouble());
                var material = MeshPhongMaterial.Create("mesh.material");
                material.SetColor(color);
                materials.Add(material);
            }

            //100 x 100 x 5;
            float distance = 3.0f;
            int halfCount = 50;
            int height = 10;
            var scene = renderer.Scene;
            for (int ii = -halfCount; ii < halfCount; ++ii)
            {
                if (ii == 0)
                    continue;

                for (int jj = -halfCount; jj < halfCount; ++jj)
                {
                    if (jj == 0)
                        continue;

                    for (int kk = 0; kk < height; ++kk)
                    {
                        var material = materials[random.Next(10)];
                        var node = PrimitiveSceneNode.Create(shape, material);
                        node.SetTransform(Matrix4d.makeTranslation(ii * distance, jj * distance, kk * distance));
                        scene.AddNode(node);
                    }
                }
            }

            renderer.ZoomAll();
        }
    }
}
