using System;
using System.Collections.Generic;

namespace PatternGame.Core.States
{
    public sealed class StateMachine
    {
        const int MaxTransitionsPerCall = 32;

        readonly Dictionary<Type, IState> states = new();

        IState currentState;
        ITickableState currentTickable;
        Type pendingStateType;
        bool isDeferringTransitions;

        public Type CurrentStateType => currentState?.GetType();

        public void AddState(IState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            var stateType = state.GetType();

            if (states.ContainsKey(stateType))
            {
                throw new InvalidOperationException($"State '{stateType.Name}' is already registered.");
            }

            states.Add(stateType, state);
        }

        public void ChangeState<TState>() where TState : IState
        {
            var stateType = typeof(TState);

            if (!states.ContainsKey(stateType))
            {
                throw new InvalidOperationException($"State '{stateType.Name}' is not registered.");
            }

            pendingStateType = stateType;

            if (isDeferringTransitions)
            {
                return;
            }

            ProcessPendingTransitions();
        }

        public void Tick(float deltaTime)
        {
            if (currentTickable != null)
            {
                isDeferringTransitions = true;

                try
                {
                    currentTickable.Tick(deltaTime);
                }
                finally
                {
                    isDeferringTransitions = false;
                }
            }

            if (pendingStateType != null)
            {
                ProcessPendingTransitions();
            }
        }

        public bool IsInState<TState>() where TState : IState
        {
            return currentState is TState;
        }

        public void Stop()
        {
            pendingStateType = null;

            var exitingState = currentState;
            currentState = null;
            currentTickable = null;

            exitingState?.Exit();
        }

        void ProcessPendingTransitions()
        {
            isDeferringTransitions = true;

            try
            {
                int transitionCount = 0;

                while (pendingStateType != null)
                {
                    transitionCount++;

                    if (transitionCount > MaxTransitionsPerCall)
                    {
                        pendingStateType = null;
                        throw new InvalidOperationException(
                            $"Exceeded {MaxTransitionsPerCall} transitions in a single call. " +
                            "Two states are most likely entering each other in a loop.");
                    }

                    var nextState = states[pendingStateType];
                    pendingStateType = null;

                    currentState?.Exit();

                    currentState = nextState;
                    currentTickable = nextState as ITickableState;

                    currentState.Enter();
                }
            }
            finally
            {
                isDeferringTransitions = false;
            }
        }
    }
}
