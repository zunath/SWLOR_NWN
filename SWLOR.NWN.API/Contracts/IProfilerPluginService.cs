namespace SWLOR.NWN.API.Contracts
{
    public interface IProfilerPluginService
    {
        /// <summary>
        /// Push a timing metric scope - note that every push must be matched by a corresponding pop.
        ///
        /// A timing metric contains the following information.
        ///  ```c
        ///  {
        ///    metricName: [name], // Mandatory, from user code
        ///    metricFields: { time, nanoseconds }, // Automatically captured by the push/pop pair
        ///    metricTags: { [tag0_tag], [tag0_value] } // Optional, from user code, can be used to
        ///                                                filter metrics based on some category or,
        ///                                                constant e.g. objectType or area
        ///  }
        ///  ```
        ///
        /// If you don't understand how this works and you wish to use it, you should research
        /// the Metrics system (see Metrics.hpp) as well as googling about how InfluxDB stores metrics
        /// It's possible to have more than one tag pair per metric, It is just limited
        /// to one arbitrarily here. You can edit the prototype to include more and the C++
        /// code will cope with it correctly.
        /// </summary>
        /// <param name="name">The name to use for your metric</param>
        /// <param name="tag0_tag">An optional tag to filter your metrics</param>
        /// <param name="tag0_value">The tag's value for which to filter.</param>
        /// <param name="tag1_tag">An optional second tag to filter your metrics</param>
        /// <param name="tag1_value">The second tag's value for which to filter.</param>
        /// <param name="tag2_tag">An optional third tag to filter your metrics</param>
        /// <param name="tag2_value">The third tag's value for which to filter.</param>
        void PushPerfScope(
            string name, 
            string tag0_tag = "", 
            string tag0_value = "",
            string tag1_tag = "",
            string tag1_value = "",
            string tag2_tag = "",
            string tag2_value = "");

        /// <summary>
        /// Pushes a timing metric scope based on a specified object.
        /// </summary>
        /// <param name="target">The object to target</param>
        /// <param name="scriptName">The name of the script</param>
        void PushPerfScope(uint target, string scriptName);

        /// <summary>
        /// Pops a timing metric.
        /// </summary>
        /// <remarks>A metric must already be pushed.</remarks>
        void PopPerfScope();
    }
}