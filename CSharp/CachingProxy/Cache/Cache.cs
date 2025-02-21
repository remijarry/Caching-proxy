using System.Collections.Concurrent;
using CachingProxy.Exceptions;

namespace CachingProxy.Caching;

public class Cache
{
  private ConcurrentDictionary<string, string> _dictionary = new();

  internal bool HasKey(string targetUrl)
  {
    if (string.IsNullOrEmpty(targetUrl))
    {
      return false;
    }

    return _dictionary.ContainsKey(targetUrl);
  }

  internal string GetValue(string key)
  {
    if (!HasKey(key))
    {
      return string.Empty;
    }

    return _dictionary[key];
  }


  internal bool AddEntry(string key, string value)
  {
    if (HasKey(key))
    {
      throw new KeyAlreadyExistsException();
    }

    if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(value))
    {
      throw new ArgumentException("key or value cannot be null or an empty string.");
    }

    return _dictionary.TryAdd(key, value);

  }
}