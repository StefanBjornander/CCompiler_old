using System.Numerics;
using System.Collections.Generic;

namespace CCompiler {
  public class MiddleCodeOptimizer {
    private bool m_update;
    private List<MiddleCode> m_middleCodeList;

    public MiddleCodeOptimizer(List<MiddleCode> middleCodeList) {
      m_middleCodeList = middleCodeList;
    }

    public void Optimize() {
      ObjectToIntegerAddresses();

      do {
        m_update = false;
        ClearGotoNextStatements();
        ClearDoubleRelationStatements();
        TraceGotoChains();
        ClearUnreachableCode();
        RemovePushPop();
        //MergePopPushToTop();
        MergeTopPopToPop();
        //AssignFloat(); // XXX
        MergeBinary(); // XXX
        //MergeDoubleAssign(); // XXX
        SematicOptimization();
        //OptimizeRelation(); // XXX
        OptimizeCommutative();
        SwapRelation();
        RemoveTrivialAssign();
        //OptimizeBinary();
        CheckIntegral(); // XXX
        CheckFloating(); // XXX
        RemoveClearedCode();
      } while (m_update);
    }

    public void ObjectToIntegerAddresses() {
      IDictionary<MiddleCode,int> addressMap =
        new Dictionary<MiddleCode,int>();
    
      for (int index = 0; index < m_middleCodeList.Count; ++index) {
        addressMap.Add(m_middleCodeList[index], index);
      }
    
      for (int index = 0; index < m_middleCodeList.Count; ++index) {
        MiddleCode sourceCode = m_middleCodeList[index];
      
        if (sourceCode.IsGoto() || sourceCode.IsCarry() ||
            sourceCode.IsRelation()) {
          Assert.ErrorXXX(sourceCode[0] is MiddleCode);
          MiddleCode targetCode = (MiddleCode) sourceCode[0];
          Assert.ErrorXXX(addressMap.ContainsKey(targetCode));
          sourceCode[0] = addressMap[targetCode];
        }
      }
    }

  // --------------------------------------------------------------------------

    // 1. goto 2
    // 2. ...
  
    private void ClearGotoNextStatements() {
      for (int index = 0; index < (m_middleCodeList.Count - 1); ++index) {        
        MiddleCode middleCode = m_middleCodeList[index];
      
        if (middleCode.IsRelationCarryOrGoto()) {
          int target = (int) middleCode[0];
  
          if (target == (index + 1)) {
            middleCode.Clear();
            m_update = true;
          }
        }
      }
    }

  // --------------------------------------------------------------------------

    // 1. if a < b goto 3
    // 2. goto 10
  
    // 1. if a >= b goto 10
    // 2. Clear

    // 1. if a < b goto 3
    // 2. goto 10

    // 1. if a >= b goto 10
    // 2. empty

    public static IDictionary<MiddleOperator, MiddleOperator> m_inverseMap =
      new Dictionary<MiddleOperator, MiddleOperator>() {
        {MiddleOperator.Equal, MiddleOperator.NotEqual},
        {MiddleOperator.NotEqual, MiddleOperator.Equal},
        {MiddleOperator.Carry, MiddleOperator.NotCarry},
        {MiddleOperator.NotCarry, MiddleOperator.Carry},
        {MiddleOperator.SignedLessThan, MiddleOperator.SignedGreaterThanEqual},
        {MiddleOperator.SignedLessThanEqual, MiddleOperator.SignedGreaterThan},
        {MiddleOperator.SignedGreaterThan, MiddleOperator.SignedLessThanEqual},
        {MiddleOperator.SignedGreaterThanEqual, MiddleOperator.SignedLessThan},
        {MiddleOperator.UnsignedLessThan, MiddleOperator.UnsignedGreaterThanEqual},
        {MiddleOperator.UnsignedLessThanEqual, MiddleOperator.UnsignedGreaterThan},
        {MiddleOperator.UnsignedGreaterThan, MiddleOperator.UnsignedLessThanEqual},
        {MiddleOperator.UnsignedGreaterThanEqual, MiddleOperator.UnsignedLessThan}
      };

    private void ClearDoubleRelationStatements() {
      for (int index = 0; index < (m_middleCodeList.Count - 1); ++index) {
        MiddleCode thisCode = m_middleCodeList[index],
                   nextCode = m_middleCodeList[index + 1];

        if ((thisCode.IsRelation() || thisCode.IsCarry()) &&
            nextCode.IsGoto()) {
          int target1 = (int) thisCode[0],
              target2 = (int) nextCode[0];

          if (target1 == (index + 2)) {
            MiddleOperator operator1 = thisCode.Operator;
            thisCode.Operator = m_inverseMap[operator1];
            thisCode[0] = target2;
            nextCode.Clear();
            m_update = true;
          }
        }
      }
    }

  // --------------------------------------------------------------------------

    // 1. goto 9
    // 9. goto 21

    // 1. goto 21
    // 9. goto 21

    private void TraceGotoChains() {
      for (int index = 1; index < m_middleCodeList.Count; ++index) {
        MiddleCode middleCode = m_middleCodeList[index];

        if (middleCode.IsRelationCarryOrGoto()) {
          ISet<int> sourceSet = new HashSet<int>();
          sourceSet.Add(index);
        
          int firstTarget = (int) middleCode[0];
          int finalTarget = TraceGoto(firstTarget, sourceSet);

          if (firstTarget != finalTarget) {
            foreach (int source in sourceSet) {
              MiddleCode sourceCode = m_middleCodeList[source];
              sourceCode[0] = finalTarget;
            }

            m_update = true;
          }
        }
      }
    }

    private int TraceGoto(int target, ISet<int> sourceSet) {
      MiddleCode objectCode = m_middleCodeList[target];
    
      if (!sourceSet.Contains(target) && objectCode.IsGoto()) {
        sourceSet.Add(target);
        int nextTarget = (int) objectCode[0];
        return TraceGoto(nextTarget, sourceSet);
      }
      else {
        return target;
      }
    }

  // --------------------------------------------------------------------------

    private void ClearUnreachableCode() {
      ISet<MiddleCode> visitedSet = new HashSet<MiddleCode>();
      SearchReachableCode(0, visitedSet);

      for (int index = 0; index < (m_middleCodeList.Count - 1); ++index) {
        MiddleCode middleCode = m_middleCodeList[index];
        if (!visitedSet.Contains(middleCode)) {
          m_middleCodeList[index].Clear();
          m_update = true;
        }
      }
    }
    
    private void SearchReachableCode(int index, ISet<MiddleCode> visitedSet) {
      for (; index < m_middleCodeList.Count; ++index) {
        MiddleCode middleCode = m_middleCodeList[index];

        if (visitedSet.Contains(middleCode)) {
          return;
        }

        visitedSet.Add(middleCode);

        if (middleCode.IsRelation() || middleCode.IsCarry()) {
          int target = (int) middleCode[0];
          SearchReachableCode(target, visitedSet);
        }
        else if (middleCode.IsGoto()) {
          int target = (int) middleCode[0];
          SearchReachableCode(target, visitedSet);
          return;
        }
        else if (middleCode.Operator == MiddleOperator.Return) {
          if (m_middleCodeList[index + 1].Operator == MiddleOperator.Exit) {
            visitedSet.Add(m_middleCodeList[index + 1]);
          }

          return;
        }
        else if (middleCode.Operator == MiddleOperator.FunctionEnd) {
          Symbol funcSymbol = (Symbol) middleCode[0];
          Assert.Error(funcSymbol.Type.ReturnType.IsVoid(),
                       funcSymbol.Name,
                       Message.Reached_the_end_of_a_non__void_function);
          return;
        }
      }
    }
  
    // Push x + Pop => empty

    // push x
    // pop

    // empty

    public void RemovePushPop() {
      for (int index = 0; index < (m_middleCodeList.Count - 1); ++index) {
        MiddleCode thisCode = m_middleCodeList[index],
                   nextCode = m_middleCodeList[index + 1];
        if ((thisCode.Operator == MiddleOperator.PushFloat) &&
            (nextCode.Operator == MiddleOperator.PopFloat) &&
            ((thisCode[0] == nextCode[0]) || (nextCode[0] == null))) {
          thisCode.Clear();
          nextCode.Clear();
          m_update = true;
        }
      }
    }
  
    // Pop x + Push x => Top x

    // pop x
    // push x

    // top x

    public void MergePopPushToTop() {
      for (int index = 0; index < (m_middleCodeList.Count - 1); ++index) {
        MiddleCode thisCode = m_middleCodeList[index],
                   nextCode = m_middleCodeList[index + 1];

        if ((thisCode.Operator == MiddleOperator.PopFloat) &&
            (nextCode.Operator == MiddleOperator.PushFloat) &&
            (thisCode[0] == nextCode[0])) {
          thisCode.Operator = MiddleOperator.TopFloat;
          nextCode.Clear();
          m_update = true;
        }
      }
    }
  
    // Top x + Pop => Pop x

    // top x
    // pop

    // pop x

    public void MergeTopPopToPop() {
      for (int index = 0; index < (m_middleCodeList.Count - 1); ++index) {
        MiddleCode thisCode = m_middleCodeList[index],
                   nextCode = m_middleCodeList[index + 1];

        if ((thisCode.Operator == MiddleOperator.TopFloat) &&
            (nextCode.Operator == MiddleOperator.PopFloat) &&
            (nextCode[0] == null)) {
          thisCode.Operator = MiddleOperator.PopFloat;
          nextCode.Clear();
          m_update = true;
        }
      }
    }

    // assign a = b => Pop a

    // a = b
    // pop a

    public void AssignFloat() {
      for (int index = 0; index < (m_middleCodeList.Count - 1); ++index) {
        MiddleCode middleCode = m_middleCodeList[index];

        if (middleCode.Operator == MiddleOperator.Assign) {
          Symbol resultSymbol = (Symbol) middleCode[1];
        
          if (resultSymbol.Type.IsFloating()) {
            middleCode.Operator = MiddleOperator.PopFloat;
            m_update = true;
          }
        }
      }
    }

    // t = b + c
    // a = t

    // a = b + c

    private void MergeBinary() {
      for (int index = 0; index < (m_middleCodeList.Count - 1); ++index) {
        MiddleCode thisCode = m_middleCodeList[index],
                   nextCode = m_middleCodeList[index + 1];

        if ((/*thisCode.IsUnary() ||*/ thisCode.IsBinary()) &&
            (nextCode.Operator == MiddleOperator.Assign) &&
            //((Symbol) thisCode[0]).Temporary &&
            (thisCode[0] == nextCode[1])) {
          thisCode[0] = nextCode[0];
          nextCode.Clear();
          m_update = true;
        }
      }
    }
  
    // t = a
    // b = t

    // b = a

    private void MergeDoubleAssign() {
      for (int index = 0; index < (m_middleCodeList.Count - 1); ++index) {
        MiddleCode thisCode = m_middleCodeList[index],
                   nextCode = m_middleCodeList[index + 1];

        if ((thisCode.Operator == MiddleOperator.Assign) &&
            (nextCode.Operator == MiddleOperator.Assign) &&
            thisCode[0] == nextCode[1]) {
          thisCode[0] = nextCode[0];
          nextCode.Clear();
          m_update = true;
        }
      }
    }
  
    private void SematicOptimization() {
      for (int index = 0; index < m_middleCodeList.Count; ++index) {
        MiddleCode thisCode = m_middleCodeList[index];

        if (thisCode.IsBinary()) {
          Symbol resultSymbol = (Symbol) thisCode[0],
                 leftSymbol = (Symbol) thisCode[1],
                 rightSymbol = (Symbol) thisCode[2],
                 newSymbol = null;

          if ((leftSymbol.Value is BigInteger) && // t0 = 2 * 3
              (rightSymbol.Value is BigInteger)) {
            newSymbol =
              ConstantExpression.ArithmeticIntegral(thisCode.Operator,
                                                    leftSymbol, rightSymbol);
          }
          // t0 = 0 + i
          else if ((thisCode.Operator == MiddleOperator.BinaryAdd) &&
                    (leftSymbol.Value is BigInteger) &&
                    (leftSymbol.Value.Equals(BigInteger.Zero))) {
            newSymbol = rightSymbol;
          }
          // t0 = 0 - i; t0 = -i
          else if ((thisCode.Operator == MiddleOperator.BinarySubtract) &&
                    (leftSymbol.Value is BigInteger) &&
                    (leftSymbol.Value.Equals(BigInteger.Zero))) {
            thisCode.Operator = MiddleOperator.UnarySubtract;
            thisCode[0] = thisCode[1];
            thisCode[1] = null;
          }

          // t0 = i + 0
          // t0 = i - 0
          else if (((thisCode.Operator == MiddleOperator.BinaryAdd) ||
                    (thisCode.Operator == MiddleOperator.BinarySubtract)) &&
                    (rightSymbol.Value is BigInteger) &&
                    (rightSymbol.Value.Equals(BigInteger.Zero))) {
            newSymbol = leftSymbol;
          }
          // t0 = 0 * i
          else if (((thisCode.Operator == MiddleOperator.SignedMultiply) ||
                    (thisCode.Operator == MiddleOperator.UnsignedMultiply)) &&
                    (leftSymbol.Value is BigInteger) &&
                    (leftSymbol.Value.Equals(BigInteger.Zero))) {
            newSymbol = new Symbol(resultSymbol.Type, BigInteger.Zero);
          }
          // t0 = 1 * i
          else if (((thisCode.Operator == MiddleOperator.SignedMultiply) ||
                    (thisCode.Operator == MiddleOperator.UnsignedMultiply)) &&
                    (leftSymbol.Value is BigInteger) &&
                    (leftSymbol.Value.Equals(BigInteger.One))) {
            newSymbol = rightSymbol;
          }
          // t0 = i * 0
          else if (((thisCode.Operator == MiddleOperator.SignedMultiply) ||
                    (thisCode.Operator == MiddleOperator.UnsignedMultiply)) &&
                    (rightSymbol.Value is BigInteger) &&
                    (rightSymbol.Value.Equals(BigInteger.Zero))) {
            newSymbol = new Symbol(resultSymbol.Type, BigInteger.Zero);
          }
          // t0 = i * 1
          // t0 = i / 1
          // t0 = i % 1
          else if (((thisCode.Operator == MiddleOperator.SignedMultiply) ||
                    (thisCode.Operator == MiddleOperator.UnsignedMultiply) ||
                    (thisCode.Operator == MiddleOperator.SignedDivide) ||
                    (thisCode.Operator == MiddleOperator.UnsignedDivide) ||
                    (thisCode.Operator == MiddleOperator.SignedModulo) ||
                    (thisCode.Operator == MiddleOperator.UnsignedModulo)) &&
                    (rightSymbol.Value is BigInteger) &&
                    (rightSymbol.Value.Equals(BigInteger.One))) {
            newSymbol = leftSymbol;
          }

          if (newSymbol != null) {
            if (resultSymbol.IsTemporary()) {              
              thisCode.Operator = MiddleOperator.Empty;

              int index2;
              for (index2 = index + 1; index2 < m_middleCodeList.Count;
                   ++index2) {
                MiddleCode nextCode = m_middleCodeList[index2];

                if (nextCode[1] == resultSymbol) {
                  nextCode[1] = newSymbol;
                  break;
                }
                
                if (nextCode[2] == resultSymbol) {
                  nextCode[2] = newSymbol;
                  break;
                }
              }

              Assert.ErrorXXX(index2 < m_middleCodeList.Count);
            }
            else {
              thisCode.Operator = MiddleOperator.Assign; // i = 0 + j;
              thisCode[1] = newSymbol;                   // i = j;
              thisCode[2] = null;
            }

            m_update = true;
          }
        }
      }
    }

    public static IDictionary<MiddleOperator, MiddleOperator> m_swapMap =
       new Dictionary<MiddleOperator, MiddleOperator>() {
        {MiddleOperator.Equal, MiddleOperator.Equal},
        {MiddleOperator.NotEqual, MiddleOperator.NotEqual},
        {MiddleOperator.SignedLessThan, MiddleOperator.SignedGreaterThan},
        {MiddleOperator.SignedGreaterThan, MiddleOperator.SignedLessThan},
        {MiddleOperator.SignedLessThanEqual, MiddleOperator.SignedGreaterThanEqual},
        {MiddleOperator.SignedGreaterThanEqual, MiddleOperator.SignedLessThanEqual},
        {MiddleOperator.UnsignedLessThan, MiddleOperator.UnsignedGreaterThan},
        {MiddleOperator.UnsignedGreaterThan, MiddleOperator.UnsignedLessThan},
        {MiddleOperator.UnsignedLessThanEqual, MiddleOperator.UnsignedGreaterThanEqual},
        {MiddleOperator.UnsignedGreaterThanEqual, MiddleOperator.UnsignedLessThanEqual}
      };

    // if 1 < x goto
    // if x > 1 goto
    private void SwapRelation() {
      foreach (MiddleCode middleCode in m_middleCodeList) {
        if (middleCode.IsRelation()) {
          Symbol leftSymbol = (Symbol) middleCode[1],
                 rightSymbol = (Symbol) middleCode[2];
          if ((leftSymbol.Value is BigInteger) ||
              (leftSymbol.Type.IsArrayFunctionOrString()) &&
              !rightSymbol.Type.IsArrayFunctionOrString() &&
              !(rightSymbol.Value is BigInteger)) {
            middleCode.Operator = m_swapMap[middleCode.Operator];
            middleCode[1] = rightSymbol;
            middleCode[2] = leftSymbol;
          }
        }
      }
    }

/*    private void OptimizeRelation() {
      foreach (MiddleCode middleCode in m_middleCodeList) {
        if (middleCode.IsRelation()) {
          Symbol leftSymbol = (Symbol) middleCode[1],
                 rightSymbol = (Symbol) middleCode[2];
   
          if ((leftSymbol.Value is BigInteger) &&
              (leftSymbol.IsStaticOrExtern() &&
               leftSymbol.Type.IsArrayFunctionOrString() &&
               !(rightSymbol.Value is BigInteger)))  {
            middleCode.Operator = m_swapMap[middleCode.Operator];
            middleCode[1] = rightSymbol;
            middleCode[2] = leftSymbol;
          }
        }
      }
    }*/

    // a = b + c
    // a = c + b
  
    private void OptimizeCommutative() {
      foreach (MiddleCode middleCode in m_middleCodeList) {
        if (middleCode.IsCommutative()) {
          Symbol leftSymbol = (Symbol) middleCode[1],
                 rightSymbol = (Symbol) middleCode[2];
   
          if (leftSymbol.Value is BigInteger) {
            middleCode[1] = rightSymbol;
            middleCode[2] = leftSymbol;
          }
        }
      }
    }

    // i = i + 1
    // ++i

    // i = i + (-1)
    // --i

    // i = i - 1
    // --i

    // i = i - (-1)
    // ++i

    // i = i
    // empty

    /*private void OptimizeBinary() {
      foreach (MiddleCode middleCode in m_middleCodeList) {
        MiddleOperator middleOperator = middleCode.Operator;
      
        if (middleOperator == MiddleOperator.BinaryAdd) {
          Symbol resultSymbol = (Symbol) middleCode[0], // i = i + 1
                 leftSymbol = (Symbol) middleCode[1],
                 rightSymbol = (Symbol) middleCode[2];
   
          if (resultSymbol.Type.IsIntegralPointerArrayStringOrFunction() &&
              resultSymbol.Equals(leftSymbol)) {
            if (rightSymbol.IsValue() && rightSymbol.Value.Equals(((BigInteger) 1))) {
              middleCode.Operator = MiddleOperator.Increment;
              middleCode[0] = null;
              middleCode[2] = null;
              m_update = true;
            }
            else if (rightSymbol.IsValue() && rightSymbol.Value.Equals(-((BigInteger) 1))) {
              middleCode.Operator = MiddleOperator.Decrement;
              middleCode[0] = null;
              middleCode[2] = null;
              m_update = true;
            }
          }
        }
        else if (middleOperator == MiddleOperator.BinarySubtract) {
          Symbol resultSymbol = (Symbol) middleCode[0], // i = i - 1
                 leftSymbol = (Symbol) middleCode[1],
                 rightSymbol = (Symbol) middleCode[2];
        
          if (resultSymbol.Type.IsIntegralPointerArrayStringOrFunction() &&
              resultSymbol.Equals(leftSymbol)) {
            if (rightSymbol.IsValue() &&
                rightSymbol.Value.Equals(((BigInteger) 1))) {
              middleCode.Operator = MiddleOperator.Decrement;
              middleCode[0] = null;
              middleCode[2] = null;
              m_update = true;
            }
            else if (rightSymbol.IsValue() &&
                     rightSymbol.Value.Equals(-((BigInteger) 1))) {
              middleCode.Operator = MiddleOperator.Increment;
              middleCode[0] = null;
              middleCode[2] = null;
              m_update = true;
            }
          }
        }
        else if (middleOperator == MiddleOperator.Assign) {
          Symbol resultSymbol = (Symbol) middleCode[0], // i = i;
                 assignSymbol = (Symbol) middleCode[1];
        
          if (resultSymbol.Type.IsIntegralPointerArrayStringOrFunction() &&
             resultSymbol.Equals(assignSymbol)) {
            middleCode.Operator = MiddleOperator.Empty;
            m_update = true;
          }
        }
      }
    }*/

    private void RemoveTrivialAssign() {
      foreach (MiddleCode middleCode in m_middleCodeList) {
        MiddleOperator middleOperator = middleCode.Operator;
      
        if (middleOperator == MiddleOperator.Assign) {
          Symbol resultSymbol = (Symbol) middleCode[0],
                 assignSymbol = (Symbol) middleCode[1];
        
          if (resultSymbol == assignSymbol) {
            middleCode.Operator = MiddleOperator.Empty;
            m_update = true;
          }
        }
      }
    }

    private static ISet<Symbol> CloneSet(ISet<Symbol> inSet) {
      ISet<Symbol> outSet = new HashSet<Symbol>();

      foreach (Symbol symbol in inSet) {
        outSet.Add(symbol);
      }

      return outSet;
    }

    private void CheckIntegral() {
      Stack<ISet<Symbol>> integralSetStack = new Stack<ISet<Symbol>>();
      ISet<Symbol> integralSet = new HashSet<Symbol>();

      for (int line = 0; line < m_middleCodeList.Count; ++line) {
        MiddleCode middleCode = m_middleCodeList[line];

        object operand0 = middleCode[0],
               operand1 = middleCode[1],
               operand2 = middleCode[2];

        Symbol symbol0 = (operand0 is Symbol) ? ((Symbol) operand0) : null,
               symbol1 = (operand1 is Symbol) ? ((Symbol) operand1) : null,
               symbol2 = (operand2 is Symbol) ? ((Symbol) operand2) : null;

        switch (middleCode.Operator) {
          case MiddleOperator.Empty:
            //Assert.ErrorXXX((symbol0 == null) && (symbol1 == null) && (symbol2 == null));
            break;

          case MiddleOperator.PreCall: {
              middleCode[1] = CloneSet(integralSet);
              integralSetStack.Push(integralSet);
              integralSet = new HashSet<Symbol>();
            }
            break;

          case MiddleOperator.PostCall:
            integralSet = integralSetStack.Pop();
            break;

          case MiddleOperator.Dereference: {
              Symbol resultSymbol = (Symbol) middleCode[0];
              integralSet.Add(resultSymbol.AddressSymbol);
              //integralSet.Remove(resultSymbol.AddressSymbol);

/*              if (resultSymbol.Switch) {
                integralSet.Remove(resultSymbol.AddressSymbol);
              }
              else {
                integralSet.Add(resultSymbol.AddressSymbol);
              }*/
            }
            break;

          /*case MiddleOperator.SysCall: {
              List<Pair<Register,Symbol>> outParameterList = SystemCode.OutParameterList();

              foreach (Pair<Register,Symbol> pair in outParameterList) {
                Symbol outSymbol = pair.Second;
                integralSet.Add(outSymbol);
              }
            }
            break;*/

          case MiddleOperator.Case:
            if (symbol1.AddressSymbol != null) {
              integralSet.Remove(symbol1.AddressSymbol);
            }
            break;

          case MiddleOperator.CaseEnd:
            integralSet.Remove(symbol0);
            break;

          default:
            if ((symbol0 != null) && symbol0.IsTemporary() &&
                (symbol0.AddressSymbol == null) &&
                symbol0.Type.IsIntegralPointerOrArray()) {
              integralSet.Add(symbol0);
            }

            if ((symbol1 != null) && symbol1.IsTemporary() &&
                symbol1.Type.IsIntegralPointerOrArray()) {
              integralSet.Remove(symbol1);
            }

            if ((symbol2 != null) && symbol2.IsTemporary() &&
                symbol2.Type.IsIntegralPointerOrArray()) {
              integralSet.Remove(symbol2);
            }

            if ((symbol0 != null) && (symbol0.AddressSymbol != null)) {
              integralSet.Remove(symbol0.AddressSymbol);
            }

            if ((symbol1 != null) && (symbol1.AddressSymbol != null)) {
              integralSet.Remove(symbol1.AddressSymbol);
            }

            if ((symbol2 != null) && (symbol2.AddressSymbol != null)) {
              integralSet.Remove(symbol2.AddressSymbol);
            }
            break;
        }
      }
    }

    private void CheckFloating() {
      int stackSize = 0;

      for (int line = 0; line < m_middleCodeList.Count; ++line) {
        MiddleCode middleCode = m_middleCodeList[line];

        object operand0 = middleCode[0],
               operand1 = middleCode[1],
               operand2 = middleCode[2];

        Symbol symbol0 = (operand0 is Symbol) ? ((Symbol) operand0) : null,
               symbol1 = (operand1 is Symbol) ? ((Symbol) operand1) : null,
               symbol2 = (operand2 is Symbol) ? ((Symbol) operand2) : null;

        switch (middleCode.Operator) {
          case MiddleOperator.PreCall:
            middleCode[2] = stackSize;
            break;

          case MiddleOperator.PushOne:
          case MiddleOperator.PushZero:
          case MiddleOperator.PushFloat:
          case MiddleOperator.IntegralToFloating:
            ++stackSize;
            break;
          
          case MiddleOperator.GetReturnValue:
            if (symbol0.Type.IsFloating()) {
              ++stackSize;
            }
            break;

          case MiddleOperator.PopFloat:
          case MiddleOperator.FloatingToIntegral:
          case MiddleOperator.DecreaseStack:
            --stackSize;
            break;

          case MiddleOperator.Return:
            stackSize = 0;
            break;

          case MiddleOperator.Equal:
          case MiddleOperator.NotEqual:
          case MiddleOperator.SignedLessThan:
          case MiddleOperator.SignedLessThanEqual:
          case MiddleOperator.SignedGreaterThan:
          case MiddleOperator.SignedGreaterThanEqual: {
              if (symbol1.Type.IsFloating()) {
                stackSize -= 2;
              }
            }
            break;

          case MiddleOperator.BinaryAdd:
          case MiddleOperator.BinarySubtract:
          case MiddleOperator.SignedMultiply:
          case MiddleOperator.SignedDivide:
          case MiddleOperator.Parameter:
            if (symbol1.Type.IsFloating()) {
              --stackSize;
            }
            break;
        }
      }
    }

    public void RemoveClearedCode() {
      for (int index = (m_middleCodeList.Count - 1); index >= 0;--index){        
        if (m_middleCodeList[index].Operator == MiddleOperator.Empty) {
          foreach (MiddleCode middleCode in m_middleCodeList) {
            if (middleCode.IsRelationCarryOrGoto()) {
              int target = (int) middleCode[0];
            
              if (target > index) {
                middleCode[0] = target - 1;
              }
            }
          }
        
          m_middleCodeList.RemoveAt(index);
        }
      }
    }

    // x = (y + 1) * f(x);

    // t = y + 1
    // parameter x
    // call f
  }
}

  /*  
    Ej röda dagar:
      Julafton?
      Midsommarafton?
      Nyårsafton?
  
    Röda dagar:
      Juldagen
      Annandag Jul
      Långfredagen
      Annandag påsk

    switch (x + 1) {
      ...
    }
  
    1. $0 = x + 1
    2. if $0 == 1 goto 11
    3. if $0 == 2 goto 12
    4. ...
  
    switch (a < b) {
      ...
    }
  
    11. if a < b goto 21
    12. goto 23
    13. ...

    21. $0 = 1 soft Backpatch
    22. goto 24 soft goto
  
    23. $0 = 0 soft Backpatch
  
    24. if $0 == 0 goto 1
    25. if $0 == 1 goto 3
    25. if $0 == 2 goto 5
    26. ...  
  */