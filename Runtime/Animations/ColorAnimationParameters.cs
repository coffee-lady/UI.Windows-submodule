using System;
using Sirenix.OdinInspector;

namespace UnityEngine.UI.Windows.Modules
{
    [Serializable]
    public class ColorAnimationParameters : AnimationParameters
    {
        [ReadOnly] [SerializeField] private ColorState currentState = new();

        [Space(10f)] public Graphic graphic;

        public ColorState resetState = new() {color = Color.white};
        public ColorState shownState = new() {color = Color.white};
        public ColorState hiddenState = new() {color = Color.white};

        public override State LerpState(State from, State to, float value)
        {
            var toState = (ColorState) to;
            if (from != null)
            {
                var fromState = (ColorState) from;
                currentState.color = Color.Lerp(fromState.color, toState.color, value);
            }
            else
            {
                currentState.color = Color.Lerp(currentState.color, toState.color, value);
            }

            return currentState;
        }

        public override void ApplyState(State state)
        {
            var toState = (ColorState) state;

            if (graphic != null)
            {
                graphic.color = toState.color;
            }

            currentState.CopyFrom(state);
        }

        public override State CreateState()
        {
            return PoolClass<ColorState>.Spawn();
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
        public class ColorState : State
        {
            public Color color;

            public override void CopyFrom(State other)
            {
                var otherColorState = (ColorState) other;
                color = otherColorState.color;
            }

            public override void Recycle()
            {
                color = default;
                PoolClass<ColorState>.Recycle(this);
            }
        }
    }
}