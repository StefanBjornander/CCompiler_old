namespace CCompiler {
  public class TrackEntryZ {
    private int m_line, m_position, m_size;

    public TrackEntryZ(int position, int line, int size) {
      m_line = line;
      m_position = position;
      m_size = size;
    }

    public int Line {
      get { return m_line; }
    }
  
    public int Position {
      get { return m_position; }
    }
  
    public int Size {
      get { return m_size; }
    }
  }
}