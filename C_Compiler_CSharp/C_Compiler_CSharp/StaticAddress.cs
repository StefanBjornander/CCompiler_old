namespace CCompiler {
  public class StaticAddress {
    private string m_uniqueName;
    private int m_offset;
  
    public StaticAddress(string name, int offset) {
      m_uniqueName = name;
      m_offset = offset;
    }
  
    public string UniqueName {
      get { return m_uniqueName; }
    }
  
    public int Offset {
      get { return m_offset; }
    }

    public override string ToString() {
      if (m_offset > 0) {
        return m_uniqueName + " + " + m_offset;
      }
      else if (m_offset < 0) {
        return m_uniqueName + " - " + (-m_offset);
      }
      else {
        return m_uniqueName;
      }
    }
  }
}