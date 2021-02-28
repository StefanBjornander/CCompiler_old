using System;
using System.Text;
using System.Numerics;
using System.Collections.Generic;

namespace CCompiler {
  public class MiddleCode {
    private MiddleOperator m_middleOperator;
    private object[] m_operandArray = new object[3];

    public MiddleCode(MiddleOperator middleOp, object operand0 = null,
                      object operand1 = null, object operand2 = null) {
      m_middleOperator = middleOp;
      m_operandArray[0] = operand0;
      m_operandArray[1] = operand1;
      m_operandArray[2] = operand2;

      if (m_middleOperator == MiddleOperator.InitializerZero) {
         int size = (int) operand0;
      }

      /*string s = ToString();
      if ((s != null) && s.Equals("PopEmpty")) {
        int i = 1;
      }*/
    }

    public MiddleOperator Operator {
      get { return m_middleOperator; }
      set { m_middleOperator = value; }
    }

    public object this[int index] {
      get { return m_operandArray[index]; }
      set { m_operandArray[index] = value;  }
    }

    public void Clear() {
      m_middleOperator = MiddleOperator.Empty;
      m_operandArray[0] = null;
      m_operandArray[1] = null;
      m_operandArray[2] = null;
    }

    public bool IsGoto() {
      return (m_middleOperator == MiddleOperator.Jump);
    }

    public bool IsCarry() {
      return (m_middleOperator == MiddleOperator.Carry) ||
             (m_middleOperator == MiddleOperator.NotCarry);
    }

    public static bool IsRelation(MiddleOperator middleOperator) {
      switch (middleOperator) {
        case MiddleOperator.Case:
        case MiddleOperator.Equal:
        case MiddleOperator.NotEqual:
        case MiddleOperator.LessThan:
        case MiddleOperator.LessThanEqual:
        case MiddleOperator.GreaterThan:
        case MiddleOperator.GreaterThanEqual:
          return true;
        
        default:
          return false;
      }
    }

    public bool IsRelation() {
      return IsRelation(m_middleOperator);
    }

    public bool IsRelationCarryOrGoto() {
      return IsRelation() || IsCarry() || IsGoto();
    }

    public bool IsBinary() {
      switch (m_middleOperator) {
        case MiddleOperator.Add:
        case MiddleOperator.Subtract:
        case MiddleOperator.Multiply:
        case MiddleOperator.Divide:
        case MiddleOperator.Modulo:
        case MiddleOperator.LogicalOr:
        case MiddleOperator.LogicalAnd:
        case MiddleOperator.BitwiseOr:
        case MiddleOperator.BitwiseXOr:
        case MiddleOperator.BitwiseAnd:
        case MiddleOperator.ShiftLeft:
        case MiddleOperator.ShiftRight:
          return true;
        
        default:
          return false;
      }
    }
 
    public bool IsCommutative() {
      switch (m_middleOperator) {
        case MiddleOperator.Add:
        case MiddleOperator.Multiply:
        case MiddleOperator.BitwiseOr:
        case MiddleOperator.BitwiseXOr:
        case MiddleOperator.BitwiseAnd:
          return true;
        
        default:
          return false;
      }
    }

    public static bool IsMultiply(MiddleOperator middleOp) {
      switch (middleOp) {
        case MiddleOperator.Multiply:
        case MiddleOperator.Divide:
        case MiddleOperator.Modulo:
                  return true;
        
        default:
          return false;
      }
    }

    public static bool IsShift(MiddleOperator middleOp) {
      switch (middleOp) {
        case MiddleOperator.ShiftLeft:
        case MiddleOperator.ShiftRight:
          return true;
        
        default:
          return false;
      }
    }

    public override string ToString() {
      /*if (m_middleOperator == MiddleOperator.IntegralToIntegral) {
        int toSize = ((Symbol) m_operandArray[0]).Type.Size(),
            fromSize = ((Symbol) m_operandArray[1]).Type.Size();
        return m_middleOperator + " " + fromSize + " -> " +
               toSize + " " + ToString(m_operandArray[0]) +
               ToString(m_operandArray[1]) + ToString(m_operandArray[2]);
            
      }
      else*/ {
        return m_middleOperator + ToString(m_operandArray[0]) +
               ToString(m_operandArray[1]) + ToString(m_operandArray[2]);
      }
    }

    private static string ToString(object value) {
      if (value != null) {
        return (" "  + value.ToString().Replace("\n", "\\n"));
      }
      else {
        return "";
      }
    }
  }
}