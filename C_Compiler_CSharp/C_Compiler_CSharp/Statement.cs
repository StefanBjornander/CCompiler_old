using System.Collections.Generic;

namespace CCompiler {
  public class Statement {
    private List<MiddleCode> m_codeList;
    private ISet<MiddleCode> m_nextSet;
  
    public Statement(List<MiddleCode> codeList,
                     ISet<MiddleCode> nextSet = null) {
      Assert.ErrorXXX(codeList != null);
      m_codeList = codeList;
      m_nextSet = (nextSet != null) ? nextSet : (new HashSet<MiddleCode>());
    }
  
    public List<MiddleCode> CodeList {
      get { return m_codeList; }
    }
  
    public ISet<MiddleCode> NextSet {
      get { return m_nextSet; }
    }
  }
}
