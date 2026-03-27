using System.Collections.Generic;
using System.Reflection;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class SwitchUI : MonoBehaviour
{
    [Header("UI")]
    [Tooltip("切り替えを行う UI")]
    [SerializeField] private List<GameObject> panels;

    [Header("Input System")]
    [Tooltip("押下を検知したい InputAction（例：Interact/Confirm など）。Action Type=Button を推奨。")]
    [SerializeField] private InputActionReference actionRef;

    private void OnEnable()
    {
        EnableInput(true);
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
        foreach (GameObject panel in panels)
        {
            // ボタンが押下されたタイミングで呼ばれる
            // ここでハイライト中の名前を使って Debug 出力
            if (panel.activeSelf)
            {
                panel.SetActive(false);
                Debug.Log($"[Input] 押下: {ctx.action.name} / 現在の状態: {panel.activeSelf}");
            }
            else
            {
                panel.SetActive(true);
                Debug.Log($"[Input] 押下: {ctx.action.name} / 現在の状態: {panel.activeSelf}");
            }
        }
    }
}
