/*
    WIP
    Loops are not implemented yet
    Prone to crashing instead of giving readable errors
    So make sure there aren't any syntax errors in test.lua otherwise its more likely for the program to infinitely yield instead of erroring.
*/

using System;
using System.IO;
using CSLua;

class MainClass {

  public static void Print(IndexStack<LuaValue> Stack) {
      LuaValue val = Stack.Pop();
      Console.WriteLine(val.Value.ToString());
  }

  public static void Main (string[] args) {
      Interpreter intrp = new Interpreter();
      LuaFunction func = new LuaFunction(intrp.State, Print);
      intrp.State.Global.Set("print", func);
      intrp.State.Global.Set("true", new LuaValue(intrp.State, Lua_Type.Boolean, true));
      intrp.State.Global.Set("false", new LuaValue(intrp.State, Lua_Type.Boolean, false));

      // Quick fix to infinite loop bug if input to lexer doesn't contain a space at the end
      // TODO fix, then add comments to the rest of the code
      intrp.Evaluate(File.ReadAllText("test.lua") + " ");
  }
}