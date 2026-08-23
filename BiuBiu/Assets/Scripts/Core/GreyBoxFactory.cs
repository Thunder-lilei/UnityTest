using UnityEngine;

namespace BiuBiu.Core
{
    /// <summary>
    /// 灰盒工厂：程序化生成占位视觉（圆/方 Sprite + 颜色），M1 循环先行跑通；
    /// 素材批次交付后由 prefab/SO 引用替换（EnemyData.prefab 等字段非空时优先用素材）。
    /// Sprite 均静态缓存（同一张白圆/白方重复着色使用，不重复创建贴图）。
    /// </summary>
    public static class GreyBoxFactory
    {
        /// <summary>白色圆点 Sprite 缓存（弹丸/经验块/血瓶占位）</summary>
        private static Sprite circleSprite;

        /// <summary>白色方块 Sprite 缓存（敌人/墙体占位）</summary>
        private static Sprite squareSprite;

        /// <summary>获取白圆 Sprite（32×32，PPU 32 → 1×1 tile）</summary>
        public static Sprite Circle
        {
            get
            {
                if (circleSprite == null) circleSprite = CreateSprite(true);
                return circleSprite;
            }
        }

        /// <summary>获取白方 Sprite（32×32，PPU 32 → 1×1 tile）</summary>
        public static Sprite Square
        {
            get
            {
                if (squareSprite == null) squareSprite = CreateSprite(false);
                return squareSprite;
            }
        }

        /// <summary>创建 32×32 白色 Sprite（圆或方；Point filter 无压缩——像素风基线）</summary>
        private static Sprite CreateSprite(bool circle)
        {
            int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            var px = new Color32[size * size];
            float c = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool inside = true;
                    if (circle)
                    {
                        float dx = x - c, dy = y - c;
                        inside = dx * dx + dy * dy <= c * c; // 圆内=白
                    }
                    px[y * size + x] = inside ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0);
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            // PPU 32：Sprite 世界尺寸 = 32/32 = 1 tile（像素风基线）
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32f);
        }

        /// <summary>
        /// 创建灰盒 GameObject（SpriteRenderer + 颜色，本地缩放=目标尺寸）。
        /// </summary>
        /// <param name="name">物体名（便于 Hierarchy 辨认）</param>
        /// <param name="circle">true=圆 / false=方</param>
        /// <param name="color">占位颜色</param>
        /// <param name="size">目标世界尺寸（tile）</param>
        public static GameObject MakeBox(string name, bool circle, Color color, Vector2 size)
        {
            var go = new GameObject(name);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = circle ? Circle : Square;
            sr.color = color;
            sr.sortingOrder = 10; // 灰盒默认压在地面(0)之上
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            return go;
        }
    }
}
