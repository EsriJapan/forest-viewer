#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class TreeGeneratorMenu
{
    [MenuItem("Tools/Trees/Generate (From StreamingAssets CSV)", priority = 101)]
    public static void Generate()
    {
        // 1) Composed prefab が無ければ作る（AssetBundle不要）
        ComposedPrefabBundleBuilder.EnsureComposedPrefabs();

        var generator = Object.FindFirstObjectByType<TreeGenerator>();
        if (generator == null)
        {
            Debug.LogError("[Menu] シーン内に TreeGenerator が見つかりません。");
            return;
        }

        generator.GenerateFromCsv_Editor(clearExisting: true);
    }

    [MenuItem("Tools/Trees/Clear All Trees", priority = 102)]
    public static void AllClear()
    {
        var generator = Object.FindFirstObjectByType<TreeGenerator>();

        if (generator == null)
        {
            Debug.LogError("[Menu] シーン内に TreeGenerator が見つかりません。");
            return;
        }

        generator.ClearChildren_Editor();
    }
}
#endif