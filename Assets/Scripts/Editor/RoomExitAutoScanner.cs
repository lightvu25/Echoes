using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class RoomExitAutoScanner
{
    [MenuItem("Tools/Echoes/Auto-Scan Room Exits")]
    public static void ScanSelectedRooms()
    {
        GameObject[] selectedObjects = Selection.gameObjects;
        int processedCount = 0;

        foreach (GameObject obj in selectedObjects)
        {
            Room room = obj.GetComponent<Room>();
            if (room == null) continue;

            List<RoomExit> foundExits = new List<RoomExit>();

            // Find all children recursively
            Transform[] allChildren = obj.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in allChildren)
            {
                if (child == obj.transform) continue;

                string lowerName = child.name.ToLower();
                if (lowerName.Contains("up"))
                {
                    foundExits.Add(new RoomExit { direction = ExitDirection.Up, exitPoint = child });
                }
                else if (lowerName.Contains("down"))
                {
                    foundExits.Add(new RoomExit { direction = ExitDirection.Down, exitPoint = child });
                }
                else if (lowerName.Contains("left"))
                {
                    foundExits.Add(new RoomExit { direction = ExitDirection.Left, exitPoint = child });
                }
                else if (lowerName.Contains("right"))
                {
                    foundExits.Add(new RoomExit { direction = ExitDirection.Right, exitPoint = child });
                }
            }

            SerializedObject serializedRoom = new SerializedObject(room);
            SerializedProperty exitsProp = serializedRoom.FindProperty("exits");
            
            if (exitsProp != null)
            {
                exitsProp.ClearArray();
                
                for (int i = 0; i < foundExits.Count; i++)
                {
                    exitsProp.InsertArrayElementAtIndex(i);
                    SerializedProperty element = exitsProp.GetArrayElementAtIndex(i);
                    
                    element.FindPropertyRelative("direction").enumValueIndex = (int)foundExits[i].direction;
                    element.FindPropertyRelative("exitPoint").objectReferenceValue = foundExits[i].exitPoint;
                }
                
                serializedRoom.ApplyModifiedProperties();
                room.CalculateExitsMask();
                EditorUtility.SetDirty(room);
                processedCount++;
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[RoomExitAutoScanner] Successfully scanned and updated {processedCount} Room(s).");
    }
}
