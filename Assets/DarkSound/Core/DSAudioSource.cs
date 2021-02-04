using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DarkSound
{
    [RequireComponent(typeof(AudioSource))]
    public class DSAudioSource : MonoBehaviour
    {
        [HideInInspector] public AudioSource audioSource;
        private AudioLowPassFilter audioLowPassFilter;
        public DSRoom currentRoom;

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
        [HideInInspector] public float debugDistance;
        [HideInInspector] public float debugObstruction;

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

        /// <summary>
        /// Calculates the direction from the audioSource to the listener and uses this position to evaluate the stereo pan of the audio.
        /// This is experimental and using the default 3D audio settings may be preferable based on the results. 
        /// </summary>
        public void CalculateSpatialisation()
        {
            float DotResult = Vector3.Dot(DSAudioListener.Instance.transform.right, (transform.position - DSAudioListener.Instance.transform.position).normalized);

            float value = 0;

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

            if (!currentRoom)
                CheckCurrentRoom();

            if (currentListenerRoom == currentRoom) //Calculates propagation when the audioListener and the audioSource are in the same room.
            {
                //TODO - Currently, audio played in the same room is 100% clear, this needs to be effected by the direct obstruction from the audio rays. 
                
                audioLowPassFilter.cutoffFrequency = 5000f;

                float propagationDistance = Vector3.Distance(actualPosition, DSAudioListener.Instance.transform.position);

                float newVolume = maxVolume * (falloffCurve.Evaluate(propagationDistance / maxDistance));


                audioSource.volume = !initialisationCall ? Mathf.Lerp(audioSource.volume, newVolume, 5 * Time.deltaTime) : newVolume;
                transform.position = !initialisationCall ? Vector3.Lerp(transform.position, actualPosition, 15 * Time.deltaTime) : actualPosition;

                float lowPassCutOff = GetObstruction(0);

                audioLowPassFilter.cutoffFrequency =  lowPassCutOff;

                debugDistance = propagationDistance;
            }
            else
            {
                //TODO - Clean up this code. 

                //Optimise this, no need to perform these calculations on audioSources that arent playing, also if we cache the results we may be able to use the same path for multiple frames at a time.
                //also, if theres a maximum direct range on the audio that can act as a cut off point for this calculation. This range would need to be bigger so that we still get the full benefit of the
                //improved fall-off calculations.

                List<DSRoom> optimalPath = DSAudioListener.Instance.FindPath(currentRoom, currentListenerRoom);

                DSRoom previousRoom = currentRoom;

                List<DSPortal> portals = new List<DSPortal>();

                foreach (DSRoom room in optimalPath)
                {
                    float bestPortalObstructionValue = float.MaxValue;
                    float closestDistanceToListener = float.MaxValue;
                    DSPortal bestPortal = null;


                    foreach (DSRoom.ConnectedRoom connection in previousRoom.connectedRooms)
                    {
                        if (room == connection.room)
                        {
                            float portalObstructionValue = connection.portal.GetAudioObstructionAmount();
                            float distanceTolistener = Vector3.Distance(connection.portal.transform.position, DSAudioListener.Instance.transform.position);

                            if (portalObstructionValue < bestPortalObstructionValue)
                            {
                                bestPortalObstructionValue = portalObstructionValue;
                                closestDistanceToListener = distanceTolistener;
                                bestPortal = connection.portal;
                            }
                            else if (portalObstructionValue == bestPortalObstructionValue)
                            {
                                if (distanceTolistener < closestDistanceToListener)
                                {
                                    bestPortalObstructionValue = portalObstructionValue;
                                    closestDistanceToListener = distanceTolistener;
                                    bestPortal = connection.portal;
                                }
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

                foreach (DSPortal pathPortal in portals)
                {
                    Vector3 closestPointInBounds = pathPortal.GetClosestPointInBounds(startPos);

                    if (debugMode)
                        Debug.DrawLine(startPos, closestPointInBounds, Color.blue);


                    propagationDistance += Vector3.Distance(startPos, closestPointInBounds) + (pathPortal.openCloseAmount * pathPortal.audioObstructionAmount * 15f);
                    portalObstruction += pathPortal.GetAudioObstructionAmount();
                    startPos = closestPointInBounds;
                }

                portalObstruction = Mathf.Clamp01(portalObstruction);

                //movedPosition += DSAudioListener.Instance.transform.position;

                //movedPosition /= (2 + portals.Count);


                if (portals.Count >= 2)
                    movedPosition = (portals[portals.Count - 1].transform.position + portals[portals.Count - 2].transform.position) / 2f;
                else if (portals.Count >= 1)
                    movedPosition = (portals[portals.Count - 1].transform.position + actualPosition) / 2f;
                else
                    movedPosition = actualPosition; 

                transform.position = !initialisationCall ? Vector3.Lerp(transform.position, movedPosition, 5 * Time.deltaTime) : movedPosition;

                if (debugMode)
                    Debug.DrawLine(startPos, DSAudioListener.Instance.transform.position, Color.blue);

                propagationDistance += Vector3.Distance(startPos, DSAudioListener.Instance.transform.position);
                propagationDistance = Mathf.Clamp(propagationDistance, 0, maxDistance);
               
                float newVolume = maxVolume * (falloffCurve.Evaluate(propagationDistance / maxDistance));

                audioSource.volume = !initialisationCall ? Mathf.Lerp(audioSource.volume, newVolume, 2 * Time.deltaTime) : newVolume;

                float lowPassCutOff = GetObstruction(portalObstruction);

                audioLowPassFilter.cutoffFrequency = !initialisationCall ? Mathf.Lerp(audioLowPassFilter.cutoffFrequency, lowPassCutOff, 2 * Time.deltaTime) : lowPassCutOff;

                debugDistance = propagationDistance;

            }

        }

        /// <summary>
        /// Returns the maximum obstruction value, whether that be from the linecasts of the portals themselves. 
        /// </summary>
        /// <param name="portalObstruction"> Obstruction level given from portal traversal </param>
        /// <returns> The maximum obstruction value </returns>
        public float GetObstruction(float portalObstruction)
        {
            //TODO - Find ways to optimise this, maybe alternate this so that it doesn't calculate every frame per audioSource. Also, if the range from the source is too far, it's likely that no value will be
            //added by performing these raycasts rather than just using the pathfinding.

            float minLowPass = 300f;
            float maxLowPass = 5000f;

            float rayObstructionPercentage = ObstructionCheck();

            debugObstruction = 0.5f * (rayObstructionPercentage + portalObstruction); ;

            float combinedObstruction = 0.5f * (rayObstructionPercentage + portalObstruction);

            return maxLowPass - ((maxLowPass - minLowPass) * combinedObstruction);
            
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
            //Potentially change this to use a raycastAll rather than linecast. Can more accurately propagate through walls etc, but will result in a significant performance hit. Additionally, this
            //is already our largest performance drain. Could maybe use 9 linecasts + 1 direct raycastAll to reduce the hit of this, however it would increase set up complexity as to get the maximum benefit
            // from this would require an additional script on wall collider to hold the properties of walls/materials. 

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
