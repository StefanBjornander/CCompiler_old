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

    /*private void SetArrayDimension(Type type) {
      if (type.IsArray()) {
        SetArrayDimension(type.ArrayType);
        type.Dimension = type.ArrayType.Dimension + 1;
      }
      else {
        type.Dimension = 0;
      }
    }*/

    public void Add(Type type) {
      if (m_firstType == null) {
        m_firstType = m_lastType = type;
      }
      else {
        switch (m_lastType.Sort) {
          case Sort.Pointer:
            m_lastType.PointerType = type;
            m_lastType = type;
            break;

          case Sort.Array:
            Assert.Error(type.IsComplete(),
                         Message.Array_of_incomplete_type_not_allowed);
            Assert.Error(!type.IsFunction(),
                         Message.Array_of_function_not_allowed);
            m_lastType.ArrayType = type;
            m_lastType = type;

            /*if (!m_lastType.IsArray()) {
              SetArrayDimension(m_firstType);
            }*/
            break;

          case Sort.Function:
            Assert.Error(!type.IsArray(),
                         Message.Function_cannot_return_array);
            Assert.Error(!type.IsFunction(),
                         Message.Function_cannot_return_function);
            m_lastType.ReturnType = type;
            m_lastType = type;
            break;
        }
      }
    }
  }
}
