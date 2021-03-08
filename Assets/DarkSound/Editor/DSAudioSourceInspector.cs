using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using DarkSound;

namespace DarkSound.Editor
{
    [CustomEditor(typeof(DSAudioSource))]
    public class DSAudioSourceInspector : UnityEditor.Editor
    {
        Texture logo;


        public override void OnInspectorGUI()
        {
            logo = UnityEditor.AssetDatabase.LoadAssetAtPath("Assets/DarkSound/DSResources/Logo.png", typeof(Texture)) as Texture;

            GUILayoutOption[] options = new GUILayoutOption[]
            {
                GUILayout.MaxWidth(200),
            };

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(logo, options);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            DrawDefaultInspector();
        }
    }


    [CustomEditor(typeof(DSAudioListener))]
    public class DSAudioListenerInspector : UnityEditor.Editor
    {
        Texture logo;


        public override void OnInspectorGUI()
        {
            logo = UnityEditor.AssetDatabase.LoadAssetAtPath("Assets/DarkSound/DSResources/Logo.png", typeof(Texture)) as Texture;

            GUILayoutOption[] options = new GUILayoutOption[]
            {
                GUILayout.MaxWidth(200),
            };

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(logo, options);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            DrawDefaultInspector();
        }
    }
}

