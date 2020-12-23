using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DarkSound
{
    [RequireComponent(typeof(AudioSource))]
    public class DSAudioSource : MonoBehaviour
    {
        private AudioSource audioSource;
        private AudioLowPassFilter audioLowPassFilter;
        private DSRoom currentRoom;

        public List<DSRoom> path;

        [Header("General Settings")]
        [Tooltip("Will this audio source move around the scene?")] public bool isDynamic;
        [Tooltip("Setting this value to true will enable propagation calculations on this audio source")] public bool usePropagation;
        [Tooltip("Use this instead of standard audio source falloff")] public AnimationCurve falloffCurve;
        public float maxDistance;
        public float maxVolume;
        public LayerMask obstructionLayerMask;

        Vector3 actualPosition;
        Vector3 movedPosition;

        public void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            audioLowPassFilter = GetComponent<AudioLowPassFilter>();

            actualPosition = transform.position;
        }

        public void OnDisable()
        {
            transform.position = actualPosition;
        }

        public void Update()
        {
            CheckCurrentRoom();

            CalculatePropagation();
        }

        public void CalculatePropagation()
        {
            DSRoom currentPlayerRoom = DSAudioListener.Instance.currentRoom;

            if (currentPlayerRoom == currentRoom)
            {
                audioLowPassFilter.cutoffFrequency = 5000f;

                float propagationDistance = Vector3.Distance(actualPosition, DSAudioListener.Instance.transform.position);
                audioSource.volume = Mathf.Lerp(audioSource.volume, maxVolume * (falloffCurve.Evaluate(propagationDistance / maxDistance)), 5 * Time.deltaTime);
                transform.position = Vector3.Lerp(transform.position, actualPosition, 15 * Time.deltaTime);

                float lowPassCutOff = GetObstruction(0);

                audioLowPassFilter.cutoffFrequency = Mathf.Lerp(audioLowPassFilter.cutoffFrequency, lowPassCutOff, 5 * Time.deltaTime);
            }
            else
            {
                path = DSAudioListener.Instance.FindPath(currentRoom, currentPlayerRoom);

                DSRoom previousRoom = currentRoom;

                List<DSPortal> portals = new List<DSPortal>();

                foreach (DSRoom room in path)
                {
                    
                    foreach (DSRoom.ConnectedRoom connection in previousRoom.connectedRooms)
                    {
                        if (room == connection.room)
                            portals.Add(connection.portal);

                        previousRoom = room;
                    }

                }

                Vector3 startPos = actualPosition;
                float propagationDistance = 0f;
                float portalObstruction = 0f;
                movedPosition = actualPosition;

                foreach (DSPortal v in portals)
                {
                    Debug.DrawLine(startPos, v.transform.position, Color.red);
                    propagationDistance += Vector3.Distance(startPos, v.transform.position);
                    portalObstruction += v.GetAudioObstructionAmount();
                    startPos = v.transform.position;
                    movedPosition += v.transform.position;
                }

                movedPosition += DSAudioListener.Instance.transform.position;

                movedPosition /= (2 + portals.Count);

                transform.position = Vector3.Lerp(transform.position, movedPosition, 15 * Time.deltaTime);

                Debug.DrawLine(startPos, DSAudioListener.Instance.transform.position, Color.red);
                propagationDistance += Vector3.Distance(startPos, DSAudioListener.Instance.transform.position);

                propagationDistance = Mathf.Clamp(propagationDistance, 0, maxDistance);
               

                audioSource.volume = Mathf.Lerp(audioSource.volume, maxVolume * (falloffCurve.Evaluate(propagationDistance / maxDistance)), 5 * Time.deltaTime);

                float lowPassCutOff = GetObstruction(portalObstruction);

                Debug.Log(lowPassCutOff);
                audioLowPassFilter.cutoffFrequency = Mathf.Lerp(audioLowPassFilter.cutoffFrequency, lowPassCutOff, 5 * Time.deltaTime);

            }

        }

        public float GetObstruction(float portalObstruction)
        {
            float minLowPass = 300f;
            float maxLowPass = 5000f;

            float rayObstructionPercentage = ObstructionCheck();

            float portalLowPass = maxLowPass - ((maxLowPass - minLowPass) * portalObstruction);
            float rayLowPass = maxLowPass - ((maxLowPass - minLowPass) * (rayObstructionPercentage));


            return Mathf.Max(portalLowPass, rayLowPass);
        }

        public float ObstructionCheck()
        {
            float numberOfRaysObstructed = 0; // Out of 9.

            Vector3 playerToEmitterDirection = actualPosition - DSAudioListener.Instance.transform.position;
            Vector3 emitterToPlayerDirection = DSAudioListener.Instance.transform.position - actualPosition;

            Vector3 playerPosition = DSAudioListener.Instance.transform.position;

            Vector3 emitterPosition = actualPosition;

            Vector3 leftFromPlayerDirection = Vector3.Cross(playerToEmitterDirection, Vector3.up).normalized;
            Vector3 leftFromPlayerPosition = DSAudioListener.Instance.transform.position + (leftFromPlayerDirection * 1f);

            Vector3 leftFromEmitterDirection = Vector3.Cross(emitterToPlayerDirection, Vector3.up).normalized;
            Vector3 leftFromEmitterPosition = actualPosition + (leftFromEmitterDirection * 1f);

            Vector3 rightFromPlayerDirection = -Vector3.Cross(playerToEmitterDirection, Vector3.up).normalized;
            Vector3 rightFromPlayerPosition = DSAudioListener.Instance.transform.position + (rightFromPlayerDirection * 1f);

            Vector3 rightFromEmitterDirection = -Vector3.Cross(emitterToPlayerDirection, Vector3.up).normalized;
            Vector3 rightFromEmitterPosition = actualPosition + (rightFromEmitterDirection * 1f);

            numberOfRaysObstructed += ObstructionLinecast(emitterPosition, playerPosition);

            numberOfRaysObstructed += ObstructionLinecast(leftFromEmitterPosition, leftFromPlayerPosition);
            numberOfRaysObstructed += ObstructionLinecast(rightFromEmitterPosition, leftFromPlayerPosition);
            numberOfRaysObstructed += ObstructionLinecast(leftFromEmitterPosition, rightFromPlayerPosition);

            numberOfRaysObstructed += ObstructionLinecast(rightFromEmitterPosition, rightFromPlayerPosition);
            numberOfRaysObstructed += ObstructionLinecast(emitterPosition, leftFromPlayerPosition);
            numberOfRaysObstructed += ObstructionLinecast(emitterPosition, rightFromPlayerPosition);

            numberOfRaysObstructed += ObstructionLinecast(leftFromEmitterPosition, playerPosition);
            numberOfRaysObstructed += ObstructionLinecast(rightFromEmitterPosition, playerPosition);

            float obstructionPercentage = numberOfRaysObstructed / 9;

            return obstructionPercentage;
        }

        private int ObstructionLinecast(Vector3 start, Vector3 end)
        {
            RaycastHit hit;

            if (Physics.Linecast(start, end, out hit, obstructionLayerMask, QueryTriggerInteraction.Ignore))
            {
                Debug.DrawLine(start, end, Color.red);

                return 1;
            }
            else
            {
                Debug.DrawLine(start, end, Color.green);

                return 0;
            }
        }

        public void CheckCurrentRoom()
        {
            if (isDynamic || currentRoom == null)
            {
                currentRoom = DSAudioListener.Instance.GetRoomForPosition(transform.position);
            }
        }
    }
}
