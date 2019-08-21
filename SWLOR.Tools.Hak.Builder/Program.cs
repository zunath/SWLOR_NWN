using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SWLOR.Tools.Hak.Builder
{
    class Program
    {
        private static volatile int _processesComplete;
        const string OutputFolder = "./output/";

        static void Main(string[] args)
        {

            // Create the output folder.
            if(!Directory.Exists(OutputFolder))
            {
                Directory.CreateDirectory(OutputFolder);
            }

            // Copy the TLK to the output folder.
            File.Copy("./swlor_tlk.tlk", $"{OutputFolder}/swlor_tlk.tlk");

            // Build non-model haks.
            var hakFolders = GetHakFolders();
            Parallel.ForEach(hakFolders, (folder) =>
            {
                CompileHak(folder);
            });

            // Model files need to be compiled before being added to a hak file.
            if(!Directory.Exists($"{OutputFolder}compiled_models"))
            {
                Directory.CreateDirectory($"{OutputFolder}compiled_models");
            }

            hakFolders = new string[] { "swlor_mdl", "swlor_mdl_p" };
            Parallel.ForEach(hakFolders, (folder) =>
            {
                CompileModels(folder);
            });

            // Compilation is done. Now we insert the compiled models into the appropriate hak files.

            // Clean up the compiled models folder.
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

        private static string[] GetHakFolders()
        {
            // Most of the haks get imported directly from the folder to the hak file.
            return new[]
            {
                "swlor_2da",
                "swlor_add_doors",
                "swlor_add_loads",
                "swlor_add_skies",
                "swlor_add_tiles1",
                "swlor_add_tiles2",
                "swlor_core0",
                "swlor_core1",
                "swlor_core2",
                "swlor_core3",
                "swlor_core4",
                "swlor_core5",
                "swlor_core6",
                "swlor_core7",
                "swlor_dds",
                "swlor_dwk",
                "swlor_ext_tiles",
                "swlor_gui",
                "swlor_ini",
                "swlor_itp",
                // Note: swlor_mdl and swlor_mdl_p have some custom processing involved so we skip them for now.
                "swlor_mtr",
                "swlor_plt",
                "swlor_portraits",
                "swlor_pwk",
                "swlor_set",
                "swlor_shd",
                "swlor_ssf",
                "swlor_tga",
                "swlor_tga_ip",
                "swlor_txi",
                "swlor_utp",
                "swlor_wav",
                "swlor_wok"
            };
        }

        private static void CompileHak(string folder)
        {
            string command = $"nwn_erf -f \"{OutputFolder}{folder}.hak\" -e HAK -c ./{folder}";
            Console.WriteLine($"Building hak: {folder}.hak");

            using (var process = CreateProcess(command))
            {
                process.Start();

                process.StandardInput.Flush();
                process.StandardInput.Close();

                string output = process.StandardOutput.ReadToEnd();

                process.WaitForExit();
            }
        }

        private static void CompileModels(string folder)
        {
            string command = $"nwnmdlcomp ./{folder}/* ./{OutputFolder}compiled_models/ -e";

            Console.WriteLine($"Compiling models in folder: {folder}");

            using (var process = CreateProcess(command))
            {
                process.Start();

                process.StandardInput.Flush();
                process.StandardInput.Close();

                string output = process.StandardOutput.ReadToEnd();

                process.WaitForExit();
            }
        }
    }
}
