using System;

namespace PatternGame.Core.Events
{
    public interface IEvent
    {
    }

    public interface IEventBus
    {
        IDisposable Subscribe<TEvent>(Action<TEvent> handler) where TEvent : struct, IEvent;

        void Publish<TEvent>(in TEvent message) where TEvent : struct, IEvent;
    }
}
