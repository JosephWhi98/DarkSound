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

        public bool playOnAwake;

        private Vector3 actualPosition;
        private Vector3 movedPosition;

        public bool useOwnSpatialisation; 

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

        public void Start()
        {
            CheckCurrentRoom();
            CalculatePropagation(true);

            if (playOnAwake)
            {
                audioSource.Play();
            }
        }

        public void OnDisable()
        {
            transform.position = actualPosition;
        }

        public void Update()
        {
            CheckCurrentRoom();
            CalculatePropagation();


            if(useOwnSpatialisation)
                CalculateSpatialisation();
        }

        public void CalculateSpatialisation()
        {
            float DotResult = Vector3.Dot(DSAudioListener.Instance.transform.right, (transform.position - DSAudioListener.Instance.transform.position).normalized);

            float value = 0;

            Debug.Log(DotResult);

            if (DotResult > 0)
            {
                float perc = DotResult / 1f;

                value = perc * 0.8f;
            }
            else if (DotResult < 0)
            {
                float perc = Mathf.Abs(DotResult) / 1f;

                value = perc * -0.8f; 
            }



            audioSource.panStereo = Mathf.Lerp(audioSource.panStereo, value, 15 * Time.deltaTime); 

        }

        /// <summary>
        /// Calculates the propagation of audio from this source. The calculated values are then applied to the source to effect the audio. 
        /// </summary>
        public void CalculatePropagation(bool initialisationCall = false)
        { 
            DSRoom currentListenerRoom = DSAudioListener.Instance.CurrentRoom;

            if (!currentListenerRoom) //General fail safe for if the Listener room isn't set. This should only be an issue during startup.
            {
                currentListenerRoom = DSAudioListener.Instance.GetRoomForPosition(DSAudioListener.Instance.transform.position);
            }

            if (currentListenerRoom == currentRoom)
            {
                audioLowPassFilter.cutoffFrequency = 5000f;

                float propagationDistance = Vector3.Distance(actualPosition, DSAudioListener.Instance.transform.position);
                float newVolume = maxVolume * (falloffCurve.Evaluate(propagationDistance / maxDistance));

                audioSource.volume = !initialisationCall ? Mathf.Lerp(audioSource.volume, newVolume, 5 * Time.deltaTime) : newVolume;
                transform.position = !initialisationCall ? Vector3.Lerp(transform.position, actualPosition, 15 * Time.deltaTime) : actualPosition;

                float lowPassCutOff = GetObstruction(0);

                audioLowPassFilter.cutoffFrequency = !initialisationCall ? Mathf.Lerp(audioLowPassFilter.cutoffFrequency, lowPassCutOff, 5 * Time.deltaTime) : lowPassCutOff;
            }
            else
            {
                path = DSAudioListener.Instance.FindPath(currentRoom, currentListenerRoom);

                DSRoom previousRoom = currentRoom;

                List<DSPortal> portals = new List<DSPortal>();

                foreach (DSRoom room in path)
                {
                    float bestPortalObstructionValue = float.MaxValue;
                    DSPortal bestPortal = null; 

                    foreach (DSRoom.ConnectedRoom connection in previousRoom.connectedRooms)
                    {
                        if (room == connection.room)
                        {
                            float portalObstructionValue = connection.portal.GetAudioObstructionAmount();

                            if (portalObstructionValue < bestPortalObstructionValue)
                            {
                                bestPortalObstructionValue = portalObstructionValue;
                                bestPortal = connection.portal;
                            }
                        }
                    }

                    if (bestPortal != null)
                    {
                        portals.Add(bestPortal);
                    }
                    previousRoom = room;
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

                portalObstruction = Mathf.Clamp01(portalObstruction);

                movedPosition += DSAudioListener.Instance.transform.position;

                movedPosition /= (2 + portals.Count);

                transform.position = !initialisationCall ? Vector3.Lerp(transform.position, movedPosition, 15 * Time.deltaTime) : movedPosition;

                if (debugMode)
                    Debug.DrawLine(startPos, DSAudioListener.Instance.transform.position, Color.red);

                propagationDistance += Vector3.Distance(startPos, DSAudioListener.Instance.transform.position);
                propagationDistance = Mathf.Clamp(propagationDistance, 0, maxDistance);

                float newVolume = maxVolume * (falloffCurve.Evaluate(propagationDistance / maxDistance));

                audioSource.volume = !initialisationCall ? Mathf.Lerp(audioSource.volume, newVolume, 5 * Time.deltaTime) : newVolume;

                float lowPassCutOff = GetObstruction(portalObstruction);

                audioLowPassFilter.cutoffFrequency = !initialisationCall ? Mathf.Lerp(audioLowPassFilter.cutoffFrequency, lowPassCutOff, 5 * Time.deltaTime) : lowPassCutOff;

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




            if (portalObstruction < rayObstructionPercentage)
            {
                return maxLowPass - ((maxLowPass - minLowPass) * portalObstruction);
            }
            else
            { 
                return maxLowPass - ((maxLowPass - minLowPass) * (rayObstructionPercentage));
            }
        }


        /// <summary>
        /// Checks obstruction between listener and source. This value is calculated from 9 individual linecast values. 
        /// </summary>
        /// <returns>Returns a value between 0 and 1 to represent the amount of obstruction </returns>
        public float ObstructionCheck()
        {
            float numberOfRaysObstructed = 0; // Out of 9.

            Vector3 ListenerToEmitterDirection = actualPosition - DSAudioListener.Instance.transform.position;
            Vector3 emitterToListenerDirection = DSAudioListener.Instance.transform.position - actualPosition;

            Vector3 ListenerPosition = DSAudioListener.Instance.transform.position;

            Vector3 emitterPosition = actualPosition;

            Vector3 leftFromListenerDirection = Vector3.Cross(ListenerToEmitterDirection, Vector3.up).normalized;
            Vector3 leftFromListenerPosition = DSAudioListener.Instance.transform.position + (leftFromListenerDirection * 1f);

            Vector3 leftFromEmitterDirection = Vector3.Cross(emitterToListenerDirection, Vector3.up).normalized;
            Vector3 leftFromEmitterPosition = actualPosition + (leftFromEmitterDirection * 1f);

            Vector3 rightFromListenerDirection = -Vector3.Cross(ListenerToEmitterDirection, Vector3.up).normalized;
            Vector3 rightFromListenerPosition = DSAudioListener.Instance.transform.position + (rightFromListenerDirection * 1f);

            Vector3 rightFromEmitterDirection = -Vector3.Cross(emitterToListenerDirection, Vector3.up).normalized;
            Vector3 rightFromEmitterPosition = actualPosition + (rightFromEmitterDirection * 1f);

            numberOfRaysObstructed += ObstructionLinecast(emitterPosition, ListenerPosition);

            numberOfRaysObstructed += ObstructionLinecast(leftFromEmitterPosition, leftFromListenerPosition);
            numberOfRaysObstructed += ObstructionLinecast(rightFromEmitterPosition, leftFromListenerPosition);
            numberOfRaysObstructed += ObstructionLinecast(leftFromEmitterPosition, rightFromListenerPosition);

            numberOfRaysObstructed += ObstructionLinecast(rightFromEmitterPosition, rightFromListenerPosition);
            numberOfRaysObstructed += ObstructionLinecast(emitterPosition, leftFromListenerPosition);
            numberOfRaysObstructed += ObstructionLinecast(emitterPosition, rightFromListenerPosition);

            numberOfRaysObstructed += ObstructionLinecast(leftFromEmitterPosition, ListenerPosition);
            numberOfRaysObstructed += ObstructionLinecast(rightFromEmitterPosition, ListenerPosition);

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

#if UNITY_EDITOR

        public void OnValidate()
        {
            if (!audioSource)
            {
                audioSource = GetComponent<AudioSource>();
            }

            if (audioSource.playOnAwake)
            {
                audioSource.playOnAwake = false;
                playOnAwake = true;

                Debug.LogError("Set AudioSource.PlayOnAwake to false and instead overwrote this value in DSAudioSource", gameObject);
            }
        }

#endif
    }
}
