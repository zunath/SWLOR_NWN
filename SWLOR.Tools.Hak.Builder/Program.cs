using System;
using System.Diagnostics;
using System.Threading;

namespace SWLOR.Tools.Hak.Builder
{
    class Program
    {
        private static volatile int _processesComplete;

        static void Main(string[] args)
        {
            const string OutputFolder = "./output/";

            var process = new Process
            {
                StartInfo = new ProcessStartInfo("cmd.exe", "/K ")
                {
                    UseShellExecute = false, 
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }, 
                EnableRaisingEvents = true
            };
            process.Exited += (sender, eventArgs) =>
            {
                _processesComplete++;
                Console.WriteLine("Build complete. _processesComplete = " + _processesComplete);
            };


            var hakFolders = GetHakFolders();
            foreach (var folder in hakFolders)
            {
                string command = $"nwn_erf -f \"{OutputFolder}{folder}.hak\" -e HAK -c ./{folder}";
                Console.WriteLine($"Building hak: {folder}.hak");

                process.StartInfo.Arguments = "/K /C " + command;
                process.Start();

                Console.WriteLine("Waiting for exit");
                process.WaitForExit();
                Console.WriteLine("Closing");
                process.Close();
            }

            // Model haks need to be compiled first (if the compilation flag is active).

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
    }
}
