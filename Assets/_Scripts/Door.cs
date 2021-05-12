using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DarkSound;

public class Door : MonoBehaviour
{
    public Vector3 closedPosition;
    public Vector3 openOffset;
    public DSPortal portal;

    public bool playerInRange = false; 

    public void Awake()
    {
        closedPosition = transform.localPosition;
    }

    public void Update()
    {
        transform.localPosition = Vector3.Lerp(openOffset + closedPosition, closedPosition, portal.openCloseAmount);// closedPosition + ((1 - portal.openCloseAmount) * openOffset);


        if (Vector3.Distance(portal.GetClosestPointInBounds(DSAudioListener.Instance.transform.position), DSAudioListener.Instance.transform.position) <= 1f)
        {
            playerInRange = true;

            if (Input.GetKeyDown(KeyCode.E))
            {
                OpenClosePortal();
            }
        }
        else
        {
            playerInRange = false;
        }
    }

    public void OpenClosePortal()
    { 
        portal.ToggleOpenClose();
    }
}


