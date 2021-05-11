using UnityEngine;
using System.Collections;
using System;

namespace DarkSound
{
    public class DSHeap<T> where T : IHeapItem<T>
    {

        T[] items;
        int currentItemCount;

        public int Count
        {
            get
            {
                return currentItemCount;
            }
        }

        /// <summary>
        /// Creates a heap of the specified size. 
        /// </summary>
        /// <param name="maxHeapSize"></param>
        public DSHeap(int maxHeapSize)
        {
            items = new T[maxHeapSize];
        }


        /// <summary>
        /// Adds a new item to the heap. 
        /// </summary>
        /// <param name="item"></param>
        public void Add(T item)
        {
            item.HeapIndex = currentItemCount;
            items[currentItemCount] = item;
            SortUp(item);
            currentItemCount++;
        }

        /// <summary>
        /// Removes the first item in the heap
        /// </summary>
        /// <returns>the first time in the heap</returns>
        public T RemoveFirst()
        {
            T firstItem = items[0];
            currentItemCount--;
            items[0] = items[currentItemCount];
            items[0].HeapIndex = 0;
            SortDown(items[0]);
            return firstItem;
        }

        /// <summary>
        /// Updates item in the heap
        /// </summary>
        /// <param name="item"></param>
        public void UpdateItem(T item)
        {
            SortUp(item);
        }

        /// <summary>
        /// Checks if item is contained in the heap. 
        /// </summary>
        /// <param name="item"></param>
        /// <returns>if item is contained in the heap</returns>
        public bool Contains(T item)
        {
            return Equals(items[item.HeapIndex], item);
        }

        /// <summary>
        /// Sorts the heap down.
        /// </summary>
        /// <param name="item"></param>
        void SortDown(T item)
        {
            while (true)
            {
                int swapIndex = 0;
                int indexLeftChild = item.HeapIndex * 2 + 1;
                int indexRightChild = item.HeapIndex * 2 + 2;

                if (indexLeftChild < currentItemCount)
                {
                    swapIndex = indexLeftChild;

                    if (indexRightChild < currentItemCount)
                    {
                        if (items[indexLeftChild].CompareTo(items[indexRightChild]) < 0)
                        {
                            swapIndex = indexRightChild;
                        }
                    }

                    if (item.CompareTo(items[swapIndex]) < 0)
                    {
                        Swap(item, items[swapIndex]);
                    }
                    else
                    {
                        return;
                    }

                }
                else
                {
                    return;
                }

            }
        }

        /// <summary>
        /// Sorts the heap up. 
        /// </summary>
        /// <param name="item"></param>
        void SortUp(T item)
        {
            int parentIndex = (item.HeapIndex - 1) / 2;

            while (parentIndex >= 0)
            {
                T parentItem = items[parentIndex];

                if (item.CompareTo(parentItem) > 0)
                    Swap(item, parentItem);
                else
                    break;

                parentIndex = (item.HeapIndex - 1) / 2;
            }
        }


        /// <summary>
        /// Swaps item A and B in the heap
        /// </summary>
        /// <param name="itemA"></param>
        /// <param name="itemB"></param>
        void Swap(T itemA, T itemB)
        {
            items[itemA.HeapIndex] = itemB;
            items[itemB.HeapIndex] = itemA;
            int itemAIndex = itemA.HeapIndex;
            itemA.HeapIndex = itemB.HeapIndex;
            itemB.HeapIndex = itemAIndex;
        }
    }

    public interface IHeapItem<T> : IComparable<T>
    {
        int HeapIndex
        {
            get;
            set;
        }
    }

}
