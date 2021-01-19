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

        private Vector3 actualPosition;
        private Vector3 movedPosition;

        [Tooltip("Should this source draw debug lines and log values to the console?"),SerializeField] public bool debugMode; 

        public void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            audioLowPassFilter = GetComponent<AudioLowPassFilter>();

            actualPosition = transform.position;

#if !UNITY_EDITOR
            debugMode = false; 
#endif
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

        /// <summary>
        /// Calculates the propagation of audio from this source. The calculated values are then applied to the source to effect the audio. 
        /// </summary>
        public void CalculatePropagation()
        { 
            DSRoom currentPlayerRoom = DSAudioListener.Instance.CurrentRoom;

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
                        {
                            portals.Add(connection.portal);
                        }

                        previousRoom = room;
                    }

                }

                Vector3 startPos = actualPosition;
                float propagationDistance = 0f;
                float portalObstruction = 0f;
                movedPosition = actualPosition;

                foreach (DSPortal v in portals)
                {
                    if(debugMode)
                        Debug.DrawLine(startPos, v.transform.position, Color.red);

                    propagationDistance += Vector3.Distance(startPos, v.transform.position);
                    portalObstruction += v.GetAudioObstructionAmount();
                    startPos = v.transform.position;
                    movedPosition += v.transform.position;
                }

                movedPosition += DSAudioListener.Instance.transform.position;

                movedPosition /= (2 + portals.Count);

                transform.position = Vector3.Lerp(transform.position, movedPosition, 15 * Time.deltaTime);

                if (debugMode)
                    Debug.DrawLine(startPos, DSAudioListener.Instance.transform.position, Color.red);

                propagationDistance += Vector3.Distance(startPos, DSAudioListener.Instance.transform.position);
                propagationDistance = Mathf.Clamp(propagationDistance, 0, maxDistance);
               

                audioSource.volume = Mathf.Lerp(audioSource.volume, maxVolume * (falloffCurve.Evaluate(propagationDistance / maxDistance)), 5 * Time.deltaTime);

                float lowPassCutOff = GetObstruction(portalObstruction);

                if (debugMode)
                    Debug.Log(lowPassCutOff);

                audioLowPassFilter.cutoffFrequency = Mathf.Lerp(audioLowPassFilter.cutoffFrequency, lowPassCutOff, 5 * Time.deltaTime);

            }

        }

        /// <summary>
        /// Returns the maximum obstruction value, whether that be from the linecasts of the portals themselves. 
        /// </summary>
        /// <param name="portalObstruction"> Obstruction level given from portal traversal </param>
        /// <returns> The maximum obstruction value </returns>
        public float GetObstruction(float portalObstruction)
        {
            float minLowPass = 300f;
            float maxLowPass = 5000f;

            float rayObstructionPercentage = ObstructionCheck();

            float portalLowPass = maxLowPass - ((maxLowPass - minLowPass) * portalObstruction);
            float rayLowPass = maxLowPass - ((maxLowPass - minLowPass) * (rayObstructionPercentage));


            return Mathf.Max(portalLowPass, rayLowPass);
        }


        /// <summary>
        /// Checks obstruction between listener and source. This value is calculated from 9 individual linecast values. 
        /// </summary>
        /// <returns>Returns a value between 0 and 1 to represent the amount of obstruction </returns>
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


        /// <summary>
        /// Performs a linecast from point start to point end to check for obstructions. 
        /// </summary>
        /// <param name="start">Start position of linecast</param>
        /// <param name="end">End position of linecast </param>
        /// <returns> Returns value to represent obstruction, 1 == no obstruction, 0 == Obstructed </returns>
        private int ObstructionLinecast(Vector3 start, Vector3 end)
        {
            RaycastHit hit;

            if (Physics.Linecast(start, end, out hit, obstructionLayerMask, QueryTriggerInteraction.Ignore))
            {
                if (debugMode)
                    Debug.DrawLine(start, end, Color.red);

                return 1;
            }
            else
            {
                if (debugMode)
                    Debug.DrawLine(start, end, Color.green);

                return 0;
            }
        }

        ///<summary>
        /// Checks and updated the current room position of the audioSource, if the source is dynamic this is checked and updated
        /// each frame. 
        ///</summary>
        public void CheckCurrentRoom()
        {
            if (isDynamic || currentRoom == null)
            {
                currentRoom = DSAudioListener.Instance.GetRoomForPosition(transform.position);
            }
        }
    }
}
