using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace DarkSound
{
    public class DSAudioListener : Singleton<DSAudioListener>
    {
        private List<DSRoom> allDSRooms = new List<DSRoom>();

        public DSRoom CurrentRoom {get{ return currentRoom;}}
        private DSRoom currentRoom;

        public void Start()
        {
            currentRoom = GetRoomForPosition(transform.position);
        }

        public void Update()
        {
            currentRoom = GetRoomForPosition(transform.position);
        }

        /// <summary>
        /// Calculates the current room based on the given position
        /// </summary>
        /// <param name="worldPosition">Current position in world space</param>
        /// <returns>The room that this position is in</returns>
        public DSRoom GetRoomForPosition(Vector3 worldPosition)
        {
            foreach (DSRoom dsRoom in allDSRooms)
            { 
                if (dsRoom.PositionIsInRoomBounds(worldPosition))
                {
                    return dsRoom;
                }
            }

            return currentRoom;
        }

        /// <summary>
        /// Adds a room to the list of all rooms in the scene
        /// </summary>
        /// <param name="room">The room to add</param>
        public void AddRoom(DSRoom room)
        {
            if (allDSRooms == null)
            {
                allDSRooms = new List<DSRoom>();
            }

            allDSRooms.Add(room);
        }

        /// <summary>
        /// Removes a room from the list of all rooms in the scene
        /// </summary>
        /// <param name="room">The current room to remove</param>
        public void RemoveRoom(DSRoom room)
        {
            if (allDSRooms == null)
            {
                allDSRooms = new List<DSRoom>();
            }

            allDSRooms.Remove(room);
        }


        /// <summary>
        /// Clears the list of rooms. This needs to be called on scene load if the listener is persistent!
        /// </summary>
        public void ClearRoomList()
        {
            allDSRooms = new List<DSRoom>();
        }


        /// <summary>
        /// Finds the optimal path from one specified room to another. 
        /// </summary>
        /// <param name="startingNode">Room to start path</param>
        /// <param name="endNode">Room to end path</param>
        /// <param name="nonOptimalPath">When true the portal contrubution will not be considered.</param>
        /// <returns>A list of rooms that make up the optimal path</returns>
        public List<DSRoom> FindPath(DSRoom startingNode, DSRoom endNode, bool nonOptimalPath = false)
        {
            DSHeap<DSPathNode> openSet = new DSHeap<DSPathNode>(allDSRooms.Count);
            HashSet<DSPathNode> closedSet = new HashSet<DSPathNode>();

            openSet.AddItem(startingNode.pathfindingNode);

            while (openSet.Count > 0)
            {
                DSPathNode currentNode = openSet.RemoveFirstItem();
                closedSet.Add(currentNode);

                foreach (DSRoom.ConnectedRoom connectedRoom in currentNode.thisNode.connectedRooms)
                {
                    DSPathNode neighbour = connectedRoom.room.pathfindingNode;

                    if (closedSet.Contains(neighbour))
                        continue;

                    Vector3 endWorldTarget = (neighbour.thisNode == endNode) ? transform.position : neighbour.worldPosition;
                    Vector3 startWorldTarget = connectedRoom.portal.transform.position;

                    float portalContribution = nonOptimalPath ? 0 : 15 * (connectedRoom.portal.GetAudioObstructionAmount());

                    float newMovementCostToNeighbour = currentNode.gCost + Vector3.Distance(startWorldTarget, endWorldTarget) + portalContribution;
                   

                    if (newMovementCostToNeighbour < neighbour.gCost || !openSet.Contains(neighbour))
                    {
                        neighbour.gCost = newMovementCostToNeighbour;

                        neighbour.hCost = Vector3.Distance(startWorldTarget, endWorldTarget) + portalContribution;

                        neighbour.parent = currentNode;
                        neighbour.parentPortal = connectedRoom.portal;

                        if (!openSet.Contains(neighbour))
                            openSet.AddItem(neighbour);
                    }

                }

                if (currentNode == endNode.pathfindingNode)
                {
                    break;
                }
            }

            return RetracePath(startingNode.pathfindingNode, endNode.pathfindingNode);
        }

        /// <summary>
        /// Retraces through nodoes from final node to return the final path. 
        /// </summary>
        /// <param name="startNode">Node to start path</param>
        /// <param name="endNode">Node to end path</param>
        /// <returns>The optimal path from startNode to endNode</returns>
        private List<DSRoom> RetracePath(DSPathNode startNode, DSPathNode endNode)
        {
            List<DSRoom> path = new List<DSRoom>();
            DSPathNode currentNode = endNode;
            float cost = 0;

            while (currentNode != startNode && currentNode != null)
            {
                cost += currentNode.gCost;
                path.Add(currentNode.thisNode);
                currentNode = currentNode.parent;
            }
            path.Reverse();

            return path;
        }


        //====================/EDITOR/============================//

#if UNITY_EDITOR
        /// <summary>
        /// Logs errors on validate. 
        /// </summary>
        public void OnValidate()
        {
            if (!GetComponent<AudioListener>())
            {
                Debug.LogWarning("DSAudioListener assigned on a gameObject that doesn't have an AudioListerner component! This will prevent DarkSound from working correctly.");
            }
        }
#endif
    }
}