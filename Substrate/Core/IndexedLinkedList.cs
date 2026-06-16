using System;
using System.Collections;
using System.Collections.Generic;

namespace Substrate.Core;

internal class IndexedLinkedList<T> : ICollection<T>, ICollection
{
    private readonly Dictionary<T, LinkedListNode<T>> _index;
    private readonly LinkedList<T> _list;

    public IndexedLinkedList()
    {
        _list = new LinkedList<T>();
        _index = new Dictionary<T, LinkedListNode<T>>();
    }

    public T First => _list.First.Value;

    public T Last => _list.Last.Value;

    #region IEnumerable<T> Members

    public IEnumerator<T> GetEnumerator()
    {
        return _list.GetEnumerator();
    }

    #endregion

    #region IEnumerable Members

    IEnumerator IEnumerable.GetEnumerator()
    {
        return _list.GetEnumerator();
    }

    #endregion

    public void AddFirst(T value)
    {
        var node = _list.AddFirst(value);
        _index.Add(value, node);
    }

    public void AddLast(T value)
    {
        var node = _list.AddLast(value);
        _index.Add(value, node);
    }

    public void RemoveFirst()
    {
        _index.Remove(_list.First.Value);
        _list.RemoveFirst();
    }

    public void RemoveLast()
    {
        _index.Remove(_list.First.Value);
        _list.RemoveLast();
    }

    #region ICollection<T> Members

    public void Add(T item)
    {
        AddLast(item);
    }

    public void Clear()
    {
        _index.Clear();
        _list.Clear();
    }

    public bool Contains(T item)
    {
        return _index.ContainsKey(item);
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        _list.CopyTo(array, arrayIndex);
    }

    public bool IsReadOnly => false;

    public bool Remove(T value)
    {
        LinkedListNode<T> node;
        if (_index.TryGetValue(value, out node))
        {
            _index.Remove(value);
            _list.Remove(node);
            return true;
        }

        return false;
    }

    #endregion

    #region ICollection Members

    void ICollection.CopyTo(Array array, int index)
    {
        (_list as ICollection).CopyTo(array, index);
    }

    public int Count => _list.Count;

    bool ICollection.IsSynchronized => false;

    object ICollection.SyncRoot => (_list as ICollection).SyncRoot;

    #endregion
}