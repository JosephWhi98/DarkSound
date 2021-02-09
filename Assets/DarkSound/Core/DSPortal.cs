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

        /// <summary>
        /// Calculated the amount of obstruction this portal will contribute to audio traveling through it. 
        /// </summary>
        /// <returns></returns>
        public float GetAudioObstructionAmount()
        {
            float obstructionAmount = openCloseAmount * audioObstructionAmount;

            if (obstructionAmount < 0.1f)
                obstructionAmount = 0.1f;

            return obstructionAmount;
        }


        /// <summary>
        /// Gets the closest point to a position within the bounds of the portal. 
        /// </summary>
        /// <param name="position"></param>
        /// <returns>Vector3 - closest point to the given position with the portal bounds</returns>
        public Vector3 GetClosestPointInBounds(Vector3 position)
        {
            return boundsCollider.ClosestPoint(position);
        }

        /// <summary>
        /// Toggles this portal between open and closed states. 
        /// </summary>
        public void ToggleOpenClose()
        {
            if (!opened)
                OpenPortal(1f);
            else
                ClosePortal(1f);
        }

        /// <summary>
        /// Sets portal state to be open over a specified duration. 
        /// </summary>
        /// <param name="duration">Duration to lerp over</param>
        public void OpenPortal(float duration)
        {
            opened = true;

            if (openCloseRoutine != null)
            {
                StopCoroutine(openCloseRoutine);
            }

            openCloseRoutine = StartCoroutine(LerpPortalOpenCloseAmount(0, duration));
        }


        /// <summary>
        /// Sets portal state to be closed over a specified duration. 
        /// </summary>
        /// <param name="duration">Duration to lerp over</param>
        public void ClosePortal(float duration)
        {
            opened = false;

            if (openCloseRoutine != null)
            {
                StopCoroutine(openCloseRoutine);
            }

            openCloseRoutine = StartCoroutine(LerpPortalOpenCloseAmount(1, duration));

        }

        /// <summary>
        /// Lerps portal openClosedValue over duration. 
        /// </summary>
        /// <param name="target">Target to lerp to. </param>
        /// <param name="duration">Time to lerp over. </param>
        /// <returns></returns>
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