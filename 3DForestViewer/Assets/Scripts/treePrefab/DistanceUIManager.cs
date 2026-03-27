
using System.Collections.Generic;
using UnityEngine;


[DefaultExecutionOrder(-10000)]
public class DistanceUIManager : MonoBehaviour
{
    public static DistanceUIManager Instance { get; private set; }

    [Header("Player")]
    public Transform player; // タグ検索でもOK（Awakeで1回だけ）

    [Header("Performance")]
    [Tooltip("1秒あたり何個更新するか。2000なら 2000～6000 くらいから調整")]
    public int updatesPerSecond = 3000;

    [Tooltip("更新を止める距離よりさらに遠い時は登録解除…など拡張用")]
    public bool keepRegistered = true;

    readonly List<DistanceUIItem> _items = new();
    int _cursor;

    
    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (!player)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }
    }

    public void Register(DistanceUIItem item)
    {
        if (!item) return;
        if (!_items.Contains(item)) _items.Add(item);
    }

    public void Unregister(DistanceUIItem item)
    {
        if (!item) return;
        int idx = _items.IndexOf(item);
        if (idx >= 0)
        {
            _items.RemoveAt(idx);
            if (_cursor > idx) _cursor--; // カーソル補正
            if (_cursor >= _items.Count) _cursor = 0;
        }
    }

    void Update()
    {
        if (!player) return;
        int count = _items.Count;
        if (count == 0) return;

        // 1フレームに更新する個数を計算（フレームレート依存で均す）
        int perFrame = Mathf.Max(1, Mathf.RoundToInt(updatesPerSecond * Time.deltaTime));
        Vector3 playerPos = player.position;

        for (int i = 0; i < perFrame; i++)
        {
            if (count == 0) break;

            if (_cursor >= count) _cursor = 0;
            var item = _items[_cursor];
            _cursor++;

            if (!item) continue;
            if (!item.isActiveAndEnabled) continue;

            item.Evaluate(playerPos);
        }
    }
}
