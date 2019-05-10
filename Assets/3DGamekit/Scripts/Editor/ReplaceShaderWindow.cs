using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using UnityEditor;
using UnityEngine;

public class ReplaceShaderWindow : EditorWindow
{
    enum TargetMode { Selection, Scene, Project }

    TargetMode targetMode = TargetMode.Project;

    Shader sourceShader;
    Shader targetShader;
    
    [MenuItem("Tools/Replace Shader Window")]
    public static void OpenWindow()
    {
        GetWindow<ReplaceShaderWindow>();
    }

    void OnGUI()
    {
        targetMode = (TargetMode) EditorGUILayout.EnumPopup("Target mode", targetMode);

        sourceShader = EditorGUILayout.ObjectField("Source Shader", sourceShader, typeof(Shader), false) as Shader;
        targetShader = EditorGUILayout.ObjectField("Target Shader", targetShader, typeof(Shader), false) as Shader;

        if (GUILayout.Button("Replace"))
        {
            if (sourceShader == null && targetShader == null) return;
            if (targetMode == TargetMode.Selection && Selection.objects.Length == 0) return;
            
            List<Material> targets = new List<Material>();
            
            switch (targetMode)
            {
                    case TargetMode.Selection:
                        targets.AddRange( // Add
                            Selection.GetFiltered<Renderer>( SelectionMode.Unfiltered ) // All Selected renderers
                            .Select( m => m.materials ) // all their materials
                            .Aggregate( new List<Material>(), (current, next) => // aggretate to a big list
                                {
                                    current.AddRange(next.ToList());
                                    return current;
                                })
                            .Where( m => !targets.Contains(m) ) // remove doubles
                            );
                        
                        targets.AddRange( // Add
                            Selection.GetFiltered<Material>(SelectionMode.Unfiltered) // All Selected materials
                            .Where( m => !targets.Contains(m) ) // remove doubles
                            );
                        break;
                    case TargetMode.Scene:
                        targets.AddRange( // Add
                            FindObjectsOfType<Renderer>() // All renderers in the scene
                                .Select( m => m.materials ) // all their materials
                                .Aggregate( new List<Material>(), (current, next) => // aggretate to a big list
                                {
                                    current.AddRange(next.ToList());
                                    return current;
                                })
                                .Where( m => !targets.Contains(m) ) // remove doubles
                        );
                        break;
                    case TargetMode.Project:
                        targets.AddRange( // Add
                            AssetDatabase.FindAssets("t:Material") // All materials in the project
                                .Select( a => AssetDatabase.LoadAllAssetsAtPath( AssetDatabase.GUIDToAssetPath(a) ).Select( a2 => (Material) a2 ) ) // Get all materials in found assets
                                .Aggregate( new List<Material>(), (current, next) => // aggretate to a big list
                                {
                                    current.AddRange(next.ToList());
                                    return current;
                                })
                                .Where( m => !targets.Contains(m) ) // remove doubles
                        );
                        break;
            }

            targets.Distinct();

            foreach (var target in targets)
                ReplaceShader(target, sourceShader, targetShader);
            
            Debug.Log( targets.Aggregate("Replaced: ", (current, next) =>
            {
                current += Environment.NewLine + next.name;
                return current;
            } ) );
        }
    }

    void ReplaceShader(Material material, Shader sourceShader, Shader targetShader)
    {
        if (sourceShader == null && targetShader == null) return;
        if (material.shader != sourceShader) return;

        material.shader = targetShader;
    }
}
