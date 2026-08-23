using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SWLOR.NWN.Formats.Common;

namespace SWLOR.CLI
{
    public class ModulePacker
    {
        private const int CleanupRetryCount = 5;
        private const int CleanupRetryDelayMilliseconds = 250;
        private const int MaxDefaultWorkerCount = 12;
        private const int ProgressInterval = 100;
        private const int ReservedProcessorCount = 2;
        private const int ResourceConversionRetryCount = 3;
        private const int ResourceConversionRetryDelayMilliseconds = 250;
        private const string PackingDirectory = "./packing";
        private const string PaletteRefreshDirectory = "./palette-refresh";
        private const string WorkerCountEnvironmentVariable = "SWLOR_RESOURCE_CONVERSION_WORKERS";
        // Mirrors NewAreaWriter.PendingMarkerPrefix in SWLOR.Toolset.Domain - this project cannot
        // reference that one (see RequireNoInterruptedAreaCreation), so the literal is duplicated.
        private const string NewAreaPendingMarkerPrefix = ".swlor-toolset-new-area-";
        private const string NewAreaPendingMarkerSuffix = ".pending";
        private const string ErfImportPendingMarkerPattern = ".swlor-toolset-erf-import-*.pending.json";
        private const string ItemRenamePendingMarkerPattern = ".swlor-toolset-item-rename-*.pending.json";
        // Mirrors ModuleResourceDeletionService.DeleteTransactionSuffix. SWLOR.CLI cannot reference
        // SWLOR.Toolset, so it refuses the durable manifest and lets the toolset roll it back.
        private const string ResourceDeleteTransactionPattern = ".*.resource-delete-transaction.json";

        public void PackModule(string filePath, bool noPrompt = false)
        {
            using var moduleWriteLock = ModuleWriteLock.Acquire(Environment.CurrentDirectory);
            var sw = new Stopwatch();
            Exception packException = null;
            sw.Start();
            var moduleFileName = Path.GetFileName(filePath);
            var temporaryModuleFileName = GetTemporaryModuleFileName(moduleFileName);

            try
            {
                RequireNoInterruptedSaves();
                RequireNoInterruptedResourceDelete();
                RequireNoInterruptedAreaCreation();
                RequireNoInterruptedErfImport();
                RequireNoInterruptedItemRename();
                RecreateDirectory(PackingDirectory);
                RecreateDirectory(PaletteRefreshDirectory);
                DeleteFileWithRetry(temporaryModuleFileName);

                // Aurora refreshes each custom blueprint palette from the module resources before it
                // saves/builds. Do the same on temporary JSON copies so newly added, renamed, moved, or
                // deleted blueprints are represented in the packed ITPs without dirtying Module/itp.
                Console.WriteLine("Refreshing custom blueprint palettes...");
                var paletteRefresh = ModulePaletteRefresher.Refresh(
                    Environment.CurrentDirectory,
                    PaletteRefreshDirectory);
                foreach (var result in paletteRefresh.Results)
                {
                    Console.WriteLine(
                        $"Refreshed {result.PaletteName}: {result.Included:N0} blueprints " +
                        $"({result.Added:N0} added, {result.Removed:N0} removed, " +
                        $"{result.Updated:N0} updated, {result.MissingCategory:N0} category not found).");
                }

                // Get all JSON files, run them through nwn_gff to convert them to files NWN can read.
                // Put the output files in the ./packing folder.

                var files = GetFileList();
                var parallelOptions = CreateResourceConversionParallelOptions();
                Console.WriteLine($"Packing {files.Count} files with up to {parallelOptions.MaxDegreeOfParallelism} resource conversion workers...");
                var packedFileCount = 0;
                Parallel.ForEach(files, parallelOptions, (file) =>
                {
                    try
                    {
                        var fileNameNoJson = Path.GetFileNameWithoutExtension(file);
                        var outputFile = Path.Combine(PackingDirectory, fileNameNoJson);
                        var fullSourcePath = Path.GetFullPath(file);
                        var conversionInput = paletteRefresh.Replacements.TryGetValue(
                            fullSourcePath,
                            out var refreshedPalette)
                            ? refreshedPalette
                            : file;

                        RunResourceConversion(
                            file,
                            outputFile,
                            "-l", "json",
                            "-i", conversionInput,
                            "-o", outputFile,
                            "-k", "gff");

                        WriteProgress("Packed", Interlocked.Increment(ref packedFileCount), files.Count);
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException($"Failed to pack resource '{file}'.", ex);
                    }
                });

                // By extension, not by folder contents. The GFF folders above already filter
                // transaction debris; the script folders did not, so an interrupted script save
                // left a "foo.nss.tmp" beside its target and the next pack copied that into the
                // .mod as though it were a script resource.
                var scriptFiles = Directory.GetFiles("./ncs/", "*.ncs")
                    .Union(Directory.GetFiles("./nss/", "*.nss"))
                    // A resref never contains a dot, so a dotted stem is transaction debris - the
                    // toolset's compiler writes to "resref.<guid>.ncs" and installs atomically, and
                    // a kill mid-write can leave that temp behind. It was never a committed
                    // compile, so it must not ship in the module.
                    .Where(file => !Path.GetFileNameWithoutExtension(file).Contains('.'))
                    .ToList();
                // Copy the uncompiled (.nss) and compiled (.ncs) scripts to ./packing
                Console.WriteLine($"Copying {scriptFiles.Count} script files...");
                var copiedScriptCount = 0;
                Parallel.ForEach(scriptFiles, parallelOptions, (file) =>
                {
                    var fileName = Path.GetFileName(file);
                    File.Copy(file, $"{PackingDirectory}/{fileName}");
                    WriteProgress("Copied scripts", Interlocked.Increment(ref copiedScriptCount), scriptFiles.Count);
                });

                // Finally, use nwn_erf to build a .mod file from the files inside the packing directory.
                Console.WriteLine("Building module...");
                RunProcess(
                    "nwn_erf.exe",
                    "-e", "MOD",
                    "-c", $"{PackingDirectory}/",
                    "-f", temporaryModuleFileName);

                ReplaceFile(temporaryModuleFileName, moduleFileName);
            }
            catch (Exception ex)
            {
                packException = ex;
                Console.Error.WriteLine($"Packing failed: {FormatException(ex)}");
                throw;
            }
            finally
            {
                try
                {
                    DeleteDirectoryWithRetry(PackingDirectory);
                    DeleteDirectoryWithRetry(PaletteRefreshDirectory);
                    DeleteFileWithRetry(temporaryModuleFileName);
                }
                catch (Exception ex)
                {
                    if (packException == null)
                    {
                        throw;
                    }

                    Console.Error.WriteLine($"Packing failed and temporary cleanup also failed: {ex.Message}");
                }
            }

            sw.Stop();
            Console.WriteLine($"Packing module completed in {sw.ElapsedMilliseconds}ms");
            moduleWriteLock.Dispose();
            if (!noPrompt)
            {
                WaitForKeyIfInteractive();
            }
        }

        public void UnpackModule(string filePath, bool noPrompt = false)
        {
            using var moduleWriteLock = ModuleWriteLock.Acquire(Environment.CurrentDirectory);
            var sw = new Stopwatch();
            sw.Start();
            var moduleFileName = Path.GetFileName(filePath);
            if (!File.Exists(filePath))
            {
                Console.WriteLine("Module at specified path '' not found. Did you enter the right path?");
                return;
            }

            // The same preflight packing runs, for the inverse hazard: unpacking recursively
            // deletes every resource directory first, which would erase an interrupted save's
            // .save-backup files (leaving a manifest that can never restore them - the next
            // toolset open then throws SaveRecoveryException) and would leave a new-area
            // .pending marker pointing at nothing, failing every subsequent pack. Refusing here
            // lets the toolset recover the transaction before the evidence is destroyed.
            RequireNoInterruptedSaves();
            RequireNoInterruptedResourceDelete();
            RequireNoInterruptedAreaCreation();
            RequireNoInterruptedErfImport();
            RequireNoInterruptedItemRename();

            var folders = GetModuleFolders();
            // Create any missing folders and clear out any files in existing folders.
            Parallel.ForEach(folders, CreateResourceConversionParallelOptions(), (folder) =>
            {
                if (Directory.Exists($"./{folder}"))
                {
                    DeleteDirectoryWithRetry($"./{folder}");
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
            RunProcess("nwn_erf.exe", "-f", moduleFileName, "-x");

            // Get all of the files we just unpacked.
            Console.WriteLine("Getting files...");
            var files = Directory.EnumerateFiles("./", "*.*")
                .Where(x => folders.Contains("./" + x.ToLower().Substring(x.Length - 3, 3))).ToList();

            // Make sure that extensions are lowercase because nwn_gff only supports these
            for (int i = 0; i < files.Count; i++)
            {
                var fileWithFormattedExtension = Path.ChangeExtension(files[i], Path.GetExtension(files[i]).ToLower());
                File.Move(files[i], fileWithFormattedExtension);
                files[i] = fileWithFormattedExtension;
            }

            var parallelOptions = CreateResourceConversionParallelOptions();
            Console.WriteLine($"Processing {files.Count} extracted files with up to {parallelOptions.MaxDegreeOfParallelism} resource conversion workers...");
            var processedFileCount = 0;
            Parallel.ForEach(files, parallelOptions, (file) =>
            {
                try
                {
                    var extension = Path.GetExtension(file)?.Replace(".", string.Empty);

                    var outputFile = $"./{extension}/{file}.json";
                    RunResourceConversion(
                        file,
                        outputFile,
                        "-i", file,
                        "-o", outputFile,
                        "-p");

                    // Remove the extracted file.
                    File.Delete(file);
                    WriteProgress("Processed", Interlocked.Increment(ref processedFileCount), files.Count);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to unpack resource '{file}'.", ex);
                }
            });

            files = Directory.GetFiles("./", "*.nss").Union(Directory.GetFiles("./", "*.ncs")).ToList();
            Console.WriteLine($"Moving {files.Count} script files...");
            var movedScriptCount = 0;
            Parallel.ForEach(files, parallelOptions, (file) =>
            {
                var fileName = Path.GetFileName(file);
                var extension = Path.GetExtension(file)?.Replace(".", string.Empty);
                File.Move(file, $"./{extension}/{fileName}");
                WriteProgress("Moved scripts", Interlocked.Increment(ref movedScriptCount), files.Count);
            });

            sw.Stop();
            Console.WriteLine($"Unpacking module completed in {sw.ElapsedMilliseconds}ms");
            moduleWriteLock.Dispose();
            if (!noPrompt)
            {
                WaitForKeyIfInteractive();
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

        private static void RunResourceConversion(
            string resource,
            string outputFile,
            params string[] arguments)
        {
            Exception lastException = null;

            for (var attempt = 1; attempt <= ResourceConversionRetryCount; attempt++)
            {
                try
                {
                    if (attempt > 1)
                    {
                        DeleteFileWithRetry(outputFile);
                    }

                    RunProcess("nwn_gff.exe", arguments);
                    return;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                }

                if (attempt < ResourceConversionRetryCount)
                {
                    var retryDelay = ResourceConversionRetryDelayMilliseconds * attempt;
                    Console.Error.WriteLine(
                        $"Resource conversion failed for '{resource}' " +
                        $"(attempt {attempt}/{ResourceConversionRetryCount}). " +
                        $"Retrying in {retryDelay}ms...");
                    Thread.Sleep(retryDelay);
                }
            }

            DeleteFileWithRetry(outputFile);
            throw new InvalidOperationException(
                $"Resource conversion failed after {ResourceConversionRetryCount} attempts.",
                lastException);
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

        private static string GetTemporaryModuleFileName(string moduleFileName)
        {
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(moduleFileName);
            var extension = Path.GetExtension(moduleFileName);
            return $"{fileNameWithoutExtension}.packing{extension}";
        }

        private static void ReplaceFile(string sourceFile, string destinationFile)
        {
            if (File.Exists(destinationFile))
            {
                File.Replace(sourceFile, destinationFile, null, true);
                return;
            }

            File.Move(sourceFile, destinationFile);
        }

        private static ParallelOptions CreateResourceConversionParallelOptions()
        {
            return new ParallelOptions
            {
                MaxDegreeOfParallelism = GetResourceConversionWorkerCount()
            };
        }

        private static int GetResourceConversionWorkerCount()
        {
            var workerCountOverride = Environment.GetEnvironmentVariable(WorkerCountEnvironmentVariable);
            if (int.TryParse(workerCountOverride, out var workerCount) && workerCount > 0)
            {
                return workerCount;
            }

            var availableProcessorCount = Math.Max(1, Environment.ProcessorCount - ReservedProcessorCount);
            return Math.Max(1, Math.Min(availableProcessorCount, MaxDefaultWorkerCount));
        }

        private static void WriteProgress(string label, int completedCount, int totalCount)
        {
            if (completedCount == totalCount || completedCount % ProgressInterval == 0)
            {
                Console.WriteLine($"{label} {completedCount:N0}/{totalCount:N0}");
            }
        }

        private static void WaitForKeyIfInteractive()
        {
            if (Console.IsInputRedirected)
            {
                return;
            }

            Console.WriteLine("Program finished. Press any key to end.");
            Console.ReadKey();
        }

        private static void RecreateDirectory(string directory)
        {
            DeleteDirectoryWithRetry(directory);
            Directory.CreateDirectory(directory);
        }

        private static void DeleteDirectoryWithRetry(string directory)
        {
            if (!Directory.Exists(directory))
            {
                return;
            }

            Exception lastException = null;
            for (var attempt = 1; attempt <= CleanupRetryCount; attempt++)
            {
                try
                {
                    ClearReadOnlyAttributes(directory);
                    Directory.Delete(directory, true);
                    return;
                }
                catch (DirectoryNotFoundException)
                {
                    return;
                }
                catch (IOException ex)
                {
                    lastException = ex;
                }
                catch (UnauthorizedAccessException ex)
                {
                    lastException = ex;
                }

                if (attempt < CleanupRetryCount)
                {
                    Thread.Sleep(CleanupRetryDelayMilliseconds * attempt);
                }
            }

            throw new IOException(
                $"Unable to delete temporary packing directory '{directory}' after {CleanupRetryCount} attempts.",
                lastException);
        }

        private static void DeleteFileWithRetry(string file)
        {
            if (!File.Exists(file))
            {
                return;
            }

            Exception lastException = null;
            for (var attempt = 1; attempt <= CleanupRetryCount; attempt++)
            {
                try
                {
                    var attributes = File.GetAttributes(file);
                    if ((attributes & FileAttributes.ReadOnly) != 0)
                    {
                        File.SetAttributes(file, attributes & ~FileAttributes.ReadOnly);
                    }

                    File.Delete(file);
                    return;
                }
                catch (FileNotFoundException)
                {
                    return;
                }
                catch (IOException ex)
                {
                    lastException = ex;
                }
                catch (UnauthorizedAccessException ex)
                {
                    lastException = ex;
                }

                if (attempt < CleanupRetryCount)
                {
                    Thread.Sleep(CleanupRetryDelayMilliseconds * attempt);
                }
            }

            throw new IOException(
                $"Unable to delete temporary file '{file}' after {CleanupRetryCount} attempts.",
                lastException);
        }

        private static void ClearReadOnlyAttributes(string directory)
        {
            foreach (var path in Directory.EnumerateFileSystemEntries(directory, "*", SearchOption.AllDirectories))
            {
                var attributes = File.GetAttributes(path);
                if ((attributes & FileAttributes.ReadOnly) != 0)
                {
                    File.SetAttributes(path, attributes & ~FileAttributes.ReadOnly);
                }
            }

            var directoryAttributes = File.GetAttributes(directory);
            if ((directoryAttributes & FileAttributes.ReadOnly) != 0)
            {
                File.SetAttributes(directory, directoryAttributes & ~FileAttributes.ReadOnly);
            }
        }

        private static string FormatException(Exception exception)
        {
            if (exception is AggregateException aggregateException)
            {
                return string.Join(
                    Environment.NewLine,
                    aggregateException.Flatten().InnerExceptions.Select(FormatException));
            }

            return exception.InnerException == null
                ? exception.Message
                : $"{exception.Message}{Environment.NewLine}{FormatException(exception.InnerException)}";
        }

        /// <summary>
        /// Refuses to pack while an interrupted toolset save is pending. Every transaction manifest
        /// is pending, as is a backup whose canonical target is missing. A backup beside an existing
        /// canonical file is only stale cleanup debris from a save that already committed and is safe
        /// to ignore. The toolset's SaveService.RecoverInterruptedSaves performs the recovery when
        /// the module is opened; this CLI does not duplicate that logic, it fails loudly instead.
        /// </summary>
        private static void RequireNoInterruptedSaves()
        {
            var pending = GetModuleFolders()
                .Concat(new[] { "./nss", "./ncs" })
                .Where(Directory.Exists)
                .Append(".")
                .SelectMany(folder => Directory.GetFiles(folder)
                    .Where(file =>
                        file.EndsWith(".save-transaction.json", StringComparison.OrdinalIgnoreCase) ||
                        file.EndsWith(".save-backup", StringComparison.OrdinalIgnoreCase) &&
                        !File.Exists(InterruptedSaveTarget(file))))
                .ToList();

            if (pending.Count == 0)
                return;

            throw new InvalidOperationException(
                "Interrupted toolset save detected - packing now could ship an incomplete area. " +
                "Open the module in the SWLOR Toolset to recover it, then pack again. Pending files:" +
                Environment.NewLine +
                string.Join(Environment.NewLine, pending));
        }

        /// <summary>
        /// Refuses to pack a partially moved logical resource. The toolset writes this manifest
        /// before moving the first area/dialog/script companion and removes it only at the commit
        /// point. This check runs after the CLI acquires the module lease, so it also catches a
        /// second toolset that crashes during PackService's preceding CLI build.
        /// </summary>
        private static void RequireNoInterruptedResourceDelete()
        {
            var pending = Directory.GetFiles(
                ".",
                ResourceDeleteTransactionPattern,
                SearchOption.TopDirectoryOnly);
            if (pending.Length == 0)
                return;

            throw new InvalidOperationException(
                "Interrupted toolset resource delete detected - packing now could ship an incomplete " +
                "area, dialog, or script. Open the module in the SWLOR Toolset to recover it, then " +
                "pack again. Pending files:" + Environment.NewLine +
                string.Join(Environment.NewLine, pending));
        }

        private static string InterruptedSaveTarget(string backupPath)
        {
            // "area.git.json.<transaction-id>.save-backup" -> "area.git.json"
            var withoutSuffix = backupPath[..^".save-backup".Length];
            return Path.ChangeExtension(withoutSuffix, null);
        }

        /// <summary>
        /// Refuses to pack while an interrupted new-area creation is pending. NewAreaWriter (in
        /// SWLOR.Toolset.Domain) writes a ".swlor-toolset-new-area-&lt;resref&gt;.pending" marker to the
        /// module root before the first ARE/GIT/GIC destination file, precisely so a kill mid-write
        /// leaves recoverable evidence - the marker is deleted only once module.ifo is committed with
        /// the new area registered. Unlike an interrupted save, simply reopening the module in the
        /// toolset does NOT clear this marker: NewAreaWriter only recognizes and rolls back its own
        /// partial triplet the next time the SAME resref is created again through the New Area wizard.
        /// GetFileList would otherwise pack whichever partial area files exist with no module.ifo
        /// entry pointing at them, so this fails loudly instead of guessing at recovery.
        /// </summary>
        private static void RequireNoInterruptedAreaCreation()
        {
            var markers = Directory.GetFiles(".", NewAreaPendingMarkerPrefix + "*" + NewAreaPendingMarkerSuffix);
            if (markers.Length == 0)
                return;

            var pendingResRefs = markers
                .Select(marker => Path.GetFileNameWithoutExtension(Path.GetFileName(marker)))
                .Select(nameWithoutMarkerExtension => nameWithoutMarkerExtension[NewAreaPendingMarkerPrefix.Length..])
                .ToList();

            throw new InvalidOperationException(
                "Interrupted area creation detected - packing now could ship a partial area with no " +
                "module.ifo entry. Open the module in the SWLOR Toolset and use New Area with the " +
                "same ResRef to complete or roll back the interrupted creation, then pack again. " +
                $"Pending area(s): {string.Join(", ", pendingResRefs)}" +
                Environment.NewLine +
                string.Join(Environment.NewLine, markers));
        }

        private static void RequireNoInterruptedErfImport()
        {
            var markers = Directory.GetFiles(".", ErfImportPendingMarkerPattern);
            if (markers.Length == 0)
                return;

            throw new InvalidOperationException(
                "Interrupted ERF import detected. Open the module in the SWLOR Toolset to recover " +
                "the import before packing or unpacking." +
                Environment.NewLine +
                string.Join(Environment.NewLine, markers));
        }

        private static void RequireNoInterruptedItemRename()
        {
            var markers = Directory.GetFiles(".", ItemRenamePendingMarkerPattern);
            if (markers.Length == 0)
                return;

            throw new InvalidOperationException(
                "Interrupted item rename detected. Open the module in the SWLOR Toolset to recover " +
                "the rename before packing or unpacking." +
                Environment.NewLine +
                string.Join(Environment.NewLine, markers));
        }

        private static List<string> GetFileList()
        {
            var results = new List<string>();
            foreach (var folder in GetModuleFolders())
            {
                if (!Directory.Exists(folder))
                    continue;

                // Only the real resources. Atomic-save temporaries and rollback backups can remain
                // beside their target after an interrupted/locked write. GetFileNameWithoutExtension
                // strips their final suffix, so accepting either would convert and pack transaction
                // debris as a real module resource.
                results.AddRange(Directory.GetFiles(folder)
                    .Where(file =>
                        !file.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase) &&
                        !file.EndsWith(".save-backup", StringComparison.OrdinalIgnoreCase)));
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
