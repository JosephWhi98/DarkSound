using System;

namespace DarkSound
{
	public class DSHeap<T> where T : IHeapItem<T>
    {

        T[] heapItems;
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
        /// <param name="heapMaxSize"></param>
        public DSHeap(int heapMaxSize)
        {
            heapItems = new T[heapMaxSize];
        }


        /// <summary>
        /// Adds a new item to the heap. 
        /// </summary>
        /// <param name="item"></param>
        public void AddItem(T item)
        {
            item.index = currentItemCount;
            heapItems[currentItemCount] = item;
            SortHeapUp(item);
            currentItemCount++;
        }

        /// <summary>
        /// Removes the first item in the heap
        /// </summary>
        /// <returns>the first time in the heap</returns>
        public T RemoveFirstItem()
        {
            T first = heapItems[0];
            currentItemCount--;
            heapItems[0] = heapItems[currentItemCount];
            heapItems[0].index = 0;
            SortHeapDown(heapItems[0]);
            return first;
        }


        /// <summary>
        /// Checks if item is contained in the heap. 
        /// </summary>
        /// <param name="item"></param>
        /// <returns>if item is contained in the heap</returns>
        public bool Contains(T item)
        {
            return Equals(heapItems[item.index], item);
        }

        /// <summary>
        /// Sorts the heap down.
        /// </summary>
        /// <param name="item"></param>
        void SortHeapDown(T item)
        {
            while (true)
            {
                int swapIndex = 0;
                int indexLeftChild = item.index * 2 + 1;
                int indexRightChild = item.index * 2 + 2;

                if (indexLeftChild < currentItemCount)
                {
                    swapIndex = indexLeftChild;

                    if (indexRightChild < currentItemCount)
                    {
                        if (heapItems[indexLeftChild].CompareTo(heapItems[indexRightChild]) < 0)
                        {
                            swapIndex = indexRightChild;
                        }
                    }

                    if (item.CompareTo(heapItems[swapIndex]) < 0)
                    {
                        SwapItems(item, heapItems[swapIndex]);
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
        void SortHeapUp(T item)
        {
            int indexParent = (item.index - 1) / 2;

            while (indexParent >= 0)
            {
                T parent = heapItems[indexParent];

                if (item.CompareTo(parent) > 0)
                    SwapItems(item, parent);
                else
                    break;

                indexParent = (item.index - 1) / 2;
            }
        }


        /// <summary>
        /// Swaps item A and B in the heap
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        void SwapItems(T a, T b)
        {
            heapItems[a.index] = b;
            heapItems[b.index] = a;
            int itemAIndex = a.index;
            a.index = b.index;
            b.index = itemAIndex;
        }
    }

    public interface IHeapItem<T> : IComparable<T>
    {
        int index
        {
            get;
            set;
        }
    }

}
