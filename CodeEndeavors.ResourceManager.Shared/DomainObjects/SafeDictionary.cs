    using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace CodeEndeavors.ResourceManager.DomainObjects
{
    /// <summary>
    /// SafeDictionary is a thread-safe wrapper around a generic
    /// Dictionary<TKey,TValue> object. Thread-safety is implemented using
    /// System.Threading.ReaderWriterLockSlim. This lock allows multi-thread
    /// access for reads, and exclusive access for writes.
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public class SafeDictionary<TKey,TValue>
    {
        private readonly ReaderWriterLockSlim _dictLock = new ReaderWriterLockSlim();
        private readonly Dictionary<TKey,TValue> _dict = new Dictionary<TKey, TValue>();

        /// <summary>
        /// Adds a value to the dictionary. The inner dictionary's Add method
        /// is used, so if the key already exists, an ArgumentException is
        /// thrown. 
        /// </summary>
        /// <param name="key">A key</param>
        /// <param name="value">A value</param>
        public void Add(TKey key, TValue value)
        {
            _dictLock.EnterWriteLock();
            try
            {
                _dict.Add(key, value);
            }
            finally
            {
                _dictLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Removes the value with the specified key from the dictionary.
        /// </summary>
        /// <param name="key">A key</param>
        /// <returns>
        /// true if the value was found and removed, false if the key is not 
        /// found.
        /// </returns>
        public bool Remove(TKey key)
        {
            _dictLock.EnterWriteLock();
            try
            {
                return _dict.Remove(key);
            }
            finally
            {
                _dictLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Removes all keys and values from the dictionary.
        /// </summary>
        public void Clear()
        {
            _dictLock.EnterWriteLock();
            try
            {
                _dict.Clear();
            }
            finally
            {
                _dictLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Gets or sets the value associated with the specified key. The
        /// setter uses the inner dictionary's indexer property to set the
        /// value, so if the key already exists, its value is replaced.
        /// </summary>
        /// <param name="key">A key</param>
        /// <returns>
        /// The getter returns the value associated with the key. Unlike the
        /// .NET framework's Dictionary, if the key is not found, 
        /// default(TValue) is returned (null for reference types).
        /// </returns>
        public TValue this[TKey key]
        {
            get
            {
                _dictLock.EnterReadLock();
                try
                {
                    //don't throw exception if it can be avoided
                    if (_dict.ContainsKey(key) == false)
                        return default(TValue);

                    return _dict[key];
                }
                catch (KeyNotFoundException ex)
                {
                    return default(TValue);
                }
                finally
                {
                    _dictLock.ExitReadLock();
                }
            }
            set
            {
                _dictLock.EnterWriteLock();
                try
                {
                    _dict[key] = value;
                }
                finally
                {
                    _dictLock.ExitWriteLock();
                }
            }
        }

        /// <summary>
        /// Gets the number of key/value pairs in the dictionary.
        /// </summary>
        public int Count
        {
            get
            {
                _dictLock.EnterReadLock();
                try
                {
                    return _dict.Count;
                }
                finally
                {
                    _dictLock.ExitReadLock();
                }
            }
        }

        /// <summary>
        /// Determines whether the dictionary contains the specified key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool ContainsKey(TKey key)
        {
            _dictLock.EnterReadLock();
            try
            {
                return _dict.ContainsKey(key);
            }
            finally
            {
                _dictLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Returns a list of keys in the dictionary. If the keys are of a
        /// reference type, references to the actual keys are returned, not
        /// copies. The caller should not modify them.
        /// </summary>
        public List<TKey> Keys
        {
            get
            {
                _dictLock.EnterReadLock();
                try
                {
                    var keys = new List<TKey>();
                    foreach (var kvp in _dict)
                    {
                        keys.Add(kvp.Key);
                    }
                    return keys;
                }
                finally
                {
                    _dictLock.ExitReadLock();
                }
            }
        }

        /// <summary>
        /// Returns a list of vlaues in the dictionary. If the values are of a
        /// reference type, references to the actual values are returned, not
        /// copies. The caller should not modify them.
        /// </summary>
        public List<TValue> Values
        {
            get
            {
                _dictLock.EnterReadLock();
                try
                {
                    var values = new List<TValue>();
                    foreach (var kvp in _dict)
                    {
                        values.Add(kvp.Value);
                    }
                    return values;
                }
                finally
                {
                    _dictLock.ExitReadLock();
                }
            }
        }
    }
}

