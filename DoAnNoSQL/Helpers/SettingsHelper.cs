using System;
using System.Linq;
using DoAnNoSQL.Models;
using MongoDB.Driver;

using System.Collections.Concurrent;

namespace DoAnNoSQL.Helpers
{
    public static class SettingsHelper
    {
        private static readonly ConcurrentDictionary<string, string> _cache = new ConcurrentDictionary<string, string>();
        private static DateTime _lastLoaded = DateTime.MinValue;
        private static readonly object _lock = new object();

        public static string GetValue(string key, string defaultValue = "")
        {
            try
            {
                if (_cache.IsEmpty || (DateTime.Now - _lastLoaded).TotalMinutes > 10)
                {
                    RefreshCache();
                }

                if (_cache.TryGetValue(key, out var val) && !string.IsNullOrEmpty(val))
                {
                    return val;
                }
            }
            catch (Exception)
            {
                // Fallback to default value on error
            }
            return defaultValue;
        }

        public static void ClearCache()
        {
            lock (_lock)
            {
                _cache.Clear();
                _lastLoaded = DateTime.MinValue;
            }
        }

        private static void RefreshCache()
        {
            lock (_lock)
            {
                if (!_cache.IsEmpty && (DateTime.Now - _lastLoaded).TotalMinutes <= 10)
                {
                    return;
                }

                try
                {
                    var db = new MongoDbContext();
                    var settings = db.tb_CauHinh.Collection.Find(Builders<tb_CauHinh>.Filter.Empty).ToList();
                    _cache.Clear();
                    foreach (var s in settings)
                    {
                        if (!string.IsNullOrEmpty(s.MaCauHinh))
                        {
                            _cache[s.MaCauHinh] = s.GiaTri ?? "";
                        }
                    }
                    _lastLoaded = DateTime.Now;
                }
                catch { }
            }
        }
    }
}
