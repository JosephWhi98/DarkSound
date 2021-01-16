using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace DarkSound
{
    public class DSAudioListener : MonoBehaviour
    {
        public static DSAudioListener Instance;


        public DSRoom CurrentRoom {get{ return currentRoom;}}

        private DSRoom currentRoom; 
        private List<DSRoom> allDSRooms = new List<DSRoom>();


        public void Awake()
        {
            if (!Instance)
            {
                Instance = this;
            }
            else
            {
                enabled = false;
            }
        }

        public void Update()
        {
            currentRoom = GetRoomForPosition(transform.position);


            if (Input.GetKeyDown(KeyCode.E))
            {
                foreach (DSRoom.ConnectedRoom connection in currentRoom.connectedRooms)
                {
                    if (Vector3.Distance(connection.portal.transform.position, transform.position) < 3f)
                    {
                        connection.portal.ToggleOpenClose();
                    }
                }
          
            }
        }

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

        public void AddRoom(DSRoom room)
        {
            if (allDSRooms == null)
            {
                allDSRooms = new List<DSRoom>();
            }

            allDSRooms.Add(room);
        }

        public void RemoveRoom(DSRoom room)
        {
            if (allDSRooms == null)
            {
                allDSRooms = new List<DSRoom>();
            }

            allDSRooms.Remove(room);
        }

        public List<DSRoom> FindPath(DSRoom startingNode, DSRoom endNode)
        {
            DSHeap<DSPathNode> openSet = new DSHeap<DSPathNode>(allDSRooms.Count);
            HashSet<DSPathNode> closedSet = new HashSet<DSPathNode>();

            openSet.Add(startingNode.pathfindingNode);

            while (openSet.Count > 0)
            {
                DSPathNode currentNode = openSet.RemoveFirst();
                closedSet.Add(currentNode);

                foreach (DSRoom.ConnectedRoom connectedRoom in currentNode.thisNode.connectedRooms)
                {
                    DSPathNode neighbour = connectedRoom.room.pathfindingNode;

                    if (closedSet.Contains(neighbour))
                        continue;

                    Vector3 endWorldTarget = (neighbour.thisNode == endNode) ? transform.position : neighbour.worldPosition;
                    Vector3 startWorldTarget = connectedRoom.portal.transform.position;

                    float portalContribution = 10 * (connectedRoom.portal.openCloseAmount * connectedRoom.portal.audioObstructionAmount);

                    float newMovementCostToNeighbour = currentNode.gCost + Vector3.Distance(startWorldTarget, endWorldTarget) + portalContribution;
                   

                    if (newMovementCostToNeighbour < neighbour.gCost || !openSet.Contains(neighbour))
                    {
                        neighbour.gCost = newMovementCostToNeighbour;

                        neighbour.hCost = Vector3.Distance(startWorldTarget, endWorldTarget) + portalContribution;

                        neighbour.parent = currentNode;

                        if (!openSet.Contains(neighbour))
                            openSet.Add(neighbour);
                    }

                }

                if (currentNode == endNode.pathfindingNode)
                {
                    break;
                }
            }

            return RetracePath(startingNode.pathfindingNode, endNode.pathfindingNode);
        }

        private List<DSRoom> RetracePath(DSPathNode startNode, DSPathNode endNode)
        {
            List<DSRoom> path = new List<DSRoom>();
            DSPathNode currentNode = endNode;
            float cost = 0;

            while (currentNode != startNode)
            {
                cost += currentNode.gCost;
                path.Add(currentNode.thisNode);
                currentNode = currentNode.parent;
            }
            path.Reverse();

            Debug.Log(cost);
            return path;
        }


#if UNITY_EDITOR
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