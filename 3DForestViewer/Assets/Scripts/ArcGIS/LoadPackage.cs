using Esri.ArcGISMapsSDK.Components;
using Esri.GameEngine.Elevation;
using Esri.GameEngine.Layers.Base;
using Esri.GameEngine.Map;
using Esri.Unity;
using System.IO;
using UnityEngine;

public class LoadPackage : MonoBehaviour
{
    [SerializeField]private ArcGISMapComponent mapComponent;
    [SerializeField]private bool subMap;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        string targetFolder = Application.streamingAssetsPath;
        string tileExtension = "*.tpkx";
        string sceneLayerExtension = "*.slpk";

        string[] tpkxFiles = Directory.GetFiles(targetFolder, tileExtension, SearchOption.AllDirectories);
        string[] slpkFiles = Directory.GetFiles(targetFolder, sceneLayerExtension, SearchOption.AllDirectories);

        float elevEx = 1.0f;
        if (subMap)
        {
            elevEx = 0f;
        }

        
        if(tpkxFiles.Length > 0)
        {
            for (int i = 0; i < tpkxFiles.Length; i++)
            {
                string elevationPath = tpkxFiles[i];

                mapComponent.Map.Elevation = new ArcGISMapElevation(new ArcGISImageElevationSource(elevationPath, i.ToString(), ""))
                {
                    ExaggerationFactor = elevEx
                };
            }
        }
        else
        {
            string elevationPath = "https://elevation3d.arcgis.com/arcgis/rest/services/WorldElevation3D/Terrain3D/ImageServer";

            mapComponent.Map.Elevation = new ArcGISMapElevation(new ArcGISImageElevationSource(elevationPath, "Terrein 3D", ""))
            {
                ExaggerationFactor = elevEx
            };
        }


        if (slpkFiles.Length > 0)
        {
            for (int i = 0; i < slpkFiles.Length; i++)
            {
                string sceneLayerPath = slpkFiles[i];

                ArcGISLayer layer = new ArcGISLayer(sceneLayerPath, ArcGISLayerType.ArcGIS3DObjectSceneLayer, "");

                mapComponent.Map.Layers.Add(layer);
            }
        }
    }
}
