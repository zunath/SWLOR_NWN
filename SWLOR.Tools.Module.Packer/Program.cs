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

                RunProcess(command);
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
            RunProcess($"nwn_erf -e MOD -c \"./packing/\" -f \"Star Wars LOR.mod\"");
            
            // Clean up the packing directory.
            Directory.Delete("./packing/", true);
        }

        private static void UnpackModule()
        {
            var folders = GetModuleFolders();
            // Create any missing folders and clear out any files in existing folders.
            Parallel.ForEach(folders, (folder) =>
            {
                if (Directory.Exists($"./{folder}"))
                {
                    Directory.Delete($"./{folder}", true);
                }

                Directory.CreateDirectory($"./{folder}");
            });

            // Create any missing script folders and clear out files in existing script folders.
            if (Directory.Exists($"./nss")) Directory.Delete("./nss", true);
            if (Directory.Exists($"./ncs")) Directory.Delete("./ncs", true);
            Directory.CreateDirectory("./nss");
            Directory.CreateDirectory("./ncs");

            // Run the extraction process.
            Console.WriteLine("Extracting module...");
            RunProcess("nwn_erf -f \"Star Wars LOR.mod\" -x");

            // Get all of the files we just unpacked.
            Console.WriteLine("Getting files...");
            var files = Directory.EnumerateFiles("./", "*.*")
                .Where(x => folders.Contains("./" + x.ToLower().Substring(x.Length - 3, 3)));

            Parallel.ForEach(files, (file) =>
            {
                Console.WriteLine("Processing file: " + file);
                string extension = Path.GetExtension(file)?.Replace(".", string.Empty);
                string command = $"nwn_gff -i {file} -o ./{extension}/{file}.json -p";

                RunProcess(command);

                // Remove the extracted file.
                File.Delete(file);
            });

            files = Directory.GetFiles("./", "*.nss").Union(Directory.GetFiles("./", "*.ncs"));
            Parallel.ForEach(files, (file) =>
            {
                Console.WriteLine("Moving script: " + file);
                string fileName = Path.GetFileName(file);
                string extension = Path.GetExtension(file)?.Replace(".", string.Empty);
                File.Move(file, $"./{extension}/{fileName}");
            });
        }

        private static void RunProcess(string command)
        {
            using (var process = new Process
            {
                StartInfo = new ProcessStartInfo("cmd.exe", "/K " + command)
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardInput = true,
                    CreateNoWindow = false
                },
                EnableRaisingEvents = true
            })
            {
                process.Start();

                process.StandardInput.Flush();
                process.StandardInput.Close();

                process.StandardOutput.ReadToEnd();
                process.WaitForExit();
            }
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
