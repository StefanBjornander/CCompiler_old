using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;

namespace CCompiler {
  public class ListMap<KeyType,ValueType> :
               IDictionary<KeyType,ValueType> {
    private List<KeyValuePair<KeyType,ValueType>> m_list =
      new List<KeyValuePair<KeyType,ValueType>>();

    public int Count {
      get { return m_list.Count; }
    }

    public void Clear() {
      m_list.Clear();
    }

    public bool IsReadOnly {
      get { return false; }
    }

    public ValueType this[KeyType key] {
      get {
        if (key == null) {
          throw (new ArgumentNullException());
        }

        ValueType value;

        if (TryGetValue(key, out value)) {
          return value;
        }
        else {
          throw (new InvalidOperationException());
        }
      }

      set {
        if (ContainsKey(key)) {
          Remove(key);
        }

        m_list.Add(new KeyValuePair<KeyType,ValueType>(key, value));
      }
    }

    public ICollection<KeyType> Keys {
      get {
        ICollection<KeyType> collection = new ListSet<KeyType>();

        foreach (KeyValuePair<KeyType,ValueType> pair in m_list) {
          collection.Add(pair.Key);
        }

        return collection;
      }
    }

    public ICollection<ValueType> Values {
      get {
        ICollection<ValueType> collection = new ListSet<ValueType>();

        foreach (KeyValuePair<KeyType,ValueType> pair in m_list) {
          collection.Add(pair.Value);
        }

        return collection;
      }
    }

    public void Add(KeyType key, ValueType value) {
      Add(new KeyValuePair<KeyType,ValueType>(key, value));
    }

    public void Add(KeyValuePair<KeyType,ValueType> pair) {
      if (ContainsKey(pair.Key)) {
        throw (new ArgumentException());
      }
      
      m_list.Add(pair);
    }

    public bool ContainsKey(KeyType key) {
      foreach (KeyValuePair<KeyType,ValueType> pair in m_list) {
        if (pair.Key.Equals(key)) {
          return true;
        }
      }

      return false;
    }

    public bool Contains(KeyValuePair<KeyType,ValueType> pair) {
      return m_list.Contains(pair);
    }

    public bool Remove(KeyType key) {
      foreach (KeyValuePair<KeyType,ValueType> pair in m_list) {
        if (key.Equals(pair.Key)) {
          m_list.Remove(pair);
          return true;
        }
      }

      return false;
    }

    public bool Remove(KeyValuePair<KeyType,ValueType> pair) {
      if (m_list.Contains(pair)) {
        m_list.Remove(pair);
        return true;
      }

      return false;
    }

    public void CopyTo(KeyValuePair<KeyType,ValueType>[] array, int index) {
      foreach (KeyValuePair<KeyType,ValueType> pair in m_list) {
        array[index++] = pair;
      }
    }

    public bool TryGetValue(KeyType key, out ValueType value) {
      foreach (KeyValuePair<KeyType,ValueType> pair in m_list) {
        if (key.Equals(pair.Key)) {
          value = pair.Value;
          return true;
        }
      }

      value = default(ValueType);
      return false;
    }

    IEnumerator IEnumerable.GetEnumerator() {
      return (new ListMapEnumerator<KeyType,ValueType>(m_list));
    }

    IEnumerator<KeyValuePair<KeyType,ValueType>>
    IEnumerable<KeyValuePair<KeyType,ValueType>>.GetEnumerator() {
      return (new ListMapEnumerator<KeyType,ValueType>(m_list));
    }

    public override int GetHashCode() {
      return base.GetHashCode();
    }

    public override bool Equals(object obj) {
      if (obj is IEnumerable<KeyValuePair<KeyType,ValueType>>) {
        IEnumerable<KeyValuePair<KeyType,ValueType>> enumerable =
          (IEnumerable<KeyValuePair<KeyType,ValueType>>) obj;
        int count = 0;

        foreach (KeyValuePair<KeyType,ValueType> pair in enumerable) {
          if (!m_list.Contains(pair)) {
            return false;
          }

          ++count;
        }

        return (count == m_list.Count);
      }

      return false;
    }

    public override string ToString() {
      bool first = true;
      StringBuilder buffer = new StringBuilder();

      foreach (KeyValuePair<KeyType,ValueType> pair in m_list) {
        buffer.Append((first ? "" : ",") + "(" + pair.Key.ToString() + "," + pair.Value.ToString() + ")");
        first = false;
      }

      return "{" + buffer.ToString() + "}";
    }
  }
}