using System;
using Sirenix.OdinInspector;

namespace UnityEngine.UI.Windows.Modules
{
    [Serializable]
    public class RectAnimationParameters : AnimationParameters
    {
        [Flags]
        public enum AnimationParameter
        {
            Position = 0x1,
            Rotation = 0x2,
            Scale = 0x4
        }

        [Space(10f)] public RectTransform rectTransform;
        public AnimationParameter parameters = (AnimationParameter) (-1);

        [ReadOnly] [SerializeField] private RectState currentState = new();

        public RectState resetState = new()
        {
            anchorPosition = new Vector2(0f, -100f),
            rotation = Vector3.zero,
            scale = Vector3.one
        };

        public RectState shownState = new()
        {
            anchorPosition = Vector2.zero,
            rotation = Vector3.zero,
            scale = Vector3.one
        };

        public RectState hiddenState = new()
        {
            anchorPosition = new Vector2(0f, 100f),
            rotation = Vector3.zero,
            scale = Vector3.one
        };

        public override State LerpState(State from, State to, float value)
        {
            RectState fromState = null;
            var toState = (RectState) to;
            if (from != null)
            {
                fromState = (RectState) from;
            }
            else
            {
                fromState = currentState;
            }

            if ((parameters & AnimationParameter.Position) != 0)
            {
                currentState.anchorPosition = Vector2.Lerp(fromState.anchorPosition, toState.anchorPosition, value);
            }

            if ((parameters & AnimationParameter.Rotation) != 0)
            {
                currentState.rotation = Vector3.Slerp(fromState.rotation, toState.rotation, value);
            }

            if ((parameters & AnimationParameter.Scale) != 0)
            {
                currentState.scale = Vector3.Lerp(fromState.scale, toState.scale, value);
            }

            return currentState;
        }

        public override void ApplyState(State state)
        {
            if (rectTransform != null)
            {
                var toState = (RectState) state;
                if ((parameters & AnimationParameter.Position) != 0)
                {
                    rectTransform.anchoredPosition = toState.anchorPosition;
                }

                if ((parameters & AnimationParameter.Rotation) != 0)
                {
                    rectTransform.rotation = Quaternion.Euler(toState.rotation);
                }

                if ((parameters & AnimationParameter.Scale) != 0)
                {
                    rectTransform.localScale = toState.scale;
                }
            }

            currentState.CopyFrom(state);
        }

        public override State CreateState()
        {
            return PoolClass<RectState>.Spawn();
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
        public class RectState : State
        {
            public Vector2 anchorPosition;
            public Vector3 rotation;
            public Vector3 scale;

            public override void CopyFrom(State other)
            {
                var _other = (RectState) other;
                anchorPosition = _other.anchorPosition;
                rotation = _other.rotation;
                scale = _other.scale;
            }

            public override void Recycle()
            {
                anchorPosition = default;
                rotation = default;
                scale = default;
                PoolClass<RectState>.Recycle(this);
            }
        }
    }
}