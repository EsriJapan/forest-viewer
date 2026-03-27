using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ControllHiddenTree : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private Transform hiddenRoot; // 非表示オブジェクトを集約する親
    [SerializeField] private TargetSelectionHighlighter highlighter;
    [SerializeField] private TreeFeatureRepository repository;

    private readonly Dictionary<Transform, Transform> originalParents = new();

    private readonly Stack<Transform> hiddenStack = new();

    [Header("Input Actions")]
    [SerializeField] private InputAction hideAction;
    [SerializeField] private InputAction restoreLastAction;
    [SerializeField] private InputAction restoreAllAction;

    private void OnEnable()
    {
        if (hideAction != null)
        {
            hideAction.Enable();
            hideAction.performed += OnHidePerformed;
        }

        if (restoreLastAction != null)
        {
            restoreLastAction.Enable();
            restoreLastAction.performed += OnRestoreLastPerformed;
        }

        if (restoreAllAction != null)
        {
            restoreAllAction.Enable();
            restoreAllAction.performed += OnRestoreAllPerformed;
        }

    }

    private void OnDisable()
    {
        if (hideAction != null)
            hideAction.performed -= OnHidePerformed;

        if (restoreLastAction != null)
            restoreLastAction.performed -= OnRestoreLastPerformed;

        if (restoreAllAction != null) 
            restoreAllAction.performed -= OnRestoreAllPerformed;
    }

    private void OnHidePerformed(InputAction.CallbackContext ctx)
    {
        if (highlighter == null || repository == null)
            return;

        var target = highlighter.CurrentClosest;
        if (target == null)
        {
            Debug.Log($"[Input] 押下: {ctx.action.name} / 現在ハイライト中の対象はありません");
            return;
        }

        // 操作対象を「祖父」にする
        var grand = GetGrandParentOrNull(target);
        if (grand == null)
        {
            Debug.Log($"[Input] Hide: {ctx.action.name} / 祖父が存在しないため操作できません: target={target.name}");
            return;
        }

        // Feature の存在チェックは残す（対象が正しいか担保）
        if (!repository.TryGetByTarget(target, out var feature) || feature == null)
        {
            Debug.Log($"[Input] 押下: {ctx.action.name} / Feature が見つかりません: {target.name}");
            return;
        }

        // ここからが目的：別親に移して非表示
        HideAndRecord(grand);

        Debug.Log($"[Input] 押下: {ctx.action.name} / {grand.name} を退避して非表示にしました");
    }

    private void OnRestoreLastPerformed(InputAction.CallbackContext ctx)
    {
        bool ok = RestoreLastHidden();
        Debug.Log(ok
            ? $"[Input] RestoreLast: {ctx.action.name} / 最後の1つを復帰しました"
            : $"[Input] RestoreLast: {ctx.action.name} / 復帰対象がありません");
    }

    private Transform GetGrandParentOrNull(Transform target)
    {
        if (target == null) return null;
        var p = target.parent;
        if (p == null) return null;
        var gp = p.parent;
        if (gp == null) return null;
        return gp;
    }


    private void HideAndRecord(Transform target)
    {
        if (target == null) return;
        if (hiddenRoot == null) return;

        // すでに hiddenRoot 配下なら二重記録しない
        if (target.parent == hiddenRoot) return;

        // 元親を記録（初回だけ）
        if (!originalParents.ContainsKey(target))
        {
            originalParents[target] = target.parent;
            hiddenStack.Push(target); // 最後に隠した順を保持
        }

        target.SetParent(hiddenRoot, worldPositionStays: true);
        target.gameObject.SetActive(false);
    }
    private bool RestoreLastHidden()
    {
        // 破棄済みなど null を飛ばしつつ Pop
        Transform t = null;
        while (hiddenStack.Count > 0)
        {
            var candidate = hiddenStack.Pop();
            if (candidate != null && !candidate.Equals(null))
            {
                t = candidate;
                break;
            }
        }

        if (t == null) return false;

        return RestoreSingle(t);
    }

    private int RestoreAllHidden()
    {
        if (hiddenRoot == null) return 0;

        // Dictionary を直接 foreach しながら変更すると例外になるので、キーを退避
        var keys = new List<Transform>(originalParents.Keys);

        int count = 0;
        for (int i = 0; i < keys.Count; i++)
        {
            var t = keys[i];
            if (t == null || t.Equals(null))
            {
                // 破棄済みの掃除
                originalParents.Remove(t);
                continue;
            }

            // 「隠れている=hiddenRoot配下」としたい場合はここで条件を加える
            // 例：hiddenRoot配下のみ復帰
            if (t.parent != hiddenRoot) continue;

            if (RestoreSingle(t))
                count++;
        }

        // すべて復帰したら hiddenStack も掃除（残骸除去）
        CleanupHiddenStack();

        return count;
    }

    private void OnRestoreAllPerformed(InputAction.CallbackContext ctx)
    {
        int restored = RestoreAllHidden();
        Debug.Log($"[Input] RestoreAll: {ctx.action.name} / {restored} 個を一括復帰しました");
    }

    private bool RestoreSingle(Transform t)
    {
        if (t == null || t.Equals(null)) return false;

        if (!originalParents.TryGetValue(t, out var parent) || parent == null || parent.Equals(null))
        {
            // 元親が不明/破棄済み：戻し先の代替を用意するならここで
            // 例：hiddenRoot.parent に戻す、など
            parent = hiddenRoot != null ? hiddenRoot.parent : null;
            if (parent == null)
            {
                Debug.LogWarning($"[RestoreSingle] 元親不明かつ代替親も無いため復帰不可: {t.name}");
                return false;
            }
        }

        // 表示して戻す
        t.gameObject.SetActive(true);
        t.SetParent(parent, worldPositionStays: true);

        // 記録を消す（復帰済み）
        originalParents.Remove(t);

        return true;
    }

    private void CleanupHiddenStack()
    {
        if (hiddenStack.Count == 0) return;

        // Stackを直接フィルタできないので作り直す
        var temp = new Stack<Transform>();
        while (hiddenStack.Count > 0)
        {
            var t = hiddenStack.Pop();
            if (t == null || t.Equals(null)) continue;

            // まだ hiddenRoot 配下＆ originalParents に残っているものだけ保持
            if (t.parent == hiddenRoot && originalParents.ContainsKey(t))
                temp.Push(t);
        }

        // temp は逆順になるので再度反転して元の順（LIFO）を概ね維持
        while (temp.Count > 0)
            hiddenStack.Push(temp.Pop());
    }


}
