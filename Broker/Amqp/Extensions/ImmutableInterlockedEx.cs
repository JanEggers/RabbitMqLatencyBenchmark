using System.Collections.Immutable;

namespace Broker.Amqp.Extensions;

public static class ImmutableInterlockedEx
{
    public static TValue Update<TKey, TValue>(ref ImmutableDictionary<TKey, TValue> location, TKey key, Func<TValue, TValue> updateValueFunction) 
    {
        return ImmutableInterlocked.AddOrUpdate(ref location, key, _ => throw new NotSupportedException(), (key, current) => updateValueFunction(current));
    }
}
