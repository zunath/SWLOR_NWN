using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using SWLOR.Game.Server;

namespace SWLOR.Tools.DependencyAnalyzer
{
    /// <summary>
    /// Analyzes service dependencies to identify circular references and lazy loading patterns
    /// </summary>
    public class ServiceDependencyAnalyzer
    {
        private readonly Dictionary<Type, List<Type>> _dependencies = new();
        private readonly Dictionary<Type, List<Type>> _lazyDependencies = new();
        private readonly List<CircularDependency> _circularDependencies = new();

        public class CircularDependency
        {
            public List<Type> Cycle { get; set; } = new();
            public string Description => string.Join(" -> ", Cycle.Select(t => t.Name)) + " -> " + Cycle.First().Name;
        }

        public class ServiceAnalysis
        {
            public Type ServiceType { get; set; } = null!;
            public List<Type> DirectDependencies { get; set; } = new();
            public List<Type> LazyDependencies { get; set; } = new();
            public int DependencyCount => DirectDependencies.Count + LazyDependencies.Count;
            public bool HasCircularDependency { get; set; }
            public List<CircularDependency> CircularDependencies { get; set; } = new();
        }

        /// <summary>
        /// Analyzes all services in the application
        /// </summary>
        public List<ServiceAnalysis> AnalyzeAllServices()
        {
            var services = new ServiceCollection();
            services.AddServices();
            
            var serviceTypes = services.Select(s => s.ServiceType).ToList();
            var analyses = new List<ServiceAnalysis>();

            foreach (var serviceType in serviceTypes)
            {
                var analysis = AnalyzeService(serviceType);
                analyses.Add(analysis);
            }

            // Detect circular dependencies
            DetectCircularDependencies();

            return analyses;
        }

        /// <summary>
        /// Analyzes a specific service type
        /// </summary>
        private ServiceAnalysis AnalyzeService(Type serviceType)
        {
            var analysis = new ServiceAnalysis
            {
                ServiceType = serviceType
            };

            // Get constructor dependencies
            var constructors = serviceType.GetConstructors();
            if (constructors.Length > 0)
            {
                var constructor = constructors.OrderByDescending(c => c.GetParameters().Length).First();
                var parameters = constructor.GetParameters();
                
                foreach (var param in parameters)
                {
                    if (param.ParameterType.IsInterface)
                    {
                        analysis.DirectDependencies.Add(param.ParameterType);
                        AddDependency(serviceType, param.ParameterType);
                    }
                }
            }

            // Get lazy-loaded dependencies (properties with GetRequiredService)
            var properties = serviceType.GetProperties(BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var property in properties)
            {
                if (property.PropertyType.IsInterface && HasLazyLoadingPattern(property))
                {
                    analysis.LazyDependencies.Add(property.PropertyType);
                    AddLazyDependency(serviceType, property.PropertyType);
                }
            }

            // Get lazy-loaded dependencies (fields with GetRequiredService)
            var fields = serviceType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var field in fields)
            {
                if (field.FieldType.IsInterface && HasLazyLoadingPattern(field))
                {
                    analysis.LazyDependencies.Add(field.FieldType);
                    AddLazyDependency(serviceType, field.FieldType);
                }
            }

            return analysis;
        }

        /// <summary>
        /// Checks if a property or field has lazy loading pattern
        /// </summary>
        private bool HasLazyLoadingPattern(MemberInfo member)
        {
            // This is a simplified check - in reality, you'd need to analyze the IL code
            // For now, we'll look for common patterns in the codebase
            return member.Name.Contains("Service") || 
                   member.Name.Contains("Cache") || 
                   member.Name.Contains("Manager");
        }

        /// <summary>
        /// Adds a direct dependency relationship
        /// </summary>
        private void AddDependency(Type service, Type dependency)
        {
            if (!_dependencies.ContainsKey(service))
                _dependencies[service] = new List<Type>();
            
            if (!_dependencies[service].Contains(dependency))
                _dependencies[service].Add(dependency);
        }

        /// <summary>
        /// Adds a lazy dependency relationship
        /// </summary>
        private void AddLazyDependency(Type service, Type dependency)
        {
            if (!_lazyDependencies.ContainsKey(service))
                _lazyDependencies[service] = new List<Type>();
            
            if (!_lazyDependencies[service].Contains(dependency))
                _lazyDependencies[service].Add(dependency);
        }

        /// <summary>
        /// Detects circular dependencies using DFS
        /// </summary>
        private void DetectCircularDependencies()
        {
            var visited = new HashSet<Type>();
            var recursionStack = new HashSet<Type>();

            foreach (var service in _dependencies.Keys)
            {
                if (!visited.Contains(service))
                {
                    DetectCircularDependencyDFS(service, visited, recursionStack, new List<Type>());
                }
            }
        }

        /// <summary>
        /// DFS implementation for circular dependency detection
        /// </summary>
        private void DetectCircularDependencyDFS(Type service, HashSet<Type> visited, HashSet<Type> recursionStack, List<Type> path)
        {
            visited.Add(service);
            recursionStack.Add(service);
            path.Add(service);

            if (_dependencies.ContainsKey(service))
            {
                foreach (var dependency in _dependencies[service])
                {
                    if (!visited.Contains(dependency))
                    {
                        DetectCircularDependencyDFS(dependency, visited, recursionStack, new List<Type>(path));
                    }
                    else if (recursionStack.Contains(dependency))
                    {
                        // Found a circular dependency
                        var cycleStart = path.IndexOf(dependency);
                        var cycle = path.Skip(cycleStart).ToList();
                        cycle.Add(dependency); // Complete the cycle
                        
                        _circularDependencies.Add(new CircularDependency { Cycle = cycle });
                    }
                }
            }

            recursionStack.Remove(service);
        }

        /// <summary>
        /// Generates a comprehensive report
        /// </summary>
        public string GenerateReport()
        {
            var analyses = AnalyzeAllServices();
            var report = new System.Text.StringBuilder();

            report.AppendLine("=== SWLOR Service Dependency Analysis Report ===");
            report.AppendLine($"Total Services: {analyses.Count}");
            report.AppendLine($"Services with Lazy Dependencies: {analyses.Count(a => a.LazyDependencies.Any())}");
            report.AppendLine($"Total Circular Dependencies: {_circularDependencies.Count}");
            report.AppendLine();

            // Top services by dependency count
            report.AppendLine("=== Top 10 Services by Dependency Count ===");
            var topDependencies = analyses
                .OrderByDescending(a => a.DependencyCount)
                .Take(10);
            
            foreach (var analysis in topDependencies)
            {
                report.AppendLine($"{analysis.ServiceType.Name}: {analysis.DependencyCount} dependencies");
                report.AppendLine($"  Direct: {analysis.DirectDependencies.Count}, Lazy: {analysis.LazyDependencies.Count}");
            }
            report.AppendLine();

            // Circular dependencies
            if (_circularDependencies.Any())
            {
                report.AppendLine("=== Circular Dependencies ===");
                foreach (var circular in _circularDependencies)
                {
                    report.AppendLine($"CYCLE: {circular.Description}");
                }
                report.AppendLine();
            }

            // Services with most lazy dependencies
            report.AppendLine("=== Services with Most Lazy Dependencies ===");
            var topLazy = analyses
                .Where(a => a.LazyDependencies.Any())
                .OrderByDescending(a => a.LazyDependencies.Count)
                .Take(10);
            
            foreach (var analysis in topLazy)
            {
                report.AppendLine($"{analysis.ServiceType.Name}: {analysis.LazyDependencies.Count} lazy dependencies");
                foreach (var lazyDep in analysis.LazyDependencies)
                {
                    report.AppendLine($"  - {lazyDep.Name}");
                }
            }

            return report.ToString();
        }
    }
}
