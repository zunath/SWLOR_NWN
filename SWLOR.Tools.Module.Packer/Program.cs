using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SWLOR.Tools.Module.Packer
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length <= 0)
            {
                Console.WriteLine("Must pass either 'p' to pack the module or 'u' to unpack the module.");
                return;
            }

            string mode = args[0];
            var sw = new Stopwatch();

            if(mode == "p")
            {
                sw.Start();
                PackModule();
                sw.Stop();

                Console.WriteLine("Packing module took: " + sw.ElapsedMilliseconds + "ms");
            }
            else if (mode == "u")
            {
                sw.Start();
                UnpackModule();
                sw.Stop();

                Console.WriteLine("Unpacking module took: " + sw.ElapsedMilliseconds + "ms");
            }
            else
            {
                Console.WriteLine("Must pass either 'p' to pack the module or 'u' to unpack the module.");
            }

            Console.WriteLine("Program finished. Press any key to end.");
            Console.ReadKey();
        }

        private static void PackModule()
        {
            // Create a temporary directory.
            if(!Directory.Exists("./packing"))
            {
                Directory.CreateDirectory("./packing");
            }

            // Clean out the temporary directory if the last run failed.
            Parallel.ForEach(Directory.GetFiles("./packing"), (file) =>
            {
                File.Delete(file);
            });

            // Get all JSON files, run them through nwn_gff to convert them to files NWN can read.
            // Put the output files in the ./packing folder.
            
            Console.WriteLine("Packing files...");
            Parallel.ForEach(GetFileList(), (file) =>
            {
                string fileNameNoJson = Path.GetFileNameWithoutExtension(file);
                Console.WriteLine("Packing " + fileNameNoJson);
                string command = $"nwn_gff -i {file} -o ./packing/{fileNameNoJson} -k gff";

                using(var process = CreateProcess(command))
                {
                    process.Start();

                    process.StandardInput.Flush();
                    process.StandardInput.Close();

                    process.StandardOutput.ReadToEnd();
                    process.WaitForExit();
                }
            });

            var scriptFiles = Directory.GetFiles("./ncs/").Union(Directory.GetFiles("./nss/"));
            // Copy the uncompiled (.nss) and compiled (.ncs) scripts to ./packing
            Parallel.ForEach(scriptFiles, (file) =>
            {
                string fileName = Path.GetFileName(file);
                Console.WriteLine("Copying script file: " + fileName);
                File.Copy(file, $"./packing/{fileName}");
            });

            // Finally, use nwn_erf to build a .mod file from the files inside the packing directory.
            Console.WriteLine("Building module...");
            using (var process = CreateProcess($"nwn_erf -e MOD -c \"./packing/\" -f \"Star Wars LOR.mod\""))
            {
                process.Start();
                process.StandardInput.Flush();
                process.StandardInput.Close();

                process.StandardOutput.ReadToEnd();
                process.WaitForExit();
            }

            // Clean up the packing directory.
            Directory.Delete("./packing/", true);
        }

        private static void UnpackModule()
        {

        }

        private static Process CreateProcess(string command)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo("cmd.exe", "/K " + command)
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardInput = true,
                    CreateNoWindow = false
                },
                EnableRaisingEvents = true
            };

            return process;
        }

        private static List<string> GetFileList()
        {
            List<string> results = new List<string>();
            foreach (var folder in GetModuleFolders())
            {
                results.AddRange(Directory.GetFiles(folder));
            }

            return results;
        }

        private static List<string> GetModuleFolders()
        {
            return new List<string>
            {
                "./are", 
                "./dlg",
                "./fac",
                "./gic",
                "./git",
                "./ifo",
                "./itp",
                "./jrl",
                "./utc",
                "./utd",
                "./ute",
                "./uti",
                "./utm",
                "./utp",
                "./uts",
                "./utt",
                "./utw"
            };
        }
    }
}
