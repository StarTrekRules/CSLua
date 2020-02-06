using System;
using System.Collections.Generic;

namespace CSLua {

    public enum Lua_Type {
        Number,
        String,
        Function,
        Boolean,
        Any
    }

    // Thrown to interrupt execution of functions when a return statement is executed
    public class ReturnThrow : Exception {
        public ReturnThrow() : base("Not implemented") {

        }
    }

    public class LuaValue {
        public Lua_Type Type;
        public object Value;
        public Lua_State State;

        public double? Number() {
            double? val = Value as double?;

            if (Value != null && val == null) {
                return State.Current.Get(Value.ToString())?.Number();
            }

            return val;
        }

        public string ExplicitString() {
            return null;
        }

        public string String() {
            string val = Value as string;

            if (Value != null && val == null) {
                return State.Current.Get(Value.ToString())?.String();
            }

            return val;
        }

        public LuaValue Compare(LuaValue other) {
            if (other.Type == Lua_Type.Number) {
                double? thisnum = Number();

                if (thisnum == null) {
                    if (Type == Lua_Type.String) {

                    }
                }
            }

            return null;
        }

        public LuaValue(Lua_State st, Lua_Type type, object value) {
            State = st;
            Type = type;
            Value = value;
        }
    }

    public delegate void Lua_Delegate(IndexStack<LuaValue> LuaStack);

    public class LuaFunction : LuaValue {
        public Tree Body;

        public List<string> ParamNames = new List<string>();

        public Lua_Delegate CSNative;

        public LuaFunction(Lua_State st, string source) : base(st, Lua_Type.Function, null) {
            Parser parse = new Parser(source);
            Body = parse.Parse();
        }

        public LuaFunction(Lua_State st, Lua_Delegate del) : base(st, Lua_Type.Function, null) {
            CSNative = del;
        }
    }

    public class IndexStack<T> {
        public List<T> Elements = new List<T>();
        public int Pos = -1;

        public void Push(T element) {
            Elements.Add(element);
            Pos++;
        }

        public T Pop() {
            T el = Elements[Pos];
            Elements.RemoveAt(Pos);
            Pos--;
            return el;
        }

        public T this[int ind] {
            get {
                return Elements[ind];
            }
        }

        public int Count {
            get {
                return Elements.Count;
            }
        }
    }

    public class Scope {
        public Dictionary<string, LuaValue> Locals = new Dictionary<string, LuaValue>();
        public Scope Parent;

        public LuaValue Get(string name) {
            LuaValue val;
            Locals.TryGetValue(name, out val);

            if (val == null && Parent != null)
                val = Parent.Get(name);

            if (val == null)
                return null;

            return val;
        }

        public Scope GetDef(string name) {
            LuaValue val;
            Locals.TryGetValue(name, out val);
            Scope sc = this;

            if (val == null && Parent != null) {
                val = Parent.Get(name);

                if (val == null)
                    return sc;

                sc = Parent.GetDef(name);
            }

            return sc;
        }

        public void Set(string name, LuaValue value) {
            if (Get(name) == null)
                Locals.Add(name, value);
            else
                GetDef(name).Locals[name] = value;
        }
    }

    public class Lua_State {
        public IndexStack<LuaValue> MainStack = new IndexStack<LuaValue>();
        public Interpreter Visitor;
        public Scope Global = new Scope();
        public Scope Current;

        public void PushNumber(double num) {
            MainStack.Push(new LuaValue(this, Lua_Type.Number, num));
        }

        public string ToString(int index) {
            return MainStack[MainStack.Count - index].Value.ToString();
        }

        public LuaValue Pop(int num = 1) {
            while (num > 1) {
                MainStack.Pop();
                num--;
            }

            return MainStack.Pop();
        }

        public void Call(string name, int args = 0) {
            Scope context = new Scope();
            context.Parent = Current;
            Current = context;
            LuaValue val = MainStack[MainStack.Count - args - 1];
            
            if (! (val is LuaFunction)) {
                Console.WriteLine("CSLua:?: Attempt to call a " + val.Type.ToString().ToLower() + " value (global '" + name + "')");

                // Clean the stack
                Pop(args);

                MainStack.Pop();
                return;
            }

            LuaFunction func = (LuaFunction) val;

            if (func.CSNative != null) {
                func.CSNative(MainStack);
                return;
            }

            if (args > 0) {
              for (int i = func.ParamNames.Count - 1; i >= 0; i--) {
                  Current.Set(func.ParamNames[i], Pop());
              }
            }
            
            MainStack.Pop();

            try {
                foreach (Node node in (val as LuaFunction).Body.Entry) {
                    Visitor.Visit(node);
                }
            }
            catch (ReturnThrow) {}

            Current = context.Parent;
        }

        public Lua_State(Interpreter vis) {
            Visitor = vis;
            Current = Global;
        }
    }

    public interface TreeTraverser {
        LuaValue Visit(Node n);
    }

    public class Interpreter : TreeTraverser {
        public Tree AST;
        public Lua_State State;

        public LuaValue Visit(Node node) {
            if (node.Value.Type == "Operator") {
                // Resolved left and resolved right
                LuaValue resl = Visit(node.Left);
                LuaValue resr = Visit(node.Right);

                double? l = resl.Number();
                double? r = resr.Number();

                switch (node.Value.Value) {
                    case "+":
                        return new LuaValue(State, Lua_Type.Number, l + r);
                    case "-":
                        return new LuaValue(State, Lua_Type.Number, l - r);
                    case "/":
                        return new LuaValue(State, Lua_Type.Number, l / r);
                    case "*":
                        return new LuaValue(State, Lua_Type.Number, l * r);
                    case "==":
                        return new LuaValue(State, Lua_Type.Boolean, resl.Value.ToString() == resr.Value.ToString());
                    case ">":
                        return new LuaValue(State, Lua_Type.Boolean, resl.Number() > resr.Number());
                    case "<":
                        return new LuaValue(State, Lua_Type.Boolean, resl.Number() < resr.Number());
                    case ">=":
                        return new LuaValue(State, Lua_Type.Boolean, resl.Number() >= resr.Number());
                    case "<=":
                        return new LuaValue(State, Lua_Type.Boolean, resl.Number() <= resr.Number());
                    case "..":
                        return new LuaValue(State, Lua_Type.String, resl.String() + resr.String());
                    case "=":
                        if (node.Attributes.Contains("Local"))
                            State.Current.Set(node.Left.Value.Value, resr);
                        else
                            State.Global.Set(node.Left.Value.Value, resr);
                        
                        return new LuaValue(State, Lua_Type.Any, null);
                    default:
                        return new LuaValue(State, Lua_Type.Any, null);
                }
            }

            if (node.Value.Type == "Call") {
                LuaValue val = State.Current.Get(node.Value.Value);

                if (val == null) {
                    Console.WriteLine("CSLua:?: Attempt to call a nil value (global '" + node.Value.Value + "')");
                    return new LuaValue(State, Lua_Type.Any, null);
                }
                
                string[] args = node.Value.Extra.Split(',');
                
                State.MainStack.Push(val);
                
                foreach (string arg in args) {
                    Parser parser = new Parser(arg + " ");

                    State.MainStack.Push(Visit(parser.Expression()));
                }
                
                State.Call(node.Value.Value, args.Length);
                return State.Pop();
            }

            if (node.Type == "Function") {
                LuaFunction func = new LuaFunction(State, node.Custom[2].Value.Value + " ");

                foreach (string par in node.Custom[1].Value.Value.Split(',')) {
                    func.ParamNames.Add(par.Trim());
                }

                if (node.Attributes.Contains("Local"))
                    State.Current.Set(node.Custom[0].Value.Value, func);
                else
                    State.Global.Set(node.Custom[0].Value.Value, func);
                
                return new LuaValue(State, Lua_Type.Any, null);
            }

            if (node.Type == "Not") {
                return new LuaValue(State, Lua_Type.Boolean, ! (Visit(node.Custom[0]).Value as bool?));
            }

            if (node.Type == "And") {
                return new LuaValue(State, Lua_Type.Boolean, ((Visit(node.Custom[0]).Value as bool?) == true) && ((Visit(node.Custom[1]).Value as bool?) == true));
            }

            if (node.Type == "If") {
                LuaValue cond = Visit(node.Custom[0]);
                
                Tree Body = new Parser(node.Custom[1].Value.Value + " ").Parse();
                
                if ((cond.Value as bool?) == true) {
                    foreach (Node node2 in Body.Entry) {
                        Visit(node2);
                    }
                }
                
                return new LuaValue(State, Lua_Type.Any, null);
            }

            if (node.Type == "While") {
                LuaValue cond = Visit(node.Custom[0]);
                
                Tree Body = new Parser(node.Custom[1].Value.Value + " ").Parse();
                
                while ((cond.Value as bool?) == true || ((cond.Value as bool?) == null && cond.Number() != 0)) {
                    foreach (Node node2 in Body.Entry) {
                        Visit(node2);
                    }

                    cond = Visit(node.Custom[0]);
                }
                
                return new LuaValue(State, Lua_Type.Any, null);
            }

            if (node.Type == "Return") {
                State.MainStack.Push(Visit(node.Custom[0]));
                throw new ReturnThrow();
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

        public Interpreter() {
            State = new Lua_State(this);
        }
    }
}