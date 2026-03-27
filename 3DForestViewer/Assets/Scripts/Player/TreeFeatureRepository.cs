using System.Collections.Generic;
using UnityEngine;

public class TreeFeatureRepository : MonoBehaviour
{
    [SerializeField] private TreesCsvReader reader;

    private Dictionary<string, CopseOfTreesFeature> byTreeId;

    private void OnEnable()
    {
        if (reader != null)
            reader.OnTreesReady += HandleTreesReady;

        // すでに生成済みなら即構築（保険）
        if (reader != null && reader.Trees != null && reader.Trees.Count > 0)
            BuildIndex(reader.Trees);
    }

    private void OnDisable()
    {
        if (reader != null)
            reader.OnTreesReady -= HandleTreesReady;
    }

    private void HandleTreesReady(List<CopseOfTreesFeature> trees)
    {
        BuildIndex(trees);
    }


    public void BuildIndex(List<CopseOfTreesFeature> trees)
    {
        byTreeId = new Dictionary<string, CopseOfTreesFeature>(256);

        if (reader == null || reader.Trees == null) return;

        foreach (var f in reader.Trees)
        {
            if (f == null) continue;
            if (string.IsNullOrEmpty(f.TreeID)) continue;

            // 同じIDが来たら後勝ち（必要ならログ出してもOK）
            byTreeId[f.TreeID] = f;
        }
    }

    /// <summary>
    /// 祖父名（target.parent.parent.name）をキーとして検索
    /// </summary>
    public bool TryGetByTarget(Transform target, out CopseOfTreesFeature feature)
    {
        feature = null;
        if (target == null) return false;

        var parent = target.parent;
        var grandParent = parent != null ? parent.parent : null;
        if (grandParent == null) return false;

        return TryGetByTreeId(grandParent.name, out feature);
    }

    public bool TryGetByTreeId(string treeId, out CopseOfTreesFeature feature)
    {
        feature = null;
        if (string.IsNullOrEmpty(treeId)) return false;
        if (byTreeId == null) return false;

        return byTreeId.TryGetValue(treeId, out feature);
    }
}