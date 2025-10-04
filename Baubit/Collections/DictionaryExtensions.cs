using FluentResults;

namespace Baubit.Collections
{
    public static class DictionaryExtensions
    {
        public static Result<TVal?> TryGetValueOrDefault<TDictionary, TKey, TVal>(this TDictionary dictionary, TKey key) where TDictionary : IDictionary<TKey, TVal>
        {
            // TODO - Wrap in Result.Try(...)
            return dictionary.TryGetValue(key, out var value) ? Result.Ok<TVal?>(value) : Result.Ok(default(TVal));
        }
    }
}
