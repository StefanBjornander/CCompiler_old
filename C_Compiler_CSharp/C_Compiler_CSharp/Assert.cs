//Huawei Kundtjänst 08-51255555

using System;

namespace CCompiler {
  public class Assert {
    public static void ErrorXXX(bool test) {
      if (!test) {
        Error(null, null);
      }
    }
  
    public static void Error(string message) {
      Error(message, null);
    }

    public static void Error(bool test, Message message) {
      if (!test) {
        Error(message, null);
      }
    }

    public static void Error(object value, Message message) {
      Error(false, value, message);
    }

    public static void Error(bool test, object value, Message message) {
      if (!test) {
        Error(message, value.ToString());
      }
    }

    public static void Error(Message message) {
      Error(message, null);
    }

    private static void Error(Message message, string text) {
      Error(Enum.GetName(typeof(Message), message).
            Replace("___", ",").Replace("__", "-").
            Replace("_", " "), text);
    }

    private static void Error(string message, string text) {
      Message("Error", message, text);    
      
      Console.In.ReadLine();
      System.Environment.Exit(-1);
    }


    private static void Message(string type, string message,
                                string text) {
      string funcText;

      if (SymbolTable.CurrentFunction != null) {
        funcText = " in function " + SymbolTable.CurrentFunction.UniqueName;
      }
      else {
        funcText = " in global space";
      }

      string extraText = (text != null) ? (": " + text) : "";
    
      if ((message != null) &&
          (CCompiler_Main.Scanner.Path != null)) {
        Console.Error.WriteLine(type + " at line " +
                CCompiler_Main.Scanner.Line + funcText +
                " in file " + CCompiler_Main.Scanner.Path.Name +
                ". " + message + extraText + ".");
      }
      else if ((message == null) &&
               (CCompiler_Main.Scanner.Path != null)) {
        Console.Error.WriteLine(type + " at line " +
                CCompiler_Main.Scanner.Line + funcText +
                " in file " + CCompiler_Main.Scanner.Path.Name +
                extraText + ".");
      }
      else if ((message != null) &&
               (CCompiler_Main.Scanner.Path == null)) {
        Console.Error.WriteLine(type + ". " + message +
                                extraText + ".");
      }
      else if ((message == null) &&
               (CCompiler_Main.Scanner.Path == null)) {
        Console.Error.WriteLine(type + extraText + ".");
      }
    }
  }
}