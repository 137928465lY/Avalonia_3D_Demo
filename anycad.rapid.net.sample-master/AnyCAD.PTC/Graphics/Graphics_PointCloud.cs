using AnyCAD.Foundation;
using AnyCAD.PointCloud;
using System.IO;



namespace AnyCAD.Demo.Graphics
{
    class Graphics_PointCloud : TestCase
    {
        public Graphics_PointCloud()
        {
            Title = "点云显示";
            Description = "读取带颜色的XYZ点云文件并显示";
            Tags.Add("显示");
            Tags.Add("点云");
            Level = 2;
        }
        static Float32Buffer mPositions;
        static Float32Buffer mColors;
        bool ReadData()
        {
            if (mPositions != null)
                return true;

            string fileName = GetResourcePath("cloud.xyz");
            using (StreamReader reader = File.OpenText(fileName))
            {
                mPositions = new Float32Buffer(0);
                mColors = new Float32Buffer(0);
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var items = line.Split(' ');
                    if (items.Length != 6)
                        continue;

                    for (int ii = 0; ii < 3; ++ii)
                    {
                        mPositions.Append(float.Parse(items[ii]));
                    }

                    for (int ii = 3; ii < 6; ++ii)
                    {
                        mColors.Append(float.Parse(items[ii]) / 255.0f);
                    }
                }
            }

            return true;
        }
        public override void Run(IRenderView render)
        {
            if (!ReadData())
                return;
       

            var node = PointCloudNode.Create(mPositions, mColors, null, 1);

            render.ShowSceneNode(node);

            render.ViewContext.GetSceneManager().SelectSubShape(node.GetUuid(), EnumShapeFilter.Vertex, 0);

        }
    }
}
