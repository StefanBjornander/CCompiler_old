using System.Linq;
using System.Collections.Generic;

namespace CCompiler {
  public class Macro {
    private int m_parameters;
    private List<Token> m_tokenList;
  
    public Macro(int parameters, List<Token> tokenList) {
      m_parameters = parameters;
      m_tokenList = new List<Token>(tokenList);
    
      for (int index = (m_tokenList.Count - 1); index >= 0; --index) {
        if (m_tokenList[index].Id == CCompiler_Pre.Tokens.EOF) {
          m_tokenList.RemoveAt(index);
        }
      }
    }

    public int Parameters() {
      return m_parameters;
    }
  
    public List<Token> TokenList() {
      return m_tokenList;
    }

    public override int GetHashCode() {
      return base.GetHashCode();
    }
  
    public override bool Equals(object obj) {
      if (obj is Macro) {
        Macro macro = (Macro) obj;
        return (m_parameters == macro.m_parameters) &&
               (m_tokenList.SequenceEqual(macro.m_tokenList));
      }

      return false;
    }
  }
}