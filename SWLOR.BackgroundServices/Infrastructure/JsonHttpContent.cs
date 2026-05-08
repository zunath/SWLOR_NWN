using System.Text;
using Newtonsoft.Json;

namespace SWLOR.BackgroundServices.Infrastructure
{
    public static class JsonHttpContent
    {
        public static StringContent Create(object value)
        {
            return new StringContent(JsonConvert.SerializeObject(value), Encoding.UTF8, "application/json");
        }
    }
}
