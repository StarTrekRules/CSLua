using System;
using System.Collections.Generic;

namespace CSLua {
    public class Node {
        public Node Left;
        public Token Value;
        public Node Right;
        public string Type;
        public List<Node> Custom = new List<Node>();
        public List<string> Attributes = new List<string>();

        public Node() {}
        public Node(Token val) {
            Value = val;
        }
    }

    public class Tree {
        public List<Node> Entry = new List<Node>();
    }

    public class Parser {
        public Lexer Lex;
        public List<string> exprops = new List<string>() { "-", "+", "==", ">", "<", ">=", "<=", ".." };
        public List<string> stmtops = new List<string>() { "=" };
        public List<string> termops = new List<string>() { "*", "/" };

        public Tree Parse() {
            Tree tree = new Tree();
            
            do {
                tree.Entry.Add(Statement());
            } while (tree.Entry[tree.Entry.Count - 1].Value.Type != "EOF");

            return tree;
        }

        public Node Statement() {
            if (Lex.PeekToken().Value == "if") {
                Lex.GetToken();
                Node exp = Logical(new string[] { "then" });
                Node n = new Node();
                n.Value = new Token("Identifier", "null");
                n.Custom.Add(exp);
                Lex.GetToken();
                n.Custom.Add(new Node(Lex.GetBody()));
                n.Type = "If";
                return n;
            }

            if (Lex.PeekToken().Value == "while") {
                Lex.GetToken();
                Node exp = Logical(new string[] { "do" });
                
                Node n = new Node();
                n.Value = new Token("Identifier", "null");
                n.Custom.Add(exp);
                Lex.GetToken();
                n.Custom.Add(new Node(Lex.GetBody()));
                n.Type = "While";
                return n;
            }

            if (Lex.PeekToken().Value == "return") {
                Lex.GetToken();
                Node n = new Node();
                n.Value = new Token("Identifier", "null");
                n.Custom.Add(Logical(new string[] { "end" }));
                n.Type = "Return";

                return n;
            }

            // If the next token is local then the next tokens must be a function or variable declaration
            bool local = false;

            if (Lex.PeekToken().Value == "local") {
                local = true;
                Lex.GetToken();
            }

            if (Lex.PeekToken().Value == "function") {
                Lex.GetToken();
                Node n = new Node();
                n.Type = "Function";
                n.Value = new Token("Identifier", "null");
                Token info = Lex.GetToken();
                n.Custom.Add(new Node(new Token("Identifier", info.Value)));
                n.Custom.Add(new Node(new Token("Group", info.Extra)));
                
                n.Custom.Add(new Node(Lex.GetBody()));

                if (local)
                    n.Attributes.Add("Local");

                return n;
            }

            bool assigned = false;
            Node n2 = Logical();

            while (stmtops.Contains(Lex.PeekToken().Value)) {
                Token op = Lex.GetToken();

                if (op.Value == "=")
                    assigned = true;

                Node lf = n2;

                n2 = new Node();
                n2.Left = lf;
                n2.Value = op;
                if (local)
                    n2.Attributes.Add("Local");
                n2.Right = Logical();
            }

            if (local && ! assigned)
                throw new Exception("Expected function or variable declaration after 'local'");

            return n2;
        }

        private bool TerminatesExpression(Token tok, string[] termn) {

            if (termn != null) {
                foreach (string str in termn) {
                    if (tok.Value == str) {
                        return true;
                    }
                }
            }

            if (tok.Value == "then")
                return true;

            if (tok.Value == "do")
                return true;

            if (tok.Value == "and")
                return true;

            if (tok.Value == "end")
                return true;

            return false;
        }

        public Node Logical(string[] eterm = null, bool skipand = false) {
            // Check for logical operations
            int rewindto = Lex.Pos;

            if (Lex.PeekToken().Value == "not") {
                Lex.GetToken();
                Node n2 = new Node();
                n2.Type = "Not";
                n2.Value = new Token("Identifier", "null");
                n2.Custom.Add(Logical(null, true));

                if (Lex.PeekToken().Value == "and") {
                    Lex.GetToken();
                    Node n3 = new Node();
                    n3.Type = "And";
                    n3.Value = new Token("Identifier", "null");
                    n3.Custom.Add(n2);
                    n3.Custom.Add(Logical());

                    return n3;
                }
                
                return n2;
            }

            if (! skipand) {
                Node left = Expression(eterm);

                if (Lex.PeekToken().Value == "and") {
                    Lex.GetToken();

                    Node n3 = new Node();
                    n3.Type = "And";
                    n3.Value = new Token("Identifier", "null");
                    n3.Custom.Add(left);
                    n3.Custom.Add(Logical());

                    return n3;
                }

                Lex.Pos = rewindto; // No logical op here, rewind.
            }

            return Expression(eterm);
        }

        public Node Expression(string[] termn = null) {
            Node n = Term();

            while (exprops.Contains(Lex.PeekToken().Value) && ! TerminatesExpression(Lex.PeekToken(), termn)) {
                Token op = Lex.GetToken();

                Node lf = n;

                n = new Node();
                
                n.Left = lf;
                n.Value = op;
                n.Right = Term();
            }

            return n;
        }

        public Node Term() {
            Node n = Unary();

            while (termops.Contains(Lex.PeekToken().Value) && ! TerminatesExpression(Lex.PeekToken(), null)) {
                Token op = Lex.GetToken();

                Node lf = n;

                n = new Node();
                n.Left = lf;
                n.Value = op;
                n.Right = Factor();
            }

            return n;
        }

        public Node Unary() {
            Node n = Factor();

            

            return n;
        }

        public Node Factor() {
            Token prim = Primary();

            if (Lex.PeekToken().Type == "TypeDecl") {
                Node na = new Node(prim);
                na.Attributes.Add("TypeDecl");
                na.Attributes.Add(Lex.GetToken().Value);

                return na;
            }

            return new Node(prim);
        }

        public Token Primary() {
            return Lex.GetToken();
        }

        public Parser(string str) {
            Lex = new Lexer(str);
        }
    }
}