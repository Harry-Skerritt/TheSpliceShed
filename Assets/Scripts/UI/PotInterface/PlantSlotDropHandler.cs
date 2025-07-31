using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;


public class PlantSlotDropHandler : MonoBehaviour, IDropHandler
{
    
    [Tooltip("Reference to the PlantSomethingUI")]
    public PlantSomethingUI plantSomethingUI;
    
    private CanvasGroup canvasGroup;

    void Awak()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        canvasGroup.blocksRaycasts = true;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (plantSomethingUI == null)
        {
            Debug.LogError("PlantSlotDropHandler: 'plantSomethingUI' reference is missing! Please assign it in the Inspector.");
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.ClearDragState();
            }
            return;
        }
        
        plantSomethingUI.HandleItemDropIntoSlot(eventData);
    }
    
}