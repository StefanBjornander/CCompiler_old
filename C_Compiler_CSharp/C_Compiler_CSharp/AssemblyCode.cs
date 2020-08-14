using System;
using System.Text;
using System.Numerics;
using System.Collections.Generic;

namespace CCompiler {
  public class AssemblyCode {
    public const byte NopOperator = 144; // -112; XXX
    public const byte ShortJumpOperator = 235; // -21; XXX

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
                        object operand1, object operand2 = null, int size = 0) {
      m_operator = objectOp;
      m_operandArray[0] = operand0;
      m_operandArray[1] = operand1;
      m_operandArray[2] = operand2;

      FromAdditionToIncrement();
      CheckSize(size);
    }

    private void CheckSize(int size) {
      if (size != 0) {
        object operand0 = m_operandArray[0],
               operand1 = m_operandArray[1],
               operand2 = m_operandArray[2];

        /*if ((((operand0 is Register) || (operand0 is String)) &&
             (operand1 is int) &&
             ((operand2 is BigInteger) || (operand2 is String))) ||
            (((m_operator == AssemblyOperator.neg) ||
             (m_operator == AssemblyOperator.not) ||
             (m_operator == AssemblyOperator.inc) ||
             (m_operator == AssemblyOperator.dec)) &&
             ((operand0 is Register) || (operand0 is String)) &&
             (operand1 is int))) {
          m_operator = OperatorToSize(m_operator, size);
        }*/

        if (((operand0 is Register) || (operand0 is Track) || (operand0 is String)) &&
            (operand1 is int) && ((operand2 is BigInteger) || (operand2 is String))) {
          m_operator = OperatorToSize(m_operator, size);
        }
        else if (((m_operator == AssemblyOperator.neg) ||
                  (m_operator == AssemblyOperator.not) ||
                  (m_operator == AssemblyOperator.inc) ||
                  (m_operator == AssemblyOperator.dec) ||
                  (m_operator == AssemblyOperator.mul) ||
                  (m_operator == AssemblyOperator.imul) ||
                  (m_operator == AssemblyOperator.div) ||
                  (m_operator == AssemblyOperator.idiv)) &&
                  ((operand0 is Register) || (operand0 is Track) ||
                   (operand0 is String)) && (operand1 is int)) {
          m_operator = OperatorToSize(m_operator, size);
        }
      }
    }

    public AssemblyOperator Operator {
      get { return m_operator; }
      set { m_operator = value; }
    }

    public void FromAdditionToIncrement() {
      object operand0 = m_operandArray[0],
             operand1 = m_operandArray[1],
             operand2 = m_operandArray[2];

      //string name = Enum.GetName(typeof(AssemblyOperator), m_operator);

      if (((Operator == AssemblyOperator.add) ||
           (Operator == AssemblyOperator.sub)) &&
          ((operand0 is Register) || (operand0 is string)) &&
          (operand1 is int) && (operand2 is BigInteger)) {
        int value = (int) ((BigInteger) operand2);

        if (((Operator == AssemblyOperator.add) && (value == 1)) ||
            ((Operator == AssemblyOperator.sub) && (value == -1))) {
          m_operator = AssemblyOperator.inc;
          m_operandArray[2] = null;
        }
        else if (((Operator == AssemblyOperator.add) && (value == -1)) ||
                 ((Operator == AssemblyOperator.sub) && (value == 1))) {
          m_operator = AssemblyOperator.dec;
          m_operandArray[2] = null;
        }
      }
      /*else if ((name.Contains("add_") || name.Contains("sub_")) &&
          /*((operand0 is Register) || (operand0 is string) || (operand0 == null)) &&
          (operand1 is int) &&* (operand2 is BigInteger)) {
        int value = (int) ((BigInteger) operand2);

        if ((name.Contains("add_") && (value == 1)) ||
            (name.Contains("sub_") && (value == -1))) {
          m_operator = (AssemblyOperator) Enum.Parse(typeof(AssemblyOperator),
                       name.Replace("add_", "inc_").Replace("sub_", "inc_"));
          m_operandArray[2] = null;
        }
        else if ((name.Contains("add_") && (value == -1)) ||
                 (name.Contains("sub_") && (value == 1))) {
          m_operator = (AssemblyOperator) Enum.Parse(typeof(AssemblyOperator),
                       name.Replace("add_", "dec_").Replace("sub_", "dec_"));
          m_operandArray[2] = null;
        }
      }*/
      else if (((Operator == AssemblyOperator.add) ||
                (Operator == AssemblyOperator.sub)) && (operand0 is Track) &&
                (operand1 is BigInteger) && (operand2 == null)){
        BigInteger value = (BigInteger) operand1;

        if (((Operator == AssemblyOperator.add) && (value == 1)) ||
            ((Operator == AssemblyOperator.sub) && (value == -1))) {
          Operator = AssemblyOperator.inc;
          m_operandArray[1] = null;
        }
        else if (((Operator == AssemblyOperator.sub) && (value == 1)) ||
                 ((Operator == AssemblyOperator.add) && (value == -1))) {
          Operator = AssemblyOperator.dec;
          m_operandArray[1] = null;
        }
      }
    }

    public object this[int index] {
      get { return m_operandArray[index]; }
      set { Assert.ErrorXXX((index >= 0) && (index < 3));
            m_operandArray[index] = value; }
    }

    // -----------------------------------------------------------------------
  
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
  
    // -----------------------------------------------------------------------
  
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

      name1 = (name1.Length == 3) ? name1.Substring(1) : name1; // eax, rax
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
      else if (name.Contains("_qword")) {
        return 8;
      }

      Assert.Error(Message.Operator_size);
      return 0;
    }
  
    public static AssemblyOperator OperatorToSize
                                   (AssemblyOperator objectOp, int size) {
      string name = Enum.GetName(typeof(AssemblyOperator), objectOp);
    
      if (objectOp == AssemblyOperator.interrupt) {
        return AssemblyOperator.interrupt;
      }
    
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

        default:
          Assert.ErrorXXX(false);
          break;
      }

      return (AssemblyOperator) Enum.Parse(typeof(AssemblyOperator),name);
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

    // -----------------------------------------------------------------------
  
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
      else if (Operator == AssemblyOperator.address_return) {
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
      else if ((operand0 is Register) && (operand1 is Register) &&
              (operand2 is int)) { // mov ax, [bp + 2]
        Register toRegister = (Register) operand0,
                 baseRegister = (Register) operand1;
        int offset = (int) operand2;
        int size = SizeOfValue(offset);
        List<byte> byteList =
          LookupByteArray(Operator, toRegister, baseRegister, size);
        LoadByteList(byteList, byteList.Count - size, size, offset);
        return byteList; 
      }
      // mov ax, [global + 4]; mov ax, [null + 4]
      else if (((operand0 is Register) && (operand1 is string) &&
                (operand2 is int)) || ((operand0 is Register) &&
                (operand1 == null) && (operand2 is int))) {
        Register toRegister = (Register) operand0;
        int offset = (int) operand2;
        List<byte> byteList =
          LookupByteArray(Operator, toRegister, null, TypeSize.PointerSize);
        LoadByteList(byteList, byteList.Count - TypeSize.PointerSize,
                     TypeSize.PointerSize, offset);
        return byteList; 
      }
      else if ((operand0 is Register) && (operand1 is int) &&
               (operand2 is Register)) { // mov [bp + 2], ax
        Register baseRegister = (Register) operand0,
                 fromRegister = (Register) operand2;
        int offset = (int) operand1;
        int size = SizeOfValue(offset);
        List<byte> byteList =
          LookupByteArray(Operator, baseRegister, size, fromRegister);
        LoadByteList(byteList, byteList.Count - size, size, offset);
        return byteList; 
      }
      // mov [global + 4], ax; mov [null + 4], ax
      else if ((operand1 is int) && (operand2 is Register)) {
        Assert.ErrorXXX((operand0 is string) || (operand0 == null));
        int offset = (int)operand1;
        Register fromRegister = (Register)operand2;
        List<byte> byteList =
          LookupByteArray(Operator, null, TypeSize.PointerSize, fromRegister);
        LoadByteList(byteList, byteList.Count - TypeSize.PointerSize,
                     TypeSize.PointerSize, offset);
        return byteList; 
      }
      else if ((operand0 is Register) && (operand1 is int) &&
               (operand2 is BigInteger)) { // mov [bp + 2], 123
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
      else if ((operand0 is Register) && (operand1 is int) &&
               (operand2 is string)) { // mov [bp + 2], global
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
      // mov [global + 4], 123; mov [null + 4], 123
      else if ((operand1 is int) && (operand2 is BigInteger)) {
        Assert.ErrorXXX((operand0 is string) || (operand0 == null));
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
      // mov ax, bx
      else if ((operand0 is Register) && (operand1 is Register)) {
        Assert.ErrorXXX(operand2 == null);
        Register toRegister = (Register) operand0,
                 fromRegister = (Register) operand1;
        return LookupByteArray(Operator, toRegister, fromRegister);
      }
      // mov ax, 123
      else if ((operand0 is Register) && (operand1 is BigInteger)) {
        Assert.ErrorXXX(operand2 == null);
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
      else if ((operand0 is Register) && (operand1 is string)) {
        Assert.ErrorXXX(operand2 == null);
        Register register = (Register) operand0;
        int size = SizeOfRegister(register);
        List<byte> byteList = LookupByteArray(Operator, register, size);
        LoadByteList(byteList, byteList.Count - size, size, 0);
        return byteList;
      }
      else if ((operand0 is Register) && (operand1 is int)) { // inc [bp + 2]
        Assert.ErrorXXX(operand2 == null);
        Register baseRegister = (Register) operand0;
        int offset = (int) operand1;
        int size = SizeOfValue(offset);
        List<byte> byteList = LookupByteArray(Operator, baseRegister, size);
        LoadByteList(byteList, byteList.Count - size, size, offset);
        return byteList;
      }
      else if (operand1 is int) { // inc [global + 4]; inc [null + 4]
        Assert.ErrorXXX((operand0 is string) || (operand0 == null));
        Assert.ErrorXXX(operand2 == null);
        int offset = (int) operand1;
        List<byte> byteList =
          LookupByteArray(Operator, null, TypeSize.PointerSize);
        LoadByteList(byteList, byteList.Count - TypeSize.PointerSize,
                     TypeSize.PointerSize, offset);
        return byteList;
      }
      else if (operand0 is Register) { // inc ax
        Assert.ErrorXXX((operand1 == null) && (operand2 == null));
        Register register = (Register) operand0;
        return LookupByteArray(Operator, register);
      }
      else if (operand0 is BigInteger) { // int 33
        Assert.ErrorXXX((operand1 == null) && (operand2 == null));
        BigInteger value = (BigInteger) operand0;
        int size = SizeOfValue(value);
        List<byte> byteList = LookupByteArray(Operator, size);
        LoadByteList(byteList, byteList.Count - size, size, value);
        return byteList;
      }
      else { // lahf
        Assert.ErrorXXX((operand0 == null) && (operand1 == null) &&
                        (operand2 == null));
        return LookupByteArray(Operator);
      }
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

    public override string ToString() {
      object operand0 = m_operandArray[0],
             operand1 = m_operandArray[1],
             operand2 = m_operandArray[2];
      string operatorName = Enum.GetName(typeof(AssemblyOperator),
                                         Operator).Replace("_", " ");

      if ((Operator == AssemblyOperator.empty) ||
          (Operator == AssemblyOperator.new_middle_code)) {
        return null;
      }
      else if (Operator == AssemblyOperator.label) {
        return ((operand0 != null) ? ("\n" + operand0 + ":") : "") +
                ((operand1 != null) ? ("\t; " + operand1) : "");
      }
      else if (Operator == AssemblyOperator.comment) {
        return "\n\t" + ((operand0 != null) ? ("; " + operand0) : "");
      }
      else if (Operator == AssemblyOperator.define_address) {
        string aname = (string) operand0;
        int offset = (int) operand1;
        return "\tdq " + aname + WithSign(offset);
      }
      else if (Operator == AssemblyOperator.define_zero_sequence) {
        int size = (int) operand0;
        return "\ttimes " + size + " db 0";
      }
      else if (Operator == AssemblyOperator.define_value) {
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

          switch (TypeSize.Size(sort)) {
            case 1:
              return "\tdb " + text;

            case 2:
              return "\tdw " + text;

            case 4:
              return "\tdd " + text;

            case 8:
              return "\tdq " + text;

            default:
              Assert.ErrorXXX(false);
              return null;
          }
        }
      }
      else if (IsJumpRegister() || IsCallRegister() ||
               IsCallNotRegister()) {
        if (operand2 is string) {
          return "\tjmp " + operand2;
        }
        else {
          return "\tjmp " + operand0;
        }
      }
      else if (Operator == AssemblyOperator.address_return) {
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
          //return "\t" + operatorName + " x" + operand1;
        }
      }
      // mov ax, [bp + 2]; mov ax, [global + 4]
      else if ((operand0 is Register) && (operand1 != null) &&
               (operand2 is int)) {
        Assert.ErrorXXX((operand1 is Register) || (operand1 is string));
        return "\t" + operatorName + " " + operand0 +
               ", [" + operand1 + WithSign(operand2) + "]";
      }
      // mov ax, [null + 4]
      else if ((operand0 is Register) && (operand1 == null) &&
               (operand2 is int)) {
        return "\t" + operatorName + " " + operand0 +
                ", [" + operand2 + "]";
      }
      // mov [bp + 2], ax; mov [global + 4], ax; mov [bp + 2], 123; mov [global + 4], 123; mov [bp + 2], global; mov [global + 4], global
      else if ((operand0 != null) && (operand1 is int) &&
               (operand2 != null)) {
        Assert.ErrorXXX((operand0 is Register) || (operand0 is string));
        Assert.ErrorXXX((operand2 is Register) || (operand2 is BigInteger)
                      || (operand2 is string));
        return "\t" + operatorName +
               " [" + operand0 + WithSign(operand1) + "], " + operand2;
      }
      // mov [null + 4], ax; mov [null + 4], 123; mov [null + 4], global
      else if ((operand0 == null) && (operand1 is int) &&
               (operand2 != null)) {
        Assert.ErrorXXX((operand2 is Register) || (operand2 is BigInteger)
                      || (operand2 is string));
        return "\t" + operatorName + " [" + operand1 + "], " + operand2;
      }
      // inc [bp + 2]; inc [global + 4]
      else if ((operand0 != null) && (operand1 is int)) {
        Assert.ErrorXXX(((operand0 is Register) || (operand0 is string)) && (operand2 == null));
        if (operatorName.StartsWith("neg") || operatorName.StartsWith("not") ||
            operatorName.StartsWith("inc") || operatorName.StartsWith("dec") ||
            operatorName.StartsWith("mul") || operatorName.StartsWith("imul") ||
            operatorName.StartsWith("div") || operatorName.StartsWith("idiv") ||
            operatorName.StartsWith("fst") || operatorName.StartsWith("fld") ||
            operatorName.StartsWith("fist") || operatorName.StartsWith("fild")) {
          return "\t" + operatorName + " [" + operand0 + WithSign(operand1) + "]";
        }
        else {
          return "\t" + operatorName + " " + operand0 + ", " + operand1;
        }
      }
      // mov ax, bx; mov ax, 123; mov ax, global
      else if ((operand0 is Register) && (operand1 != null)) {
        Assert.ErrorXXX(operand2 == null);
        Assert.ErrorXXX((operand1 is Register) || (operand1 is BigInteger)
                      || (operand1 is string));
        return "\t" + operatorName + " " + operand0 + ", " + operand1;
      }
      // inc [null + 4]
      else if ((operand0 == null) && (operand1 is int)) {
        Assert.ErrorXXX(operand2 == null);
        return "\t" + operatorName + " [" + operand1 + "]";
      }
      // inc ax; int 33
      else if (operand0 != null) {
        Assert.ErrorXXX((operand0 is Register) || (operand0 is BigInteger));
        Assert.ErrorXXX((operand1 == null) && (operand2 == null));
        return "\t" + operatorName + " " + operand0;
      }
      // lahf
      else {
        Assert.ErrorXXX((operand0 == null) && (operand1 == null) &&
                      (operand2 == null));
        return "\t" + operatorName + " ";
      }
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
  }
}