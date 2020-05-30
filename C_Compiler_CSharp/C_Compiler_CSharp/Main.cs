using System;
using System.IO;
using System.Text;
using System.Numerics;
using System.Globalization;
using System.Collections.Generic;

namespace CCompiler {
  public class Start {
    public static bool Windows = true, Linux = false;
    public static IDictionary<string,ISet<FileInfo>> DependencySetMap = new Dictionary<string,ISet<FileInfo>>();

    public static void Main(string[] args){
      Assert.ErrorA((Windows && !Linux) || (!Windows && Linux));
      System.Threading.Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
      ObjectCodeTable.Init();
      args = new string[]{"-r", "-p", /*"-w", "-c",*/ "Main", "Malloc", "CType", "ErrNo", "Locale", "Math", "SetJmp",      
                          "Signal", "File", "Temp", "Scanf", "Printf", "StdLib", "Time",
                          "String", "PrintTest", "CharacterTest", "FloatTest", "LimitsTest",
                          "AssertTest", "StringTest", "LocaleTest",
                          "SetJmpTest", "MathTest", "FileTest", "StdIOTest",
                          "SignalTest", "StackTest", "MallocTest",
                          "StdLibTest", "TimeTest"};

      if (args.Length == 0) {
        Console.Error.WriteLine("usage: compiler <filename>");
        Environment.Exit(-1);
      }
    
      List<string> argList = new List<string>();
      foreach (string text in args) {
        argList.Add(text);
      }

      bool rebuild = argList.Remove("-r"),
           print = argList.Remove("-p");
      bool noLink = argList.Remove("-nolink");

      Preprocessor.IncludePath = Environment.GetEnvironmentVariable("include");
      string pathName = @"C:\Users\Stefan\Documents\vagrant\homestead\code\code\";
    
      if (Preprocessor.IncludePath == null) {
        Preprocessor.IncludePath = pathName;
      }

      try {
        bool doLink = false;
        string pathName2 = pathName;

        foreach (string arg in argList) {
          FileInfo file = new FileInfo(pathName + arg);
       
          if (rebuild || !IsObjectFileUpToDate(file)) {
            if (print) {
              Console.Out.WriteLine("Compiling \"" + file.FullName + ".c\"."); 
            }

            ReadSourceFile(file);
            doLink = true;
          }
        }

        if (Start.Windows) {
          if (!noLink && doLink) {
            FileInfo targetFile;

            AssemblyCodeGenerator.PathText = "C:\\D\\" + argList[0] + ".com";
            targetFile = new FileInfo(AssemblyCodeGenerator.PathText);
            Linker linker = new LinkerWindows(targetFile);

            CCompiler_Main.Scanner.Path = null;
            foreach (string arg in argList) {
              FileInfo file = new FileInfo(pathName + arg);

              if (print) {
                Console.Out.WriteLine("Loading \"" + file.FullName + ".obj\".");
              }
          
              ReadObjectFile(file, linker);
            }

            linker.Generate();
          }
          else if (print) {
            Console.Out.WriteLine(pathName + argList[0] +".com is up-to-date.");
          }
        }
        
        if (Start.Linux) {
          StreamWriter makeStream = new StreamWriter(@"C:\Users\Stefan\Documents\vagrant\homestead\code\code\makefile");
          makeStream.Write("main:");
          foreach (string arg in argList) {
            makeStream.Write(" " + arg.ToLower() + ".o");
          }
          makeStream.WriteLine();

          makeStream.Write("\tld -o main");
          foreach (string arg in argList) {
            makeStream.Write(" " + arg.ToLower() + ".o");
          }
          makeStream.WriteLine();
          makeStream.WriteLine();

          foreach (string arg in argList) {
            makeStream.Write(arg.ToLower() + ".o: " + arg.ToLower() + ".c " + arg.ToLower() + ".asm");
            ISet<FileInfo> dependencySet = DependencySetMap[arg];

            foreach (FileInfo dependency in dependencySet) {
              makeStream.Write(" " + dependency.Name.ToLower());
            }

            makeStream.WriteLine();
            makeStream.WriteLine("\tnasm -f elf64 -o " + arg.ToLower() + ".o " + arg.ToLower() + ".asm");
            makeStream.WriteLine();
          }

          makeStream.WriteLine("clear:");
          foreach (string arg in argList) {
            makeStream.WriteLine("\trm " + arg.ToLower() + ".o");
          }

          makeStream.WriteLine("\trm main");
          makeStream.Close();
        }
      }
      catch (Exception exception) {
        Console.Out.WriteLine(exception.StackTrace);
        Assert.Error(exception.Message, Message.Parse_error);
      }
    }

    public static void ReadObjectFile(FileInfo file, Linker linker) {
      FileInfo objectFile = new FileInfo(file.FullName + ".obj");

      try {
        BinaryReader dataInputStream = new BinaryReader(File.OpenRead(objectFile.FullName));

        int linkerSetSize = dataInputStream.ReadInt32();
        for (int count = 0; count < linkerSetSize; ++count) {
          StaticSymbolWindows staticSymbol = new StaticSymbolWindows();
          staticSymbol.Load(dataInputStream);
          linker.Add(staticSymbol);
        }

        dataInputStream.Close();
      }
      catch (Exception exception) {
        Console.Out.WriteLine(exception.StackTrace);
        Assert.Error(exception.Message);
      }
    }
  
    public static void ReadSourceFile(FileInfo file) {
      FileInfo sourceFile = new FileInfo(file.FullName + ".c"),
               preproFile = new FileInfo(file.FullName + ".p"),
               middleFile = new FileInfo(file.FullName + ".mid");
      Preprocessor.MacroMap = new Dictionary<string,Macro>();

      if (Start.Windows) {
        Preprocessor.MacroMap.Add("__WINDOWS__", new Macro(0, new List<Token>()));
      }
      
      if (Start.Linux) {
        Preprocessor.MacroMap.Add("__LINUX__", new Macro(0, new List<Token>()));
      }

      Preprocessor.IncludeSet = new HashSet<FileInfo>();
      Preprocessor preprocessor = new Preprocessor();
      preprocessor.DoProcess(sourceFile);
      Assert.Error(Preprocessor.IfStack.Count == 0, Message.If___ifdef____or_ifndef_directive_without_matching_endif);
    
      StreamWriter preproStream = File.CreateText(preproFile.FullName);
      preproStream.Write(preprocessor.GetText());
      preproStream.Close();
      DependencySetMap.Add(file.Name, Preprocessor.IncludeSet);

      byte[] byteArray = Encoding.ASCII.GetBytes(preprocessor.GetText());
      MemoryStream memoryStream = new MemoryStream(byteArray);
      CCompiler_Main.Scanner scanner = new CCompiler_Main.Scanner(memoryStream);

      try {
        SymbolTable.CurrentTable = new SymbolTable(null, Scope.Global);

        StaticSymbol integralStorageSymbol = ConstantExpression.Value(AssemblyCodeGenerator.IntegralStorageName, Type.UnsignedLongIntegerType, null);
        SymbolTable.StaticSet.Add(integralStorageSymbol);

        CCompiler_Main.Scanner.Path = sourceFile;
        CCompiler_Main.Scanner.Line = 1;
        CCompiler_Main.Parser parser = new CCompiler_Main.Parser(scanner);
        Assert.Error(parser.Parse(), Message.Syntax_error);
      }
      catch (IOException ioException) {
        Assert.Error(false, ioException.StackTrace, Message.Syntax_error);
      }

      if (Start.Windows) {
        FileInfo depFile = new FileInfo(file.FullName + ".dep");
        StreamWriter includeWriter = new StreamWriter(File.Open(depFile.FullName, FileMode.Create));
        bool first = true;
        foreach (FileInfo includeFile in Preprocessor.IncludeSet) {
          includeWriter.Write((first ? "" : " ") + includeFile.Name);
          first = false;
        }
        includeWriter.Close();

        FileInfo objectFile = new FileInfo(file.FullName + ".obj");
        BinaryWriter binaryWriter = new BinaryWriter(File.Open(objectFile.FullName, FileMode.Create));

        binaryWriter.Write(SymbolTable.StaticSet.Count);    
        foreach (StaticSymbol staticSymbol in SymbolTable.StaticSet) {
          staticSymbol.Save(binaryWriter);
        }

        binaryWriter.Close();
      }

      if (Start.Linux) {
        ISet<string> totalGlobalSet = new HashSet<string>(),
                     totalExternSet = new HashSet<string>();
        List<string> totalTextList = new List<string>(),
                     totalDataList = new List<string>();
                     
        foreach (StaticSymbol staticSymbol in SymbolTable.StaticSet) {
          StaticSymbolLinux staticSymbolLinux = (StaticSymbolLinux) staticSymbol;
          totalExternSet.UnionWith(staticSymbolLinux.ExternSet);
        }

        
        StaticSymbolLinux initSymbol = null, argsSymbol = null, mainSymbol = null;
        foreach (StaticSymbol staticSymbol in SymbolTable.StaticSet) {
          if (staticSymbol.UniqueName.Equals(AssemblyCodeGenerator.InitName)) {
            initSymbol = (StaticSymbolLinux) staticSymbol;
          }

          if (staticSymbol.UniqueName.Equals(AssemblyCodeGenerator.MainName)) {
            mainSymbol = (StaticSymbolLinux) staticSymbol;
          }

          if (staticSymbol.UniqueName.Equals(AssemblyCodeGenerator.ArgsName)) {
            argsSymbol = (StaticSymbolLinux) staticSymbol;
          }
        }

        if (initSymbol != null) {
          totalTextList.Add("\tglobal _start");
          totalTextList.Add("_start:");
          totalTextList.AddRange(initSymbol.TextList);
          SymbolTable.StaticSet.Remove(initSymbol);
          totalDataList.Add("$StackTop:\ttimes 65536 db 0");
        }

        if (argsSymbol != null) {
          totalTextList.AddRange(argsSymbol.TextList);
          SymbolTable.StaticSet.Remove(argsSymbol);
        }

        if (mainSymbol != null) {
          totalTextList.AddRange(mainSymbol.TextList);
          SymbolTable.StaticSet.Remove(mainSymbol);
        }

        foreach (StaticSymbol staticSymbol in SymbolTable.StaticSet) {
          StaticSymbolLinux staticSymbolLinux = (StaticSymbolLinux) staticSymbol;
          totalExternSet.Remove(staticSymbolLinux.UniqueName);
          
          if (!staticSymbolLinux.UniqueName.Contains(Symbol.SeparatorId)) {
            totalGlobalSet.Add(staticSymbolLinux.UniqueName);
          }
 
          if (staticSymbolLinux.TextOrDataX == StaticSymbolLinux.TextOrData.Text) {
            if (staticSymbolLinux.UniqueName.Equals(AssemblyCodeGenerator.MainName)) {
              totalTextList.Add("_start:");
              totalTextList.AddRange(staticSymbolLinux.TextList);
              totalDataList.Add("$StackTop:\ttimes 65536 db 0");
            }
            else {
              totalTextList.AddRange(staticSymbolLinux.TextList);
            }
          }
          else {
            totalDataList.AddRange(staticSymbolLinux.TextList);
          }
        }

        FileInfo assemblyFile = new FileInfo(file.FullName + ".asm");
        File.Delete(assemblyFile.FullName);
        StreamWriter streamWriter = new StreamWriter(assemblyFile.FullName);

        foreach (String globalName in totalGlobalSet) {
          if (globalName.Equals(AssemblyCodeGenerator.MainName)) {
            streamWriter.WriteLine("\tglobal _start");            
          }
          
          if (!globalName.EndsWith(Symbol.NumberId)) {
            streamWriter.WriteLine("\tglobal " + globalName);
          }
        }

        streamWriter.WriteLine();
        foreach (String externName in totalExternSet) {
          streamWriter.WriteLine("\textern " + externName);
        }

        streamWriter.WriteLine("section .text");
        foreach (String textLine in totalTextList) {
          streamWriter.WriteLine(textLine);
        }

        streamWriter.WriteLine("section .data");
        foreach (String dataLine in totalDataList) {
          streamWriter.WriteLine(dataLine);
        }

        streamWriter.Close();
      }
    }

    public static bool IsObjectFileUpToDate(FileInfo file) {
      FileInfo sourceFile = new FileInfo(file.FullName + ".c"),
               objectFile = new FileInfo(file.FullName + ".obj");

      if (!objectFile.Exists || (sourceFile.LastWriteTime > objectFile.LastWriteTime)) {
        return false;
      }
      
      FileInfo depFile = new FileInfo(file.FullName + ".dep");
      if (depFile.Exists) {
        try {
          StreamReader depReader = new StreamReader(File.OpenRead(depFile.FullName));
          string text = depReader.ReadToEnd();
          depReader.Close();

          if (text.Length > 0) {
            string[] array = text.Split(' ');

            foreach (string name in array)  {
              FileInfo nameFile = new FileInfo(file.Directory + "\\" + name);
              if (!nameFile.Exists || (nameFile.LastWriteTime > objectFile.LastWriteTime)) {
                return false;
              }
            }
          }
        }
        catch (IOException ioException) {
          Console.Out.WriteLine(ioException.StackTrace);
          return false;
        }
      }

      return true;
    }
  }
}

namespace CCompiler_Main {
  public partial class Parser : QUT.Gppg.ShiftReduceParser<ValueType, QUT.Gppg.LexLocation> {
    public Parser(Scanner scanner)
     :base(scanner) {
      // Empty.
    }
  }
}

namespace CCompiler_Exp {
  public partial class Parser : QUT.Gppg.ShiftReduceParser<ValueType, QUT.Gppg.LexLocation> {
    public Parser(Scanner scanner)
     :base(scanner) {
      // Empty.
    }
  }
}

namespace CCompiler_Pre {
  public partial class Parser : QUT.Gppg.ShiftReduceParser<ValueType, QUT.Gppg.LexLocation> {
    public Parser(Scanner scanner)
     :base(scanner) {
      // Empty.
    }
  }
}