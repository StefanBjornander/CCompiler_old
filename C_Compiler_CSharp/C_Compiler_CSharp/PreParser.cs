// This code was generated by the Gardens Point Parser Generator
// Copyright (c) Wayne Kelly, John Gough, QUT 2005-2014
// (see accompanying GPPGcopyright.rtf)

// GPPG version 1.5.2
// Machine:  STEFAN1968
// DateTime: 2020-10-18 17:19:52
// UserName: Stefan
// Input file <PreParser.gppg - 2020-05-11 12:33:08>

// options: lines gplex

using System;
using System.Collections.Generic;
using System.CodeDom.Compiler;
using System.Globalization;
using System.Text;
using QUT.Gppg;

namespace CCompiler_Pre
{
public enum Tokens {error=2,EOF=3,NAME=4,STRING=5,LEFT_PARENTHESIS=6,
    RIGHT_PARENTHESIS=7,COMMA=8,SHARP=9,DOUBLE_SHARP=10,TOKEN=11,MARK=12};

public partial struct ValueType
#line 8 "PreParser.gppg"
       {
  public string name;
}
#line default
// Abstract base class for GPLEX scanners
[GeneratedCodeAttribute( "Gardens Point Parser Generator", "1.5.2")]
public abstract class ScanBase : AbstractScanner<ValueType,LexLocation> {
  private LexLocation __yylloc = new LexLocation();
  public override LexLocation yylloc { get { return __yylloc; } set { __yylloc = value; } }
  protected virtual bool yywrap() { return true; }
}

// Utility class for encapsulating token information
[GeneratedCodeAttribute( "Gardens Point Parser Generator", "1.5.2")]
public class ScanObj {
  public int token;
  public ValueType yylval;
  public LexLocation yylloc;
  public ScanObj( int t, ValueType val, LexLocation loc ) {
    this.token = t; this.yylval = val; this.yylloc = loc;
  }
}

[GeneratedCodeAttribute( "Gardens Point Parser Generator", "1.5.2")]
public partial class Parser: ShiftReduceParser<ValueType, LexLocation>
{
  // Verbatim content from PreParser.gppg - 2020-05-11 12:33:08
#line 5 "PreParser.gppg"
  // Empty.
#line default
  // End verbatim content from PreParser.gppg - 2020-05-11 12:33:08

#pragma warning disable 649
  private static Dictionary<int, string> aliases;
#pragma warning restore 649
  private static Rule[] rules = new Rule[3];
  private static State[] states = new State[3];
  private static string[] nonTerms = new string[] {
      "translation_unit", "$accept", };

  static Parser() {
    states[0] = new State(-2,new int[]{-1,1});
    states[1] = new State(new int[]{3,2});
    states[2] = new State(-1);

    for (int sNo = 0; sNo < states.Length; sNo++) states[sNo].number = sNo;

    rules[1] = new Rule(-2, new int[]{-1,3});
    rules[2] = new Rule(-1, new int[]{});
  }

  protected override void Initialize() {
    this.InitSpecialTokens((int)Tokens.error, (int)Tokens.EOF);
    this.InitStates(states);
    this.InitRules(rules);
    this.InitNonTerminals(nonTerms);
  }

  protected override void DoAction(int action)
  {
#pragma warning disable 162, 1522
    switch (action)
    {
    }
#pragma warning restore 162, 1522
  }

  protected override string TerminalToString(int terminal)
  {
    if (aliases != null && aliases.ContainsKey(terminal))
        return aliases[terminal];
    else if (((Tokens)terminal).ToString() != terminal.ToString(CultureInfo.InvariantCulture))
        return ((Tokens)terminal).ToString();
    else
        return CharToString((char)terminal);
  }

#line 22 "PreParser.gppg"
 #line default
}
}
