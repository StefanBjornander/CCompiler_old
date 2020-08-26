using System;
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

    public static bool IsMultiply(MiddleOperator middleOperator) {
      switch (middleOperator) {
        case MiddleOperator.SignedMultiply:
        case MiddleOperator.SignedDivide:
        case MiddleOperator.SignedModulo:
        case MiddleOperator.UnsignedMultiply:
        case MiddleOperator.UnsignedDivide:
        case MiddleOperator.UnsignedModulo:
          return true;
        
        default:
          return false;
      }
    }

    public static bool IsModulo(MiddleOperator middleOperator) {
      switch (middleOperator) {
        case MiddleOperator.SignedModulo:
        case MiddleOperator.UnsignedModulo:
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

    public static bool IsAssociative(MiddleOperator middleOperator) {
      switch (middleOperator) {
        case MiddleOperator.BinaryAdd:
        case MiddleOperator.BinarySubtract:
        case MiddleOperator.BitwiseAnd:
        case MiddleOperator.BitwiseOr:
        case MiddleOperator.BitwiseXOr:
        case MiddleOperator.Index:
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
 
    private static IDictionary<MiddleOperator, string> OpToTextMap =
      new Dictionary<MiddleOperator, string>() {
        {MiddleOperator.Assign, "="},
        {MiddleOperator.BinaryAdd, "+"},
        {MiddleOperator.BinarySubtract, "-"},
        {MiddleOperator.SignedMultiply, "*"},
        {MiddleOperator.SignedDivide, "/"},
        {MiddleOperator.SignedModulo, "%"},
        {MiddleOperator.UnsignedMultiply, "*"},
        {MiddleOperator.UnsignedDivide, "/"},
        {MiddleOperator.UnsignedModulo, "%"},
        {MiddleOperator.BitwiseOr, "|"},
        {MiddleOperator.BitwiseXOr, "^"},
        {MiddleOperator.BitwiseAnd, "&"},
        {MiddleOperator.ShiftLeft, "<<"},
        {MiddleOperator.ShiftRight, ">>"},
        {MiddleOperator.LogicalOr, "||"},
        {MiddleOperator.LogicalAnd, "&&"},
        {MiddleOperator.Increment, "++"},
        {MiddleOperator.Decrement, "--"},
        {MiddleOperator.EqualZero, "==0"},
        {MiddleOperator.Equal, "=="},
        {MiddleOperator.NotEqual, "!="},
        {MiddleOperator.SignedLessThan, "<"},
        {MiddleOperator.SignedLessThanEqual, "<="},
        {MiddleOperator.SignedGreaterThan, ">"},
        {MiddleOperator.SignedGreaterThanEqual, ">="},
        {MiddleOperator.UnsignedLessThan, "<"},
        {MiddleOperator.UnsignedLessThanEqual, "<="},
        {MiddleOperator.UnsignedGreaterThan, ">"},
        {MiddleOperator.UnsignedGreaterThanEqual, ">="},
        {MiddleOperator.UnaryAdd, "+"},
        {MiddleOperator.UnarySubtract, "-"},
        {MiddleOperator.LogicalNot, "!"},
        {MiddleOperator.BitwiseNot, "~"},
        {MiddleOperator.Address, "&"},
        {MiddleOperator.Dereference, "*"},
        {MiddleOperator.Dot, "."},
        {MiddleOperator.Arrow, "->"},
        {MiddleOperator.Comma, ","},
        {MiddleOperator.IntegralToIntegral, "int_to_int"},
        {MiddleOperator.IntegralToFloating, "int_to_float"},
        {MiddleOperator.FloatingToIntegral, "float_to_int"}
      };
 
    public bool IsUnaryXXX() {
      switch (m_middleOperator) {
        case MiddleOperator.UnaryAdd:
        case MiddleOperator.UnarySubtract:
        case MiddleOperator.BitwiseNot:
        case MiddleOperator.Address:
          return true;
        
        default:
          return false;
      }
    }

    private string opToText() {
      if (OpToTextMap.ContainsKey(m_middleOperator)) {
        if (IsUnaryXXX()) {
          return OpToTextMap[m_middleOperator];
        }
        else {
          return " " + OpToTextMap[m_middleOperator] + " ";
        }
      }
      else {
        return Enum.GetName(typeof(MiddleOperator), m_middleOperator).ToLower() + " ";
      }
    }
  
    public override string ToString() {
      object operand0 = m_operandArray[0],
             operand1 = m_operandArray[1],
             operand2 = m_operandArray[2];
           
      Symbol symbol0 = (operand0 is Symbol) ? ((Symbol) operand0) : null,
             symbol1 = (operand1 is Symbol) ? ((Symbol) operand1) : null,
             symbol2 = (operand2 is Symbol) ? ((Symbol) operand2) : null;         
    
      switch (m_middleOperator) {
        /*case MiddleOperator.FunctionStart:
          return "function start " + operand0;*/

        case MiddleOperator.FunctionEnd:
          return "function end " + operand0;
        
        case MiddleOperator.PreCall:
          if (operand1 != null) {
            int setSize = ((ISet<Symbol>) operand1).Count,
                stackSize = (int) operand2;
            return "call header integral " + ((setSize != 0) ? "no zero" : "zero") + " " + setSize +
                   " stack " + ((stackSize != 0) ? "no zero" : "zero") + " " + stackSize;
          }
          else {
            return "call header";
          }
        
        /*case MiddleOperator.SaveTemporary:
          return "save temparary " + operand0;*/

        case MiddleOperator.Call:
          if (SymbolTable.CurrentFunction.Type.IsEllipse() && symbol0.Type.IsEllipse()) {
            return "call function ellipse-ellipse " + symbol0 + ", extra " + operand2;
          }
          else if (SymbolTable.CurrentFunction .Type.IsEllipse()) {
            return "call function ellipse-noellipse " + symbol0;
          }
          else if (symbol0.Type.IsEllipse() ) {
            return "call function noellipse-ellipse " + symbol0 + ", extra " + operand2;
          }
          else {
            return "call function noellipse-noellipse " + symbol0;
          }

        case MiddleOperator.PostCall:
          if (operand0 != null) {
            int setSize = ((ISet<Symbol>) operand0).Count,
                stackSize = (int) operand1;
            return "post call integral " + ((setSize != 0) ? "no zero" : "zero") + " " + setSize +
                   " stack " + ((stackSize != 0) ? "no zero" : "zero") + " " + stackSize;
          }
          else {
            return "post call";
          }

        /*case MiddleOperator.PreCall:
          if (operand1 != null) {
            int stackSize = (int) operand1,
                setSize = ((ISet<Symbol>) operand2).Count;
            return "pre call record " + operand0.ToString() + " stack " + ((stackSize != 0) ? "no zero" : "zero") + " " + stackSize +" integral " + ((setSize != 0) ? "no zero" : "zero") + " " + setSize;
          }
          else {
            return "pre call";
          }

        case MiddleOperator.PostCallIntegral:
          if (operand0 != null) {
            int stackSize = ((ISet<Symbol>)operand2).Count;
            return "post call integral  " + ((stackSize != 0) ? "no zero" : "zero") + " " + stackSize;
          }
          else {
            return "post call integral";
          }

        case MiddleOperator.PostCallFloating:
          if (operand1 != null) {
            int size = (int) operand1;
            return "post call floating record " + operand0.ToString() +
                   " stack " + ((size != 0) ? "no zero" : "zero") + " " + size +
                   " return float" + (((bool) operand2) ? "true" : "false");
          }
          else {
            return "post call floating";
          }*/

        case MiddleOperator.Parameter:
          return "parameter " + operand1 + ", offset " + operand2;

        case MiddleOperator.Return:
         return "return";

        case MiddleOperator.Exit:
          return "exit " + operand1;
        
        case MiddleOperator.IntegralToIntegral:
        case MiddleOperator.IntegralToFloating:
        case MiddleOperator.FloatingToIntegral:
          return operand0 + " =" + opToText() + operand1 +
                        " (" + Enum.GetName(typeof(Sort), symbol1.Type.Sort) +
                        " -> " + Enum.GetName(typeof(Sort), symbol0.Type.Sort) + ")";

        case MiddleOperator.UnaryAdd:
        case MiddleOperator.UnarySubtract:
        case MiddleOperator.BitwiseNot:
        case MiddleOperator.Address:
          return operand0 + " = " + opToText() + operand1;

        case MiddleOperator.Carry:
        case MiddleOperator.NotCarry:
          return "if " + opToText() + "goto " + operand0;

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
          if (operand0 is int) {
            return "if " + operand1 + opToText() + operand2 + " goto " + operand0;
          }
          else if (operand0 is MiddleCode) {
            return "if " + operand1 + opToText() + operand2 + " goto <MiddleCode>";
          }
          else {
            return "if " + operand1 + opToText() + operand2 + " goto <null>";
          }

        case MiddleOperator.Goto:
          if (operand0 is int) {
            return "goto " + operand0;
          }
          else if (operand0 is MiddleCode) {
            return "goto <MiddleCode>";
          }
          else {
            return "goto <null>";
          }

        case MiddleOperator.Empty:
          return "empty";

        case MiddleOperator.Assign:
          return operand0 + opToText() + operand1;
  
        case MiddleOperator.BinaryAdd:
        case MiddleOperator.BinarySubtract:
        case MiddleOperator.SignedMultiply:
        case MiddleOperator.SignedDivide:
        case MiddleOperator.SignedModulo:
        case MiddleOperator.UnsignedMultiply:
        case MiddleOperator.UnsignedDivide:
        case MiddleOperator.UnsignedModulo:
        case MiddleOperator.BitwiseAnd:
        case MiddleOperator.BitwiseOr:
        case MiddleOperator.BitwiseXOr:
        case MiddleOperator.ShiftLeft:
        case MiddleOperator.ShiftRight:
          return operand0 + " = " + operand1 + opToText() + operand2;

        case MiddleOperator.Increment:
          return "++" + operand1;

        case MiddleOperator.Decrement:
          return "--" + operand1;
        
        case MiddleOperator.AssignRegister:
        //case MiddleOperator.SaveFromRegister:
          return operand0 + " = " + operand1;
        
        case MiddleOperator.Interrupt:
          return "interrupt " + operand0;

        case MiddleOperator.SysCall:
          return "syscall";

        case MiddleOperator.GetReturnValue:
          return operand0 + " = return_value";
        
        case MiddleOperator.SetReturnValue:
          return "return_value = " + operand1;
        
        case MiddleOperator.Dereference:
          if (((int) operand2) != 0) {
            return operand0 + " = *" + operand1 + ", offset " + operand2;
          }
          else {
            return operand0 + " = *" + operand1;
          }

        case MiddleOperator.DecreaseStack:
          return "decrease stack";

        case MiddleOperator.PushZero:
          return "push 0";

        case MiddleOperator.PushOne:
          return "push 1";

        case MiddleOperator.PushFloat:
          return "push float " + operand0.ToString();
        
        case MiddleOperator.PopFloat:
          if (operand0 != null) {
            return "pop float " + operand0;
          }
          else {
            return "pop float empty X";
          }

        case MiddleOperator.PopEmpty:
          return "Pop empty";

        case MiddleOperator.TopFloat:
          return "top float " + operand0;

        case MiddleOperator.InspectRegister:
          return operand0 + " = " + operand1;

        case MiddleOperator.InspectFlagbyte:
          return operand0 + " = flagbyte";
        
        case MiddleOperator.JumpRegister:
          return "jump to " + operand1;

        /*case MiddleOperator.ClearRegisters:
          return "clear registers";*/

        case MiddleOperator.CheckTrackMapFloatStack:
          return "check track map float stack";

        case MiddleOperator.Initializer:
          return "initializer " + operand0.ToString();

        case MiddleOperator.InitializerZero:
          return "initializer zero " + operand0.ToString();

        case MiddleOperator.Case:
          return "case " + operand1.ToString() + " == " + operand2.ToString() + " goto " + operand0.ToString();

        case MiddleOperator.CaseEnd:
          return "case end " + operand0.ToString();

        default:
          Assert.ErrorXXX(false);
          return null;
      }
    }
  }
}