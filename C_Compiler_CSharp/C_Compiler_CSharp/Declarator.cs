namespace CCompiler {
  public class Declarator {
    private string m_name;
    private Type m_firstType, m_lastType;

    public Declarator(string name) {
      m_name = name;
    }
  
    public string Name {
      get { return m_name; }
      set { m_name = value; }
    }

    public Type Type {
      get { return m_firstType; }
    }

    public void Add(Type newType) {
      if (m_firstType == null) {
        m_firstType = m_lastType = newType;
      }
      else {
        switch (m_lastType.Sort) {
          case Sort.Pointer:
            m_lastType.PointerType = newType;
            m_lastType = newType;
            break;

          case Sort.Array:
            Assert.Error(newType.IsComplete(),
                         Message.Array_of_incomplete_type_not_allowed);
            Assert.Error(!newType.IsFunction(),
                         Message.Array_of_function_not_allowed);
            m_lastType.ArrayType = newType;
            m_lastType = newType;
            break;

          case Sort.Function:
            Assert.Error(!newType.IsArray(),
                         Message.Function_cannot_return_array);
            Assert.Error(!newType.IsFunction(),
                         Message.Function_cannot_return_function);
            m_lastType.ReturnType = newType;
            m_lastType = newType;
            break;
        }
      }
    }
  }
}
