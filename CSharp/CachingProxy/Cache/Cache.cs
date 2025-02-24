using System.Collections.Concurrent;
using caching_proxy.Cache.Models;
using CachingProxy.Exceptions;

namespace CachingProxy.Caching;

public class Cache
{
  private ConcurrentDictionary<string, CachedItem> _dictionary = new();

  internal bool HasKey(string targetUrl)
  {
    if (string.IsNullOrEmpty(targetUrl))
    {
      return false;
    }

    return _dictionary.ContainsKey(targetUrl);
  }

  internal CachedItem? GetValue(string key)
  {
    if (!HasKey(key))
    {
      return null;
    }

    return _dictionary[key];
  }

  internal bool AddEntry(string key, CachedItem value)
  {
    if (HasKey(key))
    {
      throw new KeyAlreadyExistsException();
    }

    if (string.IsNullOrEmpty(key) || value == null)
    {
      throw new ArgumentException("key or value cannot be null or an empty string.");
    }

    return _dictionary.TryAdd(key, value);

  }
}