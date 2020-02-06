using System;
using System.Collections.Generic;
using CSLua;

public class Compiler : TreeTraverser {
        public Tree AST;
        public Lua_State State;
        public string Source = "";

        public LuaValue Visit(Node node) {
            if (Lexer.Operators.Contains(node.Value.Value)) {
                // Resolved left and resolved right
                LuaValue resl = Visit(node.Left);
                LuaValue resr = Visit(node.Right);

                double? l = resl.Number();
                double? r = resr.Number();

                string op = node.Value.Value;

                if (op == "=") {
                  Source += $"mov dword [{resl.String()}], {l+r}";
                }
            }

            // Its a value.

            if (node.Value.Type == "Group") {
              Parser grpprs = new Parser(node.Value.Value + " ");

              return Visit(grpprs.Expression());
            }

            double num;

            if (double.TryParse(node.Value.Value, out num))
                return new LuaValue(State, Lua_Type.Number, num);

            return State.Current.Get(node.Value.Value) ?? new LuaValue(State, Lua_Type.Any, node.Value.Value);
        }

        public LuaValue Evaluate(string code) {
            AST = new Parser(code).Parse();

            try {
            for (int i = 0; i < AST.Entry.Count - 2; i++) {
                Visit(AST.Entry[i]);
            }
            }
            catch (ReturnThrow) {
                return State.Pop();
            }

            try {
            return Visit(AST.Entry[AST.Entry.Count - 2]);
            }
            catch (ReturnThrow) {
                return State.Pop();
            }
        }

        public Compiler() {
          
        }
}