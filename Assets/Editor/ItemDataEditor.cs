using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ItemData))]
public class ItemDataEditor : Editor
{
    // Use serialized properties to handle Undo/Redo and other editor features
    SerializedProperty itemName;
    SerializedProperty icon;
    SerializedProperty itemType;
    SerializedProperty growthSprites;
    SerializedProperty isStackable;
    SerializedProperty maxStackSize;
    SerializedProperty requiredPotSize;
    SerializedProperty growthTime;
    SerializedProperty drainRate;
    SerializedProperty outputItemData;

    private void OnEnable()
    {
        // Find all the properties in the ItemData script
        itemName = serializedObject.FindProperty("itemName");
        icon = serializedObject.FindProperty("icon");
        itemType = serializedObject.FindProperty("itemType");
        growthSprites = serializedObject.FindProperty("growthSprites");
        isStackable = serializedObject.FindProperty("isStackable");
        maxStackSize = serializedObject.FindProperty("maxStackSize");
        requiredPotSize = serializedObject.FindProperty("requiredPotSize");
        growthTime = serializedObject.FindProperty("growthTime");
        drainRate = serializedObject.FindProperty("drainRate");
        outputItemData = serializedObject.FindProperty("outputItemData");
    }

    public override void OnInspectorGUI()
    {
        // This tells the editor to check for changes to the scriptable object.
        serializedObject.Update();

        // Draw the standard properties that are always visible
        EditorGUILayout.PropertyField(itemName);
        EditorGUILayout.PropertyField(icon);
        EditorGUILayout.PropertyField(itemType);
        EditorGUILayout.PropertyField(isStackable);
        EditorGUILayout.PropertyField(maxStackSize);

        // Check if the itemType is ItemType.Seed
        ItemType currentItemType = (ItemType)itemType.enumValueIndex;
        if (currentItemType == ItemType.Seed)
        {
            // If it's a seed, show the seed-specific properties.
            EditorGUILayout.PropertyField(outputItemData);
        }
        else
        {
            // If it's not a seed, show other general properties
            EditorGUILayout.PropertyField(growthSprites);
            EditorGUILayout.PropertyField(growthTime);
            EditorGUILayout.PropertyField(drainRate);
            EditorGUILayout.PropertyField(requiredPotSize);
        }

        // Apply all modified properties. This saves the changes.
        serializedObject.ApplyModifiedProperties();
    }
}