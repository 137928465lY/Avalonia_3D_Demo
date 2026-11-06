using AnyCAD.Foundation;
using System;

namespace AnyCAD.Demo.Graphics
{
    /// <summary>
    /// 房间里的三只腿圆桌 + 两只萤火虫（一只点光、一只聚光）飞动投影。
    /// 参考 Graphics_Light.cs：PointLight/SpotLight SetCastShadow + SetShadowMapEnabled。
    /// </summary>
    class Graphics_Shadow : TestCase
    {
        LightSceneNode mPointNode;   // 萤火虫 A：点光源（暖色）
        LightSceneNode mSpotNode;    // 萤火虫 B：聚光灯（冷色）
        PrimitiveSceneNode mPointBody;
        PrimitiveSceneNode mSpotBody;
        PointLight mPointLight;
        SpotLight mSpotLight;

        const float mRoomSize = 10f;   // 房间边长
        const float mRoomHeight = 4f;  // 墙高

        public Graphics_Shadow()
        {
            Title = "萤火虫与阴影";
            Description = "一个小房间里摆着三条腿圆桌，两只萤火虫（一只点光、一只聚光）在屋内飞动，实时投射软阴影。";
            Tags.Add("显示");
            Tags.Add("光照");
            Tags.Add("阴影");
            Tags.Add("动画");
            Level = 2;
        }

        public override void Run(IRenderView render)
        {
            var lightMgr = render.SceneManager.GetLightManager();
            lightMgr.SetAmbientLightColor(new Vector3(0.1f));

            // ================= 房间 =================
            // 地面
            var floorMat = MeshStandardMaterial.Create("firefly.floor.v1");
            floorMat.SetMetalness(0.1f);
            floorMat.SetRoughness(0.8f);
            floorMat.SetColor(ColorTable.Gray);
            var floor = GeometryBuilder.CreatePlane(mRoomSize, mRoomSize);
            var floorNode = PrimitiveSceneNode.Create(floor, floorMat);
            floorNode.SetPickable(false);
            render.ShowSceneNode(floorNode);

            // 后墙（y = -roomSize/2，竖直面）
            var wallMat = MeshStandardMaterial.Create("firefly.wall.v1");
            wallMat.SetRoughness(0.95f);
            wallMat.SetColor(ColorTable.WhiteSmoke);
            var backWall = GeometryBuilder.CreatePlane(mRoomSize, mRoomHeight);
            var backWallNode = PrimitiveSceneNode.Create(backWall, wallMat);
            backWallNode.SetPickable(false);
            backWallNode.SetTransform(Matrix4d.makeTranslation(0, -mRoomSize / 2, mRoomHeight / 2)
                                    * Matrix4d.makeRotationAxis(Vector3d.UNIT_X, (float)(-Math.PI / 2)));
            render.ShowSceneNode(backWallNode);

            // 左墙（x = -roomSize/2）
            var leftWall = GeometryBuilder.CreatePlane(mRoomHeight, mRoomSize);
            var leftWallNode = PrimitiveSceneNode.Create(leftWall, wallMat);
            leftWallNode.SetTransform(Matrix4d.makeTranslation(-mRoomSize / 2, 0, mRoomHeight / 2)
                                    * Matrix4d.makeRotationAxis(Vector3d.UNIT_Y, (float)(Math.PI / 2)));
            leftWallNode.SetPickable(false);
            render.ShowSceneNode(leftWallNode);

            // ================= 三条腿圆桌 =================
            var tableMat = MeshStandardMaterial.Create("firefly.table.v1");
            tableMat.SetRoughness(0.6f);
            tableMat.SetMetalness(0.1f);
            tableMat.SetColor(ColorTable.SaddleBrown);

            // 桌面：薄圆盘 r=1.5, 厚 0.08, 中心 z=1.0
            var tableTop = ShapeBuilder.MakeCylinder(
                new GAx2(new GPnt(0, 0, 0.96), GP.DZ()), 1.5, 0.08, Math.PI * 2);
            var tableTopNode = BrepSceneNode.Create(tableTop, tableMat, null);
            render.ShowSceneNode(tableTopNode);

            // 三条腿：细圆柱 r=0.06, 高 0.96，在 r=1.1 圆周 120° 均布
            for (int i = 0; i < 3; i++)
            {
                double ang = i / 3.0 * 2.0 * Math.PI;
                double lx = Math.Cos(ang) * 1.1;
                double ly = Math.Sin(ang) * 1.1;
                var leg = ShapeBuilder.MakeCylinder(
                    new GAx2(new GPnt(lx, ly, 0), GP.DZ()), 0.06, 0.96, Math.PI * 2);
                var legNode = BrepSceneNode.Create(leg, tableMat, null);
                render.ShowSceneNode(legNode);
            }

            // ================= 萤火虫 A：点光源（暖色） =================
            mPointLight = new PointLight();
            mPointLight.SetColor(ColorTable.White);
            mPointLight.SetIntensity(50);
            mPointLight.SetCastShadow(true);
            mPointLight.SetPosition(new Vector3(2, 2, 2.5));
            lightMgr.AddLight(mPointLight);

            var fireflyMat = MeshStandardMaterial.Create("firefly.bodyA.v3");
            fireflyMat.SetColor(new Vector3(1.0f, 0.9f, 0.4f));
            fireflyMat.SetEmissive(new Vector3(1.0f, 0.8f, 0.2f));
            mPointBody = new PrimitiveSceneNode(GeometryBuilder.CreateSphere(0.1f, 16, 16), fireflyMat);
            mPointNode = new LightSceneNode();
            mPointNode.SetLight(mPointLight);
            render.ShowSceneNode(mPointNode);
            render.ShowSceneNode(mPointBody);

            // ================= 萤火虫 B：聚光灯（冷色） =================
            mSpotLight = new SpotLight();
            mSpotLight.SetColor(ColorTable.White);
            mSpotLight.SetIntensity(20);
            mSpotLight.SetCastShadow(true);
            mSpotLight.SetPosition(new Vector3(-2, -2, 3));
            mSpotLight.SetTarget(new Vector3(0, 0, 0));
            mSpotLight.SetAngle((float)(Math.PI / 6));   // 120° 锥角
            mSpotLight.SetPenumbra(0.3f);
            lightMgr.AddLight(mSpotLight);

            var fireflyMat2 = MeshStandardMaterial.Create("firefly.bodyB.v3");
            fireflyMat2.SetColor(new Vector3(0.5f, 0.85f, 1.0f));
            fireflyMat2.SetEmissive(new Vector3(0.3f, 0.7f, 1.0f));
            mSpotBody = new PrimitiveSceneNode(GeometryBuilder.CreateSphere(0.1f, 16, 16), fireflyMat2);
            mSpotNode = new LightSceneNode();
            mSpotNode.SetLight(mSpotLight);
            render.ShowSceneNode(mSpotNode);
            render.ShowSceneNode(mSpotBody);

            // ================= 渲染设置 =================
            render.Viewer.GetRenderSettings().SetShadowMapEnabled(true);

            render.Camera.SetProjectionType(EnumProjectionType.Perspective);
            render.Camera.SetFovy(50.0);
            render.ShowCoordinateGrid(false);

            render.EnableAnimation(true);
        }

        public override void Animation(IRenderView render, float time)
        {
            float t = time; // s 
            float R = 3.2f;

            // 萤火虫 A（点光）：Lissajous 轨迹，在房间上半部盘旋
            float ax = (float)(R * Math.Sin(t * 0.7));
            float ay = (float)(R * Math.Sin(t * 1.1 + 1.0));
            float az = (float)(2.2 + 0.8 * Math.Sin(t * 1.7));
            mPointLight.SetPosition(new Vector3(ax, ay, az));
            mPointBody.SetTransform(Matrix4d.makeTranslation(ax, ay, az));
            mPointBody.RequestUpdate();

            // 萤火虫 B（聚光）：另一条 Lissajous，始终照向圆桌中心
            float bx = (float)(R * Math.Cos(t * 0.5 + 0.5));
            float by = (float)(R * Math.Sin(t * 0.9 + 2.0));
            float bz = (float)(2.8 + 0.6 * Math.Cos(t * 1.3));
            mSpotLight.SetPosition(new Vector3(bx, by, bz));
            mSpotLight.SetTarget(new Vector3(0, 0, 1.0));
            mSpotBody.SetTransform(Matrix4d.makeTranslation(bx, by, bz));
            mSpotBody.RequestUpdate();

            render.RequestDraw(EnumUpdateFlags.Scene);
        }

        public override void Exit(IRenderView render)
        {
            render.EnableAnimation(false);
            render.SceneManager.GetLightManager().ClearLights();
            render.Viewer.GetRenderSettings().SetShadowMapEnabled(false);
        }
    }
}
