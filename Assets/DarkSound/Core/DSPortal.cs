using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace DarkSound
{
    [RequireComponent(typeof(BoxCollider))]
    public class DSPortal : MonoBehaviour
    {
        public DSRoom firstRoom;
        public DSRoom secondRoom;

        [Range(0, 1)] public float openCloseAmount;
        [Range(0, 1)] public float audioObstructionAmount;

        private Coroutine openCloseRoutine;


        private Collider boundsCollider;

        public bool opened = true;

        public void Start()
        {
            firstRoom.AddRoomConnection(this, secondRoom);
            secondRoom.AddRoomConnection(this, firstRoom);

            if (boundsCollider = GetComponent<Collider>())
            {
                boundsCollider.isTrigger = true; 
            }

        }

        public float GetAudioObstructionAmount()
        {
            float obstructionAmount = openCloseAmount * audioObstructionAmount;

            if (obstructionAmount < 0.1f)
                obstructionAmount = 0.1f;

            return obstructionAmount;
        }

        public void ToggleOpenClose()
        {
            if (!opened)
                OpenPortal(1f);
            else
                ClosePortal(1f);
        }

        public void OpenPortal(float duration)
        {
            opened = true;

            if (openCloseRoutine != null)
            {
                StopCoroutine(openCloseRoutine);
            }

            openCloseRoutine = StartCoroutine(LerpPortalOpenCloseAmount(0, duration));
        }

        public void ClosePortal(float duration)
        {
            opened = false;

            if (openCloseRoutine != null)
            {
                StopCoroutine(openCloseRoutine);
            }

            openCloseRoutine = StartCoroutine(LerpPortalOpenCloseAmount(1, duration));

        }


        public IEnumerator LerpPortalOpenCloseAmount(float target, float duration)
        {
            float start = openCloseAmount;

            for (float t = 0.0f; t < duration; t += Time.deltaTime)
            {
                openCloseAmount = Mathf.Lerp(start, target, t / duration);
                yield return null;
            }
        }
    }
}