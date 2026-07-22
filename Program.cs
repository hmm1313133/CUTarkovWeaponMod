using System;
using System.Linq;
using Mono.Cecil;

class Program
{
    static void Main()
    {
        var dllPath = @"F:\SteamLibrary\steamapps\common\Casualties Unknown Demo\CasualtiesUnknown_Data\Managed\Assembly-CSharp.dll";
        var assembly = AssemblyDefinition.ReadAssembly(dllPath);

        var soundType = assembly.MainModule.Types.FirstOrDefault(t => t.Name == "Sound");
        if (soundType == null) { Console.WriteLine("Sound not found"); return; }

        // Dump the Play(AudioClip,...) method
        var playMethod = soundType.Methods.FirstOrDefault(m =>
            m.Name == "Play" && m.Parameters.Count > 0 &&
            m.Parameters[0].ParameterType.Name == "AudioClip");

        if (playMethod != null && playMethod.HasBody)
        {
            Console.WriteLine($"=== Sound.Play(AudioClip,...) ({playMethod.Body.Instructions.Count} instrs) ===");
            Console.WriteLine($"Parameters: {string.Join(", ", playMethod.Parameters.Select(p => p.ParameterType + " " + p.Name + (p.HasDefault ? "="+p.Constant : "")))}");
            Console.WriteLine();
            foreach (var instr in playMethod.Body.Instructions)
            {
                Console.WriteLine($"  IL_{instr.Offset:X4}: {instr.OpCode} {instr.Operand}");
            }
        }
    }
}
