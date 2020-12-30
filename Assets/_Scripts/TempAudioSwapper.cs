using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TempAudioSwapper : MonoBehaviour
{
    public AudioSource dsSource;
    public AudioReverbFilter reverbds;
    public AudioSource defaultSource;
    public AudioReverbFilter reverbdef;



    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            SwapSources();
        }
    }


    public void SwapSources()
    {
        AudioSource activeSource = dsSource.enabled ? dsSource : defaultSource;
        AudioSource disabledSource = dsSource.enabled ? defaultSource : dsSource;


        reverbdef.enabled = false;
        reverbds.enabled = false;


        disabledSource.time = activeSource.time;
        activeSource.enabled = false;
        disabledSource.enabled = true;

        reverbdef.enabled = true;
        reverbds.enabled = true;
        disabledSource.Play();
    }
}
