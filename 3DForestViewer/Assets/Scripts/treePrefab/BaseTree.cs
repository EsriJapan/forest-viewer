using UnityEngine;

public class BaseTree
{
    public float BaseHeight { get; }
    public float BaseDiameter { get; }
    public float BaseCanopyHeight { get; }
    public float BaseCanopyDiameter { get; }

    public BaseTree() { }

    private BaseTree(float baseHight, float baseDiameter, float baseCanopyHeight, float baseCanopyDiameter)
    {
        BaseHeight = baseHight;
        BaseDiameter = baseDiameter;
        BaseCanopyHeight = baseCanopyHeight;
        BaseCanopyDiameter = baseCanopyDiameter;
    }

    //ベースとなっているAsh4 のPrefabは 6m の高さ、0.5m の幹の直径、
    //林冠は4m の高さ、3.5m の直径があるので、スケーリングを調整。
    public static readonly BaseTree Ash1 = new BaseTree(baseHight: 6.5f, baseDiameter: 0.35f, baseCanopyHeight: 3.5f, baseCanopyDiameter: 4f);
    public static readonly BaseTree Ash4 = new BaseTree(baseHight: 6f, baseDiameter: 0.5f, baseCanopyHeight: 4f, baseCanopyDiameter: 3.5f);
    public static readonly BaseTree Ash7 = new BaseTree(baseHight: 8f, baseDiameter: 0.25f, baseCanopyHeight: 6f, baseCanopyDiameter: 6f);
}


