using Esri.ArcGISMapsSDK.Components;
using Esri.ArcGISMapsSDK.Utils.GeoCoord;
using Esri.GameEngine.Data;
using Esri.GameEngine.Geometry;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;


#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif


public class TreeGenerator : MonoBehaviour
{
    //Editorで指定できるようにSerializeFieldで宣言
    [SerializeField] private TreesCsvReader reader;
    [SerializeField] private GameObject billboard;

    private List<CopseOfTreesFeature> treesList;

#if UNITY_EDITOR
    private const string ComposedAsh1Path = "Assets/Prefabs/ComposedAsh1.prefab";
    private const string ComposedAsh7Path = "Assets/Prefabs/ComposedAsh7.prefab";
#endif

    // ===============================
    // ★ Editor メニューから呼ぶ想定の入口
    // ===============================
#if UNITY_EDITOR

    public void GenerateFromCsv_Editor(bool clearExisting = true)
    {
        if (reader == null)
        {
            Debug.LogError("[TreeGenerator] reader (TreesCsvReader) が未設定です。");
            return;
        }

        // ★ MenuItem押下時にCSV読込
        reader.Reload();
        treesList = reader.Trees;

        if (treesList == null || treesList.Count == 0)
        {
            Debug.LogWarning("[TreeGenerator] CSV の行が 0 件です。");
            return;
        }

        if (clearExisting) ClearChildren_Editor();

        // ★ Composed Prefab を AssetDatabase で直接ロード（AssetBundle不要）
        var composedAsh1 = AssetDatabase.LoadAssetAtPath<GameObject>(ComposedAsh1Path);
        var composedAsh7 = AssetDatabase.LoadAssetAtPath<GameObject>(ComposedAsh7Path);

        if (composedAsh1 == null || composedAsh7 == null)
        {
            Debug.LogError($"[TreeGenerator] Composed Prefab が見つかりません。\n{ComposedAsh1Path}\n{ComposedAsh7Path}");
            return;
        }

        var ash1 = BaseTree.Ash1;
        var ash7 = BaseTree.Ash7;

        int spawned = 0;
        foreach (var feature in treesList)
        {
            var prefab = (feature.Tree_Height > 10.0) ? composedAsh1 : composedAsh7;
            var baseTree = (feature.Tree_Height > 10.0) ? ash1 : ash7;

            InstantiateTree_Editor(prefab, feature, baseTree);
            spawned++;
        }

        EditorSceneManager.MarkSceneDirty(gameObject.scene);
        Debug.Log($"[TreeGenerator] Spawned: {spawned}");
    }


    public void ClearChildren_Editor()
    {
        // 自分の子を全削除（Undo対応）
        var toDelete = new List<GameObject>();
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            toDelete.Add(transform.GetChild(i).gameObject);
        }

        foreach (var go in toDelete)
        {
            Undo.DestroyObjectImmediate(go);
        }
    }

    /// <summary>
    /// Editor上でPrefab接続を保ったまま生成する
    /// </summary>
    private void InstantiateTree_Editor(GameObject prefab, CopseOfTreesFeature feature, BaseTree baseTree)
    {
        if (prefab == null) return;

        // Prefab接続を保つ（EditMode向け）
        var tree = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        Undo.RegisterCreatedObjectUndo(tree, "Instantiate Tree");

        tree.transform.name = feature.TreeID.ToString();
        tree.transform.SetParent(this.transform, false);

        // ---- あなたの既存ロジック（子構造依存） ----
        // Billboard が child(1) に居る前提
        if (tree.transform.childCount > 1)
        {
            GameObject billbaord = tree.transform.GetChild(1).gameObject;
            var changeText = billbaord.GetComponent<ChangeTMP>();
            if (changeText != null) changeText.ChangeTMPforGameObject();
        }

        // Tree本体が child(0) に居る前提
        if (tree.transform.childCount > 0)
        {
            GameObject treeObject = tree.transform.GetChild(0).gameObject;
            CalcTreeScale(feature, baseTree, treeObject);
        }

        // LocationComponent 関連
        var treeLocation = tree.GetComponent<ArcGISLocationComponent>();
        if (treeLocation == null) treeLocation = Undo.AddComponent<ArcGISLocationComponent>(tree);

        treeLocation.SurfacePlacementMode = ArcGISSurfacePlacementMode.OnTheGround;
        treeLocation.Position = new ArcGISPoint(feature.POINT_X, feature.POINT_Y, 0, ArcGISSpatialReference.WGS84());

        float randomizeAngle = UnityEngine.Random.Range(0.0f, 359.9f);
        treeLocation.Rotation = new ArcGISRotation(randomizeAngle, 90, 0);
    }
#endif

    /// <summary>
    /// 樹木モデルの大きさから、適した大きさにスケールを変更するメソッド
    /// </summary>
    /// <param name="feature"></param>
    /// <param name="tree"></param>
    /// <param name="treeObject"></param>
    private void CalcTreeScale(CopseOfTreesFeature feature, BaseTree tree, GameObject treeObject)
    {
        float treeHeight = (float)feature.Tree_Height / tree.BaseHeight;
        //センチをメートルに変換
        float DiameterBreastHeightMeters = (float)feature.DiameterBreastHeightCentimeters / 100f;
        float trunkDiameter = DiameterBreastHeightMeters / tree.BaseDiameter;

        //林冠が楕円体であると仮定し、直径を使い、体積から楕円体の高さを求める。
        //V=3/4πxyz 公式を利用。直径で換算すると、高さY = 6V/πXZ
        float canopyDiameter = (float)feature.Canopy_Diameter / tree.BaseCanopyDiameter;
        float canopyHeight = ((6f * (float)feature.Canopy_Volume) / (Mathf.PI * Mathf.Pow((float)feature.Canopy_Diameter, 2))) / tree.BaseCanopyHeight;

        for (int i = 0; i < treeObject.transform.childCount; i++)
        {
            GameObject treeTrunk = treeObject.transform.GetChild(i).gameObject;
            GameObject treeCanopy = treeTrunk.transform.GetChild(0).gameObject;

            treeTrunk.transform.localScale = new Vector3(trunkDiameter, treeHeight, trunkDiameter);


            // 親と連動して変更された子オブジェクトのスケールをリセットしながら、スケールを変更。
            treeCanopy.transform.localScale = new Vector3(canopyDiameter / trunkDiameter, canopyHeight / treeHeight, canopyDiameter / trunkDiameter);

            float canopyScaleY = treeCanopy.transform.localScale.y;
            float canopyPosition = tree.BaseCanopyHeight * (1 - canopyScaleY);

            treeCanopy.transform.localPosition = new Vector3(0, canopyPosition, 0);
        }
    }
}
