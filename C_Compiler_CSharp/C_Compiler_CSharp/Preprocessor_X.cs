//345678901234567890123456789012345678901234567890123456789012345678
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace CCompiler {
  public class Preprocessor {
    private IDictionary<string,Macro> m_macroMap =
              new Dictionary<string,Macro>();
    private Stack<FileInfo> m_includeStack = new Stack<FileInfo>();
    private ISet<FileInfo> m_includeSet = new HashSet<FileInfo>();
    private Stack<IfElseChain> m_ifElseChainStack = new Stack<IfElseChain>();
    private StringBuilder m_outputBuffer = new StringBuilder();
    public static IDictionary<string,Macro> MacroMap;

    public string PreprocessedCode {
      get { return m_outputBuffer.ToString(); }
    }  

    public ISet<FileInfo> IncludeSet {
      get { return m_includeSet; }
    }

    public Preprocessor(FileInfo file) {
      if (Start.Linux) {
        m_macroMap.Add("__LINUX__", new Macro(0, new List<Token>()));
      }

      if (Start.Windows) {
        m_macroMap.Add("__WINDOWS__", new Macro(0, new List<Token>()));
      }

      DoProcess(file);
      Assert.Error(m_ifElseChainStack.Count == 0, Message.
                   If___ifdef____or_ifndef_directive_without_matching_endif);
    }

    private void DoProcess(FileInfo file) {
      StreamReader streamReader = new StreamReader(file.FullName);
      StringBuilder inputBuffer =
        new StringBuilder(streamReader.ReadToEnd());
      streamReader.Close();

      CCompiler_Main.Scanner.Path = file;
      CCompiler_Main.Scanner.Line = 3000;
      GenerateTriGraphs(inputBuffer);
      TraverseBuffer(inputBuffer);    
      List<string> lineList =
        GenerateLineList(inputBuffer.ToString());

      CCompiler_Main.Scanner.Line = 1;
      int stackSize = m_ifElseChainStack.Count;
      /*if (CCompiler_Main.Scanner.Path.Name.Contains("AssertTest")) {
        Console.Out.WriteLine("5: " + CCompiler_Main.Scanner.Line);
      }*/

      TraverseLineList(lineList);
      Assert.Error(m_ifElseChainStack.Count ==
        stackSize, Message.Unbalanced_if_and_endif_directive_structure);
    }

    private static IDictionary<string,string> m_triGraphMap =
      new Dictionary<string,string>()
        {{"??=", "#"}, {"??/", "\\"}, {"??\'", "^"},
         {"??(", "["}, {"??)", "]"}, {"??!", "|"},
         {"??<", "{"}, {"??>", "}"}, {"??-", "~"}};

    public void GenerateTriGraphs(StringBuilder buffer) {
      foreach (KeyValuePair<string,string> pair in m_triGraphMap) {
        buffer.Replace(pair.Key, pair.Value);
      }

      buffer.Replace("\r\n", "\n");
      buffer.Replace("\f", " ");
      buffer.Replace("\r", " ");
      buffer.Replace("\t", " ");
      buffer.Replace("\v", " ");
      buffer.Replace("\\\n", "\r");
    }

/* private static IDictionary<char,char> TriGraphMap =
      new Dictionary<char,char>() {{'=', '#'}, {'/', '\\'}, {'\'', '^'},
                                   {'(', '['}, {')', ']'}, {'!', '|'},
                                   {'<', '{'}, {'>', '}'}, {'-', '~'}};

    public void GenerateTriGraphs(StringBuilder buffer) {
      for (int index = 0; index < (buffer.Length - 1); ++index) {
        if ((buffer[index] == '?') &&
            TriGraphMap.ContainsKey(buffer[index + 1]) &&
            !((index > 0) && (buffer[index - 1] == '\\'))) {
          buffer[index] = TriGraphMap[buffer[index + 1]];
          buffer.Remove(index + 1, 1);
        }
      }
    }*/

    public void TraverseBuffer(StringBuilder buffer) {
      buffer.Append("\0");

      for (int index = 0; index < buffer.Length; ++index) {
        if ((buffer[index] == '/') && (buffer[index + 1] == '*')) {
          buffer[index++] = ' ';
          buffer[index++] = ' ';

          for (; true; ++index) {
            if (index == buffer.Length) {
              Assert.Error(Message.Unfinished_block_comment);
            }
            else if ((buffer[index] == '*') && (buffer[index + 1] == '/')) {
              buffer[index++] = ' ';
              buffer[index] = ' ';
              break;
            }
            else if (buffer[index] == '\n') {
              ++CCompiler_Main.Scanner.Line;
            }
            else {
              buffer[index] = ' ';
            }
          }
        }
        else if ((buffer[index] == '/') && (buffer[index + 1] == '/')) {
          buffer[index++] = ' ';
          buffer[index++] = ' ';

          for (; true; ++index) {
            if ((index == buffer.Length) || (buffer[index] == '\n')) {
              break;
            }
            else {
              buffer[index] = ' ';
            }
          }
        }
        else if ((buffer[index] == '\'')) {
          ++index;

          for (; true; ++index) {
            if (index == buffer.Length) {
              Assert.Error(Message.Unfinished_character);
            }
            else if (buffer[index] == '\n') {
              Assert.Error(Message.Newline_in_character);
            }
            else if ((buffer[index] == '\\') && (buffer[index] == '\'')) {
              ++index;
            }
            else if (buffer[index] == '\'') {
              break;
            }
          }
        }
        else if ((buffer[index] == '\"')) {
          ++index;

          for (; true; ++index) {
            if (index == buffer.Length) {
              Assert.Error(Message.Unfinished_string);
            }
            else if (buffer[index] == '\n') {
              Assert.Error(Message.Newline_in_string);
            }
            else if ((buffer[index] == '\\') && (buffer[index] == '\"')) {
              ++index;
            }
            else if (buffer[index] == '\"') {
              break;
            }
          }
        }
        else if (buffer[index] == '\n') {
          ++CCompiler_Main.Scanner.Line;
        }
      }

      buffer.Remove(buffer.Length - 1, 1);
    }

    private List<string> GenerateLineList(string text) {
      List<string> lineList = new List<string>(text.Split('\n'));
      List<string> resultList = new List<string>();

      foreach (string line in lineList) {
        int returnCount = line.Split('\r').Length - 1;
        resultList.Add(line.Replace('\r', ' ').Trim());

        for (int count = 1; count < returnCount; ++count) {
          resultList.Add("");
        }
      }

      /*for (int index = lineList.Count - 1; index >= 0; --index) {
        string line = lineList[index];
        int returnCount = line.Split('\r').Length - 1;
        lineList[index] = line.Replace('\r', ' ').Trim();
        for (int count = 1; count < returnCount; ++count) {
          lineList.Insert(index + 1, "");
        }
      }*/

      return resultList;
    }

    // #define MAX(x, y) ((x) < (y)) ? (x) : (y);
    // int i = max(1, 2);

    private List<Token> Scan(string text) {
      byte[] byteArray = Encoding.ASCII.GetBytes(text);
      MemoryStream memoryStream = new MemoryStream(byteArray);
      CCompiler_Pre.Scanner scanner = new CCompiler_Pre.Scanner(memoryStream);
      List<Token> tokenList = new List<Token>();

      while (true) {
        CCompiler_Pre.Tokens tokenId = (CCompiler_Pre.Tokens) scanner.yylex();
        tokenList.Add(new Token(tokenId, scanner.yylval.name));

        if (tokenId == CCompiler_Pre.Tokens.EOF) {
          break;
        }
      }

      memoryStream.Close();
      return tokenList;
    }

    private string TokenListToString(List<Token> tokenList) {
      StringBuilder buffer = new StringBuilder();
    
      foreach (Token token in tokenList) {
        buffer.Append(((buffer.Length > 0) ? " " : "") + token.ToString());
      }
    
      return buffer.ToString();
    }

    private List<Token> CloneList(List<Token> tokenList) {
      List<Token> resultList = new List<Token>();
    
      foreach (Token token in tokenList) {
        resultList.Add((Token) token.Clone());
      }
    
      return resultList;
    }
  
    public void TraverseLineList(List<string> lineList) {
      foreach (string line in lineList) {
        List<Token> tokenList = Scan(line);

        if (tokenList[0].Id == CCompiler_Pre.Tokens.SHARP) {
          Token secondToken = tokenList[1];

          if (secondToken.Id == CCompiler_Pre.Tokens.NAME) {
            string secondTokenName = (string) secondToken.Value;

            if (secondTokenName.Equals("ifdef")) {
              DoIfDefined(tokenList);
            }
            else if (secondTokenName.Equals("ifndef")) {
              DoIfNotDefined(tokenList);
            }
            else if (secondTokenName.Equals("if")) {
              DoIf(tokenList);
            }
            else if (secondTokenName.Equals("elif")) {
              DoElseIf(tokenList);
            }
            else if (secondTokenName.Equals("else")) {
              DoElse(tokenList);
            }
            else if (secondTokenName.Equals("endif")) {
              DoEndIf(tokenList);
            }
            else if (IsVisible()) {
              if (secondTokenName.Equals("include")) {
                DoInclude(tokenList);
              }
              else if (secondTokenName.Equals("define")) {
                DoDefine(tokenList);
              }
              else if (secondTokenName.Equals("undef")) {
                DoUndef(tokenList);
              }
              else if (secondTokenName.Equals("line")) {
                DoLine(tokenList);
              }
              else if (secondTokenName.Equals("error")) {
                Assert.Error(TokenListToString
                             (tokenList.GetRange(1, tokenList.Count - 1)));
              }
            }
          }
        }
        else {
          if (IsVisible()) {
            SearchForMacros(tokenList, new Stack<string>());
            ConcatTokens(tokenList);
            MergeStrings(tokenList);
            AddTokenListToBuffer(tokenList);
          }
        }

        m_outputBuffer.Append("\n");
        ++CCompiler_Main.Scanner.Line;
      }
    }

    private void AddTokenListToBuffer(List<Token> tokenList) {
      bool first = true;

      foreach (Token token in tokenList) {
        m_outputBuffer.Append((first ? "" : " ") + token.ToString());
        first = false;
      }

      m_outputBuffer.Append("\n");
    }
  
    /*private void AddNewlinesToBuffer(List<Token> tokenList) {
      foreach (Token token in tokenList) {
        m_outputBuffer.Append(token.ToNewlineString());
        CCompiler_Main.Scanner.Line += token.GetNewlineCount();
      }
    }*/
  
    private bool IsVisible() {
      foreach (IfElseChain ifElseChain in m_ifElseChainStack) {
        if (!ifElseChain.CurrentStatus) {          
          return false;
        }
      }
      
      return true;
    }

    private void DoLine(List<Token> tokenList) {
      int listSize = tokenList.Count;
    
      if ((listSize == 4) || (listSize == 5)) {
        string lineText = (string) tokenList[2].Value;
        Assert.Error(int.TryParse(lineText, out CCompiler_Main.Scanner.Line),
                                  lineText, Message.Invalid_line_number);

        if (listSize == 5) {
          CCompiler_Main.Scanner.Path =
            new FileInfo((string) tokenList[3].Value);
        }
      }
      else {
        Assert.Error(listSize == 3, TokenListToString(tokenList),
                     Message.Invalid_preprocessor_directive);
      }

      m_outputBuffer.Append(Symbol.SeparatorId + CCompiler_Main.Scanner.Path +
                            "," + CCompiler_Main.Scanner.Line +
                            Symbol.SeparatorId + "\n");
    }

    // ------------------------------------------------------------------------

    private void DoInclude(List<Token> tokenList)
    {
      FileInfo includeFile = null;

      if ((tokenList[2].Id == CCompiler_Pre.Tokens.STRING) &&
          (tokenList[3].Id == CCompiler_Pre.Tokens.EOF)) {
        string text = tokenList[2].ToString();
        string file = text.ToString().Substring(1, text.Length - 1);
        includeFile = new FileInfo(Start.SourcePath + file);
      }
      else {
        StringBuilder buffer = new StringBuilder();

        foreach (Token token in tokenList.GetRange(2, tokenList.Count - 2)) {
          buffer.Append(token.ToString());
        }

        string text = buffer.ToString();

        if (text.StartsWith("<") && text.EndsWith(">")) {
          string file = text.ToString().Substring(1, text.Length - 2);
          includeFile = new FileInfo(Start.SourcePath + file);
        }
        else {
          Assert.Error(TokenListToString(tokenList),
                       Message.Invalid_preprocessor_directive);
        }
      }

      Assert.Error(!m_includeStack.Contains(includeFile),
                   includeFile.FullName, Message.Repeted_include_statement);
      m_includeStack.Push(includeFile);
      m_includeSet.Add(includeFile);
      FileInfo oldPath = CCompiler_Main.Scanner.Path;
      int oldLine = CCompiler_Main.Scanner.Line;
      CCompiler_Main.Scanner.Path = includeFile;
      CCompiler_Main.Scanner.Line = 1;
      m_outputBuffer.Append(Symbol.SeparatorId + CCompiler_Main.Scanner.Path +
                            ",0" + Symbol.SeparatorId + "\n");
      DoProcess(includeFile);
      CCompiler_Main.Scanner.Line = oldLine;
      CCompiler_Main.Scanner.Path = oldPath;
      m_outputBuffer.Append(Symbol.SeparatorId + CCompiler_Main.Scanner.Path +
                            "," + (CCompiler_Main.Scanner.Line - 1) +
                            Symbol.SeparatorId + "\n");
      m_includeStack.Pop();
    }

    // ------------------------------------------------------------------------

    public void DoDefine(List<Token> tokenList) {
      Assert.Error(tokenList[2].Id == CCompiler_Pre.Tokens.NAME,
                   TokenListToString(tokenList),
                   Message.Invalid_define_directive);
      string name = tokenList[2].ToString();
      Macro macro;

      if ((tokenList[3].Id == CCompiler_Pre.Tokens.LEFT_PARENTHESIS) &&
          !tokenList[3].HasWhitespace()) {
        int tokenIndex = 4, paramIndex = 0;
        IDictionary<string,int> paramMap = new Dictionary<string,int>();

        while (true) {
          Token nextToken = tokenList[tokenIndex++];
          Assert.Error(nextToken.Id == CCompiler_Pre.Tokens.NAME,
                       nextToken.ToString(),
                       Message.Invalid_macro_definitializerion);
          string paramName = (string) nextToken.Value;
          Assert.Error(!paramMap.ContainsKey(paramName),
                       paramName, Message.Repeated_macro_parameter);
          paramMap.Add(paramName, paramIndex++);

          nextToken = tokenList[tokenIndex++];
          if (nextToken.Id == CCompiler_Pre.Tokens.COMMA) {
            // Empty.
          }
          else if (nextToken.Id == CCompiler_Pre.Tokens.RIGHT_PARENTHESIS) {
            break;
          }
          else {
            Assert.Error(nextToken.ToString(),
                         Message.Invalid_macro_definitializerion);
          }
        }
      
        List<Token> macroList =
          tokenList.GetRange(tokenIndex, tokenList.Count - tokenIndex);

        foreach (Token macroToken in macroList) {
          if (macroToken.Id == CCompiler_Pre.Tokens.NAME) {
            string macroName = (string) macroToken.Value;

            if (paramMap.ContainsKey(macroName)) {
              macroToken.Id = CCompiler_Pre.Tokens.MARK;
              macroToken.Value = paramMap[macroName];
            }
          }
        }
      
        macro = new Macro(paramMap.Count, macroList);
      }
      else {
        macro = new Macro(0, tokenList.GetRange(3, tokenList.Count - 3));
      }
    
      if (!m_macroMap.ContainsKey(name)) {
        m_macroMap.Add(name, macro);
      }
      else {
        Assert.Error(m_macroMap[name].Equals(macro), name,
                     Message.Invalid_macro_redefinitializerion);
      }
    }

    public void DoUndef(List<Token> tokenList) {
      Assert.Error((tokenList[2].Id == CCompiler_Pre.Tokens.NAME) &&
                   (tokenList[3].Id == CCompiler_Pre.Tokens.EOF),
                   TokenListToString(tokenList),
                   Message.Invalid_undef_directive);
      string name = tokenList[2].ToString();
      Assert.Error(m_macroMap.Remove(name), name,
                   Message.Macro_not_defined);
    }

    // ------------------------------------------------------------------------

    private void DoIf(List<Token> tokenList) {
      bool result = ParseExpression(TokenListToString
                         (tokenList.GetRange(2, tokenList.Count - 2)));
      m_ifElseChainStack.Push(new IfElseChain(result, result, false));
    }

    public static object PreProcessorResult;
  
    private bool ParseExpression(string line) {    
      int result = 0;

      try {
        byte[] byteArray = Encoding.ASCII.GetBytes(line);
        MemoryStream memoryStream = new MemoryStream(byteArray);
        CCompiler_Exp.Scanner expressionScanner =
          new CCompiler_Exp.Scanner(memoryStream);
        MacroMap = m_macroMap;
        CCompiler_Exp.Parser expressionParser =
          new CCompiler_Exp.Parser(expressionScanner, m_macroMap);
        Assert.Error(expressionParser.Parse(), Message.Preprocessor_parser);
        result = (int) PreProcessorResult;
        memoryStream.Close();
      }
      catch (Exception exception) {
        Console.Out.WriteLine(exception.StackTrace);
        Assert.Error(line, Message.Invalid_expression);
      }

      return (result != 0);
    }

    private void DoIfDefined(List<Token> tokenList) {
      Assert.Error((tokenList[2].Id == CCompiler_Pre.Tokens.NAME) &&
                   (tokenList[3].Id == CCompiler_Pre.Tokens.EOF),
                   TokenListToString(tokenList),
                   Message.Invalid_preprocessor_directive);
      bool result = m_macroMap.ContainsKey((string)tokenList[2].Value);
      m_ifElseChainStack.Push(new IfElseChain(result, result, false));
    }

    private void DoIfNotDefined(List<Token> tokenList) {
      Assert.Error((tokenList[2].Id == CCompiler_Pre.Tokens.NAME) &&
                   (tokenList[3].Id == CCompiler_Pre.Tokens.EOF),
                   TokenListToString(tokenList),
                   Message.Invalid_preprocessor_directive);
      bool result = !m_macroMap.ContainsKey((string)tokenList[2].Value);
      m_ifElseChainStack.Push(new IfElseChain(result, result, false));
    }

    private void DoElseIf(List<Token> tokenList) {
      Assert.Error(m_ifElseChainStack.Count > 0, Message.
       Elif_directive_without_preceeding_if____ifdef____or_ifndef_directive);
      IfElseChain ifElseChain = m_ifElseChainStack.Pop();

      Assert.Error(!ifElseChain.ElseStatus,
                   Message.Elif_directive_following_else_directive);

      if (ifElseChain.FormerStatus) {
        m_ifElseChainStack.Push(new IfElseChain(true, false, false));
      }
      else {
        bool result =
          ParseExpression(TokenListToString
                         (tokenList.GetRange(2, tokenList.Count - 2)));
        m_ifElseChainStack.Push(new IfElseChain(result, result, false));
      }
    }

    private void DoElse(List<Token> tokenList) {
      Assert.Error(m_ifElseChainStack.Count > 0, Message.
       Else_directive_without_preceeding_if____ifdef____or_ifndef_directive);
      Assert.Error(tokenList[2].Id == CCompiler_Pre.Tokens.EOF,
                   TokenListToString(tokenList),
                   Message.Invalid_preprocessor_directive);

      IfElseChain ifElseChain = m_ifElseChainStack.Pop();
      Assert.Error(!ifElseChain.ElseStatus,
                   Message.Else_directive_after_else_directive);

      bool formerStatus = ifElseChain.FormerStatus;
      m_ifElseChainStack.Push(new IfElseChain(!formerStatus,
                                              !formerStatus, true));
    }

    private void DoEndIf(List<Token> tokenList) {
      Assert.Error(m_ifElseChainStack.Count > 0, Message.
      Endif_directive_without_preceeding_if____ifdef____or_ifndef_directive);
      Assert.Error(tokenList[2].Id == CCompiler_Pre.Tokens.EOF,
                   tokenList[2].ToString(),
                   Message.Invalid_preprocessor_directive);
      m_ifElseChainStack.Pop();
    }

    // ------------------------------------------------------------------------

    private void SearchForMacros(List<Token> tokenList,
                                 Stack<string> nameStack) {
      for (int index = 0; index < tokenList.Count; ++index) {
        Token thisToken = tokenList[index];
        CCompiler_Main.Scanner.Line += thisToken.GetNewlineCount();

        if (thisToken.Id == CCompiler_Pre.Tokens.NAME) {
          string name = (string) thisToken.Value;
          int beginNewlineCount = thisToken.GetNewlineCount();

          if (!nameStack.Contains(name) && m_macroMap.ContainsKey(name)) {            
            Token nextToken = tokenList[index + 1];

            if ((nextToken.Id == CCompiler_Pre.Tokens.LEFT_PARENTHESIS) &&
                !nextToken.HasWhitespace()) {
              int countIndex = index + 2, level = 1, totalNewlineCount = 0;
              List<Token> subList = new List<Token>();
              List<List<Token>> mainList = new List<List<Token>>();
        
              while (true) {
                nextToken = tokenList[countIndex];
                int newlineCount = nextToken.GetNewlineCount();
                totalNewlineCount += newlineCount;
                CCompiler_Main.Scanner.Line += newlineCount;
                nextToken.ClearNewlineCount();
              
                Token token = tokenList[countIndex];
                Assert.Error(token.Id != CCompiler_Pre.Tokens.EOF,
                             Message.Invalid_end_of_macro_call);
              
                switch (token.Id) {
                  case CCompiler_Pre.Tokens.LEFT_PARENTHESIS:
                    ++level;
                    subList.Add(token);
                    break;
                  
                  case CCompiler_Pre.Tokens.RIGHT_PARENTHESIS:
                    if ((--level) > 0) {
                      subList.Add(token);
                    }
                    break;
                  
                  default:
                    if ((level == 1) &&
                        (token.Id == CCompiler_Pre.Tokens.COMMA)) {
                      Assert.Error(subList.Count > 0, name,
                                   Message.Empty_macro_parameter);
                      SearchForMacros(subList, nameStack); // XXX
                      mainList.Add(subList);
                      subList = new List<Token>();
                    }
                    else {
                      subList.Add(token);
                    }
                    break;
                }
              
                if (level == 0) {
                  Assert.Error(subList.Count > 0, name,
                               Message.Empty_macro_parameter_list);
                  mainList.Add(subList);
                  break;
                }
              
                ++countIndex;
              }

              Macro macro = m_macroMap[name];
              Assert.Error(macro.Parameters() == mainList.Count, name,
                         Message.Invalid_number_of_parameters_in_macro_call);
            
              List<Token> cloneListX = CloneList(macro.TokenList());
            
              for (int macroIndex = (cloneListX.Count - 1);
                   macroIndex >= 0; --macroIndex) {
                Token macroToken = cloneListX[macroIndex];

                if (macroToken.Id == CCompiler_Pre.Tokens.MARK) {
                  int markIndex = (int) macroToken.Value;
                  cloneListX.RemoveAt(macroIndex);
                  List<Token> replaceList = CloneList(mainList[markIndex]);

                  if ((macroIndex > 0) && (cloneListX[macroIndex - 1].Id ==
                                           CCompiler_Pre.Tokens.SHARP)) {
                    string text = "\"" + TokenListToString(replaceList) + "\"";
                    cloneListX.Insert(macroIndex,
                                new Token(CCompiler_Pre.Tokens.STRING, text));
                    cloneListX.RemoveAt(--macroIndex);
                  }
                  else {
                    cloneListX.InsertRange(macroIndex, replaceList);
                  }
                }              
              }

              nameStack.Push(name);
              SearchForMacros(cloneListX, nameStack);
              nameStack.Pop();

              tokenList.RemoveRange(index, countIndex - index + 1);
              tokenList.InsertRange(index, cloneListX);
              tokenList[index].AddNewlineCount(beginNewlineCount);
              tokenList[index +
                        cloneListX.Count].AddNewlineCount(totalNewlineCount);
              index += cloneListX.Count - 1;
            }
            else {
              Macro macro = m_macroMap[name];
              Assert.Error(macro.Parameters() == 0, name, Message.
                           Invalid_number_of_parameters_in_macro_call);
              List<Token> cloneListX = CloneList(macro.TokenList());
              nameStack.Push(name);
              SearchForMacros(cloneListX, nameStack);
              nameStack.Pop();

              tokenList.RemoveAt(index);
              tokenList.InsertRange(index, cloneListX);
              tokenList[index].AddNewlineCount(beginNewlineCount);
              index += cloneListX.Count - 1;
            }
          }
          else if (name.Equals("__STDC__")) {
            tokenList[index] =
              new Token(CCompiler_Pre.Tokens.TOKEN, 1, beginNewlineCount);
          }
          else if (name.Equals("__FILE__")) {
            string text = "\"" + CCompiler_Main.Scanner.Path
                          .FullName.Replace("\\", "\\\\") + "\"";
            tokenList[index] =
              new Token(CCompiler_Pre.Tokens.TOKEN, text, beginNewlineCount);
          }
          else if (name.Equals("__LINE__")) {
            tokenList[index] =
              new Token(CCompiler_Pre.Tokens.TOKEN,
                        CCompiler_Main.Scanner.Line, beginNewlineCount);
          }
          else if (name.Equals("__DATE__")) {
            string text = "\"" + DateTime.Now.ToString("MMMM dd yyyy") + "\"";
            tokenList[index] =
              new Token(CCompiler_Pre.Tokens.TOKEN, text, beginNewlineCount);
          }
          else if (name.Equals("__TIME__")) {
            string text = "\"" + DateTime.Now.ToString("HH:mm:ss") + "\"";
            tokenList[index] =
              new Token(CCompiler_Pre.Tokens.TOKEN, text, beginNewlineCount);
          }
        }
      }
    }
 
    private void ConcatTokens(List<Token> tokenList) {
      for (int index = 1; index < (tokenList.Count - 1); ++index) {
        Token thisToken = tokenList[index];

        if (thisToken.Id == CCompiler_Pre.Tokens.DOUBLE_SHARP) {
          Token prevToken = tokenList[index - 1],
                nextToken = tokenList[index + 1];

          if ((prevToken.Id == CCompiler_Pre.Tokens.STRING) ||
              (nextToken.Id == CCompiler_Pre.Tokens.STRING)) {
            nextToken.AddNewlineCount(thisToken.GetNewlineCount());
            tokenList.RemoveAt(index);
          }
          else {
            prevToken.Value = prevToken.ToString() + nextToken.ToString();
            prevToken.AddNewlineCount(thisToken.GetNewlineCount() +
                                      nextToken.GetNewlineCount());
            tokenList.RemoveAt(index);
            tokenList.RemoveAt(index);
          }
        }
      }
    }

    private void MergeStrings(List<Token> tokenList) {
      for (int index = (tokenList.Count - 2); index >= 0; --index) {
        Token thisToken = tokenList[index], nextToken = tokenList[index + 1];
            
        if ((thisToken.Id == CCompiler_Pre.Tokens.STRING) &&
            (nextToken.Id == CCompiler_Pre.Tokens.STRING)) {
          string thisText = thisToken.ToString(),
                 nextText = nextToken.ToString();
          thisToken.Value =
            thisText.ToString().Substring(0, thisText.Length - 1) +
            nextText.ToString().Substring(1, nextText.Length - 1);
          thisToken.AddNewlineCount(nextToken.GetNewlineCount());
          tokenList.RemoveAt(index + 1);
        }
      }
    }
  }
}
