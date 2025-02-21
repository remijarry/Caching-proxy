using System.Collections.Concurrent;

namespace CachingProxy.Cache;

public class Cache
{
  private ConcurrentDictionary<string, string> _dictionary;
}