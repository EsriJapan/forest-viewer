using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TargetSelectionHighlighter : MonoBehaviour
{
    [Header("Tag settings")]
    [SerializeField] private string targetTag = "Target";

    [Header("Colors (Color32 recommended)")]
    [SerializeField] private Color32 highlightColor = new Color32(255, 52, 215, 255);
    [SerializeField] private Color32 othersColor = new Color32(21, 83, 188, 255);

    [Header("Hierarchy resolution (optional)")]
    [Tooltip("祖父(=Targetのparentのparent)の子から、親以外で特定したい『叔父』の名前。未設定なら親の兄弟の先頭を使用します。")]
    private string uncleName = "Billboard";
    [Tooltip("叔父の『孫』（=叔父の子の子）のうち色を変えたいオブジェクト名。未設定なら深さ2で最初に見つかった RawImage を使用します。")]
    private string grandchildName = "RawImage";

    [Header("Debug")]
    [SerializeField] private bool showDebug = false;

    private readonly HashSet<Transform> inRange = new();
    private readonly Dictionary<Transform, RawImage> cache = new();

    public Transform CurrentClosest { get; private set; }
    public RawImage CurrentClosestRawImage => GetRawImageForTarget(CurrentClosest);

    /// <summary>
    /// 最接近ターゲットが変わったときに通知
    /// (oldTarget, newTarget, newRawImage)
    /// </summary>
    public event Action<Transform, Transform, RawImage> SelectionChanged;

    private static readonly List<Transform> tmpList = new(8);

    private void OnEnable()
    {
        CurrentClosest = null;
        inRange.Clear();
        cache.Clear();
    }

    private void OnDisable()
    {
        // 無効化時はすべて非ハイライトに戻す
        foreach (var t in inRange)
            SafeSetColor(t, othersColor);

        if (CurrentClosest != null)
            SafeSetColor(CurrentClosest, othersColor);

        CurrentClosest = null;
        inRange.Clear();
        cache.Clear();
    }

    private void OnTriggerEnter(Collider other)
    {
        var target = FindTaggedTargetRoot(other.transform);
        if (target == null) return;

        if (inRange.Add(target))
        {
            SafeSetColor(target, othersColor);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        var target = FindTaggedTargetRoot(other.transform);
        if (target == null) return;

        if (inRange.Remove(target))
        {
            SafeSetColor(target, othersColor);

            if (CurrentClosest == target)
            {
                var old = CurrentClosest;
                CurrentClosest = null;
                SelectionChanged?.Invoke(old, null, null);
            }
        }
    }

    private void Update()
    {
        RemoveNulls();

        // 最も近い Target を選定（距離の2乗で比較）
        Transform newClosest = null;
        float bestSqr = float.PositiveInfinity;
        Vector3 myPos = transform.position;

        foreach (var t in inRange)
        {
            if (t == null) continue;
            float d2 = (t.position - myPos).sqrMagnitude;
            if (d2 < bestSqr)
            {
                bestSqr = d2;
                newClosest = t;
            }
        }

        if (newClosest != CurrentClosest)
        {
            var old = CurrentClosest;

            if (CurrentClosest != null)
                SafeSetColor(CurrentClosest, othersColor);

            CurrentClosest = newClosest;

            if (CurrentClosest != null)
                SafeSetColor(CurrentClosest, highlightColor);

            // 通知（追加操作を外部スクリプトで差し込める）
            SelectionChanged?.Invoke(old, CurrentClosest, GetRawImageForTarget(CurrentClosest));
        }

        // 範囲内：最接近のみハイライト、それ以外は非ハイライト
        foreach (var t in inRange)
        {
            if (t == null) continue;
            var desired = (t == CurrentClosest) ? highlightColor : othersColor;
            SafeSetColor(t, desired);
        }
    }

    private Transform FindTaggedTargetRoot(Transform start)
    {
        if (start == null) return null;

        var t = start;
        for (int i = 0; i < 4 && t != null; i++)
        {
            if (t.CompareTag(targetTag))
                return t;
            t = t.parent;
        }
        return start.CompareTag(targetTag) ? start : null;
    }

    private void SafeSetColor(Transform target, Color32 color)
    {
        var ri = GetRawImageForTarget(target);
        if (ri == null) return;

        // alphaは保持（元の透明度を守る）
        var c = (Color32)ri.color;
        c.r = color.r;
        c.g = color.g;
        c.b = color.b;

        if (ri.color != (Color)c) // Color32->Color implicit
            ri.color = c;

        if (showDebug && target != null)
            Debug.DrawLine(transform.position, target.position, (Color)color);
    }

    /// <summary>
    /// Target から「叔父の孫」に付いている RawImage を取得してキャッシュする。
    /// </summary>
    private RawImage GetRawImageForTarget(Transform target)
    {
        if (target == null) return null;

        if (cache.TryGetValue(target, out var cached) && cached != null)
            return cached;

        var parent = target.parent;
        var grandparent = parent != null ? parent.parent : null;
        if (grandparent == null) return null;

        // 叔父（親の兄弟）を決定
        Transform uncle = null;

        if (!string.IsNullOrEmpty(uncleName))
        {
            var named = grandparent.Find(uncleName);
            if (named != null && named != parent)
                uncle = named;
        }

        if (uncle == null)
        {
            foreach (Transform child in grandparent)
            {
                if (child != parent)
                {
                    uncle = child;
                    break;
                }
            }
        }

        if (uncle == null) return null;

        RawImage result = null;

        if (!string.IsNullOrEmpty(grandchildName))
        {
            foreach (Transform child in uncle)
            {
                foreach (Transform grandchild in child)
                {
                    if (grandchild.name == grandchildName)
                    {
                        result = grandchild.GetComponent<RawImage>();
                        if (result != null) break;
                    }
                }
                if (result != null) break;
            }
        }
        else
        {
            foreach (Transform child in uncle)
            {
                foreach (Transform grandchild in child)
                {
                    var ri = grandchild.GetComponent<RawImage>();
                    if (ri != null)
                    {
                        result = ri;
                        break;
                    }
                }
                if (result != null) break;
            }

            if (result == null)
                result = uncle.GetComponentInChildren<RawImage>(true);
        }

        if (result != null)
            cache[target] = result;

        return result;
    }

    private void RemoveNulls()
    {
        if (inRange.Count == 0) return;

        tmpList.Clear();
        foreach (var t in inRange)
            if (t == null) tmpList.Add(t);
        foreach (var t in tmpList)
            inRange.Remove(t);

        tmpList.Clear();
        foreach (var kv in cache)
            if (kv.Key == null || kv.Value == null) tmpList.Add(kv.Key);
        foreach (var k in tmpList)
            cache.Remove(k);
    }
}
