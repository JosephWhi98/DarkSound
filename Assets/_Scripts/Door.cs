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
        closedPosition = transform.localPosition;
    }


    public void Update()
    {
        transform.localPosition = Vector3.Lerp(openOffset + closedPosition, closedPosition, portal.openCloseAmount);// closedPosition + ((1 - portal.openCloseAmount) * openOffset);
    }

}


