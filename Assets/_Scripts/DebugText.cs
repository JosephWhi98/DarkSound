using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DarkSound;
using TMPro;

public class DebugText : MonoBehaviour
{
    public TMP_Text text;
    public DSAudioSource source;


    public void Update()
    {
        transform.LookAt(DSAudioListener.Instance.transform.position);


        if (source.audioSource.enabled)
            text.text = "Falloff Volume: " + source.audioSource.volume + "\nDistance: " + source.debugDistance + "\nObstruction: " + source.debugObstruction;
        else
            text.text = "";
    }
}