namespace CCompiler {
  public class UnorderedPair<FirstType,SecondType> :
               Pair<FirstType,SecondType>{
    public UnorderedPair(FirstType first, SecondType second)
     :base(first, second) {
      // Empty.
    }

    public override int GetHashCode() {
      return base.GetHashCode();
    }

    public override bool Equals(object obj) {
      if (obj is UnorderedPair<FirstType,SecondType>) {
        Pair<FirstType, SecondType> pair = (Pair<FirstType, SecondType>) obj;
        return (First.Equals(pair.First) && Second.Equals(pair.Second)) ||
               (First.Equals(pair.Second) && Second.Equals(pair.First));
      }
    
      return false;
    }
  }
}
