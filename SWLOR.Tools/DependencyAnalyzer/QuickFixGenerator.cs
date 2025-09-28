using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using SWLOR.Game.Server;

namespace SWLOR.Tools.DependencyAnalyzer
{
    /// <summary>
    /// Generates quick fixes for circular dependency issues
    /// </summary>
    public class QuickFixGenerator
    {
        /// <summary>
        /// Generates initialization code for services with lazy loading patterns
        /// </summary>
        public string GenerateInitializationFixes()
        {
            var services = new ServiceCollection();
            services.AddServices();
            
            var serviceTypes = services.Select(s => s.ServiceType).ToList();
            var fixes = new StringBuilder();
            
            fixes.AppendLine("// Generated initialization fixes for circular dependencies");
            fixes.AppendLine("// Add these to your service classes to eliminate lazy loading");
            fixes.AppendLine();

            foreach (var serviceType in serviceTypes)
            {
                var lazyProperties = GetLazyLoadingProperties(serviceType);
                if (lazyProperties.Any())
                {
                    fixes.AppendLine($"// Fix for {serviceType.Name}");
                    fixes.AppendLine($"public class {serviceType.Name} : I{serviceType.Name}, IServiceInitializerSync");
                    fixes.AppendLine("{");
                    fixes.AppendLine("    // Remove lazy loading properties and add as constructor parameters");
                    foreach (var prop in lazyProperties)
                    {
                        fixes.AppendLine($"    private readonly {prop.PropertyType.Name} _{prop.Name.ToLower()};");
                    }
                    fixes.AppendLine();
                    fixes.AppendLine("    // Add to constructor:");
                    foreach (var prop in lazyProperties)
                    {
                        fixes.AppendLine($"    // {prop.PropertyType.Name} {prop.Name.ToLower()},");
                    }
                    fixes.AppendLine();
                    fixes.AppendLine("    // Add initialization method:");
                    fixes.AppendLine("    public void Initialize()");
                    fixes.AppendLine("    {");
                    fixes.AppendLine("        // Move initialization logic here");
                    fixes.AppendLine("    }");
                    fixes.AppendLine();
                    fixes.AppendLine("    public int InitializationPriority => 200; // Adjust as needed");
                    fixes.AppendLine("    public string ServiceName => \"" + serviceType.Name + "\";");
                    fixes.AppendLine("}");
                    fixes.AppendLine();
                }
            }

            return fixes.ToString();
        }

        /// <summary>
        /// Gets properties that use lazy loading patterns
        /// </summary>
        private List<System.Reflection.PropertyInfo> GetLazyLoadingProperties(Type serviceType)
        {
            var properties = serviceType.GetProperties(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return properties.Where(p => p.PropertyType.IsInterface && 
                                       (p.Name.Contains("Service") || p.Name.Contains("Cache") || p.Name.Contains("Manager")))
                           .ToList();
        }

        /// <summary>
        /// Generates a service registration update
        /// </summary>
        public string GenerateServiceRegistrationUpdate()
        {
            return @"
// Add this to your ServiceCollectionExtensions.cs files:

public static IServiceCollection AddYourServices(this IServiceCollection services)
{
    // Register services normally
    services.AddSingleton<IMyService, MyService>();
    
    // Register as initializer if it implements IServiceInitializerSync
    if (typeof(IServiceInitializerSync).IsAssignableFrom(typeof(MyService)))
    {
        services.AddSingleton<IServiceInitializerSync>(provider => 
            provider.GetRequiredService<IMyService>());
    }
    
    return services;
}";
        }

        /// <summary>
        /// Generates a comprehensive fix report
        /// </summary>
        public string GenerateFixReport()
        {
            var analyzer = new ServiceDependencyAnalyzer();
            var analyses = analyzer.AnalyzeAllServices();
            var report = new StringBuilder();

            report.AppendLine("=== CIRCULAR DEPENDENCY FIX REPORT ===");
            report.AppendLine();

            // Services with most lazy dependencies
            var topLazyServices = analyses
                .Where(a => a.LazyDependencies.Any())
                .OrderByDescending(a => a.LazyDependencies.Count)
                .Take(10);

            report.AppendLine("TOP 10 SERVICES TO FIX (Most Lazy Dependencies):");
            foreach (var analysis in topLazyServices)
            {
                report.AppendLine($"1. {analysis.ServiceType.Name} - {analysis.LazyDependencies.Count} lazy deps");
                report.AppendLine($"   Lazy Dependencies: {string.Join(", ", analysis.LazyDependencies.Select(d => d.Name))}");
                report.AppendLine();
            }

            // Services with most total dependencies
            var topDependencyServices = analyses
                .OrderByDescending(a => a.DependencyCount)
                .Take(10);

            report.AppendLine("TOP 10 SERVICES BY TOTAL DEPENDENCIES:");
            foreach (var analysis in topDependencyServices)
            {
                report.AppendLine($"1. {analysis.ServiceType.Name} - {analysis.DependencyCount} total deps");
                report.AppendLine($"   Direct: {analysis.DirectDependencies.Count}, Lazy: {analysis.LazyDependencies.Count}");
                report.AppendLine();
            }

            // Priority order for fixes
            report.AppendLine("RECOMMENDED FIX ORDER:");
            report.AppendLine("1. Fix services with >10 lazy dependencies (Critical)");
            report.AppendLine("2. Fix services with >15 total dependencies (High)");
            report.AppendLine("3. Fix services with >5 lazy dependencies (Medium)");
            report.AppendLine("4. Fix remaining services (Low)");
            report.AppendLine();

            report.AppendLine("QUICK FIX STEPS:");
            report.AppendLine("1. Add IServiceInitializerSync interface to service");
            report.AppendLine("2. Move lazy-loaded properties to constructor parameters");
            report.AppendLine("3. Move initialization logic to Initialize() method");
            report.AppendLine("4. Register service as IServiceInitializerSync in DI");
            report.AppendLine("5. Test that circular dependency is resolved");

            return report.ToString();
        }
    }
}
