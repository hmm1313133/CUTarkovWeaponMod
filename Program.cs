using System;
using System.Linq;
using Mono.Cecil;

class Program
{
    static void Main()
    {
        var dllPath = @"F:\SteamLibrary\steamapps\common\Casualties Unknown Demo\CasualtiesUnknown_Data\Managed\Assembly-CSharp.dll";
        var assembly = AssemblyDefinition.ReadAssembly(dllPath);

        // Find Sound class
        Console.WriteLine("=== Sound class ===");
        foreach (var type in assembly.MainModule.Types)
        {
            if (type.Name == "Sound")
            {
                Console.WriteLine($"Fields ({type.Fields.Count}):");
                foreach (var f in type.Fields)
                    Console.WriteLine($"  {f.FieldType} {f.Name}");

                Console.WriteLine($"\nMethods ({type.Methods.Count}):");
                foreach (var m in type.Methods)
                {
                    Console.WriteLine($"  {m.ReturnType} {m.Name}({string.Join(", ", m.Parameters.Select(p => $"{p.ParameterType} {p.Name}"))})");
                }

                // Print Play methods IL
                foreach (var m in type.Methods)
                {
                    if (m.Name == "Play" && m.HasBody)
                    {
                        Console.WriteLine($"\n--- Method: {m.FullName} ---");
                        Console.WriteLine($"  Params: {string.Join(", ", m.Parameters.Select(p => $"{p.ParameterType} {p.Name}"))}");
                        Console.WriteLine($"  IL ({m.Body.Instructions.Count} instr):");
                        foreach (var instr in m.Body.Instructions)
                            Console.WriteLine($"    {instr}");
                    }
                }
            }
        }
    }
}
