using BiuBiu.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BiuBiu.EditorTools
{
    /// <summary>
    /// 一次性接入工具：把生成的草地地板 sprite 绑定到 Main 场景 RuntimeSceneBuilder.groundVariants。
    /// 由引擎解析 guid → Sprite，避免手写 YAML 引用 fileID 出错（团结引擎 sprite 子资源 fileID 不可靠推断）。
    /// 用法：菜单 BiuBiu/接入草地地板（确保当前已打开 Main.unity），执行后自动保存场景。
    /// 也可依赖 [InitializeOnLoad] 在编辑器启动时自动尝试绑定（仅当槽位缺失/引用丢失时）。
    /// </summary>
    public static class AssignGroundVariants
    {
        private const string MainScenePath = "Assets/Scenes/Main.unity";
        private const string GroundMainPath = "Assets/Art/Tilemaps/ground_main.png";
        private const string GroundVariantPath = "Assets/Art/Tilemaps/ground_variant.png";

        [MenuItem("BiuBiu/接入草地地板")]
        public static void Bind()
        {
            if (!EditorSceneManager.GetActiveScene().path.Equals(MainScenePath))
            {
                Debug.LogWarning("[接入草地地板] 请先打开 Main.unity 再执行。");
                return;
            }

            var builder = Object.FindObjectOfType<RuntimeSceneBuilder>();
            if (builder == null)
            {
                Debug.LogError("[接入草地地板] 场景中未找到 RuntimeSceneBuilder。");
                return;
            }

            Sprite main = AssetDatabase.LoadAssetAtPath<Sprite>(GroundMainPath);
            Sprite variant = AssetDatabase.LoadAssetAtPath<Sprite>(GroundVariantPath);
            if (main == null || variant == null)
            {
                Debug.LogError("[接入草地地板] 未加载到草地 sprite，请确认 Assets/Art/Tilemaps/ 下两张 png 已导入且为 Sprite 类型。");
                return;
            }

            // 反射设置私有序列化字段 groundVariants（[0] 主格 / [1] 变体格）
            var field = typeof(RuntimeSceneBuilder).GetField("groundVariants",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field == null)
            {
                Debug.LogError("[接入草地地板] 找不到 groundVariants 字段。");
                return;
            }

            field.SetValue(builder, new[] { main, variant });

            EditorUtility.SetDirty(builder);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[接入草地地板] 已绑定 ground_main(主) / ground_variant(变体) 并保存场景。");
        }

        // 编辑器启动时自动尝试绑定（仅当槽位为空或引用丢失时），减少手动操作负担。
        // 若已正确绑定则跳过，不覆盖用户改动。
        static AssignGroundVariants()
        {
            EditorApplication.delayCall += TryAutoBind;
        }

        private static void TryAutoBind()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!EditorSceneManager.GetActiveScene().path.Equals(MainScenePath)) return;

            var builder = Object.FindObjectOfType<RuntimeSceneBuilder>();
            if (builder == null) return;

            var field = typeof(RuntimeSceneBuilder).GetField("groundVariants",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field == null) return;

            var current = field.GetValue(builder) as Sprite[];
            bool needBind = current == null || current.Length < 2 || current[0] == null || current[1] == null;

            if (!needBind) return;

            Sprite main = AssetDatabase.LoadAssetAtPath<Sprite>(GroundMainPath);
            Sprite variant = AssetDatabase.LoadAssetAtPath<Sprite>(GroundVariantPath);
            if (main == null || variant == null) return;

            field.SetValue(builder, new[] { main, variant });
            EditorUtility.SetDirty(builder);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[接入草地地板] 自动绑定草地地板并保存。");
        }
    }
}
