using AnyCAD.Foundation;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace AnyCAD.Demo.Graphics
{
    /// <summary>
    /// 加载 Human Atlas 人体解剖数据（BodyParts3D 4.0，CC BY 4.0）
    ///
    /// 数据文件位于 data/human-atlas/ 目录：
    ///   atlas.json —— 部件清单（2,234 个解剖部件、15 个系统、chunk 索引与字节偏移）
    ///   body-N.bin —— 二进制几何块，每块内按部件顺序依次存储：
    ///                  positions : float32 * vertexCount * 3   （单位：米）
    ///                  normals   : int16   * vertexCount * 3   （值域 -32767~32767，除以 32767 归一化）
    ///                  indices   : uint32  * indexCount
    ///                  各字段起始字节偏移见 atlas.json 中对应部件记录（positions/normals/indices）。
    ///
    /// 源数据为 Y-up 米制坐标，加载时旋转为 AnyCAD 的 Z-up 坐标系：(x, y, z) -> (x, -z, y)。
    /// 每个解剖部件独立成一个网格节点并按所属系统着色，点击部件可在状态栏显示其名称。
    /// </summary>
    class Graphics_HumanAtlas : TestCase
    {
        public Graphics_HumanAtlas()
        {
            Title = "人体解剖模型";
            Description = "加载人体解剖数据按系统着色并透明显示";
            Tags.Add("显示");
            Tags.Add("解剖模型");
            Level = 2;
        }
        #region 数据模型（对应 atlas.json 结构）
        class AtlasPart
        {
            public string id { get; set; }
            public string name { get; set; }
            public string conceptId { get; set; }
            public string system { get; set; }
            public int chunk { get; set; }       // 所属数据块索引
            public int positions { get; set; }   // positions 起始字节偏移
            public int normals { get; set; }     // normals 起始字节偏移
            public int indices { get; set; }     // indices 起始字节偏移
            public int vertexCount { get; set; }
            public int indexCount { get; set; }
        }

        class AtlasChunk
        {
            public string url { get; set; }      // /models/body-N.bin
            public int bytes { get; set; }
        }

        class AtlasManifest
        {
            public string version { get; set; }
            public List<AtlasPart> parts { get; set; }
            public List<AtlasChunk> chunks { get; set; }
            public int triangles { get; set; }
        }
        #endregion

        /// <summary>15 个人体解剖系统的显示颜色</summary>
        static readonly Dictionary<string, Vector3> SystemColors = new Dictionary<string, Vector3>
        {
            { "skeletal",      new Vector3(0.86f, 0.83f, 0.76f) }, // 骨骼：骨色
            { "muscular",      new Vector3(0.72f, 0.26f, 0.28f) }, // 肌肉：暗红
            { "arterial",      new Vector3(0.85f, 0.16f, 0.16f) }, // 动脉：鲜红
            { "venous",        new Vector3(0.22f, 0.30f, 0.78f) }, // 静脉：蓝色
            { "cardiac",       new Vector3(0.55f, 0.08f, 0.12f) }, // 心脏：深红
            { "nervous",       new Vector3(0.90f, 0.70f, 0.18f) }, // 神经：金黄
            { "digestive",     new Vector3(0.88f, 0.50f, 0.18f) }, // 消化：橙
            { "respiratory",   new Vector3(0.28f, 0.68f, 0.72f) }, // 呼吸：青
            { "urinary",       new Vector3(0.58f, 0.38f, 0.75f) }, // 泌尿：紫
            { "endocrine",     new Vector3(0.90f, 0.55f, 0.66f) }, // 内分泌：粉
            { "lymphatic",     new Vector3(0.38f, 0.72f, 0.38f) }, // 淋巴：绿
            { "reproductive",  new Vector3(0.78f, 0.30f, 0.58f) }, // 生殖：洋红
            { "sensory",       new Vector3(0.38f, 0.58f, 0.90f) }, // 感官：天蓝
            { "integumentary", new Vector3(0.74f, 0.58f, 0.44f) }, // 体被：浅棕
            { "connective",    new Vector3(0.60f, 0.60f, 0.64f) }, // 结缔：灰
        };

        /// <summary>每个系统共享一个材质，减少材质数量</summary>
        Dictionary<string, MaterialInstance> mMaterials = new Dictionary<string, MaterialInstance>();

        /// <summary>需设为透明的系统及其不透明度（1=不透明，越小越透）。
        /// 皮肤（integumentary）透明化，便于透视内部骨骼。</summary>
        static readonly Dictionary<string, float> TransparentSystems = new Dictionary<string, float>
        {
            { "integumentary", 0.18f }, // 皮肤：高透明，可看清内部骨骼
        };

        public override void Run(IRenderView render)
        {
            var manifest = LoadManifest();
            if (manifest == null)
            {
                Console.WriteLine("[HumanAtlas] 未找到 data/human-atlas/atlas.json，请确认人体数据已复制到该目录。");
                return;
            }

            var chunkBytes = LoadChunks(manifest.chunks);
            if (chunkBytes == null)
                return;

            int loaded = 0, skipped = 0;
            foreach (var part in manifest.parts)
            {
                var geom = BuildGeometry(chunkBytes, part);
                if (geom == null)
                {
                    skipped++;
                    continue;
                }
                if (part.system == "reproductive")
                    continue;

                var node = new PrimitiveSceneNode(geom, GetMaterial(part.system));
                node.SetName(part.name);   // 点击部件时在状态栏显示名称
                // 皮肤（integumentary）透明且不可拾取，避免遮挡/误选内部骨骼
                if (part.system == "integumentary")
                    node.SetPickable(false);
                render.ShowSceneNode(node);
                loaded++;
            }

            Console.WriteLine(string.Format(
                "[HumanAtlas] 加载完成：{0}/{1} 个部件，{2} 个三角形，{3} 个数据块，跳过 {4} 个无效部件。",
                loaded, manifest.parts.Count, manifest.triangles, manifest.chunks.Count, skipped));

            render.ZoomAll();
        }

        /// <summary>点击部件时在状态栏显示其解剖名称</summary>
        public override void OnSelectionChanged(IRenderView render, PickedResult result)
        {
            if (result == null || result.IsEmpty())
                return;
            var item = result.GetItem();
            if (item == null || item.IsNull())
                return;
            var node = item.GetNode();
            if (node != null)
            {
                var name = node.GetName();
                if (!string.IsNullOrEmpty(name))
                    render.SetToolTip(name);
            }
        }

        #region 数据加载
        AtlasManifest LoadManifest()
        {
            var path = GetResourcePath("human-atlas/atlas.json");
            if (!File.Exists(path))
                return null;
            try
            {
                return JsonConvert.DeserializeObject<AtlasManifest>(File.ReadAllText(path));
            }
            catch (Exception ex)
            {
                Console.WriteLine("[HumanAtlas] 解析 atlas.json 失败：" + ex.Message);
                return null;
            }
        }

        byte[][] LoadChunks(List<AtlasChunk> chunks)
        {
            var result = new byte[chunks.Count][];
            for (int i = 0; i < chunks.Count; i++)
            {
                var name = System.IO.Path.GetFileName(chunks[i].url);   // body-N.bin
                var path = GetResourcePath("human-atlas/" + name);
                if (!File.Exists(path))
                {
                    Console.WriteLine("[HumanAtlas] 缺少数据块：" + path);
                    return null;
                }
                result[i] = File.ReadAllBytes(path);
            }
            return result;
        }

        /// <summary>从二进制数据块中解析单个部件并创建三角网格几何体</summary>
        BufferGeometry BuildGeometry(byte[][] chunks, AtlasPart part)
        {
            if (part.chunk < 0 || part.chunk >= chunks.Length)
                return null;
            var data = chunks[part.chunk];
            int vc = part.vertexCount;
            int ic = part.indexCount;
            if (vc <= 0 || ic <= 0)
                return null;

            // 越界校验
            long posBytes = (long)vc * 12;
            long nrmBytes = (long)vc * 6;
            long idxBytes = (long)ic * 4;
            if (part.positions < 0 || part.normals < 0 || part.indices < 0 ||
                part.positions + posBytes > data.Length ||
                part.normals + nrmBytes > data.Length ||
                part.indices + idxBytes > data.Length)
                return null;

            // positions：float32 * 3，Y-up -> Z-up (x, -z, y)
            // 注意：SetValueByXYZ 的第一个参数是“扁平浮点下标”（按 3 个浮点一个顶点），
            // 因此必须用 i*3，否则每个顶点的 3 个分量会覆盖下一个顶点 -> 网格错乱。
            var positions = new Float32Buffer((uint)(vc * 3));
            for (int i = 0; i < vc; i++)
            {
                int off = part.positions + i * 12;
                float x = BitConverter.ToSingle(data, off);
                float y = BitConverter.ToSingle(data, off + 4);
                float z = BitConverter.ToSingle(data, off + 8);
                // Y-up -> Z-up：旋转 (x, y, z) -> (x, -z, y)，使人体直立
                positions.SetValueByXYZ((uint)(i * 3), x, -z, y);
            }

            // normals：int16 * 3，除以 32767 归一化，并与 positions 做同样的旋转
            var normals = new Float32Buffer((uint)(vc * 3));
            for (int i = 0; i < vc; i++)
            {
                int off = part.normals + i * 6;
                float nx = (float)BitConverter.ToInt16(data, off) / 32767.0f;
                float ny = (float)BitConverter.ToInt16(data, off + 2) / 32767.0f;
                float nz = (float)BitConverter.ToInt16(data, off + 4) / 32767.0f;
                // 与 positions 同款旋转 (x, y, z) -> (x, -z, y)
                normals.SetValueByXYZ((uint)(i * 3), nx, -nz, ny);
            }

            // indices：uint32
            var indices = new Uint32Buffer((uint)ic);
            for (int i = 0; i < ic; i++)
                indices.Set((uint)i, BitConverter.ToUInt32(data, part.indices + i * 4));

            return GeometryBuilder.CreateGeometryByBuffer(
                EnumPrimitiveType.TRIANGLES, positions, normals, null, indices);
        }
        #endregion

        #region 材质
        MaterialInstance GetMaterial(string system)
        {
            if (!mMaterials.TryGetValue(system, out var mat))
            {
                mat = MeshPhongMaterial.Create("human-atlas." + system);
                mat.SetColor(GetSystemColor(system));
                // 需要透明的系统：开启透明并设不透明度
                if (TransparentSystems.TryGetValue(system ?? "", out float opacity))
                {
                    mat.SetTransparent(true);
                    mat.SetOpacity(opacity);
                }
                mMaterials[system] = mat;
            }
            return mat;
        }

        Vector3 GetSystemColor(string system)
        {
            return SystemColors.TryGetValue(system ?? "", out var color)
                ? color
                : new Vector3(0.60f, 0.60f, 0.64f);
        }
        #endregion
    }
}
