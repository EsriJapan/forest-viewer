
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class TreesCsvReader : MonoBehaviour
{
    public List<CopseOfTreesFeature> Trees { get; private set; } = new List<CopseOfTreesFeature>();

    public event Action<List<CopseOfTreesFeature>> OnTreesReady;

    // ★ MenuItem 押下時に明示的に呼ぶ
    public void Reload()
    {
        Trees = CreateList();
        OnTreesReady?.Invoke(Trees);
    }

    void Awake() => Trees = CreateList();

    //CSVからListを作成するメソッド
    private  List<CopseOfTreesFeature> CreateList()
    {
        string targetFolder = Application.streamingAssetsPath;
        string extension = "*.csv";
        string content = "";

        string[] files = Directory.GetFiles(targetFolder, extension, SearchOption.AllDirectories);

        if (files.Length > 0)
        {
            content = File.ReadAllText(files[0]);

            //Trees = CsvLoader.Load<CopseOfTreesFeature>(ta.text);
            var trees = CsvLoader.Load<CopseOfTreesFeature>(content);

            if (trees.Count > 0)
            {
                var t = trees[0];
            }

            return trees;
        }

        return new List<CopseOfTreesFeature>();    
    }
}

