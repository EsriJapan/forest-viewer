using Esri.ArcGISMapsSDK.Components;
using Esri.GameEngine.Geometry;
using UnityEngine;

public class SummonPlayer : MonoBehaviour
{
    [SerializeField] private GameObject player;
    private ArcGISPoint firstPlayerPoint;
    [SerializeField] private float rayCastLength = 3000.0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if(player.GetComponent<ArcGISLocationComponent>() is var lc)
        {
            firstPlayerPoint = new ArcGISPoint(lc.Position.X,lc.Position.Y,lc.Position.Z,lc.Position.SpatialReference);
        }
        player.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        Ray ray = new Ray(this.transform.localPosition, Vector3.down);
        
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, rayCastLength))
        {
            Debug.Log(hit.collider.gameObject.name);

            rayCastLength = 0f;
            player.SetActive(true);
        }
        
        // 標高値 がある程度マイナスに行ったらリスポーン
        // 数値は日本の最低標高より小さい値(八戸鉱山:約130m)
        if (player.transform.position.y < -150)
        {
            if (player.GetComponent<ArcGISLocationComponent>() is var lc)
            {
                // スタート地点に戻したい。
                lc.Position = firstPlayerPoint;
            }
        }
    }
}
