namespace CCompiler {
  public class Triple<FirstType,SecondType,ThirdType> :
               Pair<FirstType,SecondType> {
    private ThirdType m_third;

    public Triple(FirstType first, SecondType second, ThirdType third) 
     :base(first, second) {
      m_third = third;
    }
  
    public ThirdType Third {
      get { return m_third; }
      set { m_third = value; }
    }
  
    public override int GetHashCode() {
      return base.GetHashCode() + m_third.GetHashCode();
    }

    public override bool Equals(object obj) {
      if (obj is Triple<FirstType,SecondType,ThirdType>) {
        Triple<FirstType,SecondType,ThirdType> triple =
          (Triple<FirstType,SecondType,ThirdType>) obj;
        return base.Equals(triple) && m_third.Equals(triple.m_third);
      }
    
      return false;
    }
  }
}