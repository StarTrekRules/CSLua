/*
    WIP
    Loops are not implemented yet
    Prone to crashing instead of giving readable errors
    So make sure there aren't any syntax errors in test.lua otherwise its more likely for the program to infinitely yield instead of erroring.
*/

using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using CSLua;

class HttpBinding {
  public string Path;
  public LuaFunction Function;
}

class MainClass {

  public static Interpreter Intrp = new Interpreter();

  public static List<HttpBinding> Bindings = new List<HttpBinding>();

  public static void Print(IndexStack<LuaValue> Stack) {
      LuaValue val = Stack.Pop();
      Console.WriteLine(val.String());
  }

  static HttpListener Listener = new HttpListener();

  public static void Listen(IndexStack<LuaValue> Stack) {
      LuaFunction func = (LuaFunction) Stack.Pop();
      LuaValue val = Stack.Pop();

      HttpBinding binding = new HttpBinding();

      binding.Path = val.String();

      binding.Function = func;

      Bindings.Add(binding);
  }

  public static void ListenerThread() {
    while (true) {
      HttpListenerContext context = Listener.GetContext();

      HttpListenerRequest request = context.Request;
      HttpListenerResponse response = context.Response;
        
      Stream strm = response.OutputStream;

      HttpBinding binding = Bindings.Find(bind => bind.Path == request.Url.AbsolutePath);

      lock (Intrp) {
        Intrp.State.MainStack.Push(binding.Function);
        Intrp.State.Call("[Unknown]");

        LuaValue ret = Intrp.State.MainStack.Pop();

        byte[] bytes = Encoding.UTF8.GetBytes(ret.String());

        strm.Write(bytes, 0, bytes.Length);

        strm.Close();
      }
    }
  }

  public static void Main (string[] args) {
      Listener.Prefixes.Add("http://*:8080/");
      Listener.Start();

      LuaFunction func = new LuaFunction(Intrp.State, Print);
      LuaFunction func2 = new LuaFunction(Intrp.State, Listen);
      
      Intrp.State.Global.Set("print", func);
      Intrp.State.Global.Set("listen", func2);
      Intrp.State.Global.Set("true", new LuaValue(Intrp.State, Lua_Type.Boolean, true));
      Intrp.State.Global.Set("false", new LuaValue(Intrp.State, Lua_Type.Boolean, false));

      Thread thrd = new Thread(new ThreadStart(ListenerThread));

      thrd.Start();

      // Quick fix to infinite loop bug if input to lexer doesn't contain a space at the end
      // TODO fix, then add comments to the rest of the code
      Intrp.Evaluate(File.ReadAllText("test.lua") + " ");
  }
}