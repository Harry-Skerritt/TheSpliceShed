using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraZoomController : MonoBehaviour
{
    public static CameraZoomController Instance { get; private set; }
    
    [Header("Zoom Settings")]
    [Tooltip("The target Orthographic Size when the camera is zoomed in.")]
    [SerializeField] private float zoomOrthographicSize = 3f;
    [Tooltip("The duration of the zoom transition in seconds.")]
    [SerializeField] private float zoomDuration = 1.0f;
    [SerializeField] private float yOffset = 0.6f;
    
    private float originalOrthographicSize;
    private Vector3 originalPosition;
    private bool isZooming = false;
        
    private Camera mainCamera;

    private void Awake()
    {
        if (Instance == null)
        { 
            Instance = this;   
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        
        mainCamera = GetComponent<Camera>();
        
        // Ensure the camera is set to Orthographic mode.
        if (mainCamera != null && !mainCamera.orthographic)
        {
            Debug.LogError("CameraZoomController: The camera must be in Orthographic mode!");
        }
    }

    private void Start()
    {
        if (mainCamera != null)
        {
            originalOrthographicSize = mainCamera.orthographicSize;
            originalPosition = transform.position;
        }
        else
        {
            Debug.LogError("CameraZoomController: No main camera found!");
        }
    }


    public void ZoomIn(GameObject target)
    {
        if (isZooming)
        {
            StopAllCoroutines();
        }

        if (target != null)
        {
            Vector3 targetPosition = target.transform.position;
            targetPosition = new Vector3(targetPosition.x, targetPosition.y + yOffset, targetPosition.z);
            StartCoroutine(AnimateZoom(targetPosition, zoomOrthographicSize));
        }
        else
        {
            Debug.LogWarning("Target object is null. Cannot zoom in.");
        }
    }

    public void ZoomOut()
    {
        if (isZooming)
        {
            StopAllCoroutines();
        }

        StartCoroutine(AnimateZoom(originalPosition, originalOrthographicSize));
    }
    
    private IEnumerator AnimateZoom(Vector3 targetPosition, float targetOrthographicSize)
    {
        isZooming = true;
        float elapsedTime = 0f;

        Vector3 startPosition = transform.position;
        float startOrthographicSize = mainCamera.orthographicSize;

        while (elapsedTime < zoomDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / zoomDuration;
            t = Mathf.SmoothStep(0f, 1f, t);
            
            transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            mainCamera.orthographicSize = Mathf.Lerp(startOrthographicSize, targetOrthographicSize, t);

            yield return null; 
        }
        Vector3 finalPositon = targetPosition;
        finalPositon.z = startPosition.z;
        transform.position = finalPositon;
        mainCamera.orthographicSize = targetOrthographicSize;

        isZooming = false;
    }
}
