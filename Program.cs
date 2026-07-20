using System;
using System.Linq;
using Mono.Cecil;

class Program
{
    static void Main()
    {
        var dllPath = @"F:\SteamLibrary\steamapps\common\Casualties Unknown Demo\CasualtiesUnknown_Data\Managed\Assembly-CSharp.dll";
        var assembly = AssemblyDefinition.ReadAssembly(dllPath);

        foreach (var type in assembly.MainModule.Types)
        {
            if (type.Name == "Recipe")
            {
                Console.WriteLine($"=== Recipe fields ({type.Fields.Count}) ===");
                foreach (var f in type.Fields)
                    Console.WriteLine($"  {f.FieldType} {f.Name}");
            }
            if (type.Name == "RecipeItem")
            {
                Console.WriteLine($"\n=== RecipeItem fields ({type.Fields.Count}) ===");
                foreach (var f in type.Fields)
                    Console.WriteLine($"  {f.FieldType} {f.Name}");
            }
            if (type.Name == "RecipeResult")
            {
                Console.WriteLine($"\n=== RecipeResult fields ({type.Fields.Count}) ===");
                foreach (var f in type.Fields)
                    Console.WriteLine($"  {f.FieldType} {f.Name}");
            }
        }
    }
}
