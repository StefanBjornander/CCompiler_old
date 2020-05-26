using System;
using System.Text;
using System.Numerics;
using System.Collections.Generic;

namespace CCompiler {
  public class AssemblyCode {
    public const byte NopOperator = 144; // -112;
    public const byte ShortJumpOperator = 235; // -21;

    public static Register FrameRegister;
    public static Register EllipseRegister;
    public static Register ReturnValueRegister;
    public static Register ReturnPointerRegister;
    public const Register ShiftRegister = Register.cl;

    static AssemblyCode() {
      FrameRegister = RegisterToSize(Register.bp, Type.PointerSize);
      EllipseRegister = RegisterToSize(Register.di, Type.PointerSize);
      ReturnValueRegister = RegisterToSize(Register.bx, Type.PointerSize);
      ReturnPointerRegister = RegisterToSize(Register.bx, Type.PointerSize);
    }

    private AssemblyOperator m_operator;
    private object[] m_operandArray = new object[3];
  
    public AssemblyCode(AssemblyOperator objectOp, object operand0,
                        object operand1, object operand2 = null, int assemblyListIndex = -1) {
      m_operator = objectOp;
      m_operandArray[0] = operand0;
      m_operandArray[1] = operand1;
      m_operandArray[2] = operand2;
      FromAdditionToIncrement();
    }

    public void FromAdditionToIncrement() {
      object operand0 = m_operandArray[0],
             operand1 = m_operandArray[1],
             operand2 = m_operandArray[2];

      if (((m_operator == AssemblyOperator.add) || (m_operator == AssemblyOperator.sub)) &&
          (operand0 is Track) && (operand1 is BigInteger) && (operand2 == null)) {
        BigInteger value = (BigInteger) operand1;

        if (((m_operator == AssemblyOperator.add) && (value == 1)) ||
            ((m_operator == AssemblyOperator.sub) && (value == -1))) {
          m_operator = AssemblyOperator.inc;
          m_operandArray[1] = null;
        }
        else if (((m_operator == AssemblyOperator.sub) && (value == 1)) ||
                 ((m_operator == AssemblyOperator.add) && (value == -1))) {
          m_operator = AssemblyOperator.dec;
          m_operandArray[1] = null;
        }
      }
    }

    public AssemblyOperator Operator {
      get { return m_operator; }
      set { m_operator = value; }
    }

    public object this[int index] {
      get { return m_operandArray[index]; }
      set { m_operandArray[index] = value; }
    }
  
    public object GetOperand(int index) {
      return m_operandArray[index];
    }
  
    public void SetOperand(int index, object operand) {
      Assert.Error((index >= 0) && (index < 3));
      m_operandArray[index] = operand;
    }

    // ------------------------------------------------------------------------
  
    public bool IsJumpRegister() {
      return (m_operator == AssemblyOperator.jmp) && (m_operandArray[0] is Register);
    }

    public bool IsJumpNotRegister() {
      return (m_operator == AssemblyOperator.jmp) && !(m_operandArray[0] is Register);
    }

    public bool IsCall() {
       return IsCallRegister() || IsCallNotRegister();
    }

    public bool IsCallRegister() {
      return (m_operator == AssemblyOperator.call) && (m_operandArray[0] is Register);
    }

    public bool IsCallNotRegister() {
      return (m_operator == AssemblyOperator.call) && !(m_operandArray[0] is Register);
    }

    private bool IsRelationRegister() {
      return IsRelation() && (m_operandArray[0] is Register);
    }
  
    public bool IsRelationNotRegister() {
      return IsRelation() && !(m_operandArray[0] is Register);
    }
  
    private bool IsRelation() {
      switch (m_operator) {
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
  
    // ------------------------------------------------------------------------
  
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

      name1 = (name1.Length == 3) ? name1.Substring(1) : name1;
      name2 = (name2.Length == 3) ? name2.Substring(1) : name2;

      if ((name1.Contains("h") && name2.Contains("l")) ||
          (name1.Contains("l") && name2.Contains("h"))) {
        return false;
      }
    
      name1 = (name1.Contains("h") || name1.Contains("l") ||
               name1.Contains("x")) ? name1.Substring(0, 1) : name1;
      name2 = (name2.Contains("h") || name2.Contains("l") ||
               name2.Contains("x")) ? name2.Substring(0, 1) : name2;

      return name1.Equals(name2);
    }
  
    public static int SizeOfRegister(Register register) {
      string name = Enum.GetName(typeof(Register), register);
    
      if (name.Contains("r")) {
        return Type.Bytes8;
      }
      else if (name.Contains("e")) {
        return Type.Bytes4;
      }
      else if (name.Contains("l") || name.Contains("h")) {
        return Type.Bytes1;
      }
      else {
        return Type.Bytes2;
      }
    }

    public static Register RegisterToSize(Register register, int size) {
      string name = Enum.GetName(typeof(Register), register);

      switch (size) {
        case Type.Bytes1:
          name = name.Replace("x", "l").Replace("e", "").Replace("r", "");
          break;

        case Type.Bytes2:
          Assert.Error(!name.Contains("h"));
          name = name.Replace("l", "x").Replace("e", "").Replace("r", "");
          break;

        case Type.Bytes4:
          Assert.Error(!name.Contains("h"));
          name = "e" + name.Replace("l", "x").Replace("e", "").Replace("r", "");
          break;

        case Type.Bytes8:
          Assert.Error(!name.Contains("h"));
          name = "r" + name.Replace("l", "x").Replace("e", "").Replace("r", "");
          break;
      }

      return (Register) Enum.Parse(typeof(Register), name);
    }
  
    public static int SizeOfOperator(AssemblyOperator objectOp) {
      string name = Enum.GetName(typeof(AssemblyOperator), objectOp);

      if (name.Contains("_byte")) {
        return Type.Bytes1;
      }
      else if (name.Contains("_word")) {
        return Type.Bytes2;
      }
      else if (name.Contains("_dword")) {
        return Type.Bytes4;
      }
      else if (name.Contains("_qword")) {
        return Type.Bytes8;
      }

      Assert.Error(Message.Operator_size);
      return 0;
    }
  
    public static AssemblyOperator OperatorToSize(AssemblyOperator objectOp, int size) {
      string name = Enum.GetName(typeof(AssemblyOperator), objectOp);
    
      if (objectOp == AssemblyOperator.interrupt) {
        return AssemblyOperator.interrupt;
      }
    
      switch (size) {
        case Type.Bytes1:
          name = name + "_byte";
          break;
        
        case Type.Bytes2:
          name = name + "_word";
          break;
        
        case Type.Bytes4:
          name = name + "_dword";
          break;

        case Type.Bytes8:
          name = name + "_qword";
          break;

        default:
          Assert.Error(false);
          break;
      }

      return (AssemblyOperator) Enum.Parse(typeof(AssemblyOperator), name);
    }

    public static BigInteger UnsignedToSignedValue(BigInteger value) {
      if ((128 <= value) && (value <= 255)) {
        return (value - 256);
      }
      else if ((32768 <= value) && (value <= 65535)) {
        return (value - 65536);
      }
      else if ((2147483648 <= value) && (value <= 4294967295)) {
        return (value - 4294967296);
      }
      else {
        return value;
      }
    }

    public static int SizeOfSignedValue(BigInteger value) {
      if (value == 0) {
        return 0;
      }
      else if ((-128 <= value) && (value <= 127)) {
        return Type.Bytes1;
      }
      else if ((-32768 <= value) && (value <= 32767)) {
        return Type.Bytes2;
      }
      else if ((-2147483648 <= value) && (value <= 2147483647)) {
        return Type.Bytes4;
      }
      else {
        return Type.Bytes8;
      }
    }

/*    public static int SizeOfUnsignedValue(BigInteger value) {
      if (value == 0) {
        return 0;
      }
      else if (value <= 255) {
        return Type.Bytes1;
      }
      else if (value <= 65535) {
        return Type.Bytes2;
      }
      else if (value <= 4294967295) {
        return Type.Bytes4;
      }
      else {
        return Type.Bytes8;
      }
    }*/

    // ------------------------------------------------------------------------
  
    // mov [global1], global2
  
    public List<byte> ByteList() {
      Assert.Error(!((m_operator == AssemblyOperator.define_value) ||
                     (m_operator == AssemblyOperator.define_address) ||
                     (m_operator == AssemblyOperator.define_zero_sequence)));

      object operand0 = m_operandArray[0],
             operand1 = m_operandArray[1],
             operand2 = m_operandArray[2];
             
      if ((m_operator == AssemblyOperator.empty) ||
          (m_operator == AssemblyOperator.label) ||
          (m_operator == AssemblyOperator.comment)) {
        return (new List<byte>());
      }
      else if (IsJumpRegister() || IsCallRegister()) {
        Register register = (Register) operand0;
        return LookupByteArray(AssemblyOperator.jmp, register);
      }
      else if (IsCallNotRegister()) {
        List<byte> byteList = LookupByteArray(AssemblyOperator.jmp, Type.PointerSize);
        LoadByteList(byteList, byteList.Count - Type.PointerSize, Type.PointerSize, 0);
        return byteList;
      }
      else if (m_operator == AssemblyOperator.address_return) {
        Register register = (Register) operand0;
        int offset = (int) operand1;
        int size = SizeOfSignedValue(offset);
        int address = (int)((BigInteger)operand2);
        List<byte> byteList = LookupByteArray(AssemblyOperator.mov_word, register, size, Type.PointerSize);
        LoadByteList(byteList, byteList.Count - (size + Type.PointerSize), size, offset);
        LoadByteList(byteList, byteList.Count - Type.PointerSize, Type.PointerSize, address);
        return byteList;
      }
      else if (IsRelationRegister()) {
        Register register = (Register) operand0;
        return LookupByteArray(m_operator, register);
      }
      else if (IsRelationNotRegister() || IsJumpNotRegister()) {
        int address = (int) operand0;
        int size = ((address >= -128) && (address <= 127)) ? Type.Bytes1 : Type.Bytes2;
        List<byte> byteList = LookupByteArray(m_operator, size);
        LoadByteList(byteList, byteList.Count - size, size, address);
        return byteList;
      }
      else if ((operand0 is Register) && (operand1 is Register) && (operand2 is int)) { // mov ax, [bp + 2]
        Register toRegister = (Register) operand0,
                 baseRegister = (Register) operand1;
        int offset = (int) operand2;
        int size = SizeOfSignedValue(offset);
        List<byte> byteList = LookupByteArray(m_operator, toRegister, baseRegister, size);
        LoadByteList(byteList, byteList.Count - size, size, offset);
        return byteList; 
      }
      else if (((operand0 is Register) && (operand1 is string) && (operand2 is int)) || // mov ax, [global + 4]
               ((operand0 is Register) && (operand1 == null) && (operand2 is int))) {   // mov ax, [null + 4]
        Register toRegister = (Register) operand0;
        int offset = (int) operand2;
        List<byte> byteList = LookupByteArray(m_operator, toRegister, null, Type.PointerSize);
        LoadByteList(byteList, byteList.Count - Type.PointerSize, Type.PointerSize, offset);
        return byteList; 
      }

      else if ((operand0 is Register) && (operand1 is int) && (operand2 is Register)) { // mov [bp + 2], ax
        Register baseRegister = (Register) operand0,
                 fromRegister = (Register) operand2;
        int offset = (int) operand1;
        int size = SizeOfSignedValue(offset);
        List<byte> byteList = LookupByteArray(m_operator, baseRegister, size, fromRegister);
        LoadByteList(byteList, byteList.Count - size, size, offset);
        return byteList; 
      }
      else if ((operand1 is int) && (operand2 is Register)) { // mov [global + 4], ax; mov [null + 4], ax
        Assert.Error((operand0 is string) || (operand0 == null));
        Register fromRegister = (Register) operand2;
        int offset = (int) operand1;
        List<byte> byteList = LookupByteArray(m_operator, null, Type.PointerSize, fromRegister);
        LoadByteList(byteList, byteList.Count - Type.PointerSize, Type.PointerSize, offset);
        return byteList; 
      }
      else if ((operand1 is int) && (operand2 is Register)) { // mov [global + 4], ax; mov [null + 4], ax
        Assert.Error((operand0 is string) || (operand0 == null));
        Register fromRegister = (Register) operand2;
        int offset = (int) operand1;
        List<byte> byteList = LookupByteArray(m_operator, null, Type.PointerSize, fromRegister);
        LoadByteList(byteList, byteList.Count - Type.PointerSize, Type.PointerSize, offset);
        return byteList; 
      }
      else if ((operand0 is Register) && (operand1 is int) && (operand2 is BigInteger)) { // mov [bp + 2], 123
        Register baseRegister = (Register) operand0;
        int offset = (int) operand1, value = (int) ((BigInteger) operand2);
        int offsetSize = SizeOfSignedValue(offset), valueSize = SizeOfSignedValue(value);
        List<byte> byteList = LookupByteArray(m_operator, baseRegister, offsetSize, valueSize);
        LoadByteList(byteList, byteList.Count - (offsetSize + valueSize), offsetSize, offset);
        LoadByteList(byteList, byteList.Count - valueSize, valueSize, value);
        return byteList; 
      }
      else if ((operand1 is int) && (operand2 is BigInteger)) { // mov [global + 4], 123; mov [null + 4], 123
        Assert.Error((operand0 is string) || (operand0 == null));
        int offset = (int) operand1, value = (int) ((BigInteger) operand2);
        int valueSize = SizeOfSignedValue(value);
        List<byte> byteList = LookupByteArray(m_operator, null, Type.PointerSize, valueSize);
        LoadByteList(byteList, byteList.Count - (Type.PointerSize + valueSize), Type.PointerSize, offset);
        LoadByteList(byteList, byteList.Count - valueSize, valueSize, value);
        return byteList; 
      }

      else if ((operand0 is Register) && (operand1 is int) && (operand2 is string)) { // mov [bp + 2], global
        Register baseRegister = (Register) operand0;
        int offset = (int) operand1;
        int offsetSize = SizeOfSignedValue(offset);
        List<byte> byteList = LookupByteArray(m_operator, baseRegister, offsetSize, Type.PointerSize);
        LoadByteList(byteList, byteList.Count - (offsetSize + Type.PointerSize), offsetSize, offset);
        LoadByteList(byteList, byteList.Count - Type.PointerSize, Type.PointerSize, 0);
        return byteList; 
      }
      else if ((operand0 is string) && (operand1 is int) && (operand2 is string)) { // mov [global + 4], global; mov [null + 4], global
        Assert.Error((operand0 is string) || (operand0 == null));
        int offset = (int) operand1;
        List<byte> byteList = LookupByteArray(m_operator, null, Type.PointerSize, Type.PointerSize);
        LoadByteList(byteList, byteList.Count - (Type.PointerSize + Type.PointerSize), Type.PointerSize, offset);
        LoadByteList(byteList, byteList.Count - Type.PointerSize, Type.PointerSize, 0);
        return byteList; 
      }

      else if ((operand0 is BigInteger) && (operand1 is Register) && (operand2 is int)) { // cmp 123, [bp + 2]
        int value = (int) ((BigInteger) operand0), offset = (int) operand2;
        int valueSize = SizeOfSignedValue(value), offsetSize = SizeOfSignedValue(offset);
        Register baseRegister = (Register) operand1;
        List<byte> byteList = LookupByteArray(m_operator, valueSize, baseRegister, offsetSize);
        LoadByteList(byteList, byteList.Count - (valueSize + offsetSize), valueSize, value);
        LoadByteList(byteList, byteList.Count - offsetSize, offsetSize, offset);
        return byteList; 
      }
      else if ((operand0 is BigInteger) && (operand2 is int)) { // cmp 123, [global + 4]; cmp 123, [null + 4]
        Assert.Error((operand1 is string) || (operand1 == null));
        int value = (int) ((BigInteger) operand0), offset = (int) operand2;
        int valueSize = SizeOfSignedValue(value), offsetSize = SizeOfSignedValue(offset);
        List<byte> byteList = LookupByteArray(m_operator, valueSize, null, offsetSize);
        LoadByteList(byteList, byteList.Count - (valueSize + offsetSize), valueSize, value);
        LoadByteList(byteList, byteList.Count - offsetSize, offsetSize, offset);
        return byteList; 
      }

      else if ((operand0 is Register) && (operand1 is Register)) { // mov ax, bx
        Assert.Error(operand2 == null);
        Register toRegister = (Register) operand0,
                 fromRegister = (Register) operand1;
        return LookupByteArray(m_operator, toRegister, fromRegister);
      }
      else if ((operand0 is Register) && (operand1 is BigInteger)) { // mov ax, 123
        Assert.Error(operand2 == null);
        Register register = (Register) operand0;
        int value = (int) ((BigInteger) operand1);
        int size = SizeOfRegister(register);
        List<byte> byteList = LookupByteArray(m_operator, register, size);
        LoadByteList(byteList, byteList.Count - size, size, value);
        return byteList;
      }
      else if ((operand0 is Register) && (operand1 is string)) { // mov ax, global
        Assert.Error(operand2 == null);
        Register register = (Register) operand0;
        int size = SizeOfRegister(register);
        List<byte> byteList = LookupByteArray(m_operator, register, size);
        LoadByteList(byteList, byteList.Count - size, size, 0);
        return byteList;
      }
      else if ((operand0 is BigInteger) && (operand1 is Register)) { // cmp 123, ax
        Assert.Error(operand2 == null);
        int value = (int) ((BigInteger) operand0);
        Register register = (Register)operand1;
        int size = SizeOfRegister(register);
        List<byte> byteList = LookupByteArray(m_operator, size, register);
        LoadByteList(byteList, byteList.Count - size, size, value);
        return byteList;
      }
      else if ((operand0 is Register) && (operand1 is int)) { // inc [bp + 2]
        Assert.Error(operand2 == null);
        Register register = (Register) operand0;
        int offset = (int) operand1;
        int size = SizeOfSignedValue(offset);
        List<byte> byteList = LookupByteArray(m_operator, register, size);
        LoadByteList(byteList, byteList.Count - size, size, offset);
        return byteList;
      }
      else if (operand1 is int) { // inc [global + 4]; inc [null + 4]
        Assert.Error((operand0 is string) || (operand0 == null));
        Assert.Error(operand2 == null);
        int offset = (int) operand1;
        List<byte> byteList = LookupByteArray(m_operator, null, Type.PointerSize);
        LoadByteList(byteList, byteList.Count - Type.PointerSize, Type.PointerSize, offset);
        return byteList;
      }
      else if (operand0 is Register) { // inc ax
        Assert.Error((operand1 == null) && (operand2 == null));
        Register register = (Register) operand0;
        return LookupByteArray(m_operator, register);
      }
      else if (operand0 is BigInteger) { // int 33
        Assert.Error((operand1 == null) && (operand2 == null));
        int value = (int) ((BigInteger) operand0);
        int size = SizeOfSignedValue(value);
        List<byte> byteList = LookupByteArray(m_operator, size);
        LoadByteList(byteList, byteList.Count - size, size, value);
        return byteList;
      }
      else { // lahf
        Assert.Error((operand0 == null) && (operand1 == null) && (operand2 == null));
        return LookupByteArray(m_operator);
      }
    }

    public List<byte> ByteListX() {
      Assert.Error(!((m_operator == AssemblyOperator.define_value) ||
                     (m_operator == AssemblyOperator.define_address) ||
                     (m_operator == AssemblyOperator.define_zero_sequence)));

      if ((m_operator == AssemblyOperator.empty) ||
          (m_operator == AssemblyOperator.label) ||
          (m_operator == AssemblyOperator.comment)) {
        return (new List<byte>());
      }
      else if (IsJumpRegister() || IsCallRegister()) {
        Register register = (Register) m_operandArray[0];
        return LookupByteArray(AssemblyOperator.jmp, register, null, null);
      }
      else if (IsCallNotRegister()) {
        List<byte> byteList = LookupByteArray(AssemblyOperator.jmp, Type.PointerSize, null, null);
        LoadByteList(byteList, byteList.Count - Type.PointerSize, Type.PointerSize, 0);
        return byteList;
      }
      else if (m_operator == AssemblyOperator.address_return) {
        Register register = (Register) m_operandArray[0];
        int offset = (int) m_operandArray[1];
        int size = SizeOfSignedValue(offset);
        int address = (int)((BigInteger)m_operandArray[2]);
        List<byte> byteList = LookupByteArray(AssemblyOperator.mov_word, register, size, Type.PointerSize);
        LoadByteList(byteList, byteList.Count - (size + Type.PointerSize), size, offset);
        LoadByteList(byteList, byteList.Count - Type.PointerSize, Type.PointerSize, address);
        return byteList;
      }
      else if (IsRelationRegister()) {
        Register register = (Register) m_operandArray[0];
        return LookupByteArray(m_operator, register, null, null);
      }
      else if (IsRelationNotRegister() || IsJumpNotRegister()) {
        int address = (int) m_operandArray[0];
        int size = ((address >= -128) && (address <= 127)) ? Type.Bytes1 : Type.Bytes2;
        List<byte> byteList = LookupByteArray(m_operator, size, null, null);
        LoadByteList(byteList, byteList.Count - size, size, address);
        return byteList;
      }
      else {
        object operand1 = null, operand2 = null, operand3 = null;
        int size1 = 0, size2 = 0, size3 = 0;
        BigInteger value1 = 0, value2 = 0, value3 = 0;
        string name = Enum.GetName(typeof(AssemblyOperator), m_operator);
      
        if (m_operandArray[2] != null) {                  
          if (m_operandArray[1] is int) {
            // mov [null + integer}, register
            // mov [string + integer}, register
            if ((m_operandArray[0] == null) ||
                (m_operandArray[0] is string)) {
              operand1 = null;
              operand2 = Type.PointerSize;
              size2 = Type.PointerSize;
              value2 = (int) m_operandArray[1];
            }
            // mov [register + integer}, not-null
            else if (m_operandArray[0] is Register) {
              operand1 = m_operandArray[0];
              size2 = SizeOfSignedValue((BigInteger) ((int) m_operandArray[1]));
            
              operand2 = size2;
              value2 = (int) m_operandArray[1];
            }
            else {
              Assert.Error(false);
            }

            if (m_operandArray[2] is Register) {
              operand3 = m_operandArray[2];
            }
            else if (m_operandArray[2] is BigInteger) {
              value3 = (BigInteger) m_operandArray[2];

              if (name.StartsWith("mov_")) {
                size3 = SizeOfOperator(m_operator);
              }
              else if (value3 == 0) {
                size3 = 1;
              }
              else {
                size3 = SizeOfSignedValue(value3);
              }

              operand3 = size3;
            }
            else if (m_operandArray[2] is string) {
              Assert.Error(name.StartsWith("mov_"));
              operand3 = SizeOfOperator(m_operator);
              size3 = SizeOfOperator(m_operator);
              value3 = 0;
            }
            else {
              Assert.Error(false);
            }
          }
          else {
            Assert.Error(m_operandArray[0] is Register);
            operand1 = m_operandArray[0];
          
            // mov register, [null + integer}
            // mov register, [string + integer}
            if ((m_operandArray[1] == null) ||
                (m_operandArray[1] is string)) {
              operand3 = SizeOfSignedValue((BigInteger) (int) m_operandArray[2]);
              size3 = Type.PointerSize;
              value3 = (BigInteger) ((int) m_operandArray[2]);
            }
            // mov register, [register + integer]
            else if (m_operandArray[1] is Register) {
              operand2 = m_operandArray[1];
              size3 = SizeOfSignedValue((BigInteger) ((int) m_operandArray[2]));
              operand3 = size3;
              value3 = (BigInteger) ((int) m_operandArray[2]);
            }
            else {
              Assert.Error(false);
            }
          }
        }
        else if (m_operandArray[1] != null) {
          if (name.Contains("_") ||
             (m_operator == AssemblyOperator.fstcw) ||
             (m_operator == AssemblyOperator.fldcw)) {
            // inc_word [null + integer}
            // inc_word [string + integer}    
            if ((m_operandArray[0] == null) ||
                (m_operandArray[0] is string)) {
              operand1 = null;
              operand2 = Type.PointerSize;
              size2 = Type.PointerSize;
              value2 = (BigInteger) ((int) m_operandArray[1]);
            }
            // inc_word [register + integer}
            else if (m_operandArray[0] is Register) {
              operand1 = m_operandArray[0];
              operand2 = SizeOfSignedValue((BigInteger) ((int) m_operandArray[1]));
              size2 = SizeOfSignedValue((BigInteger) ((int) m_operandArray[1]));
              value2 = (BigInteger) ((int) m_operandArray[1]);
            }
            else {
              Assert.Error(false);
            }
          }
          else {
            Assert.Error(m_operandArray[0] is Register);
            Register register = (Register) m_operandArray[0];
            operand1 = register;

            // mov register, integer
            if (m_operandArray[1] is BigInteger) {
              if (m_operator == AssemblyOperator.mov) {
                size2 = SizeOfRegister(register);
              }
              else if ((m_operator == AssemblyOperator.and) &&
                       (SizeOfRegister(register) == Type.Bytes4)) {
                size2 = Type.Bytes4;
              }
              else {
                size2 = SizeOfSignedValue((BigInteger) m_operandArray[1]);
              }
            
              operand2 = size2;
              value2 = (BigInteger) m_operandArray[1];
            }
            // mov register, string
            else if (m_operandArray[1] is string) {
              Assert.Error(m_operator == AssemblyOperator.mov);
              operand2 = Type.PointerSize;
              size2 = Type.PointerSize;
            }
            // mov register, register
            else if (m_operandArray[1] is Register) {
              operand2 = m_operandArray[1];
            }
            else {
              Assert.Error(false);
            }
          }
        }
        else if (m_operandArray[0] is Register) {
          operand1 = m_operandArray[0];
        }
        else if (m_operandArray[0] is BigInteger) {
          operand1 = SizeOfSignedValue((BigInteger)  m_operandArray[0]);
          size1 = SizeOfSignedValue((BigInteger) m_operandArray[0]);
          value1 = (BigInteger) m_operandArray[0];
        }
        else {
          Assert.Error((m_operandArray[0] == null) &&
                       (m_operandArray[1] == null) &&
                       (m_operandArray[2] == null));
        }

        List<byte> byteList = LookupByteArray(m_operator, operand1, operand2, operand3);
        LoadByteList(byteList, byteList.Count - (size1 + size2 + size3), size1, value1);
        LoadByteList(byteList, byteList.Count - (size2 + size3), size2, value2);
        LoadByteList(byteList, byteList.Count - size3, size3, value3);
        return byteList;
      }
    }

    public static void LoadByteList(IList<byte> byteList, int index,
                                    int size, BigInteger value) {
      switch (size) {
        case Type.Bytes1: {
            if (value < 0) {
              byteList[index] = (byte) ((sbyte) value);
            }
            else {
              byteList[index] = (byte) value;
            }
          }
          break;

        case Type.Bytes2: {
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

        case Type.Bytes4: {
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
                             object operand1 = null, object operand2 = null, object operand3 = null) {
      if ((objectOp == AssemblyOperator.shl) ||
          (objectOp == AssemblyOperator.shr)) {
        operand1 = (operand1 is BigInteger) ? 0L : operand1;
        operand2 = (operand2 is BigInteger) ? 0L : operand2;
        operand3 = (operand3 is BigInteger) ? 0L : operand3;
      }

      ObjectCodeInfo info = new ObjectCodeInfo(objectOp, operand1, operand2, operand3);
      byte[] byteArray = ObjectCodeTable.MainArrayMap[info];
      Assert.Error(byteArray != null);
      List<byte> byteList = new List<byte>();

      foreach (byte b in byteArray) {
        byteList.Add(b);
      }

      return byteList;
    }

    public override string ToString() {
      object operand0 = m_operandArray[0],
             operand1 = m_operandArray[1],
             operand2 = m_operandArray[2];

      if ((m_operator == AssemblyOperator.empty) ||
          (m_operator == AssemblyOperator.new_middle_code)) {
        return null;
      }
      else if (m_operator == AssemblyOperator.label) {
        return ((operand0 != null) ? ("\n" + operand0 + ":") : "") +
                ((operand1 != null) ? ("\t; " + operand1) : "");
      }
      else if (m_operator == AssemblyOperator.comment) {
        return "\t; " + operand0;
      }
      else if (m_operator == AssemblyOperator.define_address) {
        string aname = (string) operand0;
        int offset = (int) operand1;

        if (offset > 0) {
          return "\tdd " + aname + " + " + offset;
        }
        else if (offset < 0) {
          return "\tdd " + aname + " - " + (-offset);
        }
        else {
          return "\tdd " + aname;
        }
      }
      else if (m_operator == AssemblyOperator.define_zero_sequence) {
        int size = (int) operand0;
        return "\ttimes " + size + " db 0";
      }
      else if (m_operator == AssemblyOperator.define_value) {
        Sort sort = (Sort) operand0;
        object value = operand1;

        if (sort == Sort.String) {
          StringBuilder buffer = new StringBuilder();
          string text = (string) operand1;
          bool graph = false;

          foreach (char c in text) {
            if (Char.IsControl(c) || (c == '\"') || (c == '\'')) {
              if (graph) {
                buffer.Append("\", " + ((int) c).ToString() + ", ");
                graph = false;
              }
              else {
                buffer.Append(((int) c).ToString() + ", ");
              }
            }
            else {
              if (graph) {
                buffer.Append(c);
              }
              else {
                buffer.Append("\"" + c.ToString());
                graph = true;
              }
            }
          }

          if (graph) {
            buffer.Append("\", 0");
          }
          else {
            buffer.Append("0");
          }

          return "\tdb " + buffer.ToString();
        }
        else {
          string text = operand1.ToString();

          if (((sort == Sort.Float) || (sort == Sort.Double) ||
                (sort == Sort.Long_Double)) && !text.Contains(".")) {
                text += ".0";
          }

          switch (Type.Size(sort)) {
            case Type.Bytes1:
              return "\tdb " + text;

            case Type.Bytes2:
              return "\tdw " + text;

            case Type.Bytes4:
              return "\tdd " + text;

            default: //case Type.Bytes8:
              return "\tdq " + text;
          }
        }
      }
      else {
        string operatorName, resultName;

        if (m_operator == AssemblyOperator.call) {
          operatorName = resultName = "jmp";
        }
        else if (m_operator == AssemblyOperator.address_return) {
          operatorName = "mov_qword";
          resultName = "mov qword";
        }
        else {
          operatorName = Enum.GetName(typeof(AssemblyOperator), m_operator);
          resultName = operatorName.Replace("_", " ");
        }

        if (IsCall()) {
          return "\t" + resultName + " " + operand0.ToString();
        }
        else if (IsRelationNotRegister() || IsJumpNotRegister()) {
          if (operand2 is int) {
            string label = SymbolTable.CurrentFunction.UniqueName +
                           Symbol.SeparatorId + operand2.ToString();
            return "\t" + resultName + " " + label;
          }
          else if (operand2 is string) {
            return "\t" + resultName + " " + (string) operand2;
          }
          else {
            return "\t" + resultName + " x" + operand1.ToString();
          }
        }
        else if (m_operator == AssemblyOperator.address_return) {
          string label = SymbolTable.CurrentFunction.UniqueName +
                 Symbol.SeparatorId + operand2.ToString();
          return "\t" + resultName + " [" + operand0.ToString() +
                        withSign(operand1) + "], " + label;
        }
        else if ((operand0 != null) && (operand1 != null) &&
                 (operand2 != null)) {
          if (operand1 is int) {
            return "\t" + resultName + " [" + operand0.ToString() +
                          withSign(operand1) + "], " +
                          operand2.ToString();
          }
          else {
            return "\t" + resultName + " " + operand0.ToString() +
                          ", [" + operand1.ToString() +
                          withSign(operand2) + "]";
          }
        }
        else if ((operand0 == null) && (operand1 != null) &&
                 (operand2 != null)) {
          return "\t" + resultName + " [" + operand1.ToString() + "], " +
                        operand2.ToString();
        }
        else if ((operand0 != null) && (operand1 == null) &&
                 (operand2 != null)) {
          return "\t" + resultName + " " + operand0.ToString() + ", [" +
                        operand2.ToString() + "]";
        }
        else if ((operand0 == null) && (operand1 != null) &&
                 (operand2 == null)) {
          return "\t" + resultName + " [" + operand1.ToString() + "]";
        }
        else if (((m_operator == AssemblyOperator.fstcw) || (m_operator == AssemblyOperator.fldcw)) &&
                 (operand0 != null) && (operand1 != null) && (operand2 == null)) {
          return "\t" + resultName + " [" + operand0.ToString() +
                        withSign(operand1) + "]";
        }
        else if (operatorName.Contains("_") && (operand0 != null) &&
                 (operand1 is int) && (operand2 == null)) {
          return "\t" + resultName + " [" + operand0.ToString() +
                        withSign(operand1) + "]";
        }
        else if (!operatorName.Contains("_") && (operand0 != null) &&
                 (operand1 != null) && (operand2 == null)) {
          return "\t" + resultName + " " + operand0.ToString() + ", " +
                        operand1.ToString();
        }    
        else if (operand0 != null) {
          return "\t" + resultName + " " + operand0.ToString();
        }    
        else {
          return "\t" + resultName;
        }    
      }
    }
  
    private string withSign(object obj) {
      int value = (int) obj;
    
      if (value > 0) {
        return " + " + value;
      }
      else if (value < 0) {
        return " - " + (-value);
      }
      else {
        return "";
      }
    }
  }
}