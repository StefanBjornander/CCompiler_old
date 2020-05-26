namespace CCompiler {
  public class StaticAddress {
    private string m_uniqueName;
    private int m_offset;
  
    public StaticAddress(string name, int offset) {
      //Assert.Error(offset >= 0);
      m_uniqueName = name;
      m_offset = offset;
    }
  
    public string UniqueName {
      get { return m_uniqueName; }
    }
  
    public int Offset {
      get { return m_offset; }
    }
  }
}