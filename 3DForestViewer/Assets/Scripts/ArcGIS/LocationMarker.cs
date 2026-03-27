// Copyright 2024 Esri.
//
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at: http://www.apache.org/licenses/LICENSE-2.0
//

using Esri.ArcGISMapsSDK.Components;
using Esri.ArcGISMapsSDK.Utils.GeoCoord;
using Esri.GameEngine.Geometry;
using UnityEngine;
using UnityEngine.UI;

public class LocationMarker : MonoBehaviour
{
    [SerializeField] private ArcGISCameraComponent cameraComponent;

    private ArcGISLocationComponent playerLocationComponent;
    private ArcGISLocationComponent locationComponent;
    private ArcGISMapComponent mapComponent;
    [SerializeField] private GameObject northMarker;
    [SerializeField] private GameObject player;
    const float northOffset = 225.0f;
    [SerializeField] private RawImage overviewMap;

    private void Awake()
    {
        //cameraController = FindFirstObjectByType<ArcGISCameraControllerComponent>();
        //cameraLocationComponent = cameraController.GetComponent<ArcGISLocationComponent>();
        playerLocationComponent = player.GetComponent<ArcGISLocationComponent>();
        locationComponent = GetComponent<ArcGISLocationComponent>();
        mapComponent = GetComponentInParent<ArcGISMapComponent>();
    }

    private void Update()
    {
        if (playerLocationComponent == null || locationComponent == null || mapComponent == null)
        {
            Debug.Log("null");
            return;
        }

        UpdateLocationAndRotation();
    }

    private void UpdateLocationAndRotation()
    {
        var playerPosition = playerLocationComponent.Position;
        var playerRotation = playerLocationComponent.Rotation;

        var newPosition = new ArcGISPoint(playerPosition.X, playerPosition.Y, locationComponent.Position.Z, playerPosition.SpatialReference);
        locationComponent.Position = newPosition;

        var newRotation = new ArcGISRotation(playerRotation.Heading + northOffset, locationComponent.Rotation.Pitch, locationComponent.Rotation.Roll);
        locationComponent.Rotation = newRotation;


        var cameraComponentLocation = cameraComponent.GetComponent<ArcGISLocationComponent>();

        if (cameraComponentLocation != null)
        {
            cameraComponentLocation.Position = new ArcGISPoint(playerPosition.X, playerPosition.Y, cameraComponentLocation.Position.Z, playerPosition.SpatialReference);
        }
    }
}
