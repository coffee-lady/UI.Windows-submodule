using System;
using UnityEngine.UI.Windows.Utilities;

namespace UnityEngine.UI.Windows.Modules
{
    public enum AnimationState
    {
        Current = 0,
        Reset = 1,
        Show = 2,
        Hide = 3
    }

    [Serializable]
    public abstract class AnimationParameters
    {
        [Tooltip("Use config file or parameters at this component")]
        public AnimationParametersConfig config;

        [Space] public float durationShow = 0.3f;

        public float durationHide = 0.3f;
        public float delayShow;
        public float delayHide;
        public Tweener.EaseFunction easeShow = Tweener.EaseFunction.Linear;
        public Tweener.EaseFunction easeHide = Tweener.EaseFunction.Linear;
        
        public virtual void OnValidate()
        {
        }

        public float GetDuration(AnimationState animationState)
        {
            var delay = 0f;
            switch (animationState)
            {
                case AnimationState.Show:
                    delay = durationShow;
                    break;

                case AnimationState.Hide:
                    delay = durationHide;
                    break;
            }

            return delay;
        }

        public float GetDelay(AnimationState animationState)
        {
            var delay = 0f;
            switch (animationState)
            {
                case AnimationState.Show:
                    delay = delayShow;
                    break;

                case AnimationState.Hide:
                    delay = delayHide;
                    break;
            }

            return delay;
        }

        public abstract State LerpState(State from, State to, float value);

        public abstract void ApplyState(State state);

        public State GetState(AnimationState state, bool clone = false)
        {
            State copy = null;
            if (clone)
            {
                copy = CreateState();
            }

            State result = null;
            switch (state)
            {
                case AnimationState.Current:
                    result = GetCurrentState();
                    break;

                case AnimationState.Reset:
                    result = GetResetState();
                    break;

                case AnimationState.Show:
                    result = GetInState();
                    break;

                case AnimationState.Hide:
                    result = GetOutState();
                    break;
            }

            if (clone)
            {
                copy.CopyFrom(result);
                result = copy;
            }

            return result;
        }

        public abstract State CreateState();
        public abstract State GetCurrentState();
        public abstract State GetResetState();
        public abstract State GetInState();
        public abstract State GetOutState();

        public abstract class State
        {
            public abstract void CopyFrom(State other);
            public abstract void Recycle();
        }
    }
}