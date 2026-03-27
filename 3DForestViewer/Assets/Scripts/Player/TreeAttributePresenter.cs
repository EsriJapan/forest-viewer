using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class TreeAttributePresenter : MonoBehaviour
{
    [Header("Input System")]
    [SerializeField] private InputActionReference actionRef;

    [Header("Dependencies")]
    [SerializeField] private TargetSelectionHighlighter highlighter;
    [SerializeField] private TreeFeatureRepository repository;

    [Header("Attribute Panel")]
    [SerializeField] private Transform attributePanel;

    private void OnEnable()
    {
        EnableInput(true);
    }

    private void OnDisable()
    {
        EnableInput(false);
    }

    private void EnableInput(bool enable)
    {
        if (actionRef == null) return;
        var action = actionRef.action;
        if (action == null) return;

        if (enable)
        {
            if (!action.enabled) action.Enable();
            action.performed -= OnActionPerformed;
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
        if (highlighter == null || repository == null || attributePanel == null)
            return;

        var target = highlighter.CurrentClosest;
        if (target == null)
        {
            Debug.Log($"[Input] 押下: {ctx.action.name} / 現在ハイライト中の対象はありません");
            return;
        }

        if (!repository.TryGetByTarget(target, out var feature) || feature == null)
        {
            Debug.Log($"[Input] 押下: {ctx.action.name} / Feature が見つかりません: {target.name}");
            return;
        }

        // Reflectionでpublic fieldを列挙（元コード踏襲）
        var type = feature.GetType();
        var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

        for (int i = 0; i < fields.Length; i++)
        {
            if (i >= attributePanel.childCount) break;

            object value = fields[i].GetValue(feature);

            TMP_Text tmp = attributePanel.GetChild(i).GetChild(1).GetComponentInChildren<TMP_Text>();
            tmp.text = value != null ? value.ToString() : string.Empty;
        }
    }
}
