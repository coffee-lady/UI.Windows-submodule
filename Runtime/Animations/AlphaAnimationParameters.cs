﻿using System;
using Sirenix.OdinInspector;

namespace UnityEngine.UI.Windows.Modules
{
    [Serializable]
    public class AlphaAnimationParameters : AnimationParameters
    {
        [ReadOnly] [SerializeField] private AlphaState currentState = new();

        [Space(10f)] public CanvasGroup canvasGroup;
        public AlphaState resetState = new() {alpha = 0f};
        public AlphaState shownState = new() {alpha = 1f};
        public AlphaState hiddenState = new() {alpha = 0f};

        public override State LerpState(State from, State to, float value)
        {
            var toState = (AlphaState) to;
            if (from != null)
            {
                var fromState = (AlphaState) from;
                currentState.alpha = Mathf.Lerp(fromState.alpha, toState.alpha, value);
            }
            else
            {
                currentState.alpha = Mathf.Lerp(currentState.alpha, toState.alpha, value);
            }

            return currentState;
        }

        public override void ApplyState(State state)
        {
            var toState = (AlphaState) state;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = toState.alpha;
            }

            currentState.CopyFrom(state);
        }

        public override State CreateState()
        {
            return PoolClass<AlphaState>.Spawn();
        }

        public override State GetCurrentState()
        {
            return currentState;
        }

        public override State GetResetState()
        {
            return resetState;
        }

        public override State GetInState()
        {
            return shownState;
        }

        public override State GetOutState()
        {
            return hiddenState;
        }

        [Serializable]
        public class AlphaState : State
        {
            [Range(0f, 1f)] public float alpha;

            public override void CopyFrom(State other)
            {
                var _other = (AlphaState) other;
                alpha = _other.alpha;
            }

            public override void Recycle()
            {
                alpha = default;
                PoolClass<AlphaState>.Recycle(this);
            }
        }
    }
}