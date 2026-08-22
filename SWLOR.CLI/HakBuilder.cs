using System;
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
            _haksToProcess = _config.HakList
                .Where(hak => hak != null && !string.IsNullOrWhiteSpace(hak.Name))
                .ToList();
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

                    foreach (var hak in _config.HakList.Where(hak => hak != null && !string.IsNullOrWhiteSpace(hak.Name)))
                    {
                        // Check whether .hak file exists
                        if (!File.Exists(_config.OutputPath + "hak/" + hak.Name + ".hak"))
                        {
                            Console.WriteLine(hak.Name + " needs to be built");
                            continue;
                        }

                        // Skip checksum checking if disabled
                        if (!_config.EnableChecksumChecking)
                        {
                            Console.WriteLine(hak.Name + " needs to be built (checksum checking disabled)");
                            continue;
                        }

                        var checksumFolder = ChecksumUtil.ChecksumFolder(hak.Path);
                        _checksumDictionary[hak.Name] = checksumFolder;

                        // Check whether .sha checksum file exists
                        if (!File.Exists(_config.OutputPath + "hak/" + hak.Name + ".md5"))
                        {
                            Console.WriteLine(hak.Name + " needs to be built");
                            continue;
                        }

                        // When checksums are equal or hak folder doesn't exist -> remove hak from the list
                        var checksumFile = ChecksumUtil.ReadChecksumFile(_config.OutputPath + "hak/" + hak.Name + ".md5");
                        if (checksumFolder == checksumFile)
                        {
                            _haksToProcess.Remove(hak);
                            Console.WriteLine(hak.Name + " is up to date");
                        }
                    }

                    // Delete outdated haks and checksums
                    foreach (var hak in _haksToProcess.Where(hak => hak != null && !string.IsNullOrWhiteSpace(hak.Name)))
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
                    }
                }
                else
                {
                    Directory.CreateDirectory(_config.OutputPath);
                }
            }
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
            
            Console.WriteLine($"Building hak: {hakName}.hak");

            var contentPath = Path.IsPathRooted(folderPath)
                ? folderPath
                : $"./{folderPath}";

            RunProcess(
                "nwn_erf.exe",
                "-f", $"{_config.OutputPath}hak/{hakName}.hak",
                "-e", "HAK",
                "-c", contentPath);

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

        private static void RunProcess(string fileName, params string[] arguments)
        {
            var toolPath = ResolveToolPath(fileName);
            using (var process = new Process
            {
                StartInfo = new ProcessStartInfo(toolPath)
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                },
                EnableRaisingEvents = true
            })
            {
                foreach (var argument in arguments)
                {
                    process.StartInfo.ArgumentList.Add(argument);
                }

                process.Start();

                var standardOutputTask = process.StandardOutput.ReadToEndAsync();
                var standardErrorTask = process.StandardError.ReadToEndAsync();
                process.WaitForExit();
                var standardOutput = standardOutputTask.GetAwaiter().GetResult();
                var standardError = standardErrorTask.GetAwaiter().GetResult();

                if (process.ExitCode != 0)
                {
                    var command = $"{fileName} {string.Join(" ", arguments.Select(QuoteArgument))}";
                    throw new InvalidOperationException(
                        $"Command failed with exit code {process.ExitCode}: {command}{Environment.NewLine}{standardOutput}{standardError}");
                }
            }
        }

        private static string ResolveToolPath(string fileName)
        {
            var executableDirectoryPath = Path.Combine(AppContext.BaseDirectory, fileName);
            if (File.Exists(executableDirectoryPath))
            {
                return executableDirectoryPath;
            }

            var workingDirectoryPath = Path.Combine(Environment.CurrentDirectory, fileName);
            return File.Exists(workingDirectoryPath)
                ? workingDirectoryPath
                : fileName;
        }

        private static string QuoteArgument(string argument)
        {
            return argument.Contains(' ')
                ? $"\"{argument}\""
                : argument;
        }
    }
}
