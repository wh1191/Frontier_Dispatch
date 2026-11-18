using System;
using System.Collections.Generic;

namespace FrontierDispatch.Core
{
    // Assignment Phase 3. Implement as a binary min-heap.

    /// <summary>
    /// A minimum-priority queue implemented as a binary min-heap.
    ///
    /// Items with lower integer priority values should be returned before
    /// items with higher priority values.
    ///
    /// This class is intentionally incomplete in the assignment starter.
    /// </summary>
    public sealed class MinPriorityQueue<T>
    {
        // The list stores the physical representation of the binary heap.
        // Your implementation is responsible for preserving the min-heap invariant.
        private readonly List<Entry> _heap = new List<Entry>();

        /// <summary>
        /// Number of items currently stored in the queue.
        /// </summary>
        public int Count => _heap.Count;

        /// <summary>
        /// Adds an item with the supplied priority.
        /// Lower priority values are removed before higher priority values.
        /// </summary>
        public void Enqueue(T item, int priority)
        {
            // TODO: insert the new entry and restore the min-heap invariant.
            //throw new NotImplementedException();
            _heap.Add(new Entry(item, priority));
            SiftUp(_heap.Count - 1);
        }

        /// <summary>
        /// Returns the minimum-priority item without removing it.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the queue is empty.
        /// </exception>
        public T Peek()
        {
            // TODO: return the current minimum without changing the queue.
            //throw new NotImplementedException();
            if(_heap.Count <= 0) {
                throw new InvalidOperationException("The priority queue is empty.");
            }

            return _heap[0].Item;
        }

        /// <summary>
        /// Removes and returns the minimum-priority item.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the queue is empty.
        /// </exception>
        public T Dequeue()
        {
            // TODO: remove the minimum item and restore the min-heap invariant.
            //throw new NotImplementedException();

            if (_heap.Count <= 0) {
                throw new InvalidOperationException("The priority queue is empty.");
            }

            T minItem = _heap[0].Item;
            int lastIndex = _heap.Count - 1;

            // Move the last element to the root
            _heap[0] = _heap[lastIndex];
            _heap.RemoveAt(lastIndex);

            if (_heap.Count > 0) {
                SiftDown(0);
            }

            return minItem;
        }

        private void SiftUp(int index) {
            while (index > 0) {
                int parentIndex = (index - 1) / 2;

                if (_heap[index].Priority < _heap[parentIndex].Priority) {
                    // Swap with parent
                    Entry temp = _heap[index];
                    _heap[index] = _heap[parentIndex];
                    _heap[parentIndex] = temp;

                    index = parentIndex;
                } else {
                    break;
                }
            }
        }

        private void SiftDown(int index) {
            int lastIndex = _heap.Count - 1;

            while (true) {
                int leftChild = 2 * index + 1;
                int rightChild = 2 * index + 2;
                int smallest = index;

                if (leftChild <= lastIndex && _heap[leftChild].Priority < _heap[smallest].Priority) {
                    smallest = leftChild;
                }

                if (rightChild <= lastIndex && _heap[rightChild].Priority < _heap[smallest].Priority) {
                    smallest = rightChild;
                }

                if (smallest != index) {
                    // Swap with smallest child
                    Entry temp = _heap[index];
                    _heap[index] = _heap[smallest];
                    _heap[smallest] = temp;

                    index = smallest;
                } else {
                    break;
                }
            }
        }

        /// <summary>
        /// Stores one item together with the priority used by the heap.
        /// </summary>
        private readonly struct Entry
        {
            public Entry(T item, int priority)
            {
                Item = item;
                Priority = priority;
            }

            public T Item { get; }
            public int Priority { get; }
        }
    }
}
