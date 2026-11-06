using AnyCAD.Foundation;
using System;

namespace AnyCAD.Demo.Graphics
{
    /// <summary>
    /// 光照
    /// </summary>
    class Graphics_Light : TestCase
    {
        PointLight mPointLight;
        SpotLight mSpotLight;
        Background mBackground;
        EnumProjectionType mProjectionType = EnumProjectionType.Perspective;

        LightSceneNode mLightNode;
        double mLightRadius = 15.0;
        double mAngularSpeed = Math.PI * 0.5; // rad/s
        public Graphics_Light()
        {
            Title = "灯光与阴影";
            Description = "演示点光源、聚光灯及实时阴影投射";
            Tags.Add("显示");
            Tags.Add("光照");
            Level = 1;
            mPointLight = new PointLight();
            mPointLight.SetPosition(new Vector3(-10, -10, 10));
            mPointLight.SetColor(ColorTable.White);
            mPointLight.SetCastShadow(true);
            mPointLight.SetIntensity(100);


            mSpotLight = new SpotLight();
            mSpotLight.SetPosition(new Vector3(4, 4, 6));
            mSpotLight.SetTarget(new Vector3(0));
            mSpotLight.SetColor(new Vector3(0.3f, 0.8f, 1.0f));
            mSpotLight.SetCastShadow(true);
            mSpotLight.SetIntensity(20);
        }
        public override void Run(IRenderView render)
        {
            var lightMgr = render.SceneManager.GetLightManager();
            lightMgr.SetAmbientLightColor(new Vector3(0.12f));
            lightMgr.AddLight(mPointLight);
            lightMgr.AddLight(mSpotLight);

 
            var dirLight = new DirectionalLight();
            dirLight.SetPosition(new Vector3(0, 200, 100));
            dirLight.SetTarget(new Vector3(0));
            dirLight.SetCastShadow(true);
            lightMgr.AddLight(dirLight);


            mLightNode = new LightSceneNode();
            mPointLight.SetPosition(new Vector3(-10, -10, 10));
            mLightNode.SetLight(mPointLight);

            render.ShowSceneNode(mLightNode);


            var plane = GeometryBuilder.CreatePlane(100, 100);
            var planeMaterial = MeshStandardMaterial.Create("planeMaterial");
            planeMaterial.SetMetalness(0.1f);
            planeMaterial.SetRoughness(0.8f);
            planeMaterial.SetColor(ColorTable.BrulyWood);
            var planeNode = PrimitiveSceneNode.Create(plane, planeMaterial);
            planeNode.SetPickable(false);
            render.ShowSceneNode(planeNode);


            var sphere = GeometryBuilder.CreateSphere(5, 32, 32);
            var sphereMaterial = MeshStandardMaterial.Create("sphereMaterial");
            sphereMaterial.SetMetalness(0.5f);
            sphereMaterial.SetRoughness(0.2f);
            sphereMaterial.SetColor(ColorTable.White);
            var sphereNode = PrimitiveSceneNode.Create(sphere, sphereMaterial);
            sphereNode.SetTransform(Matrix4d.makeTranslation(new Vector3d(0, 0, 5)));

            render.ShowSceneNode(sphereNode);


            var mMaterial2 = MeshStandardMaterial.Create("std.texture");
            var cylinder = GeometryBuilder.CreateCylinder(5, 10, 50);
            var cylinderNode = new PrimitiveSceneNode(cylinder, mMaterial2);
            cylinderNode.SetTransform(Matrix4d.makeTranslation(new Vector3d(20, 30, 25)));
            render.ShowSceneNode(cylinderNode);




            render.Viewer.GetRenderSettings().SetShadowMapEnabled(true);
            mBackground = render.Viewer.GetBackground();

            //render.Viewer.SetBackgroundColor(new Vector4(0,0,0,1));
            render.Viewer.SetBackground(new SkyBackground());

            mProjectionType = render.Camera.GetProjectionType();
            render.Camera.SetProjectionType(EnumProjectionType.Perspective);

            render.EnableAnimation(true);
        }

        public override void Animation(IRenderView render, float time)
        {
            if (mLightNode == null)
                return;

            // time is in milliseconds
            double t = time /** 0.001*/; // seconds
            double angle = t * mAngularSpeed;

            double x = mLightRadius * Math.Cos(angle);
            double y = mLightRadius * Math.Sin(angle);
            double z = 10.0;

            mLightNode.SetTransform(Matrix4d.makeTranslation(new Vector3d(x, y, z)));
            mLightNode.RequestUpdate();

            render.RequestDraw(EnumUpdateFlags.Scene);
        }

        public override void Exit(IRenderView render)
        {
            var lightMgr = render.SceneManager.GetLightManager();
            lightMgr.ClearLights();

            render.Viewer.GetRenderSettings().SetShadowMapEnabled(false);
            render.Viewer.SetBackground(mBackground); 
            render.EnableAnimation(false);
        }
    }
}
