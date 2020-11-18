using System;
using System.Text;
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
      return (m_middleOperator == MiddleOperator.Goto);
    }

    public bool IsCarry() {
      return (m_middleOperator == MiddleOperator.Carry) ||
             (m_middleOperator == MiddleOperator.NotCarry);
    }

    public bool IsRelation() {
      switch (m_middleOperator) {
        case MiddleOperator.Case:
        case MiddleOperator.Equal:
        case MiddleOperator.NotEqual:
        case MiddleOperator.SignedLessThan:
        case MiddleOperator.SignedLessThanEqual:
        case MiddleOperator.SignedGreaterThan:
        case MiddleOperator.SignedGreaterThanEqual:
        case MiddleOperator.UnsignedLessThan:
        case MiddleOperator.UnsignedLessThanEqual:
        case MiddleOperator.UnsignedGreaterThan:
        case MiddleOperator.UnsignedGreaterThanEqual:
          return true;
        
        default:
          return false;
      }
    }

    public bool IsRelationCarryOrGoto() {
      return IsRelation() || IsCarry() || IsGoto();
    }

    public bool IsBinary() {
      switch (m_middleOperator) {
        case MiddleOperator.BinaryAdd:
        case MiddleOperator.BinarySubtract:
        case MiddleOperator.SignedMultiply:
        case MiddleOperator.SignedDivide:
        case MiddleOperator.SignedModulo:
        case MiddleOperator.UnsignedMultiply:
        case MiddleOperator.UnsignedDivide:
        case MiddleOperator.UnsignedModulo:
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
        case MiddleOperator.BinaryAdd:
        case MiddleOperator.SignedMultiply:
        case MiddleOperator.UnsignedMultiply:
        case MiddleOperator.BitwiseOr:
        case MiddleOperator.BitwiseXOr:
        case MiddleOperator.BitwiseAnd:
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

    public string ToString(object value) {
      if (value != null) {
        return (" "  + value.ToString().Replace("\n", "\\n"));
      }
      else {
        return "";
      }
    }
  }
}