using System.Collections.Generic;

namespace CCompiler {
  public class Statement {
    private List<MiddleCode> m_list;
    private ISet<MiddleCode> m_nextSet;
  
    public Statement(List<MiddleCode> list, ISet<MiddleCode> nextSet = null) {
      m_list = list;
      m_nextSet = (nextSet != null) ? nextSet : (new HashSet<MiddleCode>());
    }
  
    public List<MiddleCode> CodeList {
      get { return m_list; }
    }
  
    public ISet<MiddleCode> NextSet {
      get { return m_nextSet; }
    }
  }
}