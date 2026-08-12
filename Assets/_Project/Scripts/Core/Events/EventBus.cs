using System;
using System.Collections.Generic;
using UnityEngine;

namespace PatternGame.Core.Events
{
    public sealed class EventBus : IEventBus
    {
        interface IEventChannel
        {
        }

        sealed class Subscription<TEvent> : IDisposable where TEvent : struct, IEvent
        {
            readonly EventChannel<TEvent> channel;
            Action<TEvent> handler;

            public Subscription(EventChannel<TEvent> channel, Action<TEvent> handler)
            {
                this.channel = channel;
                this.handler = handler;
            }

            public bool IsActive => handler != null;

            public void Invoke(in TEvent message)
            {
                handler.Invoke(message);
            }

            public void Dispose()
            {
                if (handler == null)
                {
                    return;
                }

                handler = null;
                channel.Remove(this);
            }
        }

        sealed class EventChannel<TEvent> : IEventChannel where TEvent : struct, IEvent
        {
            static readonly Subscription<TEvent>[] EmptySnapshot = Array.Empty<Subscription<TEvent>>();

            readonly List<Subscription<TEvent>> subscriptions = new();

            Subscription<TEvent>[] snapshot = EmptySnapshot;
            bool isSnapshotStale;

            public Subscription<TEvent> Add(Action<TEvent> handler)
            {
                var subscription = new Subscription<TEvent>(this, handler);
                subscriptions.Add(subscription);
                isSnapshotStale = true;
                return subscription;
            }

            public void Remove(Subscription<TEvent> subscription)
            {
                if (subscriptions.Remove(subscription))
                {
                    isSnapshotStale = true;
                }
            }

            public Subscription<TEvent>[] GetSnapshot()
            {
                if (isSnapshotStale)
                {
                    snapshot = subscriptions.Count == 0 ? EmptySnapshot : subscriptions.ToArray();
                    isSnapshotStale = false;
                }

                return snapshot;
            }
        }

        readonly Dictionary<Type, IEventChannel> channels = new();

        public IDisposable Subscribe<TEvent>(Action<TEvent> handler) where TEvent : struct, IEvent
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            return GetOrCreateChannel<TEvent>().Add(handler);
        }

        public void Publish<TEvent>(in TEvent message) where TEvent : struct, IEvent
        {
            if (!channels.TryGetValue(typeof(TEvent), out var channel))
            {
                return;
            }

            var snapshot = ((EventChannel<TEvent>)channel).GetSnapshot();

            for (int i = 0; i < snapshot.Length; i++)
            {
                var subscription = snapshot[i];
                if (!subscription.IsActive)
                {
                    continue;
                }

                try
                {
                    subscription.Invoke(message);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }
        }

        public void Clear()
        {
            channels.Clear();
        }

        EventChannel<TEvent> GetOrCreateChannel<TEvent>() where TEvent : struct, IEvent
        {
            var eventType = typeof(TEvent);

            if (channels.TryGetValue(eventType, out var existing))
            {
                return (EventChannel<TEvent>)existing;
            }

            var created = new EventChannel<TEvent>();
            channels.Add(eventType, created);
            return created;
        }
    }
}
