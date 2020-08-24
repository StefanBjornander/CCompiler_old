using System;
using System.Text;
using System.Numerics;
using System.Collections.Generic;

namespace CCompiler {
  public class AssemblyCode {
    public static Register FrameRegister;
    public static Register EllipseRegister;
    public static Register ReturnValueRegister;
    public static Register ReturnPointerRegister;
    public const Register ShiftRegister = Register.cl;

    static AssemblyCode() {
      FrameRegister = RegisterToSize(Register.bp, TypeSize.PointerSize);
      EllipseRegister = RegisterToSize(Register.di, TypeSize.PointerSize);
      ReturnValueRegister = RegisterToSize(Register.bx, TypeSize.PointerSize);
      ReturnPointerRegister =RegisterToSize(Register.bx,TypeSize.PointerSize);
    }

    private AssemblyOperator m_operator;
    private object[] m_operandArray = new object[3];
  
    public AssemblyCode(AssemblyOperator objectOp, object operand0,
                        object operand1, object operand2 = null,
                        int size = 0) {
      m_operator = objectOp;
      m_operandArray[0] = operand0;
      m_operandArray[1] = operand1;
      m_operandArray[2] = operand2;
      FromAdditionToIncrement();
      CheckSize(size);
    }

    public AssemblyOperator Operator {
      get { return m_operator; }
      set { m_operator = value; }
    }

    public object this[int index] {
      get { return m_operandArray[index]; }
      set { m_operandArray[index] = value; }
    }

    private void FromAdditionToIncrement() {      
      if (((Operator == AssemblyOperator.add) ||
           (Operator == AssemblyOperator.sub)) &&
          ((m_operandArray[0] is Track) || (m_operandArray[0] is Register) ||
           (m_operandArray[0] is string))) {

        if ((m_operandArray[1] is int) && (m_operandArray[2] is BigInteger)) {
          CheckIncrement(2);
        }
        else if ((m_operandArray[1] is BigInteger) &&
                 (m_operandArray[2] == null)) {
          CheckIncrement(1);
        }
      }
    }

    private void CheckIncrement(int valueIndex) {
      int value = (int) ((BigInteger) m_operandArray[valueIndex]);

      if (((Operator == AssemblyOperator.add) && (value == 1)) ||
          ((Operator == AssemblyOperator.sub) && (value == -1))) {
        m_operator = AssemblyOperator.inc;
        m_operandArray[valueIndex] = null;
      }
      else if (((Operator == AssemblyOperator.add) && (value == -1)) ||
               ((Operator == AssemblyOperator.sub) && (value == 1))) {
        m_operator = AssemblyOperator.dec;
        m_operandArray[valueIndex] = null;
      }
    }

    private void CheckSize(int size) {
      if ((size != 0) && (m_operandArray[0] is string) &&
          (m_operandArray[1] is string) && (m_operandArray[2] == null)) {
        m_operator = OperatorToSize(m_operator, size);
      }
      
      if ((size != 0) && ((m_operandArray[0] is Register) ||
           (m_operandArray[0] is Track)|| (m_operandArray[0] is String)) &&
          (m_operandArray[1] is int) &&
          (IsUnary() || ((m_operandArray[2] is BigInteger) ||
                         (m_operandArray[2] is String)))) {
        m_operator = OperatorToSize(m_operator, size);
      }
    }

    public bool IsNullary() {
      string operatorName = Enum.GetName(typeof(AssemblyOperator), Operator);
      return operatorName.StartsWith("fldz") ||
             operatorName.StartsWith("fld1") ||
             operatorName.StartsWith("fchs") ||
             operatorName.StartsWith("fadd") ||
             operatorName.StartsWith("fsub") ||
             operatorName.StartsWith("fmul") ||
             operatorName.StartsWith("fdiv") ||
             operatorName.StartsWith("sahf") ||
             operatorName.StartsWith("fcomp") ||
             operatorName.StartsWith("syscall");
    }

    public bool IsUnary() {
      string operatorName = Enum.GetName(typeof(AssemblyOperator), Operator);
      return operatorName.StartsWith("neg") ||
             operatorName.StartsWith("not") ||
             operatorName.StartsWith("inc") ||
             operatorName.StartsWith("dec") ||
             operatorName.StartsWith("mul") ||
             operatorName.StartsWith("imul") ||
             operatorName.StartsWith("div") ||
             operatorName.StartsWith("idiv") ||
             operatorName.StartsWith("fst") ||
             operatorName.StartsWith("fld") ||
             operatorName.StartsWith("fist") ||
             operatorName.StartsWith("fild") ||
             operatorName.StartsWith("pop") ||
             operatorName.StartsWith("int");
    }

    public bool IsBinary() {
      string operatorName = Enum.GetName(typeof(AssemblyOperator), Operator);
      return operatorName.StartsWith("mov") ||
             operatorName.StartsWith("add") ||
             operatorName.StartsWith("sub") ||
             operatorName.StartsWith("and") ||
             operatorName.StartsWith("or") ||
             operatorName.StartsWith("xor") ||
             operatorName.StartsWith("shl") ||
             operatorName.StartsWith("shr") ||
             operatorName.StartsWith("cmp");
    }

    public bool IsJumpRegister() {
      return (Operator == AssemblyOperator.jmp) &&
             (m_operandArray[0] is Register);
    }

    public bool IsJumpNotRegister() {
      return (Operator == AssemblyOperator.jmp) &&
             !(m_operandArray[0] is Register);
    }

    public bool IsCallRegister() {
      return (Operator == AssemblyOperator.call) &&
             (m_operandArray[0] is Register);
    }

    public bool IsCallNotRegister() {
      return (Operator == AssemblyOperator.call) &&
             (m_operandArray[0] is string);
    }

    public bool IsRelationNotRegister() {
      switch (Operator) {
        case AssemblyOperator.je:
        case AssemblyOperator.jne:
        case AssemblyOperator.jl:
        case AssemblyOperator.jle:
        case AssemblyOperator.jg:
        case AssemblyOperator.jge:
        case AssemblyOperator.jb:
        case AssemblyOperator.jbe:
        case AssemblyOperator.ja:
        case AssemblyOperator.jae:
        case AssemblyOperator.jc:
        case AssemblyOperator.jnc:
          return true;

        default:
          return false;        
      }
    }
  
    public static bool RegisterOverlap(Register? register1,
                                       Register? register2) {
      if ((register1 == null) || (register2 == null)) {
        return false;
      }
    
      if (register1.Equals(register2)) {
        return true;
      }

      string name1 = Enum.GetName(typeof(Register), register1),
             name2 = Enum.GetName(typeof(Register), register2);

      if ((name1.Contains("h") && name2.Contains("l")) ||
          (name1.Contains("l") && name2.Contains("h"))) {
        return false;
      }

      name1 = name1.Replace("h", "").Replace("l", "").Replace("x", "").Replace("e", "").Replace("r", "");
      name2 = name2.Replace("h", "").Replace("l", "").Replace("x", "").Replace("e", "").Replace("r", "");
      return name1.Equals(name2);
    }
  
    public static int SizeOfRegister(Register register) {
      string name = Enum.GetName(typeof(Register), register);
    
      if (name.Contains("r")) {
        return 8;
      }
      else if (name.Contains("e")) {
        return 4;
      }
      else if (name.Contains("l") || name.Contains("h")) {
        return 1;
      }
      else {
        return 2;
      }
    }

    public static Register RegisterToSize(Register register, int size) {
      string name = Enum.GetName(typeof(Register), register);

      switch (size) {
        case 1:
          name = name.Replace("x", "l").Replace("e", "").Replace("r", "");
          break;

        case 2:
          Assert.ErrorXXX(!name.Contains("h"));
          name = name.Replace("l", "x").Replace("e", "").Replace("r", "");
          break;

        case 4:
          Assert.ErrorXXX(!name.Contains("h"));
          name = "e" + name.Replace("l", "x").Replace("e", "")
                                             .Replace("r", "");
          break;

        case 8:
          Assert.ErrorXXX(!name.Contains("h"));
          name = "r" + name.Replace("l", "x").Replace("e", "")
                                             .Replace("r", "");
          break;
      }

      return (Register) Enum.Parse(typeof(Register), name);
    }
  
    public static int SizeOfOperator(AssemblyOperator objectOp) {
      string name = Enum.GetName(typeof(AssemblyOperator), objectOp);

      if (name.Contains("_byte")) {
        return 1;
      }
      else if (name.Contains("_word")) {
        return 2;
      }
      else if (name.Contains("_dword")) {
        return 4;
      }
      else { 
        Assert.ErrorXXX(name.Contains("_qword"));
        return 8;
      }
    }
  
    public static AssemblyOperator OperatorToSize
                                   (AssemblyOperator objectOp, int size) {
      string name = Enum.GetName(typeof(AssemblyOperator), objectOp);    
      Assert.ErrorXXX(objectOp != AssemblyOperator.interrupt);
    
      switch (size) {
        case 1:
          name = name + "_byte";
          break;
        
        case 2:
          name = name + "_word";
          break;
        
        case 4:
          name = name + "_dword";
          break;

        case 8:
          name = name + "_qword";
          break;
      }

      Assert.ErrorXXX(name.Contains("_"));
      return ((AssemblyOperator) Enum.Parse(typeof(AssemblyOperator), name));
    }

    public static int SizeOfValue(BigInteger value, AssemblyOperator op) {
      string name = Enum.GetName(typeof(AssemblyOperator), op);

      if (name.StartsWith("mov")) {
        return SizeOfOperator(op);
      }
      else if (name.StartsWith("cmp") && (value == 0)) {
        return 1;
      }
      else {
        return SizeOfValue(value);
      }
    }

    public static int SizeOfValue(BigInteger value) {
      if (value == 0) {
        return 0;
      }
      else if ((-128 <= value) && (value <= 255)) {
        return 1;
      }
      else if ((-32768 <= value) && (value <= 65535)) {
        return 2;
      }
      else if ((-2147483648 <= value) && (value <= 4294967295)) {
        return 4;
      }
      else {
        return 8;
      }
    }

    public override string ToString() {
      object operand0 = m_operandArray[0],
             operand1 = m_operandArray[1],
             operand2 = m_operandArray[2];
      string operatorName = Enum.GetName(typeof(AssemblyOperator),
                                         Operator).Replace("_", " ");

      // lahf; syscall
      if (IsNullary()) {
        Assert.ErrorXXX((operand0 == null) &&  (operand1 == null) && (operand2 == null));
        return "\t" + operatorName;
      }
      else if (IsUnary()) {
        // inc [bp + 2]; inc [global + 4]
        if (((operand0 is Register) || (operand0 is string)) &&
                 (operand1 is int) && (operand2 == null)) {
          return "\t" + operatorName + " [" + operand0 +
                 WithSign(operand1) + "]";
        }
        // inc ax
        else if ((operand0 is Register) && (operand1 == null) &&
                 (operand2 == null)) {
          Assert.ErrorXXX(!(operand0 is BigInteger));
          return "\t" + operatorName + " " + operand0;
        }
      }
      else if (IsBinary()) {
        // mov ax, bx; mov ax, global; mov ax, 123
        if ((operand0 is Register) && ((operand1 is Register) ||
            (operand1 is string) || (operand1 is BigInteger)) &&
            (operand2 == null)) {
          Assert.ErrorXXX(!(operand0 is string));
          return "\t" + operatorName + " " + operand0 + ", " + operand1;
        }
        // mov ax, [bp + 2]; mov ax, [global + 4]
        else if ((operand0 is Register) && ((operand1 is Register) ||
                 (operand1 is string)) && (operand2 is int)) {
          return "\t" + operatorName + " " + operand0 +
                 ", [" + operand1 + WithSign(operand2) + "]";
        }
        // mov [bp + 2], ax; mov [global + 4], ax; mov [bp + 2], 123; mov [global + 4], 123; mov [bp + 2], global; mov [global + 4], global
        else if (((operand0 is Register) || (operand0 is string)) &&
                 (operand1 is int) && ((operand2 is Register) ||
                  (operand2 is string) || (operand2 is BigInteger))) {
          return "\t" + operatorName +
                 " [" + operand0 + WithSign(operand1) + "], " + operand2;
        }
      }
      else if (Operator == AssemblyOperator.label) {
        return "\n " + operand0 + ":";
      }
      else if (Operator == AssemblyOperator.comment) {
        return "\t; " + operand0;
      }
      else if (Operator == AssemblyOperator.define_address) {
        string name = (string) operand0;
        int offset = (int) operand1;
        return "\tdq " + name + WithSign(offset);
      }
      else if (Operator == AssemblyOperator.define_zero_sequence) {
        int size = (int) operand0;
        return "\ttimes " + size + " db 0";
      }
      else if (Operator == AssemblyOperator.define_value) {
        Sort sort = (Sort) operand0;
        object value = operand1;

        if (sort == Sort.String) {
          return "\tdb " + ToVisibleString((string) operand1);
        }
        else {
          string text = operand1.ToString();

          if (((sort == Sort.Float) || (sort == Sort.Double) ||
              (sort == Sort.Long_Double)) && !text.Contains(".")) {
            text += ".0";
          }

          switch (TypeSize.Size(sort)) {
            case 1:
              return "\tdb " + text;

            case 2:
              return "\tdw " + text;

            case 4:
              return "\tdd " + text;

            case 8: 
              return "\tdq " + text;
          }
        }
      }
      else if (IsJumpRegister() || IsCallRegister() ||
               IsCallNotRegister()) {
        return "\tjmp " + operand0;
      }
      else if (Operator == AssemblyOperator.return_address) {
        string target = SymbolTable.CurrentFunction.UniqueName +
                        Symbol.SeparatorId + operand2;
        return "\tmov qword [" + operand0 + WithSign(operand1) + "], " +
               target;
      }
      else if (IsRelationNotRegister() || IsJumpNotRegister()) {
        if (operand2 is int) {
          string label = SymbolTable.CurrentFunction.UniqueName +
                          Symbol.SeparatorId + operand2;
          return "\t" + operatorName + " " + label;
        }
        else if (operand2 is string) {
          return "\t" + operatorName + " " + operand2;
        }
        else {
          int labelIndex = (int) operand1;
          string labelText = MakeMemoryLabel(labelIndex);
          return "\t" + operatorName + " " + labelText;
        }
      }
      else if (Operator == AssemblyOperator.empty) { // XXX
        return null;
      }
      else if (Operator == AssemblyOperator.new_middle_code) {
        return null;
      }

      Assert.ErrorXXX(false);
      return null;
    }

    private static string ToVisibleString(string text) {
      StringBuilder buffer = new StringBuilder();
      bool insideString = false;

      foreach (char c in text) {
        if (Char.IsControl(c) || (c == '\"') || (c == '\'')) {
          if (insideString) {
            buffer.Append("\", " + ((int) c).ToString() + ", ");
            insideString = false;
          }
          else {
            buffer.Append(((int) c).ToString() + ", ");
          }
        }
        else {
          if (insideString) {
            buffer.Append(c);
          }
          else {
            buffer.Append("\"" + c.ToString());
            insideString = true;
          }
        }
      }

      if (insideString) {
        buffer.Append("\", 0");
      }
      else {
        buffer.Append("0");
      }

      return buffer.ToString();
    }

    public static string MakeMemoryLabel(int labelIndex) {
      return "memorycopy" + labelIndex;
    }

    private string WithSign(object value) {
      int offset = (int) value;
      
      if (offset > 0) {
        return " + " + offset;
      }
      else if (offset < 0) {
        return " - " + (-offset);
      }
      else {
        return "";
      }
    }

    public List<byte> ByteList() {
      object operand0 = m_operandArray[0],
             operand1 = m_operandArray[1],
             operand2 = m_operandArray[2];

      if ((Operator == AssemblyOperator.empty) ||
          (Operator == AssemblyOperator.label) ||
          (Operator == AssemblyOperator.comment)) {
        return (new List<byte>());
      }
      else if (Operator == AssemblyOperator.define_address) {
        int offset = (int) operand1;
        List<byte> byteList = new List<byte>(new byte[TypeSize.PointerSize]);
        AssemblyCode.LoadByteList(byteList, 0, TypeSize.PointerSize,
                                  (BigInteger) offset);
        return byteList;
      }
      else if (Operator == AssemblyOperator.define_zero_sequence) {
        int size = (int) operand0;
        return (new List<byte>(new byte[size]));
      }
      else if (Operator == AssemblyOperator.define_value) {
        Sort sort = (Sort) operand0;
        object value = operand1;

        if (sort == Sort.Pointer) {
          List<byte> byteList = new List<byte>(new byte[TypeSize.PointerSize]);

          if (value is string) {
            AssemblyCode.LoadByteList(byteList, 0, TypeSize.PointerSize,
                                      BigInteger.Zero);
          }
          else if (value is StaticAddress) {
            StaticAddress staticAddress = (StaticAddress) value;
            int offset = staticAddress.Offset;
            AssemblyCode.LoadByteList(byteList, 0, TypeSize.PointerSize,
                                      (BigInteger) offset);
          }
          else {
            AssemblyCode.LoadByteList(byteList, 0, TypeSize.PointerSize,
                                      (BigInteger) value);
          }

          return byteList;
        }
        else if (sort == Sort.Float) {
          float floatValue = (float) ((decimal) operand0);
          return (new List<byte>(BitConverter.GetBytes(floatValue)));
        }
        else if ((sort == Sort.Double) || (sort == Sort.Long_Double)) {
          double doubleValue = (double) ((decimal) value);
          return (new List<byte>(BitConverter.GetBytes(doubleValue)));
        }
        else if (sort == Sort.String) {
          string text = (string) value;
          List<byte> byteList = new List<byte>();

          foreach (char c in text) {
            byteList.Add((byte) c);
          }

          byteList.Add((byte) 0);
          return byteList;
        }
        else {
          int size = TypeSize.Size(sort);
          List<byte> byteList = new List<byte>(new byte[size]);
          AssemblyCode.LoadByteList(byteList, 0, size, (BigInteger) value);
          return byteList;
        }
      }
      else if (IsJumpRegister() || IsCallRegister()) {
        Register register = (Register) operand0;
        return LookupByteArray(AssemblyOperator.jmp, register);
      }
      else if (IsCallNotRegister()) {
        List<byte> byteList =
          LookupByteArray(AssemblyOperator.jmp, TypeSize.PointerSize);
        LoadByteList(byteList, byteList.Count - TypeSize.PointerSize,
                     TypeSize.PointerSize, 0);
        return byteList;
      }
      else if (Operator == AssemblyOperator.return_address) {
        Register register = (Register) operand0;
        int offset = (int) operand1;
        int size = SizeOfValue(offset);
        int address = (int)((BigInteger)operand2);
        List<byte> byteList =
          LookupByteArray(AssemblyOperator.mov_word, register,
                          size, TypeSize.PointerSize);
        LoadByteList(byteList, byteList.Count - (size + TypeSize.PointerSize),
                     size, offset);
        LoadByteList(byteList, byteList.Count - TypeSize.PointerSize,
                     TypeSize.PointerSize, address);
        return byteList;
      }
      else if (IsRelationNotRegister() || IsJumpNotRegister()) {
        int address = (int) operand0;
        int size = ((address >= -128) && (address <= 127)) ? 1 : 2;
        List<byte> byteList = LookupByteArray(Operator, size);
        LoadByteList(byteList, byteList.Count - size, size, address);
        return byteList;
      }













      // mov ax, bx
      else if ((operand0 is Register) && (operand1 is Register) && (operand2 == null)) {
        Register toRegister = (Register) operand0,
                 fromRegister = (Register) operand1;
        return LookupByteArray(Operator, toRegister, fromRegister);
      }
      // mov ax, 123
      else if ((operand0 is Register) && (operand1 is BigInteger) && (operand2 == null)) {
        Register register = (Register) operand0;
        BigInteger value = (BigInteger) operand1;
        int size = ((Operator == AssemblyOperator.mov) ||
                    (Operator == AssemblyOperator.and))
                   ? SizeOfRegister(register) : SizeOfValue(value);
        List<byte> byteList = LookupByteArray(Operator, register, size);
        LoadByteList(byteList, byteList.Count - size, size, value);
        return byteList;
      }
      // mov ax, global
      else if ((operand0 is Register) && (operand1 is string) && (operand2 == null)) {
        Register register = (Register) operand0;
        int size = SizeOfRegister(register);
        List<byte> byteList = LookupByteArray(Operator, register, size);
        LoadByteList(byteList, byteList.Count - size, size, 0);
        return byteList;
      }



      /*//	cmp global, bx
      else if (((operand0 is string) || (operand0 == null)) &&
               (operand1 is Register) && (operand2 == null)) {
        Assert.ErrorXXX(Operator == AssemblyOperator.cmp);
        Register fromRegister = (Register) operand1;
        List<byte> byteList = LookupByteArray(Operator, null, fromRegister);
        return byteList; 
      }

      //	cmp global, 123
      else if (((operand0 is string) || (operand0 == null)) &&
               (operand1 is BigInteger) && (operand2 == null)) {
        Assert.ErrorXXX(Operator == AssemblyOperator.cmp);
        BigInteger value = (BigInteger) operand1;
        int size = SizeOfValue(value);
        List<byte> byteList = LookupByteArray(Operator, null, size);
        LoadByteList(byteList, byteList.Count - size, size, value);
        return byteList;
      }

      //	cmp global, global
      else if (((operand0 is string) || (operand0 == null)) &&
               ((operand1 is string) || (operand1 == null)) &&
               (operand2 == null)) {
        Assert.ErrorXXX(Operator == AssemblyOperator.cmp);
        return LookupByteArray(Operator, TypeSize.PointerSize, TypeSize.PointerSize);
      }*/



      // mov [bp + 2], ax
      else if ((operand0 is Register) && (operand1 is int) &&
               (operand2 is Register)) {
        Register baseRegister = (Register) operand0,
                 fromRegister = (Register) operand2;
        int offset = (int) operand1;
        int size = SizeOfValue(offset);
        List<byte> byteList =
          LookupByteArray(Operator, baseRegister, size, fromRegister);
        LoadByteList(byteList, byteList.Count - size, size, offset);
        return byteList; 
      }
      // mov [bp + 2], 123
      else if ((operand0 is Register) && (operand1 is int) &&
               (operand2 is BigInteger)) {
        Register baseRegister = (Register) operand0;
        int offset = (int) operand1;
        BigInteger value = (BigInteger) operand2;
        int offsetSize = SizeOfValue(offset),
            valueSize = SizeOfValue(value, Operator);
        List<byte> byteList =
          LookupByteArray(Operator, baseRegister, offsetSize, valueSize);
        LoadByteList(byteList, byteList.Count - (offsetSize + valueSize),
                     offsetSize, offset);
        LoadByteList(byteList, byteList.Count - valueSize,
                     valueSize, value);
        return byteList;
      }
      // mov [bp + 2], global
      else if ((operand0 is Register) && (operand1 is int) &&
               (operand2 is string)) {
        Register baseRegister = (Register) operand0;
        int offset = (int) operand1;
        int offsetSize = SizeOfValue(offset);
        List<byte> byteList =
          LookupByteArray(Operator, baseRegister, offsetSize,
                          TypeSize.PointerSize);
        LoadByteList(byteList, byteList.Count -
                    (offsetSize + TypeSize.PointerSize), offsetSize, offset);
        LoadByteList(byteList, byteList.Count - TypeSize.PointerSize,
                     TypeSize.PointerSize, 0);
        return byteList; 
      }
      
      
      
      // mov [global + 4], ax
      else if (((operand0 is string) || (operand0 == null)) &&
               (operand1 is int) && (operand2 is Register)) {
        int offset = (int) operand1;
        Register fromRegister = (Register)operand2;
        List<byte> byteList =
          LookupByteArray(Operator, null, TypeSize.PointerSize, fromRegister);
        LoadByteList(byteList, byteList.Count - TypeSize.PointerSize,
                     TypeSize.PointerSize, offset);
        return byteList; 
      }
      // mov [global + 4], 123
      else if (((operand0 is string) || (operand0 == null)) &&
               (operand1 is int) && (operand2 is BigInteger)) {
        int offset = (int) operand1;
        BigInteger value = (BigInteger) operand2;
        int valueSize = SizeOfValue(value, Operator);
        List<byte> byteList =
          LookupByteArray(Operator, null, TypeSize.PointerSize, valueSize);
        LoadByteList(byteList, byteList.Count - (TypeSize.PointerSize +
                     valueSize), TypeSize.PointerSize, offset);
        LoadByteList(byteList, byteList.Count - valueSize,
                     valueSize, value);
        return byteList; 
      }



      // mov [global + 4], global
      else if (((operand0 is string) || (operand0 == null)) &&
               (operand1 is int) && ((operand1 is string) || (operand1 == null))) {
        int offset = (int) operand2;
        int size = SizeOfValue(offset);
        List<byte> byteList = LookupByteArray(Operator, null, null, size);
        LoadByteList(byteList, byteList.Count - size, size, offset);
        return byteList; 
      }



      // mov ax, [bp + 2]
      else if ((operand0 is Register) && (operand1 is Register) &&
               (operand2 is int)) {
        Register toRegister = (Register) operand0,
                 baseRegister = (Register) operand1;
        int offset = (int) operand2;
        int size = SizeOfValue(offset);
        List<byte> byteList =
          LookupByteArray(Operator, toRegister, baseRegister, size);
        LoadByteList(byteList, byteList.Count - size, size, offset);
        return byteList; 
      }

      /*// cmp global, [bp + 2]
      else if (((operand0 is string) || (operand0 == null)) &&
               (operand1 is Register) && (operand2 is int)) {
        Assert.ErrorXXX(Operator == AssemblyOperator.cmp);
        Register baseRegister = (Register) operand1;
        int offset = (int) operand2;
        int size = SizeOfValue(offset);
        List<byte> byteList =
          LookupByteArray(Operator, null, baseRegister, size);
        LoadByteList(byteList, byteList.Count - size, size, offset);
        return byteList; 
      }*/

      // mov ax, [global + 4]
      else if ((operand0 is Register) && ((operand1 is string) ||
                (operand1 == null)) && (operand2 is int)) {
        Register toRegister = (Register) operand0;
        int offset = (int) operand2;
        List<byte> byteList =
          LookupByteArray(Operator, toRegister, null, TypeSize.PointerSize);
        LoadByteList(byteList, byteList.Count - TypeSize.PointerSize,
                     TypeSize.PointerSize, offset);
        return byteList; 
      }


      /*// cmp global, [global + 4]
      else if (((operand0 is string) || (operand0 == null)) &&
               ((operand1 is string) || (operand1 == null)) &&
               (operand2 is int)) {
        int offset = (int) operand2;
        List<byte> byteList =
          LookupByteArray(Operator, null, null, TypeSize.PointerSize);
        LoadByteList(byteList, byteList.Count - TypeSize.PointerSize,
                     TypeSize.PointerSize, offset);
        return byteList; 
      }*/











      // inc ax
      else if ((operand0 is Register) && (operand1 == null) && (operand2 == null)) {
        Register register = (Register) operand0;
        return LookupByteArray(Operator, register);
      }
      // int 33
      else if ((operand0 is BigInteger) && (operand1 == null) && (operand2 == null)) {
        BigInteger value = (BigInteger) operand0;
        int size = SizeOfValue(value);
        List<byte> byteList = LookupByteArray(Operator, size);
        LoadByteList(byteList, byteList.Count - size, size, value);
        return byteList;
      }



      // inc [bp + 2]
      else if ((operand0 is Register) && (operand1 is int) && (operand2 == null)) {
        Register baseRegister = (Register) operand0;
        int offset = (int) operand1;
        int size = SizeOfValue(offset);
        List<byte> byteList = LookupByteArray(Operator, baseRegister, size);
        LoadByteList(byteList, byteList.Count - size, size, offset);
        return byteList;
      }
      // inc [global + 4]
      else if (((operand0 is string) || (operand0 == null)) &&
               (operand1 is int) && (operand2 == null)) {
        int offset = (int) operand1;
        List<byte> byteList =
          LookupByteArray(Operator, null, TypeSize.PointerSize);
        LoadByteList(byteList, byteList.Count - TypeSize.PointerSize,
                     TypeSize.PointerSize, offset);
        return byteList;
      }



      // lahf
      else if ((operand0 == null) && (operand1 == null) &&
               (operand2 == null)) {
        return LookupByteArray(Operator);
      }

      Assert.ErrorXXX(false);
      return null;
    }

    public static void LoadByteList(IList<byte> byteList, int index,
                                    int size, BigInteger value) {
      while (byteList.Count < (index + size)) {
        byteList.Add((byte) 0);
      }

      switch (size) {
        case 1: {
            if (value < 0) {
              byteList[index] = (byte) ((sbyte) value);
            }
            else {
              byteList[index] = (byte) value;
            }
          }
          break;

        case 2: {
            if (value < 0) {
              short shortValue = (short) value; 
              byteList[index] = (byte) ((sbyte) shortValue);
              byteList[index + 1] = (byte) ((sbyte) (shortValue >> 8));
            }
            else {
              ushort ushortValue = (ushort) value; 
              byteList[index] = (byte) ushortValue;
              byteList[index + 1] = (byte) (ushortValue >> 8);
            }
          }
          break;

        case 4: {
            if (value < 0) {
              int intValue = (int) value; 
              byteList[index] = (byte) ((sbyte) intValue);
              byteList[index + 1] = (byte) ((sbyte) (intValue >> 8));
              byteList[index + 2] = (byte) ((sbyte) (intValue >> 16));
              byteList[index + 3] = (byte) ((sbyte) (intValue >> 24));
            }
            else {
              uint uintValue = (uint) value; 
              byteList[index] = (byte) uintValue;
              byteList[index + 1] = (byte) (uintValue >> 8);
              byteList[index + 2] = (byte) (uintValue >> 16);
              byteList[index + 3] = (byte) (uintValue >> 24);
            }
          }
          break;
      }
    }

    public static List<byte> LookupByteArray(AssemblyOperator objectOp,
                      object operand1 = null, object operand2 = null,
                      object operand3 = null) {
      if ((objectOp == AssemblyOperator.shl) ||
          (objectOp == AssemblyOperator.shr)) {
        operand1 = (operand1 is BigInteger) ? 0L : operand1;
        operand2 = (operand2 is BigInteger) ? 0L : operand2;
        operand3 = (operand3 is BigInteger) ? 0L : operand3;
      }

      ObjectCodeInfo info =
        new ObjectCodeInfo(objectOp, operand1, operand2, operand3);
      byte[] byteArray = ObjectCodeTable.MainArrayMap[info];
      Assert.ErrorXXX(byteArray != null);
      List<byte> byteList = new List<byte>();

      foreach (byte b in byteArray) {
        byteList.Add(b);
      }

      return byteList;
    }
  }
}