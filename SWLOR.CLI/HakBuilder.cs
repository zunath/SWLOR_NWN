﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SWLOR.CLI.Model;
using SWLOR.Game.Server.Extension;

namespace SWLOR.CLI
{
    public class HakBuilder
    {
        private const string ConfigFilePath = "./hakbuilder.json";
        private HakBuilderConfig _config;
        private List<HakBuilderHakpak> _haksToProcess;
        private readonly Dictionary<string, string> _checksumDictionary = new();

        public void Process()
        {
            // Read the config file.
            _config = GetConfig();
            _haksToProcess = _config.HakList.ToList();
            // Clean the output folder.
            CleanOutputFolder();

            // Copy the TLK to the output folder.
            Console.WriteLine($"Copying TLK: {_config.TlkPath}");

            if (File.Exists(_config.TlkPath))
            {
                var destination = $"{_config.OutputPath}tlk/{Path.GetFileName(_config.TlkPath)}";
                
                // Ensure the tlk directory exists
                var tlkDir = Path.GetDirectoryName(destination);
                if (!Directory.Exists(tlkDir))
                {
                    Directory.CreateDirectory(tlkDir);
                }
                
                try
                {
                    File.Copy(_config.TlkPath, destination, true); // true = overwrite
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Cannot copy TLK file - it is locked by another process (likely the game).");
                    Console.WriteLine($"The build will continue, but you may need to restart the game to see TLK changes.");
                    Console.WriteLine($"Exception: {ex.ToMessageAndCompleteStacktrace()}");
                }
            }
            else
            {
                Console.WriteLine("Error: TLK does not exist");
            }

            // Iterate over every configured hakpak folder and build the hak file.
            Parallel.ForEach(_haksToProcess, hak =>
            {
                CompileHakpak(hak.Name, hak.Path);
            });

        }

        /// <summary>
        /// Retrieves the configuration file for the hak builder.
        /// Throws an exception if the file is missing.
        /// </summary>
        /// <returns>The hak builder config settings.</returns>
        private HakBuilderConfig GetConfig()
        {
            if (!File.Exists(ConfigFilePath))
            {
                throw new Exception($"Unable to locate config file. Ensure file '{ConfigFilePath}' exists in the same folder as this application.");
            }

            var json = File.ReadAllText(ConfigFilePath);

            return JsonConvert.DeserializeObject<HakBuilderConfig>(json);
        }

        /// <summary>
        /// Cleans the output folder.
        /// </summary>
        private void CleanOutputFolder()
        {
            {
                if (Directory.Exists(_config.OutputPath))
                {
                    // Try to delete .tlk, but don't fail if it's locked by the game
                    var tlkPath = $"{_config.OutputPath}tlk/{Path.GetFileName(_config.TlkPath)}";
                    if (File.Exists(tlkPath))
                    {
                        try
                        {
                            File.Delete(tlkPath);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Warning: TLK file is locked by another process (likely the game). Skipping deletion.");
                            Console.WriteLine($"Exception: {ex.ToMessageAndCompleteStacktrace()}");
                        }
                    }

                    Parallel.ForEach(_config.HakList, hak =>
                    {
                        // Check whether .hak file exists
                        if (!File.Exists(_config.OutputPath + "hak/" + hak.Name + ".hak"))
                        {
                            Console.WriteLine(hak.Name + " needs to be built");
                            return;
                        }

                        // Skip checksum checking if disabled
                        if (!_config.EnableChecksumChecking)
                        {
                            Console.WriteLine(hak.Name + " needs to be built (checksum checking disabled)");
                            return;
                        }

                        var checksumFolder = ChecksumUtil.ChecksumFolder(hak.Path);
                        _checksumDictionary.Add(hak.Name, checksumFolder);

                        // Check whether .sha checksum file exists
                        if (!File.Exists(_config.OutputPath + "hak/" + hak.Name + ".md5"))
                        {
                            Console.WriteLine(hak.Name + " needs to be built");
                            return;
                        }

                        // When checksums are equal or hak folder doesn't exist -> remove hak from the list
                        var checksumFile = ChecksumUtil.ReadChecksumFile(_config.OutputPath + "hak/" + hak.Name + ".md5");
                        if (checksumFolder == checksumFile)
                        {
                            _haksToProcess.Remove(hak);
                            Console.WriteLine(hak.Name + " is up to date");
                        }
                    });

                    // Delete outdated haks and checksums
                    Parallel.ForEach(_haksToProcess, hak =>
                    {
                        var filePath = _config.OutputPath + "hak/" + hak.Name;
                        if (File.Exists(filePath + ".hak"))
                        {
                            try
                            {
                                File.Delete(filePath + ".hak");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Warning: Hak file {hak.Name}.hak is locked by another process. Skipping deletion.");
                                Console.WriteLine($"Exception: {ex.ToMessageAndCompleteStacktrace()}");
                            }
                        }

                        if (File.Exists(filePath + ".md5"))
                        {
                            try
                            {
                                File.Delete(filePath + ".md5");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Warning: Checksum file {hak.Name}.md5 is locked by another process. Skipping deletion.");
                                Console.WriteLine($"Exception: {ex.ToMessageAndCompleteStacktrace()}");
                            }
                        }
                    });
                }
                else
                {
                    Directory.CreateDirectory(_config.OutputPath);
                }
            }
        }

        /// <summary>
        /// Creates a new background process used for running external programs.
        /// </summary>
        /// <param name="command">The command to pass into the cmd instance.</param>
        /// <returns>A new process</returns>
        private Process CreateProcess(string command)
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

        /// <summary>
        /// Compiles files contained in a folder into a hakpak.
        /// </summary>
        /// <param name="hakName">The name of the hak without the .hak extension</param>
        /// <param name="folderPath">The folder where the assets are.</param>
        private void CompileHakpak(string hakName, string folderPath)
        {
            // Ensure the hak directory exists
            var hakDir = $"{_config.OutputPath}hak/";
            if (!Directory.Exists(hakDir))
            {
                Directory.CreateDirectory(hakDir);
            }
            
            var command = $"nwn_erf -f \"{_config.OutputPath}hak/{hakName}.hak\" -e HAK -c ./{folderPath}";
            Console.WriteLine($"Building hak: {hakName}.hak");

            using (var process = CreateProcess(command))
            {
                process.Start();

                process.StandardInput.Flush();
                process.StandardInput.Close();

                process.StandardOutput.ReadToEnd();

                process.WaitForExit();
            }

            // Only perform checksum operations if enabled
            if (_config.EnableChecksumChecking)
            {
                if (!_checksumDictionary.TryGetValue(hakName, out var checksum))
                {
                    checksum = ChecksumUtil.ChecksumFolder(folderPath);
                }

                ChecksumUtil.WriteChecksumFile(_config.OutputPath + "hak/" + hakName + ".md5", checksum);
            }
        }
    }
}
