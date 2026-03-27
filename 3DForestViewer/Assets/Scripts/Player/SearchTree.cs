using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using Color = UnityEngine.Color;
using System;

public class SearchTree : MonoBehaviour
{
    [Header("Tag settings")]
    [SerializeField] private string targetTag = "Target";

    [Header("Colors")]
    [SerializeField] private Color highlightColor = new Color(255, 0, 83); // 最も近い時の色
    [SerializeField] private Color othersColor = new Color(21, 83, 188);  // それ以外の時の色

    [Header("Hierarchy resolution (optional)")]
    [Tooltip("祖父(=Targetのparentのparent)の子から、親以外で特定したい『叔父』の名前。未設定なら親の兄弟の先頭を使用します。")]
    [SerializeField] private string uncleName = "Billboard";
    [Tooltip("叔父の『孫』（=叔父の子の子）のうち色を変えたいオブジェクト名。未設定なら深さ2で最初に見つかった RawImage を使用します。")]
    [SerializeField] private string grandchildName = "RawImage";


    [Header("Input System")]
    [Tooltip("押下を検知したい InputAction。Action Type=Button を推奨。")]
    [SerializeField] private InputActionReference actionRef;

    [Header("Tree CSV Reader")]
    [SerializeField] private TreesCsvReader reader;

    [Header("Attribute Panel")]
    [SerializeField] private Transform attributePanel;

    [Header("Debug")]
    [SerializeField] private bool showDebug = false;

    private readonly HashSet<Transform> inRange = new HashSet<Transform>();
    private readonly Dictionary<Transform, RawImage> cache = new Dictionary<Transform, RawImage>();
    public Transform currentClosest { get; private set; }

    List<CopseOfTreesFeature> CopseOfTreesFeatures;

    private void OnEnable()
    {
        currentClosest = null;
        inRange.Clear();
        cache.Clear();

        EnableInput(true);
    }

    private void OnDisable()
    {
        // 無効化時はすべて非ハイライトに戻す
        foreach (var t in inRange)
            SafeSetColor(t, othersColor);

        if (currentClosest != null)
            SafeSetColor(currentClosest, othersColor);
    }

    private void EnableInput(bool enable)
    {
        if (actionRef == null) return;
        var action = actionRef.action;
        if (action == null) return;

        if (enable)
        {
            if (!action.enabled) action.Enable();
            action.performed -= OnActionPerformed; // 二重登録防止
            action.performed += OnActionPerformed;
        }
        else
        {
            action.performed -= OnActionPerformed;
            if (action.enabled) action.Disable();
        }
    }


    private void OnActionPerformed(InputAction.CallbackContext ctx)
    {
        // ボタンが押下されたタイミングで呼ばれる
        // ここでハイライト中の名前を使って Debug 出力
        if (currentClosest != null)
        {
            Transform grandParent = currentClosest.parent.parent;
            
            foreach (var t in CopseOfTreesFeatures)
            {
                var item = t.GetType();
                if (grandParent.name == t.TreeID)
                {
                    var fields = item.GetFields (BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

                    Debug.Log($"[Input] fields: {fields.Length}");
                    for (int i = 0; i < fields.Length; i++)
                    {
                        object value = fields[i].GetValue(t);

                        TMP_Text tmp = attributePanel.GetChild(i).GetChild(1).GetComponentInChildren<TMP_Text>();

                        if(value != null) 
                        {
                            tmp.text = value.ToString();
                        }
                        else
                        {
                            tmp.text = string.Empty;
                        }
                    }
                }
            }
        }
        else
        {
            Debug.Log($"[Input] 押下: {ctx.action.name} / 現在ハイライト中の対象はありません");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        var target = FindTaggedTargetRoot(other.transform);
        if (target == null) return;

        if (inRange.Add(target))
        {
            // 初期状態は非ハイライト
            SafeSetColor(target, othersColor);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        var target = FindTaggedTargetRoot(other.transform);
        if (target == null) return;

        if (inRange.Remove(target))
        {
            // 範囲外へ出たら非ハイライト
            SafeSetColor(target, othersColor);
            if (currentClosest == target)
                currentClosest = null;
        }
    }

    private void Start()
    {
        CopseOfTreesFeatures = reader.Trees;
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

        // 以前の最接近が変わったら色を戻す
        if (newClosest != currentClosest)
        {
            if (currentClosest != null)
                SafeSetColor(currentClosest, othersColor);
            currentClosest = newClosest;
        }

        // 範囲内の各 Target に対して、最接近のみハイライト、それ以外は非ハイライト
        foreach (var t in inRange)
        {
            if (t == null) continue;
            var desired = (t == currentClosest) ? highlightColor : othersColor;
            SafeSetColor(t, desired);
        }
    }

    /// <summary>
    /// 当たってきた Transform から、上位に Tag=Target を持つ Transform を探す（最大4階層上まで）
    /// </summary>
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

    private void SafeSetColor(Transform target, Color color)
    {
        var ri = GetRawImageForTarget(target);
        if (ri == null) return;

        float keepA = ri.color.a;

        var newColor = new Color(color.r, color.g, color.b, keepA);

        if (ri.color != color)
            ri.color = newColor;

        if (showDebug)
            Debug.DrawLine(transform.position, target.position, color);
    }

    /// <summary>
    /// Target から「叔父の孫」に付いている RawImage を取得してキャッシュする。
    /// 期待構造:
    /// grandparent
    ///  ├─ parent (== target.parent)
    ///  │   └─ target (Tag=Target)
    ///  └─ uncle (== parent の兄弟) ← ここから2段下の孫に RawImage
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
            // 名前が指定されていない場合、親以外の最初の兄弟を叔父とする
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

        // 叔父の「孫」(= 叔父の子の子)から探す
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
            // 名前未指定なら、深さ2の範囲で最初に見つかった RawImage を使用
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

            // 保険: どうしても見つからない場合は子孫全体から1つ拾う（孫以外でも）
            if (result == null)
                result = uncle.GetComponentInChildren<RawImage>(true);
        }

        if (result != null)
            cache[target] = result;

        return result;
    }

    /// <summary>破棄済みの参照を掃除</summary>
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

    // 一時バッファ（GC削減用）
    private static readonly List<Transform> tmpList = new List<Transform>(8);
}