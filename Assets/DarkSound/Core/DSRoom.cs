using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace DarkSound
{
    [RequireComponent(typeof(BoxCollider))]
    public class DSRoom : MonoBehaviour
    {
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
        }

        public void InitialiseRoom()
        {
           boundsColliders = GetComponents<Collider>();

           foreach(Collider boundsCollider in boundsColliders)
            {
                boundsCollider.isTrigger = true;
            }

            pathfindingNode = new DSPathNode(transform.position);
            pathfindingNode.thisNode = this;
        }

        public void Start()
        {
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

        //Adds a new connection to this room.
        public void AddRoomConnection(DSPortal portal, DSRoom room)
        {
            ConnectedRoom connection = new ConnectedRoom(portal, room);

            if (connectedRooms == null)
            {
                new List<ConnectedRoom>();
            }

            connectedRooms.Add(connection);
        }

        //Return whether a global position is within the bounds of this room
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

        //Return the portal that connects this room to the given room.
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
