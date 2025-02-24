using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace caching_proxy.Cache.Models
{
    internal class CachedItem
    {
        public string Content { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public int StatusCode { get; set; }
        public List<KeyValuePair<string, IEnumerable<string>>> Headers { get; set; } = new();
    }
}
