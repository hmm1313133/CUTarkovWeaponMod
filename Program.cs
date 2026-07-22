using System;
using System.Linq;
using Mono.Cecil;

class Program
{
    static void Main()
    {
        var dllPath = @"F:\SteamLibrary\steamapps\common\Casualties Unknown Demo\BepInEx\plugins\CUCoreLib.dll";
        var assembly = AssemblyDefinition.ReadAssembly(dllPath);

        // Check CustomItemInfo constructor for SpriteScale default
        var ciType = assembly.MainModule.Types.FirstOrDefault(t => t.Name == "CustomItemInfo");
        if (ciType != null)
        {
            var ctor = ciType.Methods.FirstOrDefault(m => m.IsConstructor && !m.HasParameters);
            if (ctor != null && ctor.HasBody)
            {
                Console.WriteLine("=== CustomItemInfo.ctor() ===");
                foreach (var instr in ctor.Body.Instructions)
                    Console.WriteLine($"  IL_{instr.Offset:X4}: {instr.OpCode} {instr.Operand}");
            }
        }

        // Check ResolveSpriteScale method
        var patchType = assembly.MainModule.Types.FirstOrDefault(t => t.Name == "ItemRegistryPatches");
        if (patchType != null)
        {
            var resolveMethod = patchType.Methods.FirstOrDefault(m => m.Name == "ResolveSpriteScale");
            if (resolveMethod != null && resolveMethod.HasBody)
            {
                Console.WriteLine($"\n=== ResolveSpriteScale ({resolveMethod.Body.Instructions.Count} instrs) ===");
                foreach (var instr in resolveMethod.Body.Instructions)
                    Console.WriteLine($"  IL_{instr.Offset:X4}: {instr.OpCode} {instr.Operand}");
            }

            var applyMethod = patchType.Methods.FirstOrDefault(m => m.Name == "ApplyCustomScale");
            if (applyMethod != null && applyMethod.HasBody)
            {
                Console.WriteLine($"\n=== ApplyCustomScale ({applyMethod.Body.Instructions.Count} instrs) ===");
                foreach (var instr in applyMethod.Body.Instructions)
                    Console.WriteLine($"  IL_{instr.Offset:X4}: {instr.OpCode} {instr.Operand}");
            }
        }
    }
}
