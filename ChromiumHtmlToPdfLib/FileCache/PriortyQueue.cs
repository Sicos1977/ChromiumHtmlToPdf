/*
Copyright 2012, 2013, 2017 Adam Carter (http://adam-carter.com)

This file is part of FileCache (http://github.com/acarteas/FileCache).

FileCache is distributed under the Apache License 2.0.
Consult "LICENSE.txt" included in this package for the Apache License 2.0.
*/

using System;
using System.Collections.Generic;
// ReSharper disable UnusedMember.Global

namespace ChromiumHtmlToPdfLib.FileCache;

/// <summary>
///     A basic min priority queue (min heap)
/// </summary>
/// <typeparam name="T">Data type to store</typeparam>
internal class PriorityQueue<T> where T : IComparable<T>
{
    #region Fields
    private readonly IComparer<T> _comparer;
    private readonly List<T> _items;
    #endregion

    #region Constructors
    /// <summary>
    ///     Default constructor.
    /// </summary>
    /// <param name="comparer">
    ///     The comparer to use.  The default comparer will make the smallest item the root of the heap.
    /// </param>
    public PriorityQueue(IComparer<T>? comparer = null)
    {
        _items = [];
        _comparer = comparer ?? new GenericComparer<T>();
    }

    /// <summary>
    ///     Constructor that will convert an existing list into a min heap
    /// </summary>
    /// <param name="unsorted">The unsorted list of items</param>
    /// <param name="comparer">The comparer to use.  The default comparer will make the smallest item the root of the heap.</param>
    public PriorityQueue(List<T> unsorted, IComparer<T>? comparer = null) : this(comparer)
    {
        foreach (var t in unsorted)
            _items.Add(t);

        BuildHeap();
    }
    #endregion

    #region BuildHeap
    private void BuildHeap()
    {
        for (var i = _items.Count / 2; i >= 0; i--) AdjustHeap(i);
    }
    #endregion

    #region AdjustHeap
    /// <summary>
    ///     Percolates the item specified at by index down into its proper location within a heap.  Used
    ///     for dequeue operations and array to heap conversions
    /// </summary>
    /// <param name="index"></param>
    private void AdjustHeap(int index)
    {
        //cannot percolate empty list
        if (_items.Count == 0) return;

        //GOAL: get value at index, make sure this value is less than children
        // IF NOT: swap with smaller of two
        // (continue to do so until we can't swap)

        //helps us figure out if a given index has children
        var endLocation = _items.Count;

        //keeps track of smallest index
        var smallestIndex = index;

        //while we're not the last thing in the heap
        while (index < endLocation)
        {
            //get left child index
            var leftChildIndex = 2 * index + 1;
            var rightChildIndex = leftChildIndex + 1;

            //Three cases:
            // 1. left index is out of range
            // 2. right index is out or range
            // 3. both indices are valid
            if (leftChildIndex < endLocation)
            {
                //CASE 1 is FALSE
                //remember that left index is the smallest
                smallestIndex = leftChildIndex;

                if (rightChildIndex < endLocation)
                    smallestIndex = _comparer.Compare(_items[leftChildIndex], _items[rightChildIndex]) < 0
                        ? leftChildIndex
                        : rightChildIndex;
            }

            //we have two things: original index and (potentially) a child index
            if (_comparer.Compare(_items[index], _items[smallestIndex]) > 0)
            {
                //move parent down (it was too big)
                (_items[index], _items[smallestIndex]) = (_items[smallestIndex], _items[index]);

                //update index
                index = smallestIndex;
            }
            else
            {
                //no swap necessary
                break;
            }
        }
    }
    #endregion

    #region IsEmpty
    public bool IsEmpty()
    {
        return _items.Count == 0;
    }
    #endregion

    #region GetSize
    public int GetSize()
    {
        return _items.Count;
    }
    #endregion

    #region Enqueue
    public void Enqueue(T item)
    {
        //calculate positions
        var currentPosition = _items.Count;
        var parentPosition = (currentPosition - 1) / 2;

        //insert element (note: may get erased if we hit the WHILE loop)
        _items.Add(item);

        //find parent, but be careful if we are an empty queue
        if (parentPosition >= 0)
        {
            //find parent
            var parent = _items[parentPosition];

            //bubble up until we're done
            while (_comparer.Compare(parent, item) > 0 && currentPosition > 0)
            {
                //move parent down
                _items[currentPosition] = parent;

                //recalculate position
                currentPosition = parentPosition;
                parentPosition = (currentPosition - 1) / 2;

                //make sure that we have a valid index
                if (parentPosition >= 0)
                    //find parent
                    parent = _items[parentPosition];
            }
        } //end check for nullptr

        //after WHILE loop, current_position will point to the place that
        //variable "item" needs to go
        _items[currentPosition] = item;
    }
    #endregion

    #region GetFirst
    public T GetFirst()
    {
        return _items[0];
    }
    #endregion

    #region Dequeue
    public T Dequeue()
    {
        var lastPosition = _items.Count - 1;
        var lastItem = _items[lastPosition];
        var top = _items[0];
        _items[0] = lastItem;
        _items.RemoveAt(_items.Count - 1);

        //percolate down
        AdjustHeap(0);
        return top;
    }
    #endregion
    
    #region GenericComparer
    private class GenericComparer<TInner> : IComparer<TInner> where TInner : IComparable<TInner>
    {
        public int Compare(TInner? x, TInner? y)
        {
            if (x == null && y == null) return 0;
            if (x == null) return -1;
            if (y == null) return 1;

            return x.CompareTo(y);
        }
    }
    #endregion
}