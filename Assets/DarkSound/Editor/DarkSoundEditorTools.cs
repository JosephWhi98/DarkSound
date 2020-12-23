using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using DarkSound;

public class DarkSoundEditorTools : EditorWindow
{
    [MenuItem("GameObject/Audio/DarkSound/DSAudioSource")]
    private static void CreateDarkSoundAudioSource()
    {
        GameObject audioSource = new GameObject("DSAudioSource");
        audioSource.AddComponent<DSAudioSource>();
    }

    [MenuItem("GameObject/Audio/DarkSound/DSRoom")]
    private static void CreateDarkSoundAudioRoom()
    {
        GameObject audioSource = new GameObject("DSRoom");
        audioSource.AddComponent<DSRoom>();
    }

    [MenuItem("GameObject/Audio/DarkSound/DSPortal")]
    private static void CreateDarkSoundAudioPortal()
    {
        GameObject audioSource = new GameObject("DSPortal");
        audioSource.AddComponent<DSPortal>();
    }
}
