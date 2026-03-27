#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class ComposedPrefabBundleBuilder
{

    [Serializable]
    private struct CapsuleColliderSpec
    {
        public string targetTransformPath;
        public Vector3 center;
        public float radius;
        public float height;
        public int direction; // 0=X, 1=Y, 2=Z
    }


    [Serializable]
    private struct ComposeJob
    {
        public string sourcePrefabPath;     // ベース（Prefab1相当）
        public string billboardPrefabPath;  // 追加する子（Prefab2相当）
        public string outputTempPrefabPath; // 生成するテンポラリPrefab
        public string runtimeAssetName;     // AssetBundle内で取り出す際の名前
        public string tagTargetPath;        // タグを追加するオブジェクトパス
        public CapsuleColliderSpec capsule;
    }

    // ===== 設定 =====
    private const string TempFolder = "Assets/Prefabs";
    private const string TargetLayerName = "Target";

    // ここで「2つ作る」ためにジョブを2つ定義
    private static readonly ComposeJob[] Jobs =
    {
        new ComposeJob
        {
            sourcePrefabPath = "Assets/Realistic Tree/Prefabs/URP/Ash/Ash 1.prefab",
            billboardPrefabPath  = "Assets/Prefabs/Billboard.prefab",
            outputTempPrefabPath = TempFolder + "/ComposedAsh1.prefab",
            runtimeAssetName = "ComposedAsh1",
            tagTargetPath = "Ash_1/Ash1_LOD0",
            capsule = new CapsuleColliderSpec
            {
                // ★ここを実際の孫Objectのパスに変更してください（例）
                targetTransformPath = "Ash_1/Ash1_LOD0",
                center = new Vector3(-0.01f, 3.5f, -0.01f),
                radius = 0.22f,
                height = 7f,
                direction = 1 // Y-Axis
            }

        },
        new ComposeJob
        {
            sourcePrefabPath = "Assets/Realistic Tree/Prefabs/URP/Ash/Ash 7.prefab",
            billboardPrefabPath  = "Assets/Prefabs/Billboard.prefab", 
            outputTempPrefabPath = TempFolder + "/ComposedAsh7.prefab",
            runtimeAssetName = "ComposedAsh7",
            tagTargetPath = "Ash_7/Ash7_LOD0",
            capsule = new CapsuleColliderSpec
            {
                // ★ここを実際の孫Objectのパスに変更してください（例）
                targetTransformPath = "Ash_7/Ash7_LOD0",
                center = new Vector3(-0.01f, 4.5f, -0.01f),
                radius = 0.15f,
                height = 9f,
                direction = 1 // Y-Axis
            }
        }
    };


    public static void EnsureComposedPrefabs()
    {
        EnsureFolder(TempFolder);

        bool ash1Exists = AssetDatabase.LoadAssetAtPath<GameObject>(Jobs[0].outputTempPrefabPath) != null;
        bool ash7Exists = AssetDatabase.LoadAssetAtPath<GameObject>(Jobs[1].outputTempPrefabPath) != null;

        if (ash1Exists && ash7Exists)
        {
            // どちらもあるなら何もしない
            return;
        }

        Debug.Log("[ComposedPrefabBundleBuilder] Missing composed prefabs. Generating now...");

        foreach (var job in Jobs)
        {
            BuildTempComposedPrefab(job);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[ComposedPrefabBundleBuilder] Composed prefabs generated/updated.");
    }

    private static void BuildTempComposedPrefab(ComposeJob job)
    {
        var basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(job.sourcePrefabPath);
        var childPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(job.billboardPrefabPath);

        if (basePrefab == null) throw new FileNotFoundException($"Prefab が見つかりません。指定のアセットを導入してください。: {job.sourcePrefabPath}");
        if (childPrefab == null) throw new FileNotFoundException($"Child prefab not found: {job.billboardPrefabPath}");

        // Prefab編集用に展開 → 子を追加 → テンポラリとして保存
        GameObject root = PrefabUtility.LoadPrefabContents(job.sourcePrefabPath); // Editor専用API [1](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/PrefabUtility.html)
        try
        {

            // 1) LayerをTargetに変更（親＋配下）
            ApplyLayerRecursively(root, TargetLayerName);
            ApplyTagToGrandChildOnly(root, TargetLayerName, job.tagTargetPath);

            // 2) 孫ObjectへCapsuleColliderを追加/更新（子Prefab追加「前」）
            EnsureCapsuleColliderOnTarget(root, job.capsule);

            // 子Prefabを追加（Prefab参照を保ちやすい）
            var child = (GameObject)PrefabUtility.InstantiatePrefab(childPrefab, root.scene); // Editor専用API [1](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/PrefabUtility.html)
            child.name = childPrefab.name;
            child.transform.SetParent(root.transform, false);

            // ルート名を AssetBundle内の取り出し名に寄せたい場合
            root.name = job.runtimeAssetName;

            PrefabUtility.SaveAsPrefabAsset(root, job.outputTempPrefabPath); // Editor専用API [1](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/PrefabUtility.html)
            Debug.Log($"[ComposedPrefabBundleBuilder] Temp prefab saved: {job.outputTempPrefabPath}");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root); // Editor専用API [1](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/PrefabUtility.html)
        }
    }

    private static void ApplyLayerRecursively(GameObject root, string layerName)
    {
        int layer = LayerMask.NameToLayer(layerName);
        if (layer < 0)
        {
            Debug.LogError($"[ComposedPrefabBundleBuilder] Layer '{layerName}' does not exist. Please create it in Unity Tags & Layers.");
            return;
        }

        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            t.gameObject.layer = layer;
        }
    }

    private static void ApplyTagToGrandChildOnly(GameObject root, string tagName, string targetPath)
    {
        // Tagが存在するかチェックして安全に処理
        if (!IsTagDefined(tagName))
        {
            Debug.LogError($"[ComposedPrefabBundleBuilder] Tag '{tagName}' does not exist. Please create it in Unity Tags & Layers.");
            return;
        }

        Transform target = root.transform.Find(targetPath);
        if (target == null)
        {
            Debug.LogError($"[ComposedPrefabBundleBuilder] Collider target not found: '{targetPath}' in prefab '{root.name}'.");
            return;
        }

        target.gameObject.tag = tagName;
        EditorUtility.SetDirty(root);
    }

    private static bool IsTagDefined(string tag)
    {
        // UnityEditor内部APIを使わずに、Tags一覧に存在するか確認
        // ※ UnityEditor を使っているので Editor 専用でOK
        try
        {
            // tag が未定義だと CompareTag で例外になる
            // 一旦 "Untagged" を使って例外確認…ではなく、タグ配列から確認するのが安全
            var tags = UnityEditorInternal.InternalEditorUtility.tags;
            for (int i = 0; i < tags.Length; i++)
            {
                if (tags[i] == tag) return true;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    private static void EnsureCapsuleColliderOnTarget(GameObject root, CapsuleColliderSpec spec)
    {
        if (string.IsNullOrEmpty(spec.targetTransformPath))
        {
            Debug.LogError("[ComposedPrefabBundleBuilder] CapsuleCollider targetTransformPath is empty. Set it per job.");
            return;
        }

        Transform target = root.transform.Find(spec.targetTransformPath);
        if (target == null)
        {
            Debug.LogError($"[ComposedPrefabBundleBuilder] Collider target not found: '{spec.targetTransformPath}' in prefab '{root.name}'.");
            return;
        }

        var col = target.GetComponent<CapsuleCollider>();
        if (col == null) col = target.gameObject.AddComponent<CapsuleCollider>();

        col.center = spec.center;
        col.radius = spec.radius;
        col.height = spec.height;
        col.direction = Mathf.Clamp(spec.direction, 0, 2); // 0=X,1=Y,2=Z

        // CapsuleColliderとして成立するように最低限補正（Unity仕様上 height は 2*radius 以上推奨）
        if (col.height < col.radius * 2f)
        {
            col.height = col.radius * 2f;
        }

        EditorUtility.SetDirty(target.gameObject);
        // root全体も変更扱いに
        EditorUtility.SetDirty(root);
    }

    private static void EnsureFolder(string assetFolderPath)
    {
        string[] parts = assetFolderPath.Split('/');
        if (parts.Length == 0) return;

        string current = parts[0]; // "Assets"
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }
            current = next;
        }
    }
}
#endif