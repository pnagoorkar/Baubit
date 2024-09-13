﻿using System.Collections;
namespace Baubit.Collections
{
    /// <summary>
    /// A threadsafe list allowing simultaneous reads and exclusive writes
    /// </summary>
    /// <typeparam name="T">The type of elements in the list</typeparam>
    public class ConcurrentList<T> : IList<T>
    {
        /// <inheritdoc/>
        public int Count
        {
            get
            {
                try
                {
                    _lock.EnterReadLock();
                    return _store.Count;
                }
                catch
                {
                    throw;
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }
        }

        /// <inheritdoc/>
        public T this[int index]
        {
            get
            {
                try
                {
                    _lock.EnterReadLock();
                    return _store[index];
                }
                catch
                {
                    throw;
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }

            set
            {
                try
                {
                    _lock.EnterWriteLock();
                    _store[index] = value;
                }
                catch
                {
                    throw;
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            }
        }


        public bool IsReadOnly => false;

        private readonly List<T> _store = new List<T>();
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public void Add(T item)
        {
            try
            {
                _lock.EnterWriteLock();
                _store.Add(item);
            }
            catch
            {
                throw;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <inheritdoc/>
        public void Clear()
        {
            try
            {
                _lock.EnterWriteLock();
                _store.Clear();
            }
            catch
            {
                throw;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <inheritdoc/>
        public bool Contains(T item)
        {
            try
            {
                _lock.EnterReadLock();
                return _store.Contains(item);
            }
            catch
            {
                throw;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        /// <inheritdoc/>
        public void CopyTo(T[] array, int arrayIndex)
        {
            try
            {
                _lock.EnterReadLock();
                _store.CopyTo(array, arrayIndex);
            }
            catch
            {
                throw;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        /// <inheritdoc/>
        public IEnumerator<T> GetEnumerator()
        {
            try
            {
                _lock.EnterReadLock();
                return _store.ToArray().AsEnumerable().GetEnumerator();
            }
            catch
            {
                throw;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        /// <inheritdoc/>
        public int IndexOf(T item)
        {
            try
            {
                _lock.EnterReadLock();
                return _store.IndexOf(item);
            }
            catch
            {
                throw;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        /// <inheritdoc/>
        public void Insert(int index, T item)
        {
            try
            {
                _lock.EnterWriteLock();
                _store.Insert(index, item);
            }
            catch
            {
                throw;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <inheritdoc/>
        public bool Remove(T item)
        {
            try
            {
                _lock.EnterWriteLock();
                return _store.Remove(item);
            }
            catch
            {
                throw;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <inheritdoc/>
        public void RemoveAt(int index)
        {
            try
            {
                _lock.EnterWriteLock();
                _store.RemoveAt(index);
            }
            catch
            {
                throw;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}