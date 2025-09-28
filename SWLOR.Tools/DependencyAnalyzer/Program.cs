using System;
using System.IO;
using SWLOR.Tools.DependencyAnalyzer;

namespace SWLOR.Tools.DependencyAnalyzer
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("SWLOR Circular Dependency Analyzer");
            Console.WriteLine("==================================");
            Console.WriteLine();

            try
            {
                // Run dependency analysis
                var analyzer = new ServiceDependencyAnalyzer();
                var report = analyzer.GenerateReport();
                
                Console.WriteLine(report);
                
                // Save report to file
                var reportPath = Path.Combine(Directory.GetCurrentDirectory(), "dependency-analysis-report.txt");
                File.WriteAllText(reportPath, report);
                Console.WriteLine($"Report saved to: {reportPath}");
                Console.WriteLine();

                // Generate fix recommendations
                var fixGenerator = new QuickFixGenerator();
                var fixReport = fixGenerator.GenerateFixReport();
                
                Console.WriteLine(fixReport);
                
                // Save fix report to file
                var fixReportPath = Path.Combine(Directory.GetCurrentDirectory(), "dependency-fix-report.txt");
                File.WriteAllText(fixReportPath, fixReport);
                Console.WriteLine($"Fix report saved to: {fixReportPath}");
                Console.WriteLine();

                // Generate code fixes
                var codeFixes = fixGenerator.GenerateInitializationFixes();
                var codeFixesPath = Path.Combine(Directory.GetCurrentDirectory(), "generated-code-fixes.cs");
                File.WriteAllText(codeFixesPath, codeFixes);
                Console.WriteLine($"Code fixes saved to: {codeFixesPath}");
                Console.WriteLine();

                Console.WriteLine("Analysis complete! Check the generated files for detailed information.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during analysis: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }

            Console.WriteLine();
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
