using SWLOR.NWN.API.Contracts;

namespace SWLOR.Test.Shared.NWScriptMocks.NWNXPluginMocks
{
    /// <summary>
    /// Mock implementation of the ProfilerPlugin for testing purposes.
    /// Provides performance profiling functionality including timing metrics
    /// and performance scope management.
    /// </summary>
    public class ProfilerPluginMock: IProfilerPluginService
    {
        // Mock data storage
        private readonly Stack<PerformanceScope> _activeScopes = new();
        private readonly List<PerformanceMetric> _recordedMetrics = new();
        private readonly Dictionary<string, List<PerformanceMetric>> _metricsByName = new();
        private readonly Dictionary<string, List<PerformanceMetric>> _metricsByTag = new();
        private int _nextMetricId = 1;

        /// <summary>
        /// Push a timing metric scope - note that every push must be matched by a corresponding pop.
        /// </summary>
        /// <param name="name">The name to use for your metric</param>
        /// <param name="tag0_tag">An optional tag to filter your metrics</param>
        /// <param name="tag0_value">The tag's value for which to filter.</param>
        /// <param name="tag1_tag">An optional second tag to filter your metrics</param>
        /// <param name="tag1_value">The second tag's value for which to filter.</param>
        /// <param name="tag2_tag">An optional third tag to filter your metrics</param>
        /// <param name="tag2_value">The third tag's value for which to filter.</param>
        public void PushPerfScope(
            string name, 
            string tag0_tag = "", 
            string tag0_value = "",
            string tag1_tag = "",
            string tag1_value = "",
            string tag2_tag = "",
            string tag2_value = "")
        {
            var scope = new PerformanceScope
            {
                Id = _nextMetricId++,
                Name = name,
                StartTime = DateTime.UtcNow,
                Tag0Tag = tag0_tag,
                Tag0Value = tag0_value,
                Tag1Tag = tag1_tag,
                Tag1Value = tag1_value,
                Tag2Tag = tag2_tag,
                Tag2Value = tag2_value
            };

            _activeScopes.Push(scope);
        }

        /// <summary>
        /// Pushes a timing metric scope based on a specified object.
        /// </summary>
        /// <param name="target">The object to target</param>
        /// <param name="scriptName">The name of the script</param>
        public void PushPerfScope(uint target, string scriptName)
        {
            // Mock object type determination
            var objectTypeName = target == 0 ? "(unknown)" : "Creature";
            var areaResref = target == 0 ? "(unknown)" : "test_area";

            PushPerfScope("RunScript", 
                "Script", scriptName,
                "Area", areaResref,
                "ObjectType", objectTypeName);
        }

        /// <summary>
        /// Pops a timing metric.
        /// </summary>
        /// <remarks>A metric must already be pushed.</remarks>
        public void PopPerfScope()
        {
            if (_activeScopes.Count == 0)
            {
                throw new InvalidOperationException("No active performance scope to pop");
            }

            var scope = _activeScopes.Pop();
            var endTime = DateTime.UtcNow;
            var duration = endTime - scope.StartTime;

            var metric = new PerformanceMetric
            {
                Id = scope.Id,
                Name = scope.Name,
                StartTime = scope.StartTime,
                EndTime = endTime,
                DurationNanoseconds = (long)(duration.TotalMilliseconds * 1000000), // Convert to nanoseconds
                Tag0Tag = scope.Tag0Tag,
                Tag0Value = scope.Tag0Value,
                Tag1Tag = scope.Tag1Tag,
                Tag1Value = scope.Tag1Value,
                Tag2Tag = scope.Tag2Tag,
                Tag2Value = scope.Tag2Value
            };

            _recordedMetrics.Add(metric);

            // Index by name
            if (!_metricsByName.ContainsKey(metric.Name))
            {
                _metricsByName[metric.Name] = new List<PerformanceMetric>();
            }
            _metricsByName[metric.Name].Add(metric);

            // Index by tags
            if (!string.IsNullOrEmpty(metric.Tag0Tag) && !string.IsNullOrEmpty(metric.Tag0Value))
            {
                var tagKey = $"{metric.Tag0Tag}:{metric.Tag0Value}";
                if (!_metricsByTag.ContainsKey(tagKey))
                {
                    _metricsByTag[tagKey] = new List<PerformanceMetric>();
                }
                _metricsByTag[tagKey].Add(metric);
            }
        }

        /// <summary>
        /// Gets all recorded performance metrics.
        /// This is a helper method for testing purposes.
        /// </summary>
        /// <returns>A list of all recorded metrics.</returns>
        public List<PerformanceMetric> GetAllMetrics()
        {
            return new List<PerformanceMetric>(_recordedMetrics);
        }

        /// <summary>
        /// Gets performance metrics by name.
        /// This is a helper method for testing purposes.
        /// </summary>
        /// <param name="name">The metric name to filter by.</param>
        /// <returns>A list of metrics with the specified name.</returns>
        public List<PerformanceMetric> GetMetricsByName(string name)
        {
            return _metricsByName.TryGetValue(name, out var metrics) 
                ? new List<PerformanceMetric>(metrics) 
                : new List<PerformanceMetric>();
        }

        /// <summary>
        /// Gets performance metrics by tag.
        /// This is a helper method for testing purposes.
        /// </summary>
        /// <param name="tag">The tag to filter by.</param>
        /// <param name="value">The tag value to filter by.</param>
        /// <returns>A list of metrics with the specified tag.</returns>
        public List<PerformanceMetric> GetMetricsByTag(string tag, string value)
        {
            var tagKey = $"{tag}:{value}";
            return _metricsByTag.TryGetValue(tagKey, out var metrics) 
                ? new List<PerformanceMetric>(metrics) 
                : new List<PerformanceMetric>();
        }

        /// <summary>
        /// Gets the count of active performance scopes.
        /// This is a helper method for testing purposes.
        /// </summary>
        /// <returns>The number of active scopes.</returns>
        public int GetActiveScopeCount()
        {
            return _activeScopes.Count;
        }

        /// <summary>
        /// Gets the total count of recorded metrics.
        /// This is a helper method for testing purposes.
        /// </summary>
        /// <returns>The number of recorded metrics.</returns>
        public int GetMetricCount()
        {
            return _recordedMetrics.Count;
        }

        /// <summary>
        /// Gets the average duration for metrics with a specific name.
        /// This is a helper method for testing purposes.
        /// </summary>
        /// <param name="name">The metric name.</param>
        /// <returns>The average duration in nanoseconds.</returns>
        public double GetAverageDuration(string name)
        {
            var metrics = GetMetricsByName(name);
            if (metrics.Count == 0) return 0;

            return metrics.Average(m => m.DurationNanoseconds);
        }

        /// <summary>
        /// Gets the total duration for all metrics with a specific name.
        /// This is a helper method for testing purposes.
        /// </summary>
        /// <param name="name">The metric name.</param>
        /// <returns>The total duration in nanoseconds.</returns>
        public long GetTotalDuration(string name)
        {
            var metrics = GetMetricsByName(name);
            return metrics.Sum(m => m.DurationNanoseconds);
        }

        /// <summary>
        /// Clears all recorded metrics.
        /// This is a helper method for testing purposes.
        /// </summary>
        public void ClearMetrics()
        {
            _recordedMetrics.Clear();
            _metricsByName.Clear();
            _metricsByTag.Clear();
        }

        /// <summary>
        /// Resets all mock data to default values for testing.
        /// </summary>
        public void Reset()
        {
            _activeScopes.Clear();
            _recordedMetrics.Clear();
            _metricsByName.Clear();
            _metricsByTag.Clear();
            _nextMetricId = 1;
        }

        /// <summary>
        /// Gets all profiler data for testing verification.
        /// </summary>
        /// <returns>A ProfilerData object containing all settings.</returns>
        public ProfilerData GetProfilerDataForTesting()
        {
            return new ProfilerData
            {
                ActiveScopes = new Stack<PerformanceScope>(_activeScopes),
                RecordedMetrics = new List<PerformanceMetric>(_recordedMetrics),
                MetricsByName = new Dictionary<string, List<PerformanceMetric>>(_metricsByName),
                MetricsByTag = new Dictionary<string, List<PerformanceMetric>>(_metricsByTag),
                NextMetricId = _nextMetricId
            };
        }

        // Helper classes
        public class PerformanceScope
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public DateTime StartTime { get; set; }
            public string Tag0Tag { get; set; } = string.Empty;
            public string Tag0Value { get; set; } = string.Empty;
            public string Tag1Tag { get; set; } = string.Empty;
            public string Tag1Value { get; set; } = string.Empty;
            public string Tag2Tag { get; set; } = string.Empty;
            public string Tag2Value { get; set; } = string.Empty;
        }

        public class PerformanceMetric
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public DateTime StartTime { get; set; }
            public DateTime EndTime { get; set; }
            public long DurationNanoseconds { get; set; }
            public string Tag0Tag { get; set; } = string.Empty;
            public string Tag0Value { get; set; } = string.Empty;
            public string Tag1Tag { get; set; } = string.Empty;
            public string Tag1Value { get; set; } = string.Empty;
            public string Tag2Tag { get; set; } = string.Empty;
            public string Tag2Value { get; set; } = string.Empty;
        }

        public class ProfilerData
        {
            public Stack<PerformanceScope> ActiveScopes { get; set; } = new();
            public List<PerformanceMetric> RecordedMetrics { get; set; } = new();
            public Dictionary<string, List<PerformanceMetric>> MetricsByName { get; set; } = new();
            public Dictionary<string, List<PerformanceMetric>> MetricsByTag { get; set; } = new();
            public int NextMetricId { get; set; }
        }
    }
}
