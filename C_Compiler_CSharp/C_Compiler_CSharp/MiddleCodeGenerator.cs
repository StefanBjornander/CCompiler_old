using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Collections.Generic;

namespace CCompiler {
  public class MiddleCodeGenerator {
    public static MiddleCode AddMiddleCode(List<MiddleCode> codeList, MiddleOperator op, object operand0 = null,
                                    object operand1 = null, object operand2 = null) {
      MiddleCode middleCode = new MiddleCode(op, operand0, operand1, operand2);
      codeList.Add(middleCode);
      return middleCode;
    }

    public static void Backpatch(ISet<MiddleCode> sourceSet,
                                 List<MiddleCode> list) {
      if (list.Count == 0) {
        AddMiddleCode(list, MiddleOperator.Empty);
      }

      Backpatch(sourceSet, list[0]);
    }

    public static void Backpatch(ISet<MiddleCode> sourceSet,
                                 MiddleCode target) {
      if (sourceSet != null) {
        foreach (MiddleCode source in sourceSet) {
          Assert.ErrorXXX(source[0] == null);
          source[0] = target;
        }
      }
    }

    // ---------------------------------------------------------------------------------------------------------------------

    public static void FunctionHeader(Specifier specifier,
                                      Declarator declarator) {
      Storage? storage;
      Type returnType;

      if (specifier != null) {
        storage = specifier.Storage;
        returnType = specifier.Type;
      }
      else {
        storage = Storage.Extern;
        returnType = Type.SignedIntegerType;
      }

      declarator.Add(returnType);
      Assert.Error(declarator.Name != null,
                   Message.Unnamed_function_definitializerion);
      Assert.Error(declarator.Type.IsFunction(), declarator.Name,
                   Message.Not_a_function);

      SymbolTable.CurrentFunction =
        new Symbol(declarator.Name, specifier.ExternalLinkage, storage.Value, declarator.Type);
      
      Assert.Error(SymbolTable.CurrentFunction.IsExternOrStatic(),
                declarator.Name, Message.A_function_must_be_static_or_extern);

      SymbolTable.CurrentFunction.FunctionDefinition = true;
      SymbolTable.CurrentTable.AddSymbol(SymbolTable.CurrentFunction);

      if (SymbolTable.CurrentFunction.UniqueName.Equals(AssemblyCodeGenerator.MainName)) {
        Assert.Error(returnType.IsVoid() || returnType.IsInteger(), AssemblyCodeGenerator.MainName,
                     Message.Function_main_must_return_void_or_integer);
      }

      SymbolTable.CurrentTable =
        new SymbolTable(SymbolTable.CurrentTable, Scope.Function);
    }

    public static void CheckFunctionDefinition() {
      Type funcType = SymbolTable.CurrentFunction.Type;

      if (funcType.Style == Type.FunctionStyle.Old) {
        List<string> nameList = funcType.NameList;
        IDictionary<string,Symbol> entryMap =
          SymbolTable.CurrentTable.EntryMap;

        Assert.Error(nameList.Count == entryMap.Count,
                     SymbolTable.CurrentFunction.Name, Message. 
          Unmatched_number_of_parameters_in_old__style_function_definitializerion);

        int offset = SymbolTable.FunctionHeaderSize;
        foreach (string name in nameList) {
          Symbol symbol;

          if (!entryMap.TryGetValue(name, out symbol)) {
            Assert.Error(name, Message. 
                      Undefined_parameter_in_old__style_function_definitializerion);
          }

          symbol.Offset = offset;
          offset += symbol.Type.Size();
        }
      }
      else {
        Assert.Error(SymbolTable.CurrentTable.EntryMap.Count == 0,
          Message.New_and_old_style_mixed_function_definitializerion);

        foreach (Symbol symbol in funcType.ParameterList) {
          SymbolTable.CurrentTable.AddSymbol(symbol);
        }
      }

      if (SymbolTable.CurrentFunction.UniqueName.Equals(AssemblyCodeGenerator.MainName)) {
        AssemblyCodeGenerator.InitializationCodeList();
        List<Type> typeList =
          SymbolTable.CurrentFunction.Type.TypeList;

        if ((typeList != null) && (typeList.Count == 2)) {
          Assert.Error(typeList[0].IsInteger() &&
                       typeList[1].IsPointer() &&
                       typeList[1].PointerType.IsPointer() &&
                       typeList[1].PointerType.PointerType.IsChar(),
                       AssemblyCodeGenerator.MainName, Message.Invalid_parameter_list);
          AssemblyCodeGenerator.ArgumentCodeList();
        }
        else {
          Assert.Error((typeList == null) || (typeList.Count == 0),
                       AssemblyCodeGenerator.MainName, Message.Invalid_parameter_list);
        }
      }
    }

    public static void FunctionEnd(Statement statement) {
      MiddleCode nextCode =
        AddMiddleCode(statement.CodeList, MiddleOperator.Empty);
      Backpatch(statement.NextSet, nextCode);

      /*if (SymbolTable.CurrentFunction.Type.ReturnType.IsVoid()) {
        if (SymbolTable.CurrentFunction.UniqueName.Equals(AssemblyCodeGenerator.MainName)) {
          Type signedShortType = new Type(Sort.Signed_Short_Int);
          Symbol zeroSymbol = new Symbol(signedShortType, ((BigInteger) 0));
          AddMiddleCode(statement.CodeList, MiddleOperator.Exit,
                        null, zeroSymbol);
        }
        else {
          AddMiddleCode(statement.CodeList, MiddleOperator.Return);
        }
      }*/

      if (SymbolTable.CurrentFunction.Type.ReturnType.IsVoid()) {
        AddMiddleCode(statement.CodeList, MiddleOperator.Return);
      }

      /*if (SymbolTable.CurrentFunction.Type.ReturnType.IsVoid()) {
        Symbol zeroSymbol = new Symbol(Type.SignedShortIntegerType, BigInteger.Zero);
        AddMiddleCode(statement.CodeList, MiddleOperator.Return,
                      null, zeroSymbol);
      }*/

      AddMiddleCode(statement.CodeList, MiddleOperator.FunctionEnd,
                    SymbolTable.CurrentFunction);

      if (SymbolTable.CurrentFunction.Name.Equals("generateTempName")) {
        string name = @"C:\Users\Stefan\Documents\vagrant\homestead\code\code\" +
                      SymbolTable.CurrentFunction.Name + ".middlebefore";
        StreamWriter streamWriter = new StreamWriter(name);

        for (int index = 0; index < statement.CodeList.Count; ++index) {
          MiddleCode middleCode = statement.CodeList[index];
          streamWriter.WriteLine(index + ": " + middleCode.ToString());
        }

        streamWriter.Close();
      }

      MiddleCodeOptimizer middleCodeOptimizer =
        new MiddleCodeOptimizer(statement.CodeList);
      middleCodeOptimizer.Optimize();

      if (SymbolTable.CurrentFunction.Name.Equals("generateTempName")) {
        string name = @"C:\Users\Stefan\Documents\vagrant\homestead\code\code\" +
                      SymbolTable.CurrentFunction.Name + ".middleafter";
        StreamWriter streamWriter = new StreamWriter(name);

        for (int index = 0; index < statement.CodeList.Count; ++index) {
          MiddleCode middleCode = statement.CodeList[index];
          streamWriter.WriteLine(index + ": " + middleCode.ToString());
        }

        streamWriter.Close();
      }

      List<AssemblyCode> assemblyCodeList = new List<AssemblyCode>();
      AssemblyCodeGenerator.GenerateAssembly(statement.CodeList,
                                             assemblyCodeList);

      if (Start.Linux) {
        List<string> textList = new List<string>();

        if (SymbolTable.CurrentFunction.UniqueName.
            Equals(AssemblyCodeGenerator.MainName)) {
          textList.AddRange(SymbolTable.InitSymbol.TextList);
          if (SymbolTable.ArgsSymbol != null) {
            textList.AddRange(SymbolTable.ArgsSymbol.TextList);
          }
        }
        else {
          textList.Add("section .text");
        }

        ISet<string> externSet = new HashSet<string>();
        AssemblyCodeGenerator.LinuxTextList(assemblyCodeList, textList,
                                            externSet);
        StaticSymbol staticSymbol =
          new StaticSymbolLinux(SymbolTable.CurrentFunction.UniqueName,
                                textList, externSet);
        SymbolTable.StaticSet.Add(staticSymbol);
      }

      if (Start.Windows) {
        List<byte> byteList = new List<byte>();
        IDictionary<int,string> accessMap = new Dictionary<int,string>();
        IDictionary<int,string> callMap = new Dictionary<int,string>();
        ISet<int> returnSet = new HashSet<int>();
        AssemblyCodeGenerator.GenerateTargetWindows
          (assemblyCodeList, byteList, accessMap, callMap, returnSet);
        StaticSymbol staticSymbol =
          new StaticSymbolWindows(SymbolTable.CurrentFunction.UniqueName,
                                  byteList, accessMap, callMap, returnSet);
        SymbolTable.StaticSet.Add(staticSymbol);
      }

      SymbolTable.CurrentTable = SymbolTable.CurrentTable.ParentTable;
      SymbolTable.CurrentFunction = null;
    }

/*    public static bool IsMainArgs(Symbol symbol) {
      List<Type> typeList = symbol.Type.TypeList;
      return (typeList != null) && (typeList.Count == 2) &&
             typeList[0].IsInteger() &&
             typeList[1].IsPointer() &&
             typeList[1].PointerType.IsPointer() &&
             typeList[1].PointerType.PointerType.IsChar();
    }

    public static void FunctionEndX(Statement statement) {
      MiddleCode nextCode =
        AddMiddleCode(statement.CodeList, MiddleOperator.Empty);
      Backpatch(statement.NextSet, nextCode);
    
      if (SymbolTable.CurrentFunction.UniqueName.Equals(AssemblyCodeGenerator.MainName) && 
          SymbolTable.CurrentFunction.Type.ReturnType.IsVoid()) {
        Type signedShortType = new Type(Sort.Signed_Short_Int);
        Symbol zeroSymbol = new Symbol(signedShortType, ((BigInteger) 0));
        AddMiddleCode(statement.CodeList, MiddleOperator.Exit,
                      null, zeroSymbol);
      }
      else {
        AddMiddleCode(statement.CodeList, MiddleOperator.Return);
      }

      AddMiddleCode(statement.CodeList, MiddleOperator.FunctionEnd,
                    SymbolTable.CurrentFunction);

      if (SymbolTable.CurrentFunction.Name.Equals("strftime")) {
        string name = @"C:\Users\Stefan\Documents\vagrant\homestead\code\code\" + SymbolTable.CurrentFunction.Name + ".middlebefore";
        StreamWriter streamWriter = new StreamWriter(name);

        for (int index = 0; index < statement.CodeList.Count; ++index) {
          MiddleCode middleCode = statement.CodeList[index];
          streamWriter.WriteLine(index + ": " + middleCode.ToString());
        }

        streamWriter.Close();
      }

      MiddleCodeOptimizer middleCodeOptimizer =
        new MiddleCodeOptimizer(statement.CodeList);
      middleCodeOptimizer.Optimize();

      if (SymbolTable.CurrentFunction.Name.Equals("strftime")) {
        string name = @"C:\Users\Stefan\Documents\vagrant\homestead\code\code\" + SymbolTable.CurrentFunction.Name + ".middleafter";
        StreamWriter streamWriter = new StreamWriter(name);

        for (int index = 0; index < statement.CodeList.Count; ++index) {
          MiddleCode middleCode = statement.CodeList[index];
          streamWriter.WriteLine(index + ": " + middleCode.ToString());
        }

        streamWriter.Close();
      }

      List<AssemblyCode> assemblyCodeList = new List<AssemblyCode>();
    
      if (SymbolTable.CurrentFunction.UniqueName.Equals(AssemblyCodeGenerator.MainName)) {
        List<Type> typeList =
          SymbolTable.CurrentFunction.Type.TypeList;
        Assert.Error((typeList == null) || (typeList.Count == 0) ||
                     IsMainArgs(SymbolTable.CurrentFunction),
                     AssemblyCodeGenerator.MainName, Message.Invalid_parameter_list);

        AssemblyCodeGenerator.InitializationCodeList();
        //assemblyCodeList.AddRange(AssemblyCodeGenerator.InitializationCodeList());

        if (IsMainArgs(SymbolTable.CurrentFunction)) {
          AssemblyCodeGenerator.ArgumentCodeList();
//          assemblyCodeList.AddRange(AssemblyCodeGenerator.ArgumentCodeList());

          /*SymbolTable.CurrentStaticFunction.EntryPoint =
            AssemblyCode.MainInitializationSize +
            AssemblyCode.MainArgumentSize;*
        }      
        else {
          /*SymbolTable.CurrentStaticFunction.EntryPoint =
            AssemblyCode.MainInitializationSize;*
        }
      }

      AssemblyCodeGenerator.GenerateAssembly(statement.CodeList, assemblyCodeList);

      if (Start.W) {
        List<byte> byteList = new List<byte>();
        IDictionary<int,string> accessMap = new Dictionary<int,string>();
        IDictionary<int,string> callMap = new Dictionary<int,string>();
        ISet<int> returnSet = new HashSet<int>();
        AssemblyCodeGenerator.GenerateTargetWindows(assemblyCodeList, byteList, accessMap, callMap, returnSet);
        StaticSymbol staticSymbol = new StaticSymbolWindows(SymbolTable.CurrentFunction.UniqueName,
                                                     byteList, accessMap, callMap, returnSet);
        SymbolTable.StaticSet.Add(staticSymbol);
      }
      
      if (Start.L) {
        List<string> textList = new List<string>();
        ISet<string> externSet = new HashSet<string>();
        GenerateStaticInitializerLinux.TextList(assemblyCodeList, textList, externSet);
        StaticSymbol staticSymbol = new StaticSymbolLinux(StaticSymbolLinux.TextOrData.Text, SymbolTable.CurrentFunction.UniqueName, textList, externSet);
        SymbolTable.StaticSet.Add(staticSymbol);
      }

      SymbolTable.CurrentTable = SymbolTable.CurrentTable.ParentTable;
      SymbolTable.CurrentFunction = null;
    }*/

    // ---------------------------------------------------------------------------------------------------------------------

    public static void StructUnionHeader(string optionalName, Sort sort) {
      if (optionalName != null) {
        Type type = new Type(sort);
        SymbolTable.CurrentTable.AddTag(optionalName, type);
      }
    }
  
    public static Type StructUnionSpecifier(string optionalName, Sort sort) {
      if (optionalName != null) {
        Type type = SymbolTable.CurrentTable.LookupTag(optionalName, sort);
        type.MemberMap = SymbolTable.CurrentTable.EntryMap;
        return type;
      }
      else {
        return (new Type(sort, SymbolTable.CurrentTable.EntryMap));
      }
    }

    public static Type LookupStructUnionSpecifier(string name,
                                                  Sort sort) {
      Type type = SymbolTable.CurrentTable.LookupTag(name, sort);

      if (type != null) {
        return type;
      }
      else {
        type = new Type(sort);
        SymbolTable.CurrentTable.AddTag(name, type);
        return type;
      }
    }

    // ---------------------------------------------------------------------------------------------------------------------
  
    public static Symbol EnumItem(string itemName,
                                  Symbol optInitializerSymbol) {
      Type itemType = new Type(Sort.SignedInt, true);      
      itemType.Constant = true;

      BigInteger value;
      if (optInitializerSymbol != null) {
        Assert.Error(optInitializerSymbol.Type.IsIntegral(), itemName,
                     Message.Non__integral_enum_value);
        Assert.Error(optInitializerSymbol.Value != null, itemName,
                     Message.Non__constant_enum_value);
        CCompiler_Main.Parser.EnumValueStack.Pop();
        value = (BigInteger) optInitializerSymbol.Value;
      }
      else {
        value = CCompiler_Main.Parser.EnumValueStack.Pop();
      }
    
      // enum {a,b,c} extern;
      Symbol itemSymbol = new Symbol(itemName, false, Storage.Auto, itemType, false, value);
      SymbolTable.CurrentTable.AddSymbol(itemSymbol);
      CCompiler_Main.Parser.EnumValueStack.Push(value + 1);
      return itemSymbol;
    }
  
    public static Type EnumSpecifier(string optionalName,
                                     ISet<Pair<Symbol,bool>> enumSet) {
      Type enumType = new Type(Sort.SignedInt, enumSet);

      if (optionalName != null) {
        SymbolTable.CurrentTable.AddTag(optionalName, enumType);
      }

      return enumType;
    }

    public static Type LookupEnum(string name) {
      Type type = SymbolTable.CurrentTable.LookupTag(name, Sort.SignedInt);
      Assert.Error(type != null, name, Message.Tag_not_found); 
      return type;
    }

    // ---------------------------------------------------------------------------------------------------------------------

    public static void Declarator(Specifier specifier, Declarator declarator){
      declarator.Add(specifier.Type);

      Storage storage = specifier.Storage;
      if (declarator.Type.IsFunction()) {
        Assert.Error((specifier.Storage == Storage.Static) ||
                     (specifier.Storage == Storage.Extern),
                     specifier.Storage, Message.
          Only_extern_or_static_storage_allowed_for_functions);
      }

      Symbol symbol = new Symbol(declarator.Name, specifier.ExternalLinkage,
                                 storage, declarator.Type);
      SymbolTable.CurrentTable.AddSymbol(symbol);

      if (symbol.IsStatic() && !symbol.Type.IsFunction()) {
        SymbolTable.StaticSet.Add(ConstantExpression.Value(symbol));
      }
    }
  
    public static List<MiddleCode> InitializedDeclarator(Specifier specifier,
                                   Declarator declarator, object initializer){
      declarator.Add(specifier.Type);
      Type type = declarator.Type;
      Storage storage = specifier.Storage;
      string name = declarator.Name;

      Assert.Error(!type.IsFunction(), null,
                   Message.Functions_cannot_be_initialized);
      Assert.Error(storage != Storage.Typedef, name,
                   Message.Typedef_cannot_be_initialized);
      Assert.Error(storage != Storage.Extern, name,
                   Message.Extern_cannot_be_initialized);
      Assert.Error((SymbolTable.CurrentTable.Scope != Scope.Struct) &&
                   (SymbolTable.CurrentTable.Scope != Scope.Union),
                   name, Message.Struct_or_union_field_cannot_be_initialized);

      if (storage == Storage.Static) {
        List<MiddleCode> middleCodeList =
          GenerateStaticInitializer.GenerateStatic(type, initializer);
        Symbol symbol = new Symbol(name, specifier.ExternalLinkage,
                                   storage, type);
        SymbolTable.CurrentTable.AddSymbol(symbol);

        StaticSymbol staticSymbol =
          ConstantExpression.Value(symbol.UniqueName, type, middleCodeList);
        SymbolTable.StaticSet.Add(staticSymbol);
        
        return (new List<MiddleCode>());
      }
      else {
        Symbol symbol =
          new Symbol(name, specifier.ExternalLinkage, storage, type);
        symbol.Offset = SymbolTable.CurrentTable.CurrentOffset;
        List<MiddleCode> codeList =
          GenerateAutoInitializer.GenerateAuto(symbol, initializer);
        SymbolTable.CurrentTable.AddSymbol(symbol);
        return codeList;
      }
    }

    public static void BitfieldDeclarator(Specifier specifier,
                                   Declarator declarator, Symbol bitsSymbol) {
      Storage storage = specifier.Storage;
      //Type specifierType = ;

      Assert.Error(SymbolTable.CurrentTable.Scope == Scope.Struct,
                   bitsSymbol, Message.Bitfields_only_allowed_on_structs);

      Assert.Error((storage == Storage.Auto) || (storage == Storage.Register),
                   null, Message.
                   Only_auto_or_register_storage_allowed_in_struct_or_union);

      object bitsValue = bitsSymbol.Value;
      int bits = int.Parse(bitsValue.ToString());

      if (declarator != null) {
        declarator.Add(specifier.Type);
        Type type = declarator.Type;
        Assert.Error(type.IsIntegral(), type,
                     Message.Non__integral_bits_expression);
        Assert.Error((bits >= 1) && (bits <= (8 * type.Size())),
                      bitsValue, Message.Bits_value_out_of_range);
      
        if (bits < (8 * type.Size())) {
          type.SetBitfieldMask(bits);
        }

        Symbol symbol = new Symbol(declarator.Name, specifier.ExternalLinkage,
                                   storage, type);
        SymbolTable.CurrentTable.AddSymbol(symbol);

        if (symbol.IsStatic()) {
          SymbolTable.StaticSet.Add(ConstantExpression.Value(symbol));
        }
      }
      else {
        Assert.Error((bits >= 1) && (bits <= (8 * 4)), bitsValue,
                      Message.Bits_value_out_of_range);
      }
    }

    // ---------------------------------------------------------------------------------------------------------------------

    public static Declarator PointerDeclarator(List<Type> typeList,
                                               Declarator declarator) {
      if (declarator == null) {
        declarator = new Declarator(null);
      }
    
      foreach (Type type in typeList) {
        Type pointerType = new Type((Type) null);
        pointerType.Constant = type.Constant;
        pointerType.Volatile = type.Volatile;
        declarator.Add(pointerType);
      }

      return declarator;
    }

    /*public static Declarator PointerListDeclarator
                  (List<Pair<bool,bool>> pointerList, Declarator declarator) {
      foreach (Pair<bool,bool> pair in pointerList) {
        Type pointerType = new Type((Type) null);
        bool isConstant = pair.First, isVolatile = pair.Second;
        pointerType.Constant = isConstant;
        pointerType.Volatile = isVolatile;
        declarator.Add(pointerType);
      }
    
      return declarator;
    }*/

    // ---------------------------------------------------------------------------------------------------------------------
  
    public static Declarator ArrayType(Declarator declarator,
                                       Expression optionalSizeExpression) {
      if (declarator == null) {
        declarator = new Declarator(null);
      }

      Type arrayType;
      if (optionalSizeExpression != null) {
        Symbol optSizeSymbol = optionalSizeExpression.Symbol;
        int arraySize = (int) ((BigInteger) optSizeSymbol.Value);
        Assert.Error(arraySize > 0, arraySize,
                     Message.Non__positive_array_size);
        arrayType = new Type(arraySize, null);
      }
      else {
        arrayType = new Type(0, null);
      }
    
      declarator.Add(arrayType);
      return declarator;
    }

    // ---------------------------------------------------------------------------------------------------------------------

    public static Declarator NewFunctionDeclaration(Declarator declarator,
                                                   List<Symbol> parameterList,
                                                   bool ellipse) {
      if (parameterList.Count == 0) {
        Assert.Error(!ellipse, "...",
            Message.An_elliptic_function_must_have_at_least_one_parameter);
      }
      else if ((parameterList.Count == 1) && parameterList[0].Type.IsVoid()) {
        Assert.Error(parameterList[0].Name == null,
                     parameterList[0].Name,
                     Message.A_void_parameter_cannot_be_named);
        Assert.Error(!ellipse, "...", Message.
                     An_elliptic_function_cannot_have_a_void_parameter);
        parameterList.Clear();
      }
      else {
        foreach (Symbol symbol in parameterList) {
          Assert.Error(!symbol.Type.IsVoid(),
                       Message.Invalid_void_parameter);
        }
      }

      declarator.Add(new Type(null, parameterList, ellipse));
      return declarator;
    }

    public static Declarator OldFunctionDeclaration(Declarator declarator,
                                                    List<string> nameList) {
      ISet<string> nameSet = new HashSet<string>();

      foreach (string name in nameList) {
        Assert.Error(nameSet.Add(name), name, Message.Name_already_defined);
      }

      declarator.Add(new Type(null, nameList));
      return declarator;
    }  

    public static Symbol Parameter(Specifier specifier,
                                   Declarator declarator) {
      Storage storage = specifier.Storage;
      Type specifierType = specifier.Type;

      /*Assert.Error((storage == Storage.Auto) || (storage == Storage.Register),
                   Message.Parameters_must_have_auto_or_register_storage);*/

      string name;
      Type type;
          
      if (declarator != null) {
        name = declarator.Name;
        declarator.Add(specifierType);
        type = declarator.Type;
      }
      else {
        name = null;
        type = specifierType;
      }

      if (type.IsArray()) {
        type = new Type(type.ArrayType);
        type.Constant = true;
      }
      else if (type.IsFunction()) {
        type = new Type(type);
        type.Constant = true;
      }

      return (new Symbol(name, false, storage, type, true));
    }

    // ---------------------------------------------------------------------------------------------------------------------
 
    public static Statement IfStatement(Expression expression,
                                        Statement innerStatement) {
      expression = TypeCast.ToLogical(expression);
      List<MiddleCode> codeList = expression.LongList;
      AddMiddleCode(codeList, MiddleOperator.CheckTrackMapFloatStack);

      Backpatch(expression.Symbol.TrueSet, innerStatement.CodeList);    
      codeList.AddRange(innerStatement.CodeList);
      MiddleCode nextCode = AddMiddleCode(codeList, MiddleOperator.Goto); // XXX
      
      ISet<MiddleCode> nextSet = new HashSet<MiddleCode>();
      nextSet.UnionWith(innerStatement.NextSet);
      nextSet.UnionWith(expression.Symbol.FalseSet);
      nextSet.Add(nextCode);
    
      return (new Statement(codeList, nextSet));
    }
  
    public static Statement IfElseStatement(Expression expression,
                                            Statement trueStatement,
                                            Statement falseStatement) {
      expression = TypeCast.ToLogical(expression);
      List<MiddleCode> codeList = expression.LongList;
      AddMiddleCode(codeList, MiddleOperator.CheckTrackMapFloatStack);

      Backpatch(expression.Symbol.TrueSet, trueStatement.CodeList);
      Backpatch(expression.Symbol.FalseSet, falseStatement.CodeList);

      codeList.AddRange(trueStatement.CodeList);
      MiddleCode gotoCode = AddMiddleCode(codeList, MiddleOperator.Goto);
      codeList.AddRange(falseStatement.CodeList);

      ISet<MiddleCode> nextSet = new HashSet<MiddleCode>();
      nextSet.UnionWith(trueStatement.NextSet);
      nextSet.UnionWith(falseStatement.NextSet);
      nextSet.Add(gotoCode);
    
      return (new Statement(codeList, nextSet));
    }

    private static Stack<IDictionary<BigInteger, MiddleCode>> m_caseMapStack =
      new Stack<IDictionary<BigInteger, MiddleCode>>();
    private static Stack<MiddleCode> m_defaultStack = new Stack<MiddleCode>();
    private static Stack<ISet<MiddleCode>> m_breakSetStack =
      new Stack<ISet<MiddleCode>>();

    public static void SwitchHeader() {
      m_caseMapStack.Push(new Dictionary<BigInteger,MiddleCode>());
      m_defaultStack.Push(null);
      m_breakSetStack.Push(new HashSet<MiddleCode>());
    }

    public static Statement SwitchStatement(Expression switchExpression,
                                            Statement statement) {
      switchExpression = TypeCast.LogicalToIntegral(switchExpression);
      List<MiddleCode> codeList = switchExpression.LongList;

      Type switchType = switchExpression.Symbol.Type;
      foreach (KeyValuePair<BigInteger,MiddleCode> entry
               in m_caseMapStack.Pop()) {
        BigInteger caseValue = entry.Key;
        MiddleCode caseTarget = entry.Value;
        Symbol caseSymbol = new Symbol(switchType, caseValue);
        AddMiddleCode(codeList, MiddleOperator.Case, caseTarget,
                      switchExpression.Symbol, caseSymbol);
      }
      AddMiddleCode(codeList, MiddleOperator.CaseEnd,
                    switchExpression.Symbol);
    
      ISet<MiddleCode> nextSet = new HashSet<MiddleCode>();
      MiddleCode defaultCode = m_defaultStack.Pop();

      if (defaultCode != null) {
        AddMiddleCode(codeList, MiddleOperator.Goto, defaultCode);
      }
      else {
        nextSet.Add(AddMiddleCode(codeList, MiddleOperator.Goto));      
      }

      codeList.AddRange(statement.CodeList);
      nextSet.UnionWith(statement.NextSet);
      nextSet.UnionWith(m_breakSetStack.Pop());
      return (new Statement(codeList, nextSet));
    }

    public static Statement CaseStatement(Expression expression,
                                          Statement statement) {
      Assert.Error(m_caseMapStack.Count > 0, Message.Case_without_switch);
      expression = TypeCast.LogicalToIntegral(expression);
      Assert.Error(expression.Symbol.Value != null, expression.Symbol.Name,
                   Message.Non__constant_case_value);
      BigInteger caseValue = (BigInteger) expression.Symbol.Value;
      IDictionary<BigInteger, MiddleCode> caseMap = m_caseMapStack.Peek();
      Assert.Error(!caseMap.ContainsKey(caseValue), caseValue,
                   Message.Repeated_case_value);
      caseMap.Add(caseValue, GetFirst(statement.CodeList));
      return statement;
    }
  
    private static MiddleCode GetFirst(List<MiddleCode> list) {
      if (list.Count == 0) {
        AddMiddleCode(list, MiddleOperator.Empty);
      }
    
      return list[0];
    }
  
    public static Statement DefaultStatement(Statement statement) {
      Assert.Error(m_defaultStack.Count > 0, Message.Default_without_switch);
      Assert.Error(m_defaultStack.Pop() == null, Message.Repeted_default);
      m_defaultStack.Push(GetFirst(statement.CodeList));
      return statement;
    }

    public static Statement BreakStatement() {
      Assert.Error(m_breakSetStack.Count > 0, Message.Break_without_switch____while____do____or____for);
      List<MiddleCode> codeList = new List<MiddleCode>();
      MiddleCode breakCode = AddMiddleCode(codeList, MiddleOperator.Goto);
      m_breakSetStack.Peek().Add(breakCode);
      return (new Statement(codeList));
    }

    private static Stack<ISet<MiddleCode>> m_continueSetStack =
      new Stack<ISet<MiddleCode>>();
  
    public static Statement ContinueStatement() {
      Assert.Error(m_continueSetStack.Count > 0, Message.Continue_without_while____do____or____for);
      List<MiddleCode> codeList = new List<MiddleCode>();
      MiddleCode continueCode = AddMiddleCode(codeList, MiddleOperator.Goto);
      m_continueSetStack.Peek().Add(continueCode);
      return (new Statement(codeList));
    }
  
    public static void LoopHeader() {
      m_breakSetStack.Push(new HashSet<MiddleCode>());
      m_continueSetStack.Push(new HashSet<MiddleCode>());    
    }
  
    public static Statement WhileStatement(Expression expression,
                                           Statement statement) {
      expression = TypeCast.ToLogical(expression);
      List<MiddleCode> codeList = expression.LongList;
      AddMiddleCode(codeList, MiddleOperator.CheckTrackMapFloatStack);

      Backpatch(expression.Symbol.TrueSet, statement.CodeList);
      codeList.AddRange(statement.CodeList);

      MiddleCode nextCode = AddMiddleCode(codeList, MiddleOperator.Goto,
                                          GetFirst(codeList));
    
      ISet<MiddleCode> nextSet = new HashSet<MiddleCode>();
      nextSet.UnionWith(expression.Symbol.FalseSet);
      nextSet.UnionWith(m_breakSetStack.Pop());
      Backpatch(statement.NextSet, codeList);
      Backpatch(m_continueSetStack.Pop(), codeList);
      return (new Statement(codeList, nextSet));
    }
  
    public static Statement DoStatement(Statement innerStatement,
                                        Expression expression) {
      List<MiddleCode> codeList = innerStatement.CodeList;
      Backpatch(innerStatement.NextSet, codeList);
      AddMiddleCode(codeList, MiddleOperator.CheckTrackMapFloatStack);
      codeList.AddRange(expression.LongList);

      Backpatch(expression.Symbol.TrueSet, codeList);
      Backpatch(m_continueSetStack.Pop(), codeList);    
      ISet<MiddleCode> nextSet = new HashSet<MiddleCode>();
      nextSet.UnionWith(expression.Symbol.FalseSet);
      nextSet.UnionWith(m_breakSetStack.Pop());

      AddMiddleCode(codeList, MiddleOperator.Goto, GetFirst(codeList));
      return (new Statement(codeList, nextSet));
    }
  
    public static Statement ForStatement(Expression initializerExpression, Expression testExpression,
                                         Expression nextExpression, Statement innerStatement) {
      List<MiddleCode> codeList = new List<MiddleCode>();
      ISet<MiddleCode> nextSet = new HashSet<MiddleCode>();
    
      if (initializerExpression != null) {
        codeList.AddRange(initializerExpression.ShortList);
      }

      MiddleCode testTarget = AddMiddleCode(codeList, MiddleOperator.Empty);
    
      if (testExpression != null) {
        testExpression = TypeCast.ToLogical(testExpression);
        codeList.AddRange(testExpression.LongList);
        AddMiddleCode(codeList, MiddleOperator.CheckTrackMapFloatStack);
        Backpatch(testExpression.Symbol.TrueSet, innerStatement.CodeList);
        nextSet.UnionWith(testExpression.Symbol.FalseSet);
      }

      codeList.AddRange(innerStatement.CodeList);
      MiddleCode nextTarget = AddMiddleCode(codeList, MiddleOperator.Empty);
      Backpatch(innerStatement.NextSet, nextTarget);
    
      if (nextExpression != null) {
        codeList.AddRange(nextExpression.ShortList);
      }

      AddMiddleCode(codeList, MiddleOperator.Goto, testTarget);
      Backpatch(m_continueSetStack.Pop(), nextTarget);
      nextSet.UnionWith(m_breakSetStack.Pop());
    
      return (new Statement(codeList, nextSet));
    }

    public static IDictionary<string, MiddleCode> m_labelMap =
      new Dictionary<string, MiddleCode>();
    public static IDictionary<string, ISet<MiddleCode>> m_gotoSetMap =
      new Dictionary<string, ISet<MiddleCode>>();

    public static Statement LabelStatement(string labelName,
                                           Statement statement) {
      Assert.Error(!m_labelMap.ContainsKey(labelName),
                   labelName, Message.Defined_twice);
      m_labelMap.Add(labelName, GetFirst(statement.CodeList));
      return statement;
    }

    public static Statement GotoStatement(string labelName) {
      List<MiddleCode> gotoList = new List<MiddleCode>();
      MiddleCode gotoCode = AddMiddleCode(gotoList, MiddleOperator.Goto);

      if (m_gotoSetMap.ContainsKey(labelName)) {
        ISet<MiddleCode> gotoSet = m_gotoSetMap[labelName];
        gotoSet.Add(gotoCode);
      }
      else {
        ISet<MiddleCode> gotoSet = new HashSet<MiddleCode>();
        gotoSet.Add(gotoCode);
        m_gotoSetMap.Add(labelName, gotoSet);
      }

      return (new Statement(gotoList));
    }

    public static void BackpatchGoto() {
      foreach (KeyValuePair<string,ISet<MiddleCode>> entry in m_gotoSetMap) {
        string labelName = entry.Key;
        ISet<MiddleCode> gotoSet = entry.Value;

        MiddleCode labelCode;
        Assert.Error(m_labelMap.TryGetValue(labelName, out labelCode),
                     labelName, Message.Missing_goto_address);
        Backpatch(gotoSet, labelCode);
      }
    }  

    public static Statement ReturnStatement(Expression expression) {
      List<MiddleCode> codeList;

      if (expression != null) {
        Assert.Error(!SymbolTable.CurrentFunction.Type.ReturnType.IsVoid(),
                     Message.Non__void_return_from_void_function);

        expression = TypeCast.ImplicitCast(expression,
                              SymbolTable.CurrentFunction.Type.ReturnType);
        codeList = expression.LongList;
        AddMiddleCode(codeList, MiddleOperator.Return,
                      null, expression.Symbol);
      }
      else {
        Assert.Error(SymbolTable.CurrentFunction.Type.ReturnType.IsVoid(),
                     Message.Void_returned_from_non__void_function);
        codeList = new List<MiddleCode>();
        AddMiddleCode(codeList, MiddleOperator.Return);
      }

      return (new Statement(codeList));
    }

    public static Statement ExpressionStatement(Expression expression) {
      List<MiddleCode> codeList = new List<MiddleCode>();

      if (expression != null) {
        codeList.AddRange(expression.ShortList);
      }
    
      return (new Statement(codeList));
    }

/*    public static Statement LoadToRegisterStatement(Register register, Expression expression) {
      List<MiddleCode> codeList = new List<MiddleCode>();
      codeList.AddRange(expression.LongList);
      //Register register = (Register) Enum.Parse(typeof(Register), name);
      AddMiddleCode(codeList, MiddleOperator.AssignRegister, register, expression.Symbol);
      return (new Statement(codeList));
    }

    public static Statement SaveFromRegisterStatement(string name, Expression expression) {
      List<MiddleCode> codeList = new List<MiddleCode>();
      Register register = (Register) Enum.Parse(typeof(Register), name);
      AddMiddleCode(codeList, MiddleOperator.SaveFromRegister, expression.Symbol, register);
      return (new Statement(codeList));
    }
  
    public static Statement LoadFlagbyteStatement(Expression expression) {
      List<MiddleCode> codeList = new List<MiddleCode>();
      AddMiddleCode(codeList, MiddleOperator.Flagbyte, expression.Symbol);
      return (new Statement(codeList));
    }*/
  
    public static Statement JumpRegisterStatement(Register register) {
      List<MiddleCode> codeList = new List<MiddleCode>();
      //Register register = (Register) Enum.Parse(typeof(Register), registerName);
      AddMiddleCode(codeList, MiddleOperator.JumpRegister, register);
      return (new Statement(codeList));
    }

    /*public static Statement JumpRegisterStatement(String registerName) {
      try {
        Register register = (Register) Enum.Parse(typeof(Register), registerName);
        List<MiddleCode> codeList = new List<MiddleCode>();
        AddMiddleCode(codeList, MiddleOperator.JumpRegister, register);
        return (new Statement(codeList));
      }
      catch (Exception) {
        Assert.Error(registerName, "invalid register name");
        return null;
      }
    }*/

    public static Statement InterruptStatement(Expression expression) {
      List<MiddleCode> codeList = new List<MiddleCode>();
      AddMiddleCode(codeList, MiddleOperator.Interrupt, expression.Symbol.Value);
      return (new Statement(codeList));
    }

    public static Statement SyscallStatement() {
      List<MiddleCode> codeList = new List<MiddleCode>();
      AddMiddleCode(codeList, MiddleOperator.SysCall);
      return (new Statement(codeList));
    }

    // ---------------------------------------------------------------------------------------------------------------------

    public static Stack<List<Type>> TypeListStack = new Stack<List<Type>>();
    public static Stack<int> ParameterOffsetStack = new Stack<int>();

    public static Expression CommaExpression(Expression leftExpression,
                                             Expression rightExpression) {
      List<MiddleCode> shortList = new List<MiddleCode>();
      shortList.AddRange(leftExpression.ShortList);
      shortList.AddRange(rightExpression.ShortList);
    
      List<MiddleCode> longList = new List<MiddleCode>();
      longList.AddRange(leftExpression.ShortList); // Obs: shortList
      longList.AddRange(rightExpression.LongList);
        
      return (new Expression(rightExpression.Symbol, shortList, longList));
    }

    /*public static Expression RegisterAssignmentExpression(Register register, Expression expression) {
      Symbol symbol = expression.Symbol;
      Assert.Error(ObjectCode.RegisterSize(register) == symbol.Type.Size());
      List<MiddleCode> codeList = new List<MiddleCode>();
      codeList.AddRange(expression.LongList);
      AddMiddleCode(codeList, MiddleOperator.LoadToRegister, register, symbol);
      return (new Expression(symbol, codeList, codeList));
    }

    public static Expression RegisterExpression(Register register, Expression expression) {
      Assert.Error(AssemblyCode.SizeOfRegister(register) == expression.Symbol.Type.Size(), Message.Unmatched_register_size);
      List<MiddleCode> codeList = new List<MiddleCode>();
      codeList.AddRange(expression.LongList);
      AddMiddleCode(codeList, MiddleOperator.AssignRegister, register, expression.Symbol);
      return (new Expression(expression.Symbol, codeList, codeList));
    }*/

    public static Expression AssignmentExpression(MiddleOperator middleOp,
                                                  Expression leftExpression,
                                                  Expression rightExpression){
/*      if (leftExpression.Symbol.Temporary) {
        int i = 1;
      }

      if (!leftExpression.Symbol.Temporary) {
        Assert.Error(leftExpression.Symbol.Assignable,
                     leftExpression.Symbol.Name, Message.Not_assignable);
      }*/

      switch (middleOp) {
        case MiddleOperator.Assign:
          return Assignment(leftExpression, rightExpression, true);

        case MiddleOperator.BinaryAdd:
        case MiddleOperator.BinarySubtract:
          return Assignment(leftExpression,
                            AdditionExpression(middleOp, leftExpression,
                                               rightExpression));

        case MiddleOperator.SignedMultiply:
        case MiddleOperator.SignedDivide:
        case MiddleOperator.SignedModulo:
          return Assignment(leftExpression,
                            MultiplyExpression(middleOp, leftExpression, 
                                                         rightExpression));

        case MiddleOperator.BitwiseAnd:
        case MiddleOperator.BitwiseOr:
        case MiddleOperator.BitwiseXOr:
          return Assignment(leftExpression,
                            BitwiseExpression(middleOp, leftExpression,
                                              rightExpression));

        default: // shift left, shift right
          return Assignment(leftExpression,
                            ShiftExpression(middleOp, leftExpression,
                                            rightExpression));
      }
    }

    public static Expression Assignment(Expression leftExpression,
                                        Expression rightExpression,
                                        bool simpleAssignment = false) {
      Register? register = leftExpression.Register;

      if (register != null) {
        Symbol rightSymbol = rightExpression.Symbol;
        Assert.Error(AssemblyCode.SizeOfRegister(register.Value) ==
                     rightExpression.Symbol.Type.Size(),
                     Message.Unmatched_register_size);
        List<MiddleCode> longList = new List<MiddleCode>();
        longList.AddRange(rightExpression.LongList);
        AddMiddleCode(longList, MiddleOperator.AssignRegister,
                      register, rightExpression.Symbol);
        return (new Expression(rightExpression.Symbol, longList, longList));
      }
      else {
        Assert.Error(leftExpression.Symbol.IsAssignable(),
                     leftExpression, Message.Not_assignable);
        List<MiddleCode> longList = new List<MiddleCode>();

        if (simpleAssignment) {
          longList.AddRange(leftExpression.LongList);
  
          if (leftExpression.Symbol.Type.IsFloating()) {
            AddMiddleCode(longList, MiddleOperator.PopFloat);
          }
        }

        rightExpression = TypeCast.ImplicitCast(rightExpression,
                                                leftExpression.Symbol.Type);
        longList.AddRange(rightExpression.LongList);

        if (leftExpression.Symbol.Type.IsFloating()) {
          AddMiddleCode(longList, MiddleOperator.TopFloat,
                        leftExpression.Symbol);
          List<MiddleCode> shortList = new List<MiddleCode>();
          shortList.AddRange(longList);
          AddMiddleCode(shortList, MiddleOperator.PopFloat);
          return (new Expression(leftExpression.Symbol, shortList, longList));
        }
        else {
          AddMiddleCode(longList, MiddleOperator.Assign,
                        leftExpression.Symbol, rightExpression.Symbol);
          BigInteger? bitFieldMask =
                     leftExpression.Symbol.Type.GetBitfieldMask();

          if (bitFieldMask != null) {
            Symbol maskSymbol = new Symbol(leftExpression.Symbol.Type,
                                           bitFieldMask);
            AddMiddleCode(longList, MiddleOperator.BitwiseAnd,
                          leftExpression.Symbol, leftExpression.Symbol,
                          maskSymbol);
          }

          return (new Expression(leftExpression.Symbol, longList, longList));
        }
      }
    }

    private static bool IsEmpty(List<MiddleCode> codeList) {
      foreach (MiddleCode middleCode in codeList) {
        if (middleCode.Operator != MiddleOperator.Empty) {
          return false;
        }
      }
    
      return true;
    }

    public static Expression ConditionalExpression(Expression testExpression,
                                                   Expression trueExpression,
                                                   Expression falseExpression) {
      testExpression = TypeCast.ToLogical(testExpression);
      if (ConstantExpression.IsConstant(testExpression)) {
        return ConstantExpression.IsTrue(testExpression)
               ? falseExpression : trueExpression;
      }

      if (trueExpression.Symbol.Type.IsLogical() &&
          falseExpression.Symbol.Type.IsLogical()) {
        Backpatch(testExpression.Symbol.TrueSet, trueExpression.LongList);
        Backpatch(testExpression.Symbol.FalseSet, falseExpression.LongList);

        ISet<MiddleCode> trueSet = new HashSet<MiddleCode>(),
                         falseSet = new HashSet<MiddleCode>();
        trueSet.UnionWith(trueExpression.Symbol.TrueSet);
        trueSet.UnionWith(falseExpression.Symbol.TrueSet);
        falseSet.UnionWith(trueExpression.Symbol.FalseSet);
        falseSet.UnionWith(falseExpression.Symbol.FalseSet);

        List<MiddleCode> shortList = new List<MiddleCode>();
       
        if (IsEmpty(trueExpression.ShortList) &&
            IsEmpty(falseExpression.ShortList)) {
          shortList.AddRange(testExpression.ShortList);
        }
        else {
          shortList.AddRange(testExpression.LongList); // Obs: LongList
          shortList.AddRange(trueExpression.ShortList);
          shortList.AddRange(falseExpression.ShortList);
        }

        List<MiddleCode> longList = new List<MiddleCode>();
        longList.AddRange(testExpression.LongList);
        longList.AddRange(trueExpression.LongList);
        longList.AddRange(falseExpression.LongList);

        Symbol symbol = new Symbol(trueSet, falseSet);
        return (new Expression(symbol, shortList, longList));
      }
      else {
        Type maxType = TypeCast.MaxType(trueExpression.Symbol.Type,
                                        falseExpression.Symbol.Type);
        trueExpression = TypeCast.ImplicitCast(trueExpression, maxType);
        Backpatch(testExpression.Symbol.TrueSet, trueExpression.LongList);

        Symbol symbol = new Symbol(maxType);
        if (maxType.IsFloating()) {
          AddMiddleCode(trueExpression.LongList,
                        MiddleOperator.DecreaseStack);
        }
        else {
          AddMiddleCode(trueExpression.LongList, MiddleOperator.Assign,
                        symbol, trueExpression.Symbol);
        }

        MiddleCode targetCode = new MiddleCode(MiddleOperator.Empty);
        AddMiddleCode(trueExpression.ShortList,
                      MiddleOperator.Goto, targetCode);
        AddMiddleCode(trueExpression.LongList,
                      MiddleOperator.Goto, targetCode);

        falseExpression = TypeCast.ImplicitCast(falseExpression, maxType);
        Backpatch(testExpression.Symbol.FalseSet, falseExpression.LongList);
        
        if (!maxType.IsFloating()) {
          AddMiddleCode(falseExpression.LongList, MiddleOperator.Assign,
                        symbol, falseExpression.Symbol);
        }

        List<MiddleCode> shortList = new List<MiddleCode>();
        if (IsEmpty(trueExpression.ShortList) &&
            IsEmpty(falseExpression.ShortList)) {
          shortList.AddRange(testExpression.ShortList); // Obs: ShortList
        }
        else {
          shortList.AddRange(testExpression.LongList); // Obs: LongList
          shortList.AddRange(trueExpression.ShortList);
          shortList.AddRange(falseExpression.ShortList);
          shortList.Add(targetCode);
        }

        List<MiddleCode> longList = new List<MiddleCode>();
        longList.AddRange(testExpression.LongList);
        longList.AddRange(trueExpression.LongList);
        longList.AddRange(falseExpression.LongList);
        longList.Add(targetCode);

        return (new Expression(symbol, shortList, longList));
      }
    }

    public static Expression ConstantIntegralExpression(Expression expression) {
      expression = ConstantExpression.ConstantCast(expression, Type.SignedLongIntegerType);
      Assert.Error(expression != null, expression, Message.Non__constant_expression);
      Assert.Error(expression.Symbol.Type.IsIntegralOrPointer(), expression.Symbol, Message.Non__integral_expression);
      return expression;
    }

    public static Expression LogicalOrExpression(Expression leftExpression,
                                                 Expression rightExpression) {
      Expression constantExpression =
        ConstantExpression.Logical(MiddleOperator.LogicalOr,
                                   leftExpression, rightExpression);

      if (constantExpression != null) {
        return constantExpression;
      }

      leftExpression = TypeCast.ToLogical(leftExpression);
      rightExpression = TypeCast.ToLogical(rightExpression);

      ISet<MiddleCode> trueSet = new HashSet<MiddleCode>();
      trueSet.UnionWith(leftExpression.Symbol.TrueSet);
      trueSet.UnionWith(rightExpression.Symbol.TrueSet);
      Backpatch(leftExpression.Symbol.FalseSet, rightExpression.LongList);
      Symbol symbol = new Symbol(trueSet, rightExpression.Symbol.FalseSet);

      List<MiddleCode> longList = new List<MiddleCode>();
      longList.AddRange(leftExpression.LongList);
      longList.AddRange(rightExpression.LongList);

      List<MiddleCode> shortList = new List<MiddleCode>();
      shortList.AddRange(leftExpression.ShortList);
      shortList.AddRange(rightExpression.ShortList);

      return (new Expression(symbol, shortList, longList));
    }

    public static Expression LogicalAndExpression(Expression leftExpression,
                                                  Expression rightExpression) {
      Expression constantExpression =
        ConstantExpression.Logical(MiddleOperator.LogicalAnd,
                                   leftExpression, rightExpression);

      if (constantExpression != null) {
        return constantExpression;
      }

      leftExpression = TypeCast.ToLogical(leftExpression);
      rightExpression = TypeCast.ToLogical(rightExpression);

      ISet<MiddleCode> falseSet = new HashSet<MiddleCode>();
      falseSet.UnionWith(leftExpression.Symbol.FalseSet);
      falseSet.UnionWith(rightExpression.Symbol.FalseSet);

      Backpatch(leftExpression.Symbol.TrueSet, rightExpression.LongList);
      Symbol symbol = new Symbol(rightExpression.Symbol.TrueSet, falseSet);
    
      List<MiddleCode> longList = new List<MiddleCode>();
      longList.AddRange(leftExpression.LongList);
      longList.AddRange(rightExpression.LongList);

      List<MiddleCode> shortList = new List<MiddleCode>();
      shortList.AddRange(leftExpression.ShortList);
      shortList.AddRange(rightExpression.ShortList);

      return (new Expression(symbol, shortList, longList));
    }

    public static Expression BitwiseExpression(MiddleOperator middleOp,
                                               Expression leftExpression,
                                               Expression rightExpression) {
      Expression constantExpression = ConstantExpression.
         Arithmetic(middleOp, leftExpression, rightExpression);

      if (constantExpression != null) {
        return constantExpression;
      }

      Type maxType = TypeCast.MaxType(leftExpression.Symbol.Type,
                                      rightExpression.Symbol.Type);
      Symbol resultSymbol = new Symbol(maxType);

      Assert.Error(maxType.IsIntegralPointerArrayStringOrFunction(),
                   maxType, Message.Invalid_type_in_bitwise_expression);

      leftExpression = TypeCast.ImplicitCast(leftExpression, maxType);
      rightExpression = TypeCast.ImplicitCast(rightExpression, maxType);

      List<MiddleCode> shortList = new List<MiddleCode>();
      shortList.AddRange(leftExpression.ShortList);
      shortList.AddRange(rightExpression.ShortList);

      List<MiddleCode> longList = new List<MiddleCode>();
      longList.AddRange(leftExpression.LongList);
      longList.AddRange(rightExpression.LongList);

      AddMiddleCode(longList, middleOp, resultSymbol,
                    leftExpression.Symbol, rightExpression.Symbol);
      return (new Expression(resultSymbol, shortList, longList));
    }

    public static Expression ShiftExpression(MiddleOperator middleOp,
                                             Expression leftExpression,
                                             Expression rightExpression) {
      Assert.Error(leftExpression.Symbol.Type.
                   IsIntegralPointerArrayStringOrFunction(),
                   leftExpression, Message.Invalid_type_in_shift_expression);

      Expression constantExpression = 
        ConstantExpression.Arithmetic(middleOp, leftExpression,
                                      rightExpression);
      if (constantExpression != null) {
        return constantExpression;
      }
     
      rightExpression =
        TypeCast.ImplicitCast(rightExpression, Type.UnsignedCharType);

      List<MiddleCode> shortList = new List<MiddleCode>();
      shortList.AddRange(leftExpression.ShortList);
      shortList.AddRange(rightExpression.ShortList);

      List<MiddleCode> longList = new List<MiddleCode>();
      longList.AddRange(leftExpression.LongList);
      longList.AddRange(rightExpression.LongList);

      Symbol resultSymbol = new Symbol(leftExpression.Symbol.Type);
      AddMiddleCode(longList, middleOp, resultSymbol,
                    leftExpression.Symbol, rightExpression.Symbol);
      return (new Expression(resultSymbol, shortList, longList));
    }

    public static Expression RelationalExpression(MiddleOperator middleOp,
                                                  Expression leftExpression,
                                                  Expression rightExpression){
      Assert.Error(!leftExpression.Symbol.Type.IsStructOrUnion(),
                    leftExpression,
                    Message.Invalid_type_in_expression);
      Assert.Error(!rightExpression.Symbol.Type.IsStructOrUnion(),
                    rightExpression,
                    Message.Invalid_type_in_expression);

      Type maxType = TypeCast.MaxType(leftExpression.Symbol.Type,
                                      rightExpression.Symbol.Type);

      leftExpression = TypeCast.ImplicitCast(leftExpression, maxType);
      rightExpression = TypeCast.ImplicitCast(rightExpression, maxType);

      List<MiddleCode> shortList = new List<MiddleCode>();
      shortList.AddRange(leftExpression.ShortList);
      shortList.AddRange(rightExpression.ShortList);

      if (maxType.IsUnsigned()) {
        string name = Enum.GetName(typeof(MiddleOperator), middleOp);
        middleOp = (MiddleOperator) Enum.Parse(typeof(MiddleOperator),
                                         name.Replace("Signed", "Unsigned"));
      }

      List<MiddleCode> longList = new List<MiddleCode>();
      longList.AddRange(leftExpression.LongList);
      longList.AddRange(rightExpression.LongList);
    
      ISet<MiddleCode> trueSet = new HashSet<MiddleCode>(),
                       falseSet = new HashSet<MiddleCode>();
      trueSet.Add(AddMiddleCode(longList, middleOp, null,
                                leftExpression.Symbol, rightExpression.Symbol));
      falseSet.Add(AddMiddleCode(longList, MiddleOperator.Goto));

      Symbol symbol = new Symbol(trueSet, falseSet);
      return (new Expression(symbol, shortList, longList));
    }

    /*public static Expression RelationalExpression(MiddleOperator middleOp,
                                                  Expression leftExpression,
                                                  Expression rightExpression){
      switch (middleOp) {
        case MiddleOperator.Equal:
        case MiddleOperator.NotEqual:
          Assert.Error(!leftExpression.Symbol.Type.IsStructOrUnion(),
                       leftExpression,
                       Message.Invalid_type_in_equality_expression);
          Assert.Error(!rightExpression.Symbol.Type.IsStructOrUnion(),
                       rightExpression,
                       Message.Invalid_type_in_equality_expression);
          break;

        default:
          Assert.Error(leftExpression.Symbol.Type.IsLogical() ||
                       leftExpression.Symbol.Type.IsArithmeticOrPointer(),
                       leftExpression,
                       Message.Invalid_type_in_relational_expression);
          Assert.Error(leftExpression.Symbol.Type.IsLogical() ||
                       rightExpression.Symbol.Type.IsArithmeticOrPointer(),
                       rightExpression,
                       Message.Invalid_type_in_relational_expression);
          break;
      }

      Type maxType = TypeCast.MaxType(leftExpression.Symbol.Type,
                                       rightExpression.Symbol.Type);
      Assert.Error(maxType.IsArithmeticPointerArrayStringOrFunction(),
                   maxType, Message.Invalid_type_in_expression);

      leftExpression = TypeCast.ImplicitCast(leftExpression, maxType);
      rightExpression = TypeCast.ImplicitCast(rightExpression, maxType);

      List<MiddleCode> shortList = new List<MiddleCode>();
      shortList.AddRange(leftExpression.ShortList);
      shortList.AddRange(rightExpression.ShortList);

      List<MiddleCode> longList = new List<MiddleCode>();
      longList.AddRange(leftExpression.LongList);
      longList.AddRange(rightExpression.LongList);
    
      if (maxType.IsUnsigned()) {
        string name = Enum.GetName(typeof(MiddleOperator), middleOp);
        middleOp = (MiddleOperator) Enum.Parse(typeof(MiddleOperator),
                                         name.Replace("Signed", "Unsigned"));
      }

      ISet<MiddleCode> trueSet = new HashSet<MiddleCode>(),
                       falseSet = new HashSet<MiddleCode>();
      trueSet.Add(AddMiddleCode(longList, middleOp, null,
                              leftExpression.Symbol, rightExpression.Symbol));
      falseSet.Add(AddMiddleCode(longList, MiddleOperator.Goto));

      Symbol symbol = new Symbol(trueSet, falseSet);
      return (new Expression(symbol, shortList, longList));
    }*/

    public static Expression AdditionExpression(MiddleOperator middleOp,
                                                Expression leftExpression,
                                                Expression rightExpression) {
      Type leftType = leftExpression.Symbol.Type,
           rightType = rightExpression.Symbol.Type;

      Expression constantExpression =
        ConstantExpression.Arithmetic(MiddleOperator.BinaryAdd,
                                      leftExpression, rightExpression);
      if (constantExpression != null) {
        return constantExpression;
      }

      Expression staticExpression =
        StaticExpression.Binary(MiddleOperator.BinarySubtract,
                                leftExpression, rightExpression);
      if (staticExpression != null) {
        return staticExpression;
      }

      List<MiddleCode> shortList = new List<MiddleCode>();
      shortList.AddRange(leftExpression.ShortList);
      shortList.AddRange(rightExpression.ShortList);    

      if (leftType.IsPointerOrArray()) {
        return PointerArithmetic(middleOp, leftExpression, rightExpression);
      }
      else if (rightType.IsPointerOrArray()) {
        Assert.Error(middleOp == MiddleOperator.BinaryAdd, middleOp,
                     Message.Invalid_types_in_subtraction_expression);
        return PointerArithmetic(middleOp, rightExpression, leftExpression);
      }
      else {
        Assert.Error(leftExpression.Symbol.Type.IsArithmetic(),
                     leftExpression, Message.Non__arithmetic_expression);
        Assert.Error(rightExpression.Symbol.Type.IsArithmetic(),
                     rightExpression, Message.Non__arithmetic_expression);

        Type maxType = TypeCast.MaxType(leftType, rightType);
        leftExpression = TypeCast.ImplicitCast(leftExpression, maxType);
        rightExpression = TypeCast.ImplicitCast(rightExpression, maxType);
        Symbol resultSymbol = new Symbol(maxType);

        List<MiddleCode> longList = new List<MiddleCode>();
        longList.AddRange(leftExpression.LongList);
        longList.AddRange(rightExpression.LongList);
        AddMiddleCode(longList, middleOp, resultSymbol,
                      leftExpression.Symbol, rightExpression.Symbol);
        return (new Expression(resultSymbol, shortList, longList));
      }
    }

    private static Expression PointerArithmetic(MiddleOperator middleOp,
                                                Expression leftExpression,
                                                Expression rightExpression) {
      List<MiddleCode> shortList = new List<MiddleCode>();
      shortList.AddRange(leftExpression.LongList);
      shortList.AddRange(rightExpression.LongList);

      Type leftType = leftExpression.Symbol.Type,
           rightType = rightExpression.Symbol.Type;
           
      if (leftType.IsPointerOrArray() && rightType.IsIntegral()) {
        Assert.Error(!leftType.PointerOrArrayType.IsVoid(),
                      leftExpression, Message.Pointer_to_void);
        rightExpression = TypeCast.ImplicitCast(rightExpression, leftType);

        List<MiddleCode> longList = new List<MiddleCode>();
        longList.AddRange(leftExpression.LongList);
        longList.AddRange(rightExpression.LongList);
        Symbol resultSymbol =
          new Symbol(new Type(leftType.PointerOrArrayType));

        int pointerSize = leftType.PointerOrArrayType.Size();
        Symbol multSymbol = new Symbol(Type.IntegerPointerType),
               sizeSymbol = new Symbol(Type.IntegerPointerType,
                                       (BigInteger) pointerSize);
        //AddStaticSymbol(sizeSymbol);
        //StaticSymbol sizeStaticSymbol = new StaticSymbol(sizeSymbol.UniqueName);
        //SymbolTable.StaticSet.Add(sizeStaticSymbol);
        AddMiddleCode(longList, MiddleOperator.UnsignedMultiply, multSymbol,
                      rightExpression.Symbol, sizeSymbol);
        AddMiddleCode(longList, middleOp, resultSymbol,
                      leftExpression.Symbol, multSymbol);
        return (new Expression(resultSymbol, shortList, longList));
      }
      else {
        Assert.Error(!leftType.PointerOrArrayType.IsVoid(),
                     leftExpression, Message.Pointer_to_void);
        Assert.Error(!rightType.PointerOrArrayType.IsVoid(),
                     rightExpression, Message.Pointer_to_void);
        Assert.Error(leftType.PointerOrArrayType.Size() ==
                     rightType.PointerOrArrayType.Size(),
                     leftType + " and " + rightType,
                     Message.Invalid_expression);
                     
        List<MiddleCode> longList = new List<MiddleCode>();
        longList.AddRange(leftExpression.LongList);
        longList.AddRange(rightExpression.LongList);
        Symbol resultSymbol = new Symbol(Type.VoidPointerType);
        
        int pointerSize = rightType.PointerOrArrayType.Size();
        Symbol subtractSymbol = new Symbol(Type.IntegerPointerType),
               sizeSymbol = new Symbol(Type.IntegerPointerType, (BigInteger) pointerSize);
        //StaticSymbol sizeStaticSymbol = new StaticSymbol(sizeSymbol.UniqueName);
        //SymbolTable.StaticSet.Add(sizeStaticSymbol);
        AddMiddleCode(longList, MiddleOperator.BinarySubtract, subtractSymbol,
                      leftExpression.Symbol, rightExpression.Symbol);
        AddMiddleCode(longList, MiddleOperator.UnsignedDivide, resultSymbol,
                      subtractSymbol, sizeSymbol);

        Expression resultExpression =
          new Expression(resultSymbol, shortList, longList);
        return TypeCast.ImplicitCast(resultExpression, Type.SignedIntegerType);
      }
    }

    /*    public static Expression SubtractionExpression(Expression leftExpression,
                                                       Expression rightExpression) {
          Expression constantExpression =
            ConstantExpression.Arithmetic(MiddleOperator.BinarySubtract,
                                          leftExpression, rightExpression);
          if (constantExpression != null) {
            return constantExpression;
          }
    
          Expression staticExpression =
            StaticExpression.Binary(MiddleOperator.BinarySubtract,
                                    leftExpression, rightExpression);
          if (staticExpression != null) {
            return staticExpression;
          }

          List<MiddleCode> shortList = new List<MiddleCode>();
          shortList.AddRange(leftExpression.ShortList);
          shortList.AddRange(rightExpression.ShortList);

          Type leftType = leftExpression.Symbol.Type,
               rightType = rightExpression.Symbol.Type;

          if (leftType.IsPointerOrArray()) {
            return PointerArithmentics(MiddleOperator.BinarySubtract,
                                       leftExpression, rightExpression);
          }
          else {
            Assert.Error(leftType.IsArithmetic(), leftExpression,
                         Message.Invalid_expression);
            Assert.Error(rightType.IsArithmetic(), rightExpression,
                         Message.Invalid_expression);

            Type maxType = TypeCast.MaxType(leftType, rightType);
            leftExpression = TypeCast.ImplicitCast(leftExpression, maxType);
            rightExpression = TypeCast.ImplicitCast(rightExpression, maxType);
            Symbol resultSymbol = new Symbol(maxType);

            List<MiddleCode> longList = new List<MiddleCode>();
            longList.AddRange(leftExpression.LongList);
            longList.AddRange(rightExpression.LongList);
            AddMiddleCode(longList, MiddleOperator.BinarySubtract, resultSymbol,
                          leftExpression.Symbol, rightExpression.Symbol);
            return (new Expression(resultSymbol, shortList, longList));
          }
        }*/
        /*if (integerExpression.Symbol.Value is BigInteger) {
          int integerValue = (int)
                             ((BigInteger) integerExpression.Symbol.Value);
          int sizeValue = integerValue * pointerType.PointerOrArrayType.Size();
          Symbol sizeSymbol = new Symbol(Type.PointerTypeX,
                                         (BigInteger) sizeValue);
          AddMiddleCode(longList, MiddleOperator.BinaryAdd, resultSymbol,
                        pointerExpression.Symbol, sizeSymbol);
        }
        else if (pointerType.PointerOrArrayType.Size() == 1) {
          AddMiddleCode(longList, MiddleOperator.BinaryAdd, resultSymbol,
                        pointerExpression.Symbol, integerExpression.Symbol);
        }
        else*/
      /*if (leftType.IsPointerOrArray()) {
        rightExpression = TypeCast.ImplicitCast(rightExpression, leftType);
        longList.AddRange(leftExpression.LongList);
        longList.AddRange(rightExpression.LongList);
        Symbol resultSymbol = new Symbol(new Type(leftType.PointerOrArrayType));

        if (rightExpression.Symbol.Value is BigInteger) {
          int rightValue = (int) ((BigInteger) rightExpression.Symbol.Value);
          Symbol sizeSymbol = new Symbol(Type.PointerTypeX, (BigInteger) (rightValue * leftType.PointerOrArrayType.Size()));
          sizeSymbol.StaticSymbol = ConstantExpression.Value(sizeSymbol.UniqueName, sizeSymbol.Type, sizeSymbol.Value);
          SymbolTable.StaticSet.Add(sizeSymbol.StaticSymbol);
          AddMiddleCode(longList, MiddleOperator.BinaryAdd, resultSymbol, leftExpression.Symbol, sizeSymbol);
        }
        else if (leftType.PointerOrArrayType.Size() > 1) {
          Symbol multSymbol = new Symbol(Type.PointerTypeX),
                 sizeSymbol = new Symbol(Type.PointerTypeX, (BigInteger) leftType.PointerOrArrayType.Size());
          sizeSymbol.StaticSymbol = ConstantExpression.Value(sizeSymbol.UniqueName, sizeSymbol.Type, sizeSymbol.Value);
          SymbolTable.StaticSet.Add(sizeSymbol.StaticSymbol);
          AddMiddleCode(longList, MiddleOperator.UnsignedMultiply, multSymbol, rightExpression.Symbol, sizeSymbol);
          AddMiddleCode(longList, MiddleOperator.BinaryAdd, resultSymbol, leftExpression.Symbol, multSymbol);
        }
        else {
          AddMiddleCode(longList, MiddleOperator.BinaryAdd, resultSymbol, leftExpression.Symbol, rightExpression.Symbol);
        }

        return (new Expression(resultSymbol, shortList, longList));
      }
      else if (rightType.IsPointerOrArray()) {
        leftExpression = TypeCast.ImplicitCast(leftExpression, rightType);
        longList.AddRange(leftExpression.LongList);
        longList.AddRange(rightExpression.LongList);
        Symbol resultSymbol;

        if (leftExpression.Symbol.Value is BigInteger) {
          int leftValue = (int) ((BigInteger) leftExpression.Symbol.Value);
          Symbol sizeSymbol = new Symbol(Type.PointerTypeX, (BigInteger) (leftValue * rightType.PointerOrArrayType.Size()));
          sizeSymbol.StaticSymbol = ConstantExpression.Value(sizeSymbol.UniqueName, sizeSymbol.Type, sizeSymbol.Value);
          SymbolTable.StaticSet.Add(sizeSymbol.StaticSymbol);
          resultSymbol = new Symbol(new Type(rightType.PointerOrArrayType));
          AddMiddleCode(longList, MiddleOperator.BinaryAdd, resultSymbol, sizeSymbol, rightExpression.Symbol);
        }
        else if (rightType.PointerOrArrayType.Size() > 1) {
          Symbol multSymbol = new Symbol(Type.PointerTypeX),
                 sizeSymbol = new Symbol(Type.PointerTypeX, (BigInteger) rightType.PointerOrArrayType.Size());
          sizeSymbol.StaticSymbol = ConstantExpression.Value(sizeSymbol.UniqueName, sizeSymbol.Type, sizeSymbol.Value);
          SymbolTable.StaticSet.Add(sizeSymbol.StaticSymbol);
          AddMiddleCode(longList, MiddleOperator.UnsignedMultiply, multSymbol, leftExpression.Symbol, sizeSymbol);
          resultSymbol = new Symbol(new Type(rightType.PointerOrArrayType));
          AddMiddleCode(longList, MiddleOperator.BinaryAdd, resultSymbol, multSymbol, rightExpression.Symbol);
        }
        else {
          resultSymbol = new Symbol(new Type(rightType.PointerOrArrayType));
          AddMiddleCode(longList, MiddleOperator.BinaryAdd, resultSymbol, leftExpression.Symbol, rightExpression.Symbol);
        }

        return (new Expression(resultSymbol, shortList, longList));
      }*/

        /*if (rightExpression.Symbol.Value is BigInteger) {
          int rightValue = (int) ((BigInteger) rightExpression.Symbol.Value);
          int sizeValue = rightValue * leftType.PointerOrArrayType.Size();
          Symbol sizeSymbol = new Symbol(Type.PointerTypeX, (BigInteger) sizeValue);
          AddMiddleCode(longList, MiddleOperator.BinarySubtract, resultSymbol,
                        leftExpression.Symbol, sizeSymbol);
        }
        else if (leftType.PointerOrArrayType.Size() == 1) {
          AddMiddleCode(longList, MiddleOperator.BinarySubtract, resultSymbol,
                        leftExpression.Symbol, rightExpression.Symbol);
        }
        else*/

    public static Expression MultiplyExpression(MiddleOperator middleOp,
                                                Expression leftExpression,
                                                Expression rightExpression) {
      List<MiddleCode> constLongList = new List<MiddleCode>();
      constLongList.AddRange(leftExpression.LongList);
      constLongList.AddRange(rightExpression.LongList);
           
      Expression constantExpression =
        ConstantExpression.Arithmetic(middleOp, leftExpression,
                                      rightExpression);
      if (constantExpression != null) {
        return constantExpression;
      }

      Type maxType = TypeCast.MaxType(leftExpression.Symbol.Type,
                                      rightExpression.Symbol.Type);

      if (MiddleCode.IsModulo(middleOp)) {
        Assert.Error(maxType.IsIntegralPointerArrayStringOrFunction(),
                      maxType, Message.Invalid_type_in_expression);
      }
      else {
        Assert.Error(maxType.IsArithmeticPointerArrayStringOrFunction(),
                      maxType, Message.Invalid_type_in_expression);
      }

      leftExpression = TypeCast.ImplicitCast(leftExpression, maxType);
      rightExpression = TypeCast.ImplicitCast(rightExpression, maxType);
      Symbol resultSymbol = new Symbol(maxType);

      if (maxType.IsUnsigned()) {
        string name = Enum.GetName(typeof(MiddleOperator), middleOp);
        middleOp = (MiddleOperator) Enum.Parse(typeof(MiddleOperator),
                                         name.Replace("Signed", "Unsigned"));
      }

      List<MiddleCode> shortList = new List<MiddleCode>(),
                       longList = new List<MiddleCode>();
      shortList.AddRange(leftExpression.ShortList);
      shortList.AddRange(rightExpression.ShortList);    
      longList.AddRange(leftExpression.LongList);
      longList.AddRange(rightExpression.LongList);

      AddMiddleCode(longList, middleOp, resultSymbol,
                    leftExpression.Symbol, rightExpression.Symbol);
      return (new Expression(resultSymbol, shortList, longList));
    }

    // ---------------------------------------------------------------------------------------------------------------------

    public static Type TypeName(Specifier specifier, Declarator declarator)
    {
      Type specifierType = specifier.Type;

      if (declarator != null) {
        declarator.Add(specifierType);
        return declarator.Type;
      }
      else {
        return specifierType;
      }
    }

    // ---------------------------------------------------------------------------------------------------------------------

    public static Expression UnaryExpression(MiddleOperator middleOp,
                                             Expression expression) {
      Type type = expression.Symbol.Type;
      Assert.Error(type.IsLogical() ||
                   type.IsArithmeticPointerArrayStringOrFunction(),
                   expression, Message.Non__arithmetic_expression);

      Expression constantExpression =
        ConstantExpression.Arithmetic(middleOp, expression);    
      if (constantExpression != null) {
        return constantExpression;
      }
    
      Symbol resultSymbol = new Symbol(expression.Symbol.Type);
      AddMiddleCode(expression.LongList, middleOp,
                    resultSymbol, expression.Symbol);
      return (new Expression(resultSymbol, expression.ShortList,
                             expression.LongList));
    }

    public static Expression LogicalNotExpression(Expression expression) {    
      Expression constantExpression =
        ConstantExpression.LogicalNot(expression);
      if (constantExpression != null) {
        return constantExpression;
      }

      expression = TypeCast.ToLogical(expression);
      Symbol notSymbol =
        new Symbol(expression.Symbol.FalseSet, expression.Symbol.TrueSet);
      return (new Expression(notSymbol, expression.ShortList,
                             expression.LongList));
    }

    public static Expression BitwiseNotExpression(Expression expression) {
      expression = TypeCast.LogicalToIntegral(expression);      
      Assert.Error(expression.Symbol.Type.IsIntegralPointerArrayOrFunction(),
                   Message.Only_integral_values_for_bitwise_not);
      Expression constantExpression =
        ConstantExpression.Arithmetic(MiddleOperator.BitwiseNot, expression);
      if (constantExpression != null) {
        return constantExpression;
      }

      expression = TypeCast.LogicalToIntegral(expression);
      Symbol resultSymbol = new Symbol(expression.Symbol.Type);
      AddMiddleCode(expression.LongList, MiddleOperator.BitwiseNot,
                    resultSymbol, expression.Symbol);
      return (new Expression(resultSymbol, expression.ShortList,
                             expression.LongList));
    }

    public static Expression SizeOfExpression(Expression expression) {
      Assert.Error(!expression.Symbol.IsRegister(), expression,
                   Message.Register_storage_not_allowed_in_sizof_expression);

      Type type = expression.Symbol.Type;
      Assert.Error(!type.IsFunction(),
                   Message.Sizeof_applied_to_function_not_allowed);
      Assert.Error(!type.IsBitfield(),
                   Message.Sizeof_applied_to_bitfield_not_allowed);

      Symbol symbol = new Symbol(Type.SignedIntegerType,
                                (BigInteger)(expression.Symbol.Type.Size()));
      /*symbol.StaticSymbol =
        ConstantExpression.Value(symbol.UniqueName, Type.SignedIntegerType,
                                (BigInteger) (expression.Symbol.Type.Size()));
      SymbolTable.StaticSet.Add(staticSymbol);*/
      return (new Expression(symbol, new List<MiddleCode>(),
                             new List<MiddleCode>()));
    }

    public static Expression SizeOfType(Type type) {
      Assert.Error(!type.IsFunction(),
                   Message.Sizeof_applied_to_function_not_allowed);
      Assert.Error(!type.IsBitfield(), 
                   Message.Sizeof_applied_to_bitfield_not_allowed);

      Symbol symbol =
        new Symbol(Type.SignedIntegerType, (BigInteger)type.Size());
      return (new Expression(symbol, new List<MiddleCode>(),
                             new List<MiddleCode>()));
    }

    public static Expression AddressExpression(Expression expression) {
      Symbol symbol = expression.Symbol;
      Assert.Error(!symbol.IsRegister() && !symbol.Type.IsBitfield(),
                   expression,  Message.Not_addressable);
      Assert.Error(!expression.Symbol.IsRegister(), expression,
                   Message.Invalid_address_of_register_storage);

      Expression staticExpression =
        StaticExpression.Unary(MiddleOperator.Address, expression);
      if (staticExpression!= null) {
        return staticExpression ;
      }
    
      if (expression.Symbol.Type.IsFloating()) {
        AddMiddleCode(expression.LongList, MiddleOperator.PopFloat);
      }

      Type pointerType = new Type(expression.Symbol.Type);
      Symbol resultSymbol = new Symbol(pointerType);
      AddMiddleCode(expression.LongList, MiddleOperator.Address,
                    resultSymbol, expression.Symbol);
      return (new Expression(resultSymbol, expression.ShortList,
                             expression.LongList));
    }
    
    /*
     *p = 1;
     a[1] = 2;
     a[i] = 3;
     s.i = 4;
     p->i = 5;
    */

    //int *p = &i;
    //int *p = &a[3];
    //int *p = a + 2;

    public static Expression DereferenceExpression(Expression expression) {
      Assert.Error(expression.Symbol.Type.IsPointerArrayStringOrFunction(),
                   Message.Invalid_dereference_of_non__pointer);
      /*Symbol staticSymbol =
        StaticExpression.Unary(MiddleOperator.Dereference, expression);
      if (staticSymbol != null) {
        return (new Expression(staticSymbol, null, null));
      }*/

      Symbol resultSymbol =
        new Symbol(expression.Symbol.Type.PointerOrArrayType);
      return Dereference(expression, resultSymbol, 0);
    }

    public static Expression ArrowExpression(Expression expression,
                                             string memberName) {
      Assert.Error(expression.Symbol.Type.IsPointer() &&
                   expression.Symbol.Type.PointerType.IsStructOrUnion(),
                   expression,
             Message.Not_a_pointer_to_a_struct_or_union_in_arrow_expression);
      Symbol memberSymbol =
        expression.Symbol.Type.PointerType.MemberMap[memberName];
      Assert.Error(memberSymbol != null, memberName,
                   Message.Unknown_member_in_arrow_expression);
      Symbol resultSymbol = new Symbol(memberSymbol.Type); 
      return Dereference(expression, resultSymbol, memberSymbol.Offset);
    }

    public static Expression IndexExpression(Expression leftExpression,
                                             Expression rightExpression) {
      Expression staticExpression =
        StaticExpression.Binary(MiddleOperator.Index, leftExpression,
                                rightExpression);
      if (staticExpression != null) {
        return staticExpression;
      }

      Expression arrayExpression, indexExpression;

      if (leftExpression.Symbol.Type.IsPointerOrArray()) {
        arrayExpression = leftExpression;
        indexExpression = rightExpression;
      }
      else {
        indexExpression = leftExpression;
        arrayExpression = rightExpression;
      }

      Type arrayType = arrayExpression.Symbol.Type,
           indexType = indexExpression.Symbol.Type;

      Assert.Error(arrayType.IsPointerOrArray() &&
                   !arrayType.PointerOrArrayType.IsVoid(),
                   arrayType, Message.Invalid_type_in_index_expression);
      Assert.Error(indexType.IsIntegral(), indexExpression,
                   Message.Invalid_type_in_index_expression);

      List<MiddleCode> shortList = new List<MiddleCode>();
      shortList.AddRange(arrayExpression.ShortList);
      shortList.AddRange(indexExpression.ShortList);

      Symbol resultSymbol =
        new Symbol(arrayExpression.Symbol.Type.PointerOrArrayType);

      if (indexExpression.Symbol.Value is BigInteger) {
        int indexValue = (int) ((BigInteger)indexExpression.Symbol.Value),
            indexSize = arrayExpression.Symbol.Type.PointerOrArrayType.Size();
        return Dereference(arrayExpression, resultSymbol,
                           indexValue * indexSize);
      }
      else {
        indexExpression =
          TypeCast.ImplicitCast(indexExpression, arrayExpression.Symbol.Type);

        List<MiddleCode> longList = new List<MiddleCode>();
        longList.AddRange(arrayExpression.LongList);
        longList.AddRange(indexExpression.LongList);

        Symbol arraySymbol = arrayExpression.Symbol,
               indexSymbol = indexExpression.Symbol;
        Symbol sizeSymbol = new Symbol(arraySymbol.Type, (BigInteger)
                               arraySymbol.Type.PointerOrArrayType.Size()),
               multSymbol = new Symbol(arraySymbol.Type);
        //StaticSymbol sizeStaticSymbol = new StaticSymbol(sizeSymbol.UniqueName);
        //SymbolTable.StaticSet.Add(sizeStaticSymbol);
        AddMiddleCode(longList, MiddleOperator.UnsignedMultiply,
                      multSymbol, indexSymbol, sizeSymbol);
        Symbol addSymbol = new Symbol(arraySymbol.Type);
        AddMiddleCode(longList, MiddleOperator.BinaryAdd,
                      addSymbol, arraySymbol, multSymbol);

        Expression addExpression =
          new Expression(addSymbol, shortList, longList);
        return Dereference(addExpression, resultSymbol, 0);
      }
    }

    private static Expression Dereference(Expression expression,
                                          Symbol resultSymbol, int offset) {
      resultSymbol.AddressSymbol = expression.Symbol;
      resultSymbol.AddressOffset = offset;
      AddMiddleCode(expression.LongList, MiddleOperator.Dereference,
                    resultSymbol, expression.Symbol, 0);

      if (resultSymbol.Type.IsFloating()) {
        AddMiddleCode(expression.LongList, MiddleOperator.PushFloat,
                      resultSymbol);
      }

      return (new Expression(resultSymbol, expression.ShortList,
                             expression.LongList));
    }

    public static Expression DotExpression(Expression expression,
                                           string memberName) {
      Symbol parentSymbol = expression.Symbol;
      Assert.Error(parentSymbol.Type.IsStructOrUnion(), expression,
                   Message.Not_a_struct_or_union_in_dot_expression);

      Symbol memberSymbol = parentSymbol.Type.MemberMap[memberName];
      Assert.Error(memberSymbol != null, memberName,
                   Message.Unknown_member_in_dot_expression);


      Symbol resultSymbol;
      if (parentSymbol.AddressSymbol != null) {
        string name = parentSymbol.Name + Symbol.SeparatorDot + memberSymbol.Name;
                      //+ Symbol.SeparatorId + memberSymbol.Offset;
        resultSymbol = new Symbol(name, parentSymbol.ExternalLinkage,
                                  parentSymbol.Storage, memberSymbol.Type,
                                  parentSymbol.Parameter);
        resultSymbol.UniqueName = parentSymbol.UniqueName;
        resultSymbol.AddressSymbol = parentSymbol.AddressSymbol;
        resultSymbol.AddressOffset = parentSymbol.AddressOffset;
        resultSymbol.Offset = parentSymbol.Offset + memberSymbol.Offset;
        /*resultSymbol.Assignable = !parentSymbol.Type.Constant &&
                                  !memberSymbol.Type.IsConstantRecursive() &&
                                  !memberSymbol.Type.IsArrayOrFunction();*/
        /*resultSymbol.Addressable = !parentSymbol.IsRegister() &&
                                   !memberSymbol.Type.IsBitfield();*/
      }
      else {
        resultSymbol = new Symbol(memberSymbol.Type); 
        resultSymbol.Name = parentSymbol.Name + Symbol.SeparatorDot + memberName;
        resultSymbol.UniqueName = parentSymbol.UniqueName;
        resultSymbol.Storage = parentSymbol.Storage;
        resultSymbol.Offset = parentSymbol.Offset + memberSymbol.Offset;
      }

      return (new Expression(resultSymbol, expression.ShortList,
                             expression.LongList));
    }

    public static Expression CastExpression(Type type, Expression expression) {
      Expression constantExpression = ConstantExpression.ConstantCast(expression, type);
      if (constantExpression != null) {
        return constantExpression;
      }
    
      return TypeCast.ExplicitCast(expression, type);
    }
  
    private static IDictionary<MiddleOperator,MiddleOperator> m_incrementMap =
      new Dictionary<MiddleOperator, MiddleOperator>();

    private static IDictionary<MiddleOperator,MiddleOperator>
      m_incrementInverseMap = new Dictionary<MiddleOperator,MiddleOperator>();

    static MiddleCodeGenerator() {
      m_incrementMap.Add(MiddleOperator.Increment,
                         MiddleOperator.BinaryAdd);
      m_incrementMap.Add(MiddleOperator.Decrement,
                         MiddleOperator.BinarySubtract);
      m_incrementInverseMap.Add(MiddleOperator.Increment,
                                 MiddleOperator.BinarySubtract);
      m_incrementInverseMap.Add(MiddleOperator.Decrement,
                                 MiddleOperator.BinaryAdd);
    }

    private static IDictionary<MiddleOperator,MiddleOperator>
      incrementMap = new Dictionary<MiddleOperator,MiddleOperator>();

    public static Expression PrefixIncrementExpression(MiddleOperator middleOp,
                                                       Expression expression) {
      Symbol symbol = expression.Symbol;
      Assert.Error(symbol.IsAssignable(),  Message.Not_assignable);
      Assert.Error(symbol.Type.IsArithmeticOrPointer(),
                   expression, Message.Invalid_type_in_increment_expression);

      if (symbol.Type.IsIntegralOrPointer()) {
        Symbol oneSymbol = new Symbol(symbol.Type, BigInteger.One);
        AddMiddleCode(expression.ShortList, m_incrementMap[middleOp], symbol, symbol, oneSymbol);
        AddMiddleCode(expression.LongList, m_incrementMap[middleOp], symbol, symbol, oneSymbol);

        BigInteger? bitFieldMask = symbol.Type.GetBitfieldMask();
        if (bitFieldMask != null) {
          Symbol maskSymbol = new Symbol(symbol.Type, bitFieldMask.Value);
          MiddleCode maskCode = new MiddleCode(MiddleOperator.BitwiseAnd, symbol, symbol, maskSymbol);
          expression.ShortList.Add(maskCode);
          expression.LongList.Add(maskCode);
        }
    
        Symbol resultSymbol = new Symbol(symbol.Type);
        AddMiddleCode(expression.LongList, MiddleOperator.Assign, resultSymbol, symbol);
      
        return (new Expression(resultSymbol, expression.ShortList, expression.LongList));
      }
      else {
        AddMiddleCode(expression.ShortList, MiddleOperator.PushOne);
        Symbol oneSymbol = new Symbol(symbol.Type, (decimal) 1);
        AddMiddleCode(expression.ShortList, m_incrementMap[middleOp],
                      symbol, symbol, oneSymbol);
        AddMiddleCode(expression.ShortList, MiddleOperator.PopFloat, symbol);

        AddMiddleCode(expression.LongList, MiddleOperator.PushOne);
        AddMiddleCode(expression.LongList, m_incrementMap[middleOp],
                      symbol, symbol, oneSymbol);
        AddMiddleCode(expression.LongList, MiddleOperator.TopFloat, symbol);

        Symbol resultSymbol = new Symbol(symbol.Type);
        return (new Expression(resultSymbol, expression.ShortList,
                               expression.LongList));
      }
    }

    public static Expression PostfixIncrementExpression
      (MiddleOperator middleOp, Expression expression) {
      Symbol symbol = expression.Symbol;
      Assert.Error(symbol.IsAssignable(), Message.Not_assignable);
      Assert.Error(symbol.Type.IsArithmeticOrPointer(),
                   expression, Message.Invalid_type_in_increment_expression);
    
      if (symbol.Type.IsIntegralOrPointer()) {
        Symbol resultSymbol = new Symbol(symbol.Type);
        AddMiddleCode(expression.LongList, MiddleOperator.Assign,
                      resultSymbol, symbol);
        Symbol oneSymbol = new Symbol(symbol.Type, BigInteger.One);
        AddMiddleCode(expression.ShortList, m_incrementMap[middleOp], symbol, symbol, oneSymbol);
        AddMiddleCode(expression.LongList, m_incrementMap[middleOp], symbol, symbol, oneSymbol);

        BigInteger? bitFieldMask = symbol.Type.GetBitfieldMask();
        if (bitFieldMask != null) {
          Symbol maskSymbol = new Symbol(symbol.Type, bitFieldMask.Value);
          AddMiddleCode(expression.ShortList, MiddleOperator.BitwiseAnd,
                        symbol, symbol, maskSymbol);
          AddMiddleCode(expression.LongList, MiddleOperator.BitwiseAnd,
                        symbol, symbol, maskSymbol);
        }

        return (new Expression(resultSymbol, expression.ShortList,
                               expression.LongList));
      }
      else {
        AddMiddleCode(expression.ShortList, MiddleOperator.PushOne);
        Symbol oneSymbol = new Symbol(symbol.Type, (decimal) 1);
        AddMiddleCode(expression.ShortList, m_incrementMap[middleOp],
                      symbol, symbol, oneSymbol);
        AddMiddleCode(expression.ShortList, MiddleOperator.PopFloat, symbol);

        //AddMiddleCode(expression.LongList, MiddleOperator.PushFloat, symbol);
        AddMiddleCode(expression.LongList, MiddleOperator.PushOne);
        AddMiddleCode(expression.LongList, m_incrementMap[middleOp],
                      symbol, symbol, oneSymbol);
        AddMiddleCode(expression.LongList, MiddleOperator.TopFloat, symbol);

        AddMiddleCode(expression.LongList, MiddleOperator.PushOne);
        AddMiddleCode(expression.LongList, m_incrementInverseMap[middleOp],
                      symbol, symbol, oneSymbol);

        Symbol resultSymbol = new Symbol(symbol.Type);
        return (new Expression(resultSymbol, expression.ShortList,
                               expression.LongList));
      }
    }

    // ---------------------------------------------------------------------------------------------------------------------

    public static void CallHeader(Expression expression) {
      Type type = expression.Symbol.Type;
      Assert.Error((type.IsPointer() && type.PointerType.IsFunction()) ||
                   type.IsFunction(), expression.Symbol,
                   Message.Not_a_function);
      Type functionType = type.IsFunction() ? type : type.PointerType;
      TypeListStack.Push(functionType.TypeList);
      ParameterOffsetStack.Push(0);
      AddMiddleCode(expression.ShortList, MiddleOperator.PreCall,
                    SymbolTable.CurrentTable.CurrentOffset);
      AddMiddleCode(expression.LongList, MiddleOperator.PreCall,
                    SymbolTable.CurrentTable.CurrentOffset);
    }

    public static Expression CallExpression(Expression functionExpression,
                                            List<Expression> argumentList){
      TypeListStack.Pop();
      ParameterOffsetStack.Pop();

      int totalOffset = 0;
      foreach (int currentOffset in ParameterOffsetStack) {
        if (currentOffset > 0) {
          totalOffset += (SymbolTable.FunctionHeaderSize + currentOffset);
        }
      }

      Type functionType = functionExpression.Symbol.Type.IsPointer() ?
                      functionExpression.Symbol.Type.PointerType :
                      functionExpression.Symbol.Type;

      List<Type> typeList = functionType.TypeList;
      Assert.Error((typeList == null) ||
                   (argumentList.Count >= typeList.Count),
                   functionExpression,
                   Message.Too_few_actual_parameters_in_function_call);
      Assert.Error(functionType.IsEllipse() || (typeList == null) ||
                   (argumentList.Count == typeList.Count),
                   functionExpression,
                   Message.Too_many_parameters_in_function_call);
    
      List<MiddleCode> longList = new List<MiddleCode>();
      longList.AddRange(functionExpression.LongList);

      int index = 0, offset = SymbolTable.FunctionHeaderSize, extra = 0;
      foreach (Expression argumentExpression in argumentList) {
        longList.AddRange(argumentExpression.LongList);

        Type type;
        if ((typeList != null) && (index < typeList.Count)) {
          type = typeList[index++];
        }
        else {
          type = ParameterType(argumentExpression.Symbol);
          extra += type.Size();
        }

        AddMiddleCode(longList, MiddleOperator.Parameter, type,
                      argumentExpression.Symbol, SymbolTable.CurrentTable.
                      CurrentOffset + totalOffset + offset);
        offset += type.Size();
      }

      Symbol functionSymbol = functionExpression.Symbol;
      AddMiddleCode(longList, MiddleOperator.Call, functionSymbol,
                    SymbolTable.CurrentTable.CurrentOffset + totalOffset,
                    extra);
      AddMiddleCode(longList, MiddleOperator.PostCall, null, null,
                    SymbolTable.CurrentTable.CurrentOffset + totalOffset);
    
      Type returnType = functionType.ReturnType;
      Symbol returnSymbol = new Symbol(returnType);
    
      List<MiddleCode> shortList = new List<MiddleCode>();
      shortList.AddRange(longList);
    
      if (!returnType.IsVoid()) {
        if (returnType.IsStructOrUnion()) {
          Type pointerType = new Type(returnType);
          Symbol addressSymbol = new Symbol(pointerType);
          returnSymbol.AddressSymbol = addressSymbol;
        }
      
        AddMiddleCode(longList, MiddleOperator.GetReturnValue,
                      returnSymbol);
      
        if (returnType.IsFloating()) {
          AddMiddleCode(shortList, MiddleOperator.PopEmpty);
        }
      }

      return (new Expression(returnSymbol, shortList, longList));
    }

    public static Expression ArgumentExpression(int index,
                                                Expression expression) {
      List<Type> typeList = TypeListStack.Peek();

      if ((typeList != null) && (index < typeList.Count)) {
        expression = TypeCast.ImplicitCast(expression, typeList[index]);
      }
      else {
        Type type = expression.Symbol.Type;
        
        if (type.IsChar() || type.IsShort()) {
          if (type.IsSigned()) {
            expression =
              TypeCast.ImplicitCast(expression, Type.SignedIntegerType);
          }
          else {
            expression =
              TypeCast.ImplicitCast(expression, Type.UnsignedIntegerType);
          }      
        }
        else if (type.IsFloat()) {
          expression = TypeCast.ImplicitCast(expression, Type.DoubleType);
        }
      }

      int offset = ParameterOffsetStack.Pop();
      ParameterOffsetStack.Push(offset +
                                ParameterType(expression.Symbol).Size());
      return (new Expression(expression.Symbol, expression.LongList,
                             expression.LongList));
                                                }
 
    private static Type ParameterType(Symbol symbol) {
      switch (symbol.Type.Sort) {
        case Sort.Array:
          return (new Type(symbol.Type.ArrayType));

        case Sort.Function:
          return (new Type(symbol.Type));

        case Sort.String:
          return (new Type(new Type(Sort.SignedChar)));

        default:
          return symbol.Type;
      }
    }
   
    // ---------------------------------------------------------------------------------------------------------------------

    public static Expression ValueExpression(Symbol symbol) {
      List<MiddleCode> longList = new List<MiddleCode>();

      if (symbol.Type.IsFloating()) {
        AddMiddleCode(longList, MiddleOperator.PushFloat, symbol);
      }
      /*else if (symbol.Type.IsString()) {
        StaticSymbol staticSymbol = ConstantExpression.Value(symbol.UniqueName, symbol.Type, symbol.Value);
        SymbolTable.StaticSet.Add(staticSymbol);
      }*/

      return (new Expression(symbol, new List<MiddleCode>(), longList));
    }

    public static Expression SymbolExpression(string name) {
      Symbol symbol = SymbolTable.CurrentTable.LookupSymbol(name);

      if (symbol == null) {
        Type type = new Type(Type.SignedIntegerType, null, false);
        symbol = new Symbol(name, true, Storage.Extern, type);
        SymbolTable.CurrentTable.AddSymbol(symbol);
      }

      //symbol.Used = true;
      List<MiddleCode> shortList = new List<MiddleCode>(),
                       longList = new List<MiddleCode>(); 

      if (symbol.Type.IsFloating()) {
        AddMiddleCode(shortList, MiddleOperator.PushFloat, symbol);
        AddMiddleCode(longList, MiddleOperator.PushFloat, symbol);
      }

      return (new Expression(symbol, shortList, longList));
    }

    /*public static Expression SystemCall(String name, List<Expression> argList) {
      List<MiddleCode> shortList = new List<MiddleCode>();
      AddMiddleCode(shortList, MiddleOperator.SystemInitializer, name);

      int index = 0;
      foreach (Expression arg in argList) {
        shortList.AddRange(arg.LongList);
        AddMiddleCode(shortList, MiddleOperator.SystemParameter, name, index++, arg.Symbol);
      }

      List<MiddleCode> longList = new List<MiddleCode>();
      longList.AddRange(shortList);
      Type type = SystemCode.ReturnType(name);
      Symbol returnSymbol = new Symbol(type);

      AddMiddleCode(shortList, MiddleOperator.SysCall, name, null);
      AddMiddleCode(longList, MiddleOperator.SysCall, name, returnSymbol);
      return (new Expression(returnSymbol, shortList, longList));
    }*/

    public static Expression RegisterExpression(Register register) {
      List<MiddleCode> longList = new List<MiddleCode>();
      int size = AssemblyCode.SizeOfRegister(register);
      Type type = TypeSize.SizeToUnsignedType(size); 
      Symbol symbol = new Symbol(type);
      AddMiddleCode(longList, MiddleOperator.InspectRegister, symbol, register);
      return (new Expression(symbol, new List<MiddleCode>(), longList, register));
    }

    public static Expression CarryFlagExpression() {
      ISet<MiddleCode> trueSet = new HashSet<MiddleCode>(),
                       falseSet = new HashSet<MiddleCode>();
      List<MiddleCode> longList = new List<MiddleCode>();
      trueSet.Add(AddMiddleCode(longList, MiddleOperator.Carry));
      falseSet.Add(AddMiddleCode(longList, MiddleOperator.Goto));
      Symbol symbol = new Symbol(trueSet, falseSet);
      return (new Expression(symbol, new List<MiddleCode>(), longList));
    }

    public static Expression StackTopExpression() {
      List<MiddleCode> longList = new List<MiddleCode>();
      Symbol symbol = new Symbol(new Type(Type.UnsignedCharType));
      AddMiddleCode(longList, MiddleOperator.StackTop, symbol);
      return (new Expression(symbol, new List<MiddleCode>(), longList));
    }

    /*public static Expression FlagbyteExpression() {
      List<MiddleCode> longList = new List<MiddleCode>();
      Symbol symbol = new Symbol(Type.UnsignedCharType);
      AddMiddleCode(longList, MiddleOperator.InspectFlagbyte, symbol);
      return (new Expression(symbol, new List<MiddleCode>(), longList));
    }*/
  }
}

/* 1. if a = b goto 2
   2. goto 3
   3. ...  
  
   a += b; => t = a + b; a = t;
 
 x += y;
  
 1. x += y
    
 1. t1 = x + y
 2. x = t1

 (i + 1) ? (s + 1) : (t + 1)
    
 1.  t1 = i + 1
 2.  if t1 != 0 goto 4
 3.  goto 6
 4.  t2 = s + 1
 5.  goto 10
 6.  t3 = t + 1
 7.  t4 = short_to_int t3
 8.  t5 = t4
 9.  goto 12
 10. t6 = short_to_int t2
 11. t5 = t6
 12. ...

 1.  t1 = i + 1
 2.  if t1 == 0 goto 7
 3.  t2 = s + 1
 4.  t3 = short_to_int t2
 5.  t4 = t3
 6.  goto 10
 7.  t5 = t + 1
 8.  t6 = short_to_int t5
 9.  t4 = t6
 10. ...
  
 1.  t1 = i + 1
 2.  if t1 == 0 goto 5
 3.  t2 = s + 1
 4.  goto 9
 5.  t3 = t + 1  
 6.  t4 = short_to_int t3
 7.  t5 = t4
 8.  goto 11
 9.  t6 = short_to_int t2
 10. t5 = t6
 11. ...

 1.  t1 = i + 1
 2.  if t1 != 0 goto 4
 3.  goto 8
 4.  t2 = s + 1
 5.  t3 = short_to_int t2
 6.  t4 = t3
 7.  goto 11
 8.  t5 = t + 1
 9.  t6 = short_to_int t5
 10. t4 = t6
 11. ...

 1.  t1 = i + 1
 2.  if t1 == 0 goto 8
 3.  nop
 4.  t2 = s + 1
 5.  t3 = short_to_int t2
 6.  t4 = t3
 7.  goto 11
 8.  t5 = t + 1
 9.  t6 = short_to_int t5
 10. t4 = t6
 11. ...

 1.  t1 = i + 1
 2.  if t1 == 0 goto 7, soft
 3.  t2 = s + 1
 4.  t3 = short_to_int t2
 5.  t4 = t3
 6.  goto 10, soft
 7.  t5 = t + 1
 8.  t6 = short_to_int t5
 9.  t4 = t6
 10. ...
*/