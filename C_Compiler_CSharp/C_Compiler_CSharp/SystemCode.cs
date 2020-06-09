using System;
using System.Linq;
using System.Collections.Generic;

namespace CCompiler {
  class SystemCode {
    private const int CARRY_FLAG = 0x01;
    private enum InOut {In, Out};
    private static List<Pair<Register, Symbol>> m_outParameterList = new List<Pair<Register, Symbol>>();
    private static IDictionary<String, List<AssemblyCode>> m_initializerMap = new Dictionary<String, List<AssemblyCode>>();
    private static IDictionary<String, List<Pair<Register,InOut>>> m_parameterMap = new Dictionary<String, List<Pair<Register, InOut>>>();
    private static IDictionary<String, Type> m_returnTypeMap = new Dictionary<String, Type>();
    private static IDictionary<String, Object> m_returnMap = new Dictionary<String, Object>();
    private static IDictionary<String, int> m_valueMap = new Dictionary<String, int>();
    private static IDictionary<String, int> m_carryMap = new Dictionary<String, int>();

    static SystemCode() {
      { { List<AssemblyCode> readCharInitializerList = new List<AssemblyCode>();
          readCharInitializerList.Add(new AssemblyCode(AssemblyOperator.mov, Register.ah, 0x3F));
          readCharInitializerList.Add(new AssemblyCode(AssemblyOperator.mov, Register.cx, 1));
          m_initializerMap.Add("read_char", readCharInitializerList);
        }

        { List<Pair<Register,InOut>> readCharParameterList = new List<Pair<Register,InOut>>();
          readCharParameterList.Add(new Pair<Register,InOut>(Register.bx, InOut.In));
          readCharParameterList.Add(new Pair<Register,InOut>(Register.dx, InOut.In));
          m_parameterMap.Add("read_char", readCharParameterList);
        }

        m_returnTypeMap.Add("read_char", Type.SignedIntegerType);
        m_returnMap.Add("read_char", Register.ax);
      }

      { { List<AssemblyCode> writeCharInitializerList = new List<AssemblyCode>();
          writeCharInitializerList.Add(new AssemblyCode(AssemblyOperator.mov, Register.ah, 0x40));
          writeCharInitializerList.Add(new AssemblyCode(AssemblyOperator.mov, Register.cx, 1));
          m_initializerMap.Add("write_char", writeCharInitializerList);
        }
        
        { List<Pair<Register,InOut>> writeCharParameterList = new List<Pair<Register,InOut>>();
          writeCharParameterList.Add(new Pair<Register,InOut>(Register.bx, InOut.In));
          writeCharParameterList.Add(new Pair<Register,InOut>(Register.dx, InOut.In));
          m_parameterMap.Add("write_char", writeCharParameterList);
        }

        m_returnTypeMap.Add("write_char", Type.SignedIntegerType);
        m_returnMap.Add("write_char", Register.ax);
      }

      { { List<AssemblyCode> fileExistsInitializerList = new List<AssemblyCode>();
          fileExistsInitializerList.Add(new AssemblyCode(AssemblyOperator.mov, Register.ah, 0x43));
          fileExistsInitializerList.Add(new AssemblyCode(AssemblyOperator.mov, Register.al, 0));
          m_initializerMap.Add("file_exists", fileExistsInitializerList);
        }

        { List<Pair<Register,InOut>> fileExistsParameterList = new List<Pair<Register,InOut>>();
          fileExistsParameterList.Add(new Pair<Register,InOut>(Register.dx, InOut.In));
          m_parameterMap.Add("file_exists", fileExistsParameterList);
        }

        m_returnTypeMap.Add("file_exists", Type.SignedIntegerType);
        m_returnMap.Add("file_exists", 1);
        m_carryMap.Add("file_exists", 0);
      }

      /*{ { List<ObjectCode> fileSizeInitializerList = new List<ObjectCode>();
          fileSizeInitializerList.Add(new ObjectCode(ObjectOperator.mov, Register.ah, 0x42));
          fileSizeInitializerList.Add(new ObjectCode(ObjectOperator.mov, Register.cx, 0));
          fileSizeInitializerList.Add(new ObjectCode(ObjectOperator.mov, Register.dx, 0));
          m_initializerMap.Add("file_size", fileSizeInitializerList);
        }

        { List<Pair<Register,InOut>> fileSizeParameterList = new List<Pair<Register,InOut>>();
          fileSizeParameterList.Add(new Pair<Register,InOut>(Register.bx, InOut.In));
          m_parameterMap.Add("file_size", fileSizeParameterList);
        }

        m_returnTypeMap.Add("file_size", Type.UnsignedLongIntegerType);
        m_returnMap.Add("file_size", Register.ax);
        m_carryMap.Add("file_size", -1);
      }*/

      { { List<AssemblyCode> fileCreateInitializerList = new List<AssemblyCode>();
          fileCreateInitializerList.Add(new AssemblyCode(AssemblyOperator.mov, Register.ah, 0x3C));
          fileCreateInitializerList.Add(new AssemblyCode(AssemblyOperator.mov, Register.cx, 0));
          m_initializerMap.Add("file_create", fileCreateInitializerList);
        }

        { List<Pair<Register,InOut>> fileCreateParameterList = new List<Pair<Register,InOut>>();
          fileCreateParameterList.Add(new Pair<Register,InOut>(Register.dx, InOut.In));
          m_parameterMap.Add("file_create", fileCreateParameterList);
        }

        m_returnTypeMap.Add("file_create", Type.SignedIntegerType);
        m_returnMap.Add("file_create", Register.ax);
        m_carryMap.Add("file_create", -1);
      }

      { { List<AssemblyCode> fileOpenInitializerList = new List<AssemblyCode>();
          fileOpenInitializerList.Add(new AssemblyCode(AssemblyOperator.mov, Register.ah, 0x3D));
          m_initializerMap.Add("file_open", fileOpenInitializerList);
        }

        { List<Pair<Register,InOut>> fileOpenParameterList = new List<Pair<Register,InOut>>();
          fileOpenParameterList.Add(new Pair<Register,InOut>(Register.dx, InOut.In));
          fileOpenParameterList.Add(new Pair<Register,InOut>(Register.al, InOut.In));
          m_parameterMap.Add("file_open", fileOpenParameterList);
        }

        m_returnTypeMap.Add("file_open", Type.SignedIntegerType);
        m_returnMap.Add("file_open", Register.ax);
        m_carryMap.Add("file_open", -1);
      }

      { { List<AssemblyCode> fileCloseInitializerList = new List<AssemblyCode>();
          fileCloseInitializerList.Add(new AssemblyCode(AssemblyOperator.mov, Register.ah, 0x3E));
          m_initializerMap.Add("file_close", fileCloseInitializerList);
        }

        { List<Pair<Register,InOut>> fileCloseParameterList = new List<Pair<Register,InOut>>();
          fileCloseParameterList.Add(new Pair<Register,InOut>(Register.bx, InOut.In));
          m_parameterMap.Add("file_close", fileCloseParameterList);
        }

        m_returnTypeMap.Add("file_close", Type.SignedIntegerType);
        m_returnMap.Add("file_close", 0);
        m_carryMap.Add("file_close", -1);
      }

      { { List<AssemblyCode> fileRemoveInitializerList = new List<AssemblyCode>();
          fileRemoveInitializerList.Add(new AssemblyCode(AssemblyOperator.mov, Register.ah, 0x41));
          fileRemoveInitializerList.Add(new AssemblyCode(AssemblyOperator.mov, Register.cl, 0));
          m_initializerMap.Add("file_remove", fileRemoveInitializerList);
        }

        { List<Pair<Register,InOut>> fileRemoveParameterList = new List<Pair<Register,InOut>>();
          fileRemoveParameterList.Add(new Pair<Register,InOut>(Register.dx, InOut.In));
          m_parameterMap.Add("file_remove", fileRemoveParameterList);
        }

        m_returnTypeMap.Add("file_remove", Type.SignedIntegerType);
        m_returnMap.Add("file_remove", 0);
        m_carryMap.Add("file_remove", -1);
      }

      { { List<AssemblyCode> fileRenameInitializerList = new List<AssemblyCode>();
          fileRenameInitializerList.Add(new AssemblyCode(AssemblyOperator.mov, Register.ah, 0x56));
          fileRenameInitializerList.Add(new AssemblyCode(AssemblyOperator.mov, Register.cl, 0));
          m_initializerMap.Add("file_rename", fileRenameInitializerList);
        }

        { List<Pair<Register,InOut>> fileRenameParameterList = new List<Pair<Register,InOut>>();
          fileRenameParameterList.Add(new Pair<Register,InOut>(Register.dx, InOut.In));
          fileRenameParameterList.Add(new Pair<Register,InOut>(Register.di, InOut.In));
          m_parameterMap.Add("file_rename", fileRenameParameterList);
        }

        m_returnTypeMap.Add("file_rename", Type.SignedIntegerType);
        m_returnMap.Add("file_rename", 0);
        m_carryMap.Add("file_rename", -1);
      }

      { { List<AssemblyCode> fileReadInitializerList = new List<AssemblyCode>();
          fileReadInitializerList.Add(new AssemblyCode(AssemblyOperator.mov, Register.ah, 0x3F));
          m_initializerMap.Add("file_read", fileReadInitializerList);
        }

        { List<Pair<Register,InOut>> fileReadParameterList = new List<Pair<Register,InOut>>();
          fileReadParameterList.Add(new Pair<Register,InOut>(Register.bx, InOut.In));
          fileReadParameterList.Add(new Pair<Register,InOut>(Register.cx, InOut.In));
          fileReadParameterList.Add(new Pair<Register,InOut>(Register.dx, InOut.In));
          m_parameterMap.Add("file_read", fileReadParameterList);
        }

        m_returnTypeMap.Add("file_read", Type.SignedIntegerType);
        m_returnMap.Add("file_read", Register.ax);
        m_carryMap.Add("file_read", -1);
      }

      { { List<AssemblyCode> fileWriteInitializerList = new List<AssemblyCode>();
          fileWriteInitializerList.Add(new AssemblyCode(AssemblyOperator.mov, Register.ah, 0x40));
          m_initializerMap.Add("file_write", fileWriteInitializerList);
        }

        { List<Pair<Register,InOut>> fileWriteParameterList = new List<Pair<Register,InOut>>();
          fileWriteParameterList.Add(new Pair<Register,InOut>(Register.bx, InOut.In));
          fileWriteParameterList.Add(new Pair<Register,InOut>(Register.cx, InOut.In));
          fileWriteParameterList.Add(new Pair<Register,InOut>(Register.dx, InOut.In));
          m_parameterMap.Add("file_write", fileWriteParameterList);
        }

        m_returnTypeMap.Add("file_write", Type.SignedIntegerType);
        m_returnMap.Add("file_write", Register.ax);
        m_carryMap.Add("file_write", -1);
      }

      { { List<AssemblyCode> fileFSeekInitializerList = new List<AssemblyCode>();
          fileFSeekInitializerList.Add(new AssemblyCode(AssemblyOperator.mov, Register.ah, 0x42));
          fileFSeekInitializerList.Add(new AssemblyCode(AssemblyOperator.mov, Register.cx, 0));
          m_initializerMap.Add("file_seek", fileFSeekInitializerList);
        }

        { List<Pair<Register,InOut>> fileFSeekParameterList = new List<Pair<Register,InOut>>();
          fileFSeekParameterList.Add(new Pair<Register,InOut>(Register.bx, InOut.In));
          fileFSeekParameterList.Add(new Pair<Register, InOut>(Register.al, InOut.In));
          fileFSeekParameterList.Add(new Pair<Register, InOut>(Register.dx, InOut.In));
          m_parameterMap.Add("file_seek", fileFSeekParameterList);
        }

        m_returnTypeMap.Add("file_seek", Type.UnsignedIntegerType);
        m_returnMap.Add("file_seek", Register.ax);
        m_carryMap.Add("file_seek", -1);
      }

      { { List<AssemblyCode> signalInitializerList = new List<AssemblyCode>();
          signalInitializerList.Add(new AssemblyCode(AssemblyOperator.mov, Register.ah, 0x25));
          m_initializerMap.Add("signal", signalInitializerList);
        }

        { List<Pair<Register,InOut>> signalParameterList = new List<Pair<Register,InOut>>();
          signalParameterList.Add(new Pair<Register,InOut>(Register.al, InOut.In));
          signalParameterList.Add(new Pair<Register,InOut>(Register.dx, InOut.In));
          m_parameterMap.Add("signal", signalParameterList);
        }

        m_returnTypeMap.Add("signal", Type.VoidType);
      }

      { { List<AssemblyCode> raiseInitializerList = new List<AssemblyCode>();
          raiseInitializerList.Add(new AssemblyCode(AssemblyOperator.mov, Register.ah, 0x35));
          m_initializerMap.Add("raise", raiseInitializerList);
        }

        { List<Pair<Register,InOut>> raiseParameterList = new List<Pair<Register,InOut>>();
          raiseParameterList.Add(new Pair<Register,InOut>(Register.al, InOut.In));
          m_parameterMap.Add("raise", raiseParameterList);
        }

        m_returnTypeMap.Add("raise", Type.UnsignedIntegerType);
        m_returnMap.Add("raise", Register.bx);
      }

      { { List<AssemblyCode> abortInitializerList = new List<AssemblyCode>();
          abortInitializerList.Add(new AssemblyCode(AssemblyOperator.mov, Register.ah, 0x4C));
          abortInitializerList.Add(new AssemblyCode(AssemblyOperator.mov, Register.al, -1));
          m_initializerMap.Add("abort", abortInitializerList);
        }

        m_returnTypeMap.Add("abort", Type.VoidType);
      }

      { { List<AssemblyCode> exitInitializerList = new List<AssemblyCode>();
          exitInitializerList.Add(new AssemblyCode(AssemblyOperator.mov, Register.ah, 0x4C));
          m_initializerMap.Add("exit", exitInitializerList);
        }

        { List<Pair<Register,InOut>> exitParameterList = new List<Pair<Register,InOut>>();
          exitParameterList.Add(new Pair<Register,InOut>(Register.al, InOut.In));
          m_parameterMap.Add("exit", exitParameterList);
        }

        m_returnTypeMap.Add("exit", Type.VoidType);
      }

      { { List<AssemblyCode> dateInitializerList = new List<AssemblyCode>();
          dateInitializerList.Add(new AssemblyCode(AssemblyOperator.mov, Register.ah, 0x2A));
          m_initializerMap.Add("date", dateInitializerList);
        }

        { List<Pair<Register,InOut>> dateParameterList = new List<Pair<Register,InOut>>();
          dateParameterList.Add(new Pair<Register,InOut>(Register.cx, InOut.Out));
          dateParameterList.Add(new Pair<Register, InOut>(Register.dh, InOut.Out));
          dateParameterList.Add(new Pair<Register, InOut>(Register.dl, InOut.Out));
          dateParameterList.Add(new Pair<Register, InOut>(Register.al, InOut.Out));
          m_parameterMap.Add("date", dateParameterList);
        }

        m_returnTypeMap.Add("date", Type.VoidType);
      }

      { { List<AssemblyCode> timeInitializerList = new List<AssemblyCode>();
          timeInitializerList.Add(new AssemblyCode(AssemblyOperator.mov, Register.ah, 0x2C));
          m_initializerMap.Add("time", timeInitializerList);
        }

        { List<Pair<Register,InOut>> timeParameterList = new List<Pair<Register,InOut>>();
          timeParameterList.Add(new Pair<Register,InOut>(Register.ch, InOut.Out));
          timeParameterList.Add(new Pair<Register, InOut>(Register.cl, InOut.Out));
          timeParameterList.Add(new Pair<Register, InOut>(Register.dh, InOut.Out));
          //timeParameterList.Add(new Pair<Register, InOut>(Register.dl, InOut.Out));
          m_parameterMap.Add("time", timeParameterList);
        }

        m_returnTypeMap.Add("time", Type.VoidType);
      }
    }

    public static Type ReturnType(String name) {
      Assert.ErrorXXX(m_returnTypeMap.ContainsKey(name));
      return m_returnTypeMap[name];
    }

    public static void GenerateInitializer(String name, AssemblyCodeGenerator objectCodeGenerator) {
      // Empty.
    }

    public static void GenerateParameter(String name, int index, Symbol argSymbol, AssemblyCodeGenerator objectCodeGenerator) {
      Assert.ErrorXXX(m_parameterMap.ContainsKey(name));
      List<Pair<Register,InOut>> parameterList = m_parameterMap[name];
      Assert.ErrorXXX(index < parameterList.Count);
      Pair<Register,InOut> pair = parameterList[index];
      Register register = pair.First;
      InOut inOut = pair.Second;

      if (inOut == InOut.In) {
        objectCodeGenerator.LoadValueToRegister(argSymbol, register);
      }
      else {
        m_outParameterList.Add(new Pair<Register,Symbol>(register, argSymbol));
      }
    }

    public static void GenerateCall(String name, Symbol returnSymbol, AssemblyCodeGenerator objectCodeGenerator) {
      List<AssemblyCode> initializerList = m_initializerMap[name];

      foreach (AssemblyCode objectCode in initializerList) {
        //objectCodeGenerator.AddAssemblyCode(objectCode);
      }
      
      objectCodeGenerator.AddAssemblyCode(AssemblyOperator.interrupt, 0x21);

      foreach (Pair<Register,Symbol> pair in m_outParameterList) {
        Register outRegister = pair.First;
        Symbol outSymbol = pair.Second;
        Track outTrack = new Track(outSymbol, outRegister);
        //objectCodeGenerator.m_trackSet.Add(outTrack);
        objectCodeGenerator.SaveValueFromRegister(outTrack, outSymbol);
      }

      m_outParameterList.Clear();

      if ((returnSymbol != null) && !returnSymbol.Type.IsVoid()) {
        Object returnObject = m_returnMap[name];
        Track returnTrack;

        if (returnObject is Register) {
          returnTrack = new Track(returnSymbol, (Register) returnObject);
        }
        else {
          returnTrack = new Track(returnSymbol);
          objectCodeGenerator.AddAssemblyCode(AssemblyOperator.mov, returnTrack, (int) returnObject);
        }

        if (m_carryMap.ContainsKey(name)) {
          objectCodeGenerator.AddAssemblyCode(AssemblyOperator.jnc, null, objectCodeGenerator.m_assemblyCodeList.Count + 2);
          objectCodeGenerator.AddAssemblyCode(AssemblyOperator.mov, returnTrack, m_carryMap[name]);
//          jumpCode = new ObjectCode(ObjectOperator.long_jmp, null, objectCodeGenerator.m_objectCodeList.Count + 3);
//          objectCodeGenerator.AddAssemblyCode(jumpCode);
        }

        objectCodeGenerator.m_trackMap.Add(returnSymbol, returnTrack);
        //objectCodeGenerator.m_trackSet.Add(returnTrack);
      }
    }

    public static List<Pair<Register,Symbol>> OutParameterList() {
      return m_outParameterList;
    }

    /*
    public static void GenerateCall(String name, List<Expression> argExprList, Symbol returnSymbol, ObjectCodeGenerator objectCodeGenerator) {
      switch (name) {
        case "read_char":
          Assert.Error(argExprList.Count == 2);
          Assert.Error(argExprList[0].LongList.Count == 0);
          //objectCodeGenerator.ObjectCodeList(argExprList[0].LongList);
          objectCodeGenerator.LoadValueToRegister(argExprList[0].Symbol, Register.bx);
          Assert.Error(argExprList[1].LongList.Count == 0);
          //objectCodeGenerator.ObjectCodeList(argExprList[1].LongList);
          objectCodeGenerator.LoadValueToRegister(argExprList[1].Symbol, Register.dx);
          objectCodeGenerator.AddAssemblyCode(ObjectOperator.mov, Register.ah, 0x3F);
          objectCodeGenerator.AddAssemblyCode(ObjectOperator.mov, Register.cx, 1);
          objectCodeGenerator.AddAssemblyCode(ObjectOperator.interrupt, 0x21);
          Assert.Error((returnSymbol == null) || returnSymbol.Type.IsVoid());
          break;

        case "write_char":
          Assert.Error(argExprList.Count == 2);
          Assert.Error(argExprList[0].LongList.Count == 0);
          //objectCodeGenerator.ObjectCodeList(argExprList[0].LongList);
          objectCodeGenerator.LoadValueToRegister(argExprList[0].Symbol, Register.bx);
          Assert.Error(argExprList[1].LongList.Count == 0);
          //objectCodeGenerator.ObjectCodeList(argExprList[1].LongList);
          objectCodeGenerator.LoadValueToRegister(argExprList[1].Symbol, Register.dx);
          objectCodeGenerator.AddAssemblyCode(ObjectOperator.mov, Register.ah, 0x40);
          objectCodeGenerator.AddAssemblyCode(ObjectOperator.mov, Register.cx, 1);
          objectCodeGenerator.AddAssemblyCode(ObjectOperator.interrupt, 0x21);
          Assert.Error((returnSymbol == null) || returnSymbol.Type.IsVoid());
          break;
      }
    }*/
  }
}
