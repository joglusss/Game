using System;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MeshCombiner)), CanEditMultipleObjects]
public class MeshCombinerEditor : Editor
{
    private Action CombineMeshMethod;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (GUILayout.Button("Test Button"))
        {
            MeshCombiner _script = target as MeshCombiner;
            _script.Start();
        }
    }
}
