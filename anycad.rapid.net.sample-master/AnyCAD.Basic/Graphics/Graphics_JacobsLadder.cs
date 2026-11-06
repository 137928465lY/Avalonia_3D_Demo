using AnyCAD.Foundation;
using AnyCAD.Simulate;
using System;
using System.Collections.Generic;

namespace AnyCAD.Demo.Graphics
{
    class Graphics_JacobsLadder : TestCase
    {
        public Graphics_JacobsLadder()
        {
            Title = "傅里叶圆链";
            Description = "六十个旋转圆组成的傅里叶级数逼近曲线";
            Tags.Add("显示");
            Tags.Add("动画");
            Level = 1;
        }
        BufferGeometry mPathGeometry;
        BufferGeometry mCirclesGeometry;
        float mTime = 0;
        const int numCircles = 60;
        const int maxPathPoints = 1500;

        float[] mCoeffs;
        int[] mFreqs;

        List<float> mPathX = new List<float>();
        List<float> mPathY = new List<float>();

        public override void Run(IRenderView render)
        {
            InitializeFourier();
            CreateGeometry(render);
            render.EnableAnimation(true);
        }

        void InitializeFourier()
        {
            mCoeffs = new float[numCircles * 2];
            mFreqs = new int[numCircles];

            for (int i = 0; i < numCircles; i++)
            {
                mFreqs[i] = i < numCircles / 2 ? i : i - numCircles;
            }

            int N = 4000;
            float[] xVals = new float[N];
            float[] yVals = new float[N];

            for (int i = 0; i < N; i++)
            {
                float t = (float)(2 * Math.PI * i / N);
                xVals[i] = 16.0f * (float)Math.Pow(Math.Sin(t), 3);
                yVals[i] = 13.0f * (float)Math.Cos(t) - 5.0f * (float)Math.Cos(2 * t) - 2.0f * (float)Math.Cos(3 * t) - (float)Math.Cos(4 * t);
            }

            for (int k = 0; k < numCircles; k++)
            {
                int freq = mFreqs[k];
                float a = 0, b = 0;

                for (int n = 0; n < N; n++)
                {
                    float t = (float)(2 * Math.PI * n / N);
                    float angle = -2 * (float)Math.PI * freq * t;
                    a += xVals[n] * (float)Math.Cos(angle) + yVals[n] * (float)Math.Sin(angle);
                    b += xVals[n] * (float)Math.Sin(angle) - yVals[n] * (float)Math.Cos(angle);
                }

                a /= N;
                b /= N;

                mCoeffs[k * 2] = a;
                mCoeffs[k * 2 + 1] = b;
            }
        }

        void CreateGeometry(IRenderView render)
        {
            {
                var pathMaterial = BasicMaterial.Create("path");
                pathMaterial.SetUniform("diffuse", ColorTable.Hex(0xFF1493));

                Float32Buffer pathVertices = new Float32Buffer(0);
                for (int i = 0; i < maxPathPoints; i++)
                {
                    pathVertices.Append(0, 0, 0);
                }

                Uint32Buffer pathIndices = new Uint32Buffer(0);
                for (int i = 0; i < maxPathPoints - 1; i++)
                {
                    pathIndices.Append((uint)i);
                    pathIndices.Append((uint)i + 1);
                }

                mPathGeometry = new BufferGeometry(EnumPrimitiveType.LINES);
                mPathGeometry.AddAttribute(EnumAttributeSemantic.Position, EnumAttributeComponents.Three, pathVertices);
                mPathGeometry.SetIndex(pathIndices);

                var pathNode = new PrimitiveSceneNode(mPathGeometry, pathMaterial);
                render.ShowSceneNode(pathNode);
            }

            {
                var circlesMaterial = BasicMaterial.Create("circles");
                circlesMaterial.SetUniform("diffuse", ColorTable.Hex(0x00CED1));

                int totalVerts = numCircles * 37 + numCircles * 2;
                Float32Buffer circlesVertices = new Float32Buffer(0);
                for (int i = 0; i < totalVerts; i++)
                {
                    circlesVertices.Append(0, 0, 0);
                }

                mCirclesGeometry = new BufferGeometry(EnumPrimitiveType.LINES);
                mCirclesGeometry.AddAttribute(EnumAttributeSemantic.Position, EnumAttributeComponents.Three, circlesVertices);

                var circlesNode = new PrimitiveSceneNode(mCirclesGeometry, circlesMaterial);
                render.ShowSceneNode(circlesNode);
            }
        }

        public override void Exit(IRenderView render)
        {
            render.EnableAnimation(false);
        }

        public override void Animation(IRenderView render, float time)
        {
            mTime = time;
            UpdateGeometry(render);
            render.RequestDraw();
        }

        void UpdateGeometry(IRenderView render)
        {
            float speed = 0.08f;
            float t = mTime * speed;

            var pathPosition = mPathGeometry.GetAttribute(0);
            pathPosition.SetDataUsage(EnumBufferDataUsage.DYNAMIC_DRAW);
            var pathPositions = new Float32Array(pathPosition.GetData());

            var circlesPosition = mCirclesGeometry.GetAttribute(0);
            circlesPosition.SetDataUsage(EnumBufferDataUsage.DYNAMIC_DRAW);
            var circlesPositions = new Float32Array(circlesPosition.GetData());

            uint circleIdx = 0;
            float centerX = 0, centerY = 0;

            for (int k = 0; k < numCircles; k++)
            {
                float a = mCoeffs[k * 2];
                float b = mCoeffs[k * 2 + 1];
                int freq = mFreqs[k];

                float radius = (float)Math.Sqrt(a * a + b * b);
                float phase = (float)Math.Atan2(b, a);
                float angle = 2 * (float)Math.PI * freq * t + phase;

                float vx = radius * (float)Math.Cos(angle);
                float vy = radius * (float)Math.Sin(angle);

                float nextX = centerX + vx;
                float nextY = centerY + vy;

                int segments = 36;
                for (int i = 0; i <= segments; i++)
                {
                    float a2 = (float)(2 * Math.PI * i / segments);
                    circlesPositions.SetValue((uint)(circleIdx * 3), centerX + radius * (float)Math.Cos(a2));
                    circlesPositions.SetValue((uint)(circleIdx * 3 + 1), centerY + radius * (float)Math.Sin(a2));
                    circlesPositions.SetValue((uint)(circleIdx * 3 + 2), 0);
                    circleIdx++;
                }

                circlesPositions.SetValue((uint)(circleIdx * 3), centerX);
                circlesPositions.SetValue((uint)(circleIdx * 3 + 1), centerY);
                circlesPositions.SetValue((uint)(circleIdx * 3 + 2), 0);
                circleIdx++;
                circlesPositions.SetValue((uint)(circleIdx * 3), nextX);
                circlesPositions.SetValue((uint)(circleIdx * 3 + 1), nextY);
                circlesPositions.SetValue((uint)(circleIdx * 3 + 2), 0);
                circleIdx++;

                centerX = nextX;
                centerY = nextY;
            }

            mPathX.Add(centerX);
            mPathY.Add(centerY);

            if (mPathX.Count > maxPathPoints)
            {
                mPathX.RemoveAt(0);
                mPathY.RemoveAt(0);
            }

            int pathCount = mPathX.Count;
            float lastX = pathCount > 0 ? mPathX[pathCount - 1] : 0;
            float lastY = pathCount > 0 ? mPathY[pathCount - 1] : 0;

            for (int i = 0; i < maxPathPoints; i++)
            {
                if (i < pathCount)
                {
                    pathPositions.SetValue((uint)(i * 3), mPathX[i]);
                    pathPositions.SetValue((uint)(i * 3 + 1), mPathY[i]);
                }
                else
                {
                    pathPositions.SetValue((uint)(i * 3), lastX);
                    pathPositions.SetValue((uint)(i * 3 + 1), lastY);
                }
                pathPositions.SetValue((uint)(i * 3 + 2), 0);
            }

            pathPosition.RequestUpdate();
            mPathGeometry.RequestUpdate();

            circlesPosition.RequestUpdate();
            mCirclesGeometry.RequestUpdate();
        }
    }
}
