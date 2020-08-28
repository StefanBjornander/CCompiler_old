using System.Text;
using System.Collections;
using System.Collections.Generic;

namespace CCompiler {
  public class ListSet<SetType> : ISet<SetType> {
    private List<SetType> m_list = new List<SetType>();

    public ListSet() {
      // Empty.
    }

    public int Count {
      get { return m_list.Count; }
    }

    public bool IsReadOnly {
      get { return false; }
    }

    public ListSet(SetType value) {
      m_list.Add(value);
    }
  
    public ListSet(SetType[] array) {
      m_list.AddRange(array);
    }

    public ListSet(IEnumerable<SetType> enumerable) {
      m_list.AddRange(enumerable);
    }

    public void CopyTo(SetType[] array, int index) {
      foreach (SetType value in m_list) {
        array[index++] = value;
      }
    }

    public void Clear() {
      m_list.Clear();
    }
        
    public bool Contains(SetType value) {
      return m_list.Contains(value);
    }

    void ICollection<SetType>.Add(SetType value) {
      if (!m_list.Contains(value)) {
        m_list.Add(value);
      }
    }

    bool ISet<SetType>.Add(SetType value) {
      if (!m_list.Contains(value)) {
        m_list.Add(value);
        return true;
      }

      return false;
    }

    public bool Remove(SetType value) {
      if (m_list.Contains(value)) {
        m_list.Remove(value);
        return true;
      }

      return false;
    }

    IEnumerator IEnumerable.GetEnumerator() {
      return (new ListSetEnumerator<SetType>(m_list));
    }

    IEnumerator<SetType> IEnumerable<SetType>.GetEnumerator() {
      return (new ListSetEnumerator<SetType>(m_list));
    }

    public void IntersectWith(IEnumerable<SetType> enumerable) {
      List<SetType> result = new List<SetType>();

      foreach (SetType value in enumerable) {
        if (m_list.Contains(value)) {
          result.Add(value);
        }
      }

      m_list = result;
    }

    public void UnionWith(IEnumerable<SetType> enumerable) {
      foreach (SetType value in enumerable) {
        if (!m_list.Contains(value)) {
          m_list.Add(value);
        }
      }
    }

    public void ExceptWith(IEnumerable<SetType> enumerable) {
      foreach (SetType value in enumerable) {
        if (m_list.Contains(value)) {
          m_list.Remove(value);
        }
      }
    }

    public void SymmetricExceptWith(IEnumerable<SetType> enumerable) {
      List<SetType> result = new List<SetType>();
      foreach (SetType value in enumerable) {
        if (!m_list.Contains(value)) {
          result.Add(value);
        }
      }

      ICollection<SetType> collection = new List<SetType>(enumerable);
      foreach (SetType value in m_list) {
        if (!collection.Contains(value)) {
          result.Add(value);
        }
      }

      m_list = result;
    }

    public bool IsSubsetOf(IEnumerable<SetType> enumerable) {
      ICollection<SetType> collection = new List<SetType>(enumerable);

      foreach (SetType value in m_list) {
        if (!collection.Contains(value)) {
          return false;
        }
      }

      return true;
    }

    public bool IsSupersetOf(IEnumerable<SetType> enumerable) {
      foreach (SetType value in enumerable) {
        if (!m_list.Contains(value)) {
          return false;
        }
      }

      return true;
    }

    public bool IsProperSubsetOf(IEnumerable<SetType> enumerable) {
      return IsSubsetOf(enumerable) && !Equals(enumerable);
    }

    public bool IsProperSupersetOf(IEnumerable<SetType> enumerable) {
      return IsSupersetOf(enumerable) && !Equals(enumerable);
    }

    public bool Overlaps(IEnumerable<SetType> enumerable) {
      foreach (SetType value in enumerable) {
        if (m_list.Contains(value)) {
          return true;
        }
      }

      return false;
    }

    public bool SetEquals(IEnumerable<SetType> enumerable) {
      int count = 0;

      foreach (SetType value in enumerable) {
        if (!m_list.Contains(value)) {
          return false;
        }

        ++count;
      }

      return (count == m_list.Count);
    }

    public override int GetHashCode() {
      return base.GetHashCode();
    }

    public override bool Equals(object obj) {
      if (obj is IEnumerable<SetType>) {
        IEnumerable<SetType> enumerable = (IEnumerable<SetType>) obj;
        return SetEquals(enumerable);
      }

      return false;
    }

    public override string ToString() {
      bool first = true;
      StringBuilder buffer = new StringBuilder();

      foreach (SetType value in m_list) {
        buffer.Append((first ? "" : ",") + value.ToString());
        first = false;
      }

      return "{" + buffer.ToString() + "}";
    }
  }
}