using System;
using System.Reflection;

class Program {
    static void Main() {
        var asm = Assembly.LoadFrom(@"D:\UnityProject\Echoes\Library\ScriptAssemblies\Unity.ShaderGraph.Editor.dll");
        var asmURP = Assembly.LoadFrom(@"D:\UnityProject\Echoes\Library\ScriptAssemblies\Unity.RenderPipelines.Universal.Editor.dll");
        
        foreach (var t in asmURP.GetTypes()) {
            if (t.Name.Contains("RenderFace")) {
                Console.WriteLine("Found: " + t.FullName);
                foreach(var val in Enum.GetValues(t)) {
                    Console.WriteLine(val.ToString() + " = " + ((int)val));
                }
            }
        }
    }
}
