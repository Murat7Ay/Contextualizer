using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Contextualizer.Core
{
    public class DynamicAssemblyLoader
    {
        public static void LoadAssembliesFromFolder(string folderPath)
        {
            foreach (var dll in Directory.GetFiles(folderPath, "*.dll"))
            {
                try
                {
                    Assembly.LoadFrom(dll);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"DLL yüklenirken hata oluştu: {dll} - {ex.Message}");
                }
            }
        }
    }
}
