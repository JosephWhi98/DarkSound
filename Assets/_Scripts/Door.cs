using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DarkSound;

public class Door : MonoBehaviour
{
    public Vector3 closedPosition;
    public Vector3 openOffset;
    public DSPortal portal;



    public void Awake()
    {
        closedPosition = transform.position;
    }


    public void Update()
    {
        transform.position = closedPosition + ((1 - portal.openCloseAmount) * openOffset);
    }

}


