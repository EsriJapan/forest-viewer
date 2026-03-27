using System;
using System.Reflection;
using TMPro;
using UnityEngine;

public class ContentsGenerator : MonoBehaviour
{
    [SerializeField] private TreesCsvReader reader;
    [SerializeField] private Transform parentPanel;
    [SerializeField] private GameObject contentPrefab;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        var treeList = reader.Trees;

        if(treeList.Count > 0)
        {
            //サンプル取得
            var cotf = treeList[0];
       
            var item = cotf.GetType();
            var fields = item.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

            //フィールドの数だけ Content を生成して、カラムの名前を書き込む。
            for (int i = 0; i < fields.Length; i++)
            {
                var column = fields[i].GetCustomAttribute<CsvColumnAttribute>(inherit: false);

                if (column != null)
                {
                    Instantiate(contentPrefab, parentPanel);

                    //Content 下の Table (要素0) 内の Text を書き換える
                    TMP_Text tmp = parentPanel.GetChild(i).GetChild(0).GetComponentInChildren<TMP_Text>();
                    tmp.text = column.Name;
                }
            }
        }
    }
}
