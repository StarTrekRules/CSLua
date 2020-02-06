using System;
using System.Collections.Generic;

namespace CSLua {
public class Token {
    public string Type;
    public string Value;
    public string Extra;

    public Token(string t, string v) {
        Type = t;
        Value = v;
    }

    public Token(string t, string v, string e) {
        Type = t;
        Value = v;
        Extra = e;
    }
}

public class Lexer {
    public int Pos = 0;
    public string Str;
    public static List<string> Keywords = new List<string>() { "then", "return", "local", "end", "function", "not", "and" };
    public static List<string> Operators = new List<string>() { "+", "/", "-", "*", "=", "==", ">", "<", ">=", "<=", ".." };

    private char Eat() {
        if (Pos >= Str.Length)
            return '\0';
        
        char c = Str[Pos];
        Pos++;
        return c;
    }

    private char Peek() {
        if (Pos >= Str.Length)
            return '\0';

        char c = Str[Pos];
        
        return c;
    }

    private void EatSpace() {
        while (Peek() == ' ' || Peek() == '\n' || Peek() == '\t') {
            Eat();
        }
    }

    private Token GetString() {
        char closer = Eat();
        string str = "";

        while (Peek() != closer) {
            str += Eat();
        }
        
        Eat();

        return new Token("String", str);
    }

    private Token GetParams() {
        Eat();
        string content = "";
        int depth = 0;

        while (depth >= 0) {
            if (Peek() == '(')
                depth++;
            
            if (Peek() == ')') {
                depth--;

                if (depth < 0)
                    break;
            }

            if (Peek() == '\0') {
              throw new Exception("Expected ')' near <EOF>");
            }

            content += Eat();
        }

        Eat();

        return new Token("Group", content);
    }

    private Token GetIndexer() {
        Eat();
        string content = "";
        int depth = 0;

        while (depth >= 0) {
            if (Peek() == '[')
                depth++;
            
            if (Peek() == ']') {
                depth--;

                if (depth < 0)
                    break;
            }

            content += Eat();
        }

        Eat();

        return new Token("Indexer", content);
    }

    private Token GetIdentifier() {
        string id = "";

        while (Peek() != ' ' && Peek() != '(' && Peek() != '\n'&& Peek() != '@'&& Peek() != ']') {
            id += Eat();
        }

        return new Token("Identifier", id);
    }

    private Token PeekIdent() {
        int pos = Pos;
        EatSpace();

        string id = "";

        while (Peek() != ' ' && Peek() != '(' && Peek() != '\n'&& Peek() != '@'&& Peek() != ']') {
            id += Eat();
        }

        Pos = pos;

        return new Token("Identifier", id);
    }

    private Token Expect(string type) {
        if (PeekToken().Type != type) {
            throw new Exception("Expected '" + type + "' got '" + GetToken().Type + "'");
        }

        return GetToken();
    }

    private Token Any(params string[] types) {
        foreach (string type in types) {
            if (PeekToken().Type == type) {
                return GetToken();
            }
        }

        throw new Exception("Expected [MULTIPLE] got '" + GetToken().Type + "'");
    }

    public Token GetBody() {
        string b = "";

        int depth = 0;
        int count = 0;

        while (depth >= 0) {
            count++;

            if (PeekIdent().Value.Trim() == "then" || PeekIdent().Value.Trim() == "do" || PeekIdent().Value.Trim() == "function") {
                depth++;
                EatSpace();
                b += " " + GetIdentifier().Value.Trim();
            }

            if (PeekIdent().Value.Trim() == "local"){
                b += GetIdentifier().Value;
                
                b += Eat();
                continue;
            }
            
            int pos = Pos;
            EatSpace();
            if (PeekIdent().Value.Trim() == "end") {
                depth--;
                
                if (depth > 0)
                    b += "\n" + GetIdentifier().Value.Trim();

                if (depth < 0)
                    break;
            
            
                b += " " + Eat();
                continue;
            }
            Pos = pos;

            if (depth < 0)
            break;
            
            
            b += Eat();
            

            
        }

        // Console.WriteLine(b);
        
        return new Token("Body", b);
    }

    public Token GetToken() {
        EatSpace();

        if (Peek() == '\0')
            return new Token("EOF", "");

        if (Peek() == '"' || Peek() == '\'') {
            return GetString();
        }

        if (Operators.Contains(PeekIdent().Value)) {
          return new Token("Operator", GetIdentifier().Value);
        }

        if (Peek() == '(') {
            return GetParams();
        }

        if (Peek() == '@') {
            Eat();

            return new Token("TypeDecl", GetIndexer().Value);
        }

        if (Peek() == '(') {
          return GetParams();
        }

        Token tok = GetIdentifier();

        if (Keywords.Contains(tok.Value)) {
            return new Token("Keyword", tok.Value);
        }

        if (PeekToken().Type == "Group") {
            return new Token("Call", tok.Value, GetToken().Value);
        }

        return tok;
    }

    public Token PeekToken() {
        int pos = Pos;
        Token tok = GetToken();
        Pos = pos;
        return tok;
    }

    public Lexer(string src) {
        Str = src;
    }
}
}