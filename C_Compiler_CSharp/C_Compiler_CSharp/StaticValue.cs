namespace CCompiler {
  public class StaticValue {
    private string m_uniqueName;
    private int m_offset;

    public StaticValue(string name, int offset) {
      m_uniqueName = name;
      m_offset = offset;
    }
  
    public string UniqueName {
      get { return m_uniqueName; }
    }
  
    public int Offset() {
      return m_offset;
    }
  }
}