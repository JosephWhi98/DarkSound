using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace DarkSound
{
    [RequireComponent(typeof(BoxCollider))]
    public class DSRoom : MonoBehaviour
    {
        [System.Serializable]
        public class ConnectedRoom
        {
            public DSPortal portal; 
            public DSRoom room;

            public ConnectedRoom(DSPortal portal, DSRoom room)
            {
                this.portal = portal;
                this.room = room; 
            }
        }

        private Collider[] boundsColliders;
        public List<ConnectedRoom> connectedRooms = new List<ConnectedRoom>();
        public DSPathNode pathfindingNode;

        public void Awake()
        {
            InitialiseRoom();

            connectedRooms = new List<ConnectedRoom>();
        }

        /// <summary>
        /// Initialises the room, setting it up for pathfinding etc. 
        /// </summary>
        public void InitialiseRoom()
        {
           boundsColliders = GetComponents<Collider>();

           foreach(Collider boundsCollider in boundsColliders)
            {
                boundsCollider.isTrigger = true;
            }

            pathfindingNode = new DSPathNode(transform.position);
            pathfindingNode.thisNode = this;

            if (DSAudioListener.Instance)
            {
                DSAudioListener.Instance.AddRoom(this);
            }
        }

        public void OnDisable()
        {
            if (DSAudioListener.Instance)
            {
                DSAudioListener.Instance.RemoveRoom(this);
            }
        }

        /// <summary>
        /// Adds a connection from this room to another through a specified portal. 
        /// </summary>
        /// <param name="portal">The portal that connects this room to the other. </param>
        /// <param name="room"> The room that this room connects to throught the portal </param>
        public void AddRoomConnection(DSPortal portal, DSRoom room)
        {
            ConnectedRoom connection = new ConnectedRoom(portal, room);

            if (connectedRooms == null)
            {
                new List<ConnectedRoom>();
            }

            connectedRooms.Add(connection);
        }

        /// <summary>
        /// Checks whether a specified world position is in the bounds of this room. 
        /// </summary>
        /// <param name="worldPosition">The position to check</param>
        /// <returns>Whether the specified position is within the bounds of this room. </returns>
        public bool PositionIsInRoomBounds(Vector3 worldPosition)
        {
            foreach (Collider boundsCollider in boundsColliders)
            {
                if (boundsCollider.bounds.Contains(worldPosition))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets the portal that connects this room to the specified room. 
        /// </summary>
        /// <param name="room">The connected room to find a portal for. </param>
        /// <returns>The portal connecting this room to the specified room </returns>
        public DSPortal GetPortal(DSRoom room)
        {
            foreach (ConnectedRoom connection in connectedRooms)
            {
                if (connection.room == room)
                {
                    return connection.portal;
                }
            }

            return null;
        }

    }
}
