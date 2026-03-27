
using System;
//クラスの定義ファイル。読み込むCSV のカラムに対応。
public class CopseOfTreesFeature
{
    [CsvColumn("樹木ID")]
    public string TreeID;

    [CsvColumn("樹高(ｍ)")]
    public double Tree_Height;

    [CsvColumn("樹冠直径(ｍ)")]
    public double Canopy_Diameter;

    [CsvColumn("樹冠体積(ｍ3)")]
    public double Canopy_Volume;

    [CsvColumn("胸高直径(cm)")]
    public int DiameterBreastHeightCentimeters;

    [CsvColumn("樹種")]
    public string Tree_species;

    [CsvColumn("経度")]
    public double POINT_X;

    [CsvColumn("緯度")]
    public double POINT_Y;

    [CsvColumn("標高")]
    public double POINT_Z;
}



[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
public sealed class CsvColumnAttribute : Attribute
{
    public string Name { get; }
    public int Order { get; }
    public CsvColumnAttribute(string name) => Name = name;
    public CsvColumnAttribute(int order) => Order = order;
}

