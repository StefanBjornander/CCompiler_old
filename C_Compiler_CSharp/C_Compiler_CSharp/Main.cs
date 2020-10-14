// -rebuild -print Main Malloc CType ErrNo Locale Math SetJmp Signal File Temp Scanf Printf StdLib Time String PrintTest CharacterTest FloatTest LimitsTest AssertTest StringTest LocaleTest SetJmpTest MathTest FileTest StdIOTest SignalTest StackTest MallocTest StdLibTest TimeTest

using System;
using System.IO;
using System.Text;
using System.Globalization;
using System.Collections.Generic;

namespace CCompiler {
  public class Start {
    public static bool Windows = true, Linux = false;
    public static IDictionary<string,ISet<FileInfo>> DependencySetMap =
      new Dictionary<string,ISet<FileInfo>>();

    public static void Main(string[] args){
      Assert.ErrorXXX((Windows && !Linux) || (!Windows && Linux));
      System.Threading.Thread.CurrentThread.CurrentCulture =
        CultureInfo.InvariantCulture;
      ObjectCodeTable.Initializer();

      if (args.Length == 0) {
        Assert.Error("usage: compiler <filename>");
      }
    
      List<string> argList = new List<string>(args);
      bool rebuild = argList.Remove("-rebuild"),
           print = argList.Remove("-print");
      bool doNotLink = argList.Remove("-nolink");

      Preprocessor.IncludePath =
        Environment.GetEnvironmentVariable("include_path");
      Assert.Error(Preprocessor.IncludePath != null,
                   Message.Missing_include_path);

      try {
        bool doLink = false;

        foreach (string arg in argList) {
          FileInfo file =
            new FileInfo(Path.Combine(Preprocessor.IncludePath, arg));
       
          if (rebuild || !IsObjectFileUpToDate(file)) {
            if (print) {
              Console.Out.WriteLine("Compiling \"" + file.FullName + ".c\"."); 
            }

            ReadSourceFile(file);
            doLink = true;
          }
        }

        if (Start.Linux) {
          string linuxPath = Environment.GetEnvironmentVariable("linux_path");
          StreamWriter makeStream = new StreamWriter(Path.Combine(linuxPath, "makefile"));

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
            makeStream.Write(arg.ToLower() + ".o: " + arg.ToLower() + ".c " +
                             arg.ToLower() + ".asm");
            ISet<FileInfo> dependencySet = DependencySetMap[arg];

            foreach (FileInfo dependency in dependencySet) {
              makeStream.Write(" " + dependency.Name.ToLower());
            }

            makeStream.WriteLine();
            makeStream.WriteLine("\tnasm -f elf64 -o " + arg.ToLower() + ".o "
                                 + arg.ToLower() + ".asm");
            makeStream.WriteLine();
          }

          makeStream.WriteLine("clear:");
          foreach (string arg in argList) {
            makeStream.WriteLine("\trm " + arg.ToLower() + ".o");
          }

          makeStream.WriteLine("\trm main");
          makeStream.Close();
        }

        if (Start.Windows) {
          if (!doNotLink && doLink) {
            string windowsPath = Environment.GetEnvironmentVariable("windows_path");
            FileInfo targetFile = new FileInfo(Path.Combine(windowsPath, argList[0] + ".com"));
            Linker linker = new Linker();

            CCompiler_Main.Scanner.Path = null;
            foreach (string arg in argList) {
              FileInfo file =
                new FileInfo(Path.Combine(Preprocessor.IncludePath, arg));

              if (print) {
                Console.Out.WriteLine("Loading \"" + file.FullName +
                                      ".obj\".");
              }
          
              ReadObjectFile(file, linker);
            }

            linker.Generate(targetFile);
          }
          else if (print) {
            Console.Out.WriteLine(Preprocessor.IncludePath + argList[0] +
                                  ".com is up-to-date.");
          }
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
        BinaryReader dataInputStream =
          new BinaryReader(File.OpenRead(objectFile.FullName));

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

      if (Start.Linux) {
        Preprocessor.MacroMap.Add("__LINUX__",
                                  new Macro(0, new List<Token>()));
      }

      if (Start.Windows) {
        Preprocessor.MacroMap.Add("__WINDOWS__",
                                  new Macro(0, new List<Token>()));
      }

      Preprocessor.IncludeSet = new HashSet<FileInfo>();
      Preprocessor preprocessor = new Preprocessor();
      preprocessor.DoProcess(sourceFile);
      Assert.Error(Preprocessor.IfStack.Count == 0, Message.
                   If___ifdef____or_ifndef_directive_without_matching_endif);
    
      StreamWriter preproStream = File.CreateText(preproFile.FullName);
      preproStream.Write(preprocessor.GetText());
      preproStream.Close();
      DependencySetMap.Add(file.Name, Preprocessor.IncludeSet);

      byte[] byteArray = Encoding.ASCII.GetBytes(preprocessor.GetText());
      MemoryStream memoryStream = new MemoryStream(byteArray);
      CCompiler_Main.Scanner scanner =
        new CCompiler_Main.Scanner(memoryStream);

      try {
        SymbolTable.CurrentTable = new SymbolTable(null, Scope.Global);
        CCompiler_Main.Scanner.Path = sourceFile;
        CCompiler_Main.Scanner.Line = 1;
        CCompiler_Main.Parser parser = new CCompiler_Main.Parser(scanner);
        Assert.Error(parser.Parse(), Message.Syntax_error);
      }
      catch (IOException ioException) {
        Assert.Error(false, ioException.StackTrace, Message.Syntax_error);
      }

      if (Start.Linux) {
        ISet<string> totalGlobalSet = new HashSet<string>(),
                     totalExternSet = new HashSet<string>();
        List<string> totalTextList = new List<string>(),
                     totalDataList = new List<string>();
                     
        foreach (StaticSymbol staticSymbol in SymbolTable.StaticSet) {
          StaticSymbolLinux staticSymbolLinux =
            (StaticSymbolLinux) staticSymbol;
          totalExternSet.UnionWith(staticSymbolLinux.ExternSet);
        }

        StaticSymbolLinux initSymbol = null, argsSymbol = null,
                          mainSymbol = null;
        foreach (StaticSymbol staticSymbol in SymbolTable.StaticSet) {
          if (staticSymbol.UniqueName.
              Equals(AssemblyCodeGenerator.InitializerName)) {
            initSymbol = (StaticSymbolLinux) staticSymbol;
          }

          if (staticSymbol.UniqueName.Equals(AssemblyCodeGenerator.MainName)){
            mainSymbol = (StaticSymbolLinux) staticSymbol;
          }

          if (staticSymbol.UniqueName.Equals(AssemblyCodeGenerator.ArgsName)){
            argsSymbol = (StaticSymbolLinux) staticSymbol;
          }
        }

        if (initSymbol != null) {
          totalTextList.Add("\tglobal _start");
          totalTextList.Add("_start:");
          totalTextList.AddRange(initSymbol.TextList);
          SymbolTable.StaticSet.Remove(initSymbol);
          totalDataList.Add(Linker.StackTopName + ":\ttimes 1048576 db 0");
          totalGlobalSet.Add(Linker.StackTopName);
        }
        else {
          totalExternSet.Add(Linker.StackTopName);
        }

        if (argsSymbol != null) {
          totalTextList.AddRange(argsSymbol.TextList);
          SymbolTable.StaticSet.Remove(argsSymbol);
        }

        if (mainSymbol != null) {
          totalTextList.AddRange(mainSymbol.TextList);
          SymbolTable.StaticSet.Remove(mainSymbol);
          totalExternSet.Remove(mainSymbol.UniqueName);
          totalGlobalSet.Add(mainSymbol.UniqueName);
        }

        foreach (StaticSymbol staticSymbol in SymbolTable.StaticSet) {
          StaticSymbolLinux staticSymbolLinux =
            (StaticSymbolLinux) staticSymbol;
          totalExternSet.Remove(staticSymbolLinux.UniqueName);
          
          if (!staticSymbolLinux.UniqueName.Contains(Symbol.SeparatorId)) {
            totalGlobalSet.Add(staticSymbolLinux.UniqueName);
          }
 
          if (staticSymbolLinux.TextOrDataX ==
              StaticSymbolLinux.TextOrData.Text) {
            totalTextList.AddRange(staticSymbolLinux.TextList);
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

      if (Start.Windows) {
        FileInfo depFile = new FileInfo(file.FullName + ".dep");
        StreamWriter includeWriter =
          new StreamWriter(File.Open(depFile.FullName, FileMode.Create));
        bool first = true;
        foreach (FileInfo includeFile in Preprocessor.IncludeSet) {
          includeWriter.Write((first ? "" : " ") + includeFile.Name);
          first = false;
        }
        includeWriter.Close();

        FileInfo objectFile = new FileInfo(file.FullName + ".obj");
        BinaryWriter binaryWriter =
          new BinaryWriter(File.Open(objectFile.FullName, FileMode.Create));

        binaryWriter.Write(SymbolTable.StaticSet.Count);    
        foreach (StaticSymbol staticSymbol in SymbolTable.StaticSet) {
          staticSymbol.Save(binaryWriter);
        }

        binaryWriter.Close();
      }
    }

    public static bool IsObjectFileUpToDate(FileInfo file) {
      FileInfo sourceFile = new FileInfo(file.FullName + ".c"),
               objectFile = new FileInfo(file.FullName + ".obj");

      if (!objectFile.Exists ||
          (sourceFile.LastWriteTime > objectFile.LastWriteTime)) {
        return false;
      }
      
      FileInfo depFile = new FileInfo(file.FullName + ".dep");
      if (depFile.Exists) {
        try {
          StreamReader depReader =
            new StreamReader(File.OpenRead(depFile.FullName));
          string text = depReader.ReadToEnd();
          depReader.Close();

          if (text.Length > 0) {
            string[] array = text.Split(' ');

            foreach (string name in array)  {
              FileInfo nameFile =
                new FileInfo(Path.Combine(file.Directory.ToString(), name));

              if (!nameFile.Exists ||
                  (nameFile.LastWriteTime > objectFile.LastWriteTime)) {
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
  public partial class Parser :
         QUT.Gppg.ShiftReduceParser<ValueType, QUT.Gppg.LexLocation> {
    public Parser(Scanner scanner)
     :base(scanner) {
      // Empty.
    }
  }
}

namespace CCompiler_Exp {
  public partial class Parser :
         QUT.Gppg.ShiftReduceParser<ValueType, QUT.Gppg.LexLocation> {
    public Parser(Scanner scanner)
     :base(scanner) {
      // Empty.
    }
  }
}

namespace CCompiler_Pre {
  public partial class Parser :
         QUT.Gppg.ShiftReduceParser<ValueType, QUT.Gppg.LexLocation> {
    public Parser(Scanner scanner)
     :base(scanner) {
      // Empty.
    }
  }
}
