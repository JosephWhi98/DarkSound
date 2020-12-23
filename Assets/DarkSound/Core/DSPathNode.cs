using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DarkSound
{
    public class DSPathNode : IHeapItem<DSPathNode>
    {
        public Vector3 worldPosition;
        public float gCost;
        public float hCost;
        public DSRoom thisNode;
        public DSPathNode parent;
        int heapIndex;


        public DSPathNode(Vector3 _worldPos)
        {
            worldPosition = _worldPos;
        }

        public float fCost
        {
            get
            {
                return gCost + hCost;
            }
        }

        public int HeapIndex
        {
            get
            {
                return heapIndex;
            }
            set
            {
                heapIndex = value;
            }
        }

        public int CompareTo(DSPathNode nodeToCompare)
        {
            int compare = fCost.CompareTo(nodeToCompare.fCost);

            if (compare == 0)
            {
                compare = hCost.CompareTo(nodeToCompare.hCost);
            }

            return -compare;
        }

    }

}