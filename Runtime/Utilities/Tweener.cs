using System;
using System.Collections.Generic;

namespace UnityEngine.UI.Windows.Utilities
{
    public class Tweener : MonoBehaviour
    {
        public enum EaseFunction
        {
            Linear,
            InQuad,
            OutQuad,
            InOutQuad,
            InCubic,
            OutCubic,
            InOutCubic,
            InQuart,
            OutQuart,
            InOutQuart,
            InQuint,
            OutQuint,
            InOutQuint,
            InSine,
            OutSine,
            InOutSine,
            InExpo,
            OutExpo,
            InOutExpo,
            InCirc,
            OutCirc,
            InOutCirc,
            InElastic,
            OutElastic,
            InOutElastic,
            InBack,
            OutBack,
            InOutBack,
            InBounce,
            OutBounce,
            InOutBounce,
            OutInQuad,
            OutInCubic,
            OutInQuart,
            OutInQuint,
            OutInSine,
            OutInExpo,
            OutInCirc,
            OutInElastic,
            OutInBack,
            OutInBounce
        }

        public List<ITween> tweens = new();
        public List<ITween> frameTweens = new();

        private readonly List<ITween> completeList = new();

        public void Update()
        {
            float dt = Time.deltaTime;
            completeList.Clear();
            frameTweens.Clear();
            frameTweens.AddRange(tweens);
            for (int cnt = frameTweens.Count, i = cnt - 1; i >= 0; --i)
            {
                if (frameTweens[i].Update(dt))
                {
                    ITween tween = frameTweens[i];
                    completeList.Add(tween);
                }
            }

            for (int i = 0, cnt = completeList.Count; i < cnt; ++i)
            {
                tweens.Remove(completeList[i]);
                completeList[i].Complete();
            }

            completeList.Clear();
        }

        public Tween<T> Add<T>(T obj, float duration, float from, float to)
        {
            var tween = new Tween<T>();
            tween.tweener = this;
            tween.obj = obj;
            tween.duration = duration;
            tween.from = from;
            tween.to = to;
            tween.direction = 1f;

            tweens.Add(tween);

            return tween;
        }

        public void Stop(object tag, bool ignoreEvents = false)
        {
            List<ITween> list = PoolList<ITween>.Spawn();
            for (int i = tweens.Count - 1; i >= 0; --i)
            {
                ITween tw = tweens[i];
                if (tw.HasTag(tag))
                {
                    tweens.RemoveAt(i);
                    list.Add(tw);
                }
            }

            for (var i = 0; i < list.Count; ++i)
            {
                list[i].Stop(ignoreEvents);
            }

            PoolList<ITween>.Recycle(ref list);
        }

        private bool Step(ITween tween, float dt)
        {
            if (tween.Update(dt))
            {
                completeList.Add(tween);
                tweens.Remove(tween);
                return true;
            }

            return false;
        }

        public static class EaseFunctions
        {
            private static readonly Func<float, float, float, float, float>[] easings =
            {
                Linear,
                InQuad,
                OutQuad,
                InOutQuad,
                InCubic,
                OutCubic,
                InOutCubic,
                InQuart,
                OutQuart,
                InOutQuart,
                InQuint,
                OutQuint,
                InOutQuint,
                InSine,
                OutSine,
                InOutSine,
                InExpo,
                OutExpo,
                InOutExpo,
                InCirc,
                OutCirc,
                InOutCirc,
                InElastic,
                OutElastic,
                InOutElastic,
                InBack,
                OutBack,
                InOutBack,
                InBounce,
                OutBounce,
                InOutBounce,
                OutInQuad,
                OutInCubic,
                OutInQuart,
                OutInQuint,
                OutInSine,
                OutInExpo,
                OutInCirc,
                OutInElastic,
                OutInBack,
                OutInBounce
            };

            public static Func<float, float, float, float, float> GetEase(EaseFunction func)
            {
                return easings[(int) func];
            }

            #region Linear

            /// <summary>
            ///     Easing equation function for a simple linear tweening, with no easing.
            /// </summary>
            /// <param name="t">Current time in seconds.</param>
            /// <param name="b">Starting value.</param>
            /// <param name="c">Final value.</param>
            /// <param name="d">Duration of animation.</param>
            /// <returns>The correct value.</returns>
            public static float Linear(float t, float b, float c, float d)
            {
                return c * t / d + b;
            }

            #endregion

            #region Expo

            /// <summary>
            ///     Easing equation function for an exponential (2^t) easing out:
            ///     decelerating from zero velocity.
            /// </summary>
            /// <param name="t">Current time in seconds.</param>
            /// <param name="b">Starting value.</param>
            /// <param name="c">Final value.</param>
            /// <param name="d">Duration of animation.</param>
            /// <returns>The correct value.</returns>
            public static float OutExpo(float t, float b, float c, float d)
            {
                return t == d ? b + c : c * (-Mathf.Pow(2, -10 * t / d) + 1) + b;
            }

            /// <summary>
            ///     Easing equation function for an exponential (2^t) easing in:
            ///     accelerating from zero velocity.
            /// </summary>
            /// <param name="t">Current time in seconds.</param>
            /// <param name="b">Starting value.</param>
            /// <param name="c">Final value.</param>
            /// <param name="d">Duration of animation.</param>
            /// <returns>The correct value.</returns>
            public static float InExpo(float t, float b, float c, float d)
            {
                return t == 0 ? b : c * Mathf.Pow(2, 10 * (t / d - 1)) + b;
            }

            /// <summary>
            ///     Easing equation function for an exponential (2^t) easing in/out:
            ///     acceleration until halfway, then deceleration.
            /// </summary>
            /// <param name="t">Current time in seconds.</param>
            /// <param name="b">Starting value.</param>
            /// <param name="c">Final value.</param>
            /// <param name="d">Duration of animation.</param>
            /// <returns>The correct value.</returns>
            public static float InOutExpo(float t, float b, float c, float d)
            {
                if (t == 0)
                {
                    return b;
                }

                if (t == d)
                {
                    return b + c;
                }

                if ((t /= d / 2) < 1)
                {
                    return c / 2 * Mathf.Pow(2, 10 * (t - 1)) + b;
                }

                return c / 2 * (-Mathf.Pow(2, -10 * --t) + 2) + b;
            }

            /// <summary>
            ///     Easing equation function for an exponential (2^t) easing out/in:
            ///     deceleration until halfway, then acceleration.
            /// </summary>
            /// <param name="t">Current time in seconds.</param>
            /// <param name="b">Starting value.</param>
            /// <param name="c">Final value.</param>
            /// <param name="d">Duration of animation.</param>
            /// <returns>The correct value.</returns>
            public static float OutInExpo(float t, float b, float c, float d)
            {
                if (t < d / 2)
                {
                    return OutExpo(t * 2, b, c / 2, d);
                }

                return InExpo(t * 2 - d, b + c / 2, c / 2, d);
            }

            #endregion

            #region Circular

            /// <summary>
            ///     Easing equation function for a circular (sqrt(1-t^2)) easing out:
            ///     decelerating from zero velocity.
            /// </summary>
            /// <param name="t">Current time in seconds.</param>
            /// <param name="b">Starting value.</param>
            /// <param name="c">Final value.</param>
            /// <param name="d">Duration of animation.</param>
            /// <returns>The correct value.</returns>
            public static float OutCirc(float t, float b, float c, float d)
            {
                return c * Mathf.Sqrt(1 - (t = t / d - 1) * t) + b;
            }

            /// <summary>
            ///     Easing equation function for a circular (sqrt(1-t^2)) easing in:
            ///     accelerating from zero velocity.
            /// </summary>
            /// <param name="t">Current time in seconds.</param>
            /// <param name="b">Starting value.</param>
            /// <param name="c">Final value.</param>
            /// <param name="d">Duration of animation.</param>
            /// <returns>The correct value.</returns>
            public static float InCirc(float t, float b, float c, float d)
            {
                return -c * (Mathf.Sqrt(1 - (t /= d) * t) - 1) + b;
            }

            /// <summary>
            ///     Easing equation function for a circular (sqrt(1-t^2)) easing in/out:
            ///     acceleration until halfway, then deceleration.
            /// </summary>
            /// <param name="t">Current time in seconds.</param>
            /// <param name="b">Starting value.</param>
            /// <param name="c">Final value.</param>
            /// <param name="d">Duration of animation.</param>
            /// <returns>The correct value.</returns>
            public static float InOutCirc(float t, float b, float c, float d)
            {
                if ((t /= d / 2) < 1)
                {
                    return -c / 2 * (Mathf.Sqrt(1 - t * t) - 1) + b;
                }

                return c / 2 * (Mathf.Sqrt(1 - (t -= 2) * t) + 1) + b;
            }

            /// <summary>
            ///     Easing equation function for a circular (sqrt(1-t^2)) easing in/out:
            ///     acceleration until halfway, then deceleration.
            /// </summary>
            /// <param name="t">Current time in seconds.</param>
            /// <param name="b">Starting value.</param>
            /// <param name="c">Final value.</param>
            /// <param name="d">Duration of animation.</param>
            /// <returns>The correct value.</returns>
            public static float OutInCirc(float t, float b, float c, float d)
            {
                if (t < d / 2)
                {
                    return OutCirc(t * 2, b, c / 2, d);
                }

                return InCirc(t * 2 - d, b + c / 2, c / 2, d);
            }

            #endregion

            #region Quad

            /// <summary>
            ///     Easing equation function for a quadratic (t^2) easing out:
            ///     decelerating from zero velocity.
            /// </summary>
            /// <param name="t">Current time in seconds.</param>
            /// <param name="b">Starting value.</param>
            /// <param name="c">Final value.</param>
            /// <param name="d">Duration of animation.</param>
            /// <returns>The correct value.</returns>
            public static float OutQuad(float t, float b, float c, float d)
            {
                return -c * (t /= d) * (t - 2) + b;
            }

            /// <summary>
            ///     Easing equation function for a quadratic (t^2) easing in:
            ///     accelerating from zero velocity.
            /// </summary>
            /// <param name="t">Current time in seconds.</param>
            /// <param name="b">Starting value.</param>
            /// <param name="c">Final value.</param>
            /// <param name="d">Duration of animation.</param>
            /// <returns>The correct value.</returns>
            public static float InQuad(float t, float b, float c, float d)
            {
                return c * (t /= d) * t + b;
            }

            /// <summary>
            ///     Easing equation function for a quadratic (t^2) easing in/out:
            ///     acceleration until halfway, then deceleration.
            /// </summary>
            /// <param name="t">Current time in seconds.</param>
            /// <param name="b">Starting value.</param>
            /// <param name="c">Final value.</param>
            /// <param name="d">Duration of animation.</param>
            /// <returns>The correct value.</returns>
            public static float InOutQuad(float t, float b, float c, float d)
            {
                if ((t /= d / 2) < 1)
                {
                    return c / 2 * t * t + b;
                }

                return -c / 2 * (--t * (t - 2) - 1) + b;
            }

            /// <summary>
            ///     Easing equation function for a quadratic (t^2) easing out/in:
            ///     deceleration until halfway, then acceleration.
            /// </summary>
            /// <param name="t">Current time in seconds.</param>
            /// <param name="b">Starting value.</param>
            /// <param name="c">Final value.</param>
            /// <param name="d">Duration of animation.</param>
            /// <returns>The correct value.</returns>
            public static float OutInQuad(float t, float b, float c, float d)
            {
                if (t < d / 2)
                {
                    return OutQuad(t * 2, b, c / 2, d);
                }

                return InQuad(t * 2 - d, b + c / 2, c / 2, d);
            }

            #endregion

            #region Sine

            /// <summary>
            ///     Easing equation function for a sinusoidal (sin(t)) easing out:
            ///     decelerating from zero velocity.
            /// </summary>
            /// <param name="t">Current time in seconds.</param>
            /// <param name="b">Starting value.</param>
            /// <param name="c">Final value.</param>
            /// <param name="d">Duration of animation.</param>
            /// <returns>The correct value.</returns>
            public static float OutSine(float t, float b, float c, float d)
            {
                return c * Mathf.Sin(t / d * (Mathf.PI / 2)) + b;
            }

            /// <summary>
            ///     Easing equation function for a sinusoidal (sin(t)) easing in:
            ///     accelerating from zero velocity.
            /// </summary>
            /// <param name="t">Current time in seconds.</param>
            /// <param name="b">Starting value.</param>
            /// <param name="c">Final value.</param>
            /// <param name="d">Duration of animation.</param>
            /// <returns>The correct value.</returns>
            public static float InSine(float t, float b, float c, float d)
            {
                return -c * Mathf.Cos(t / d * (Mathf.PI / 2)) + c + b;
            }

            /// <summary>
            ///     Easing equation function for a sinusoidal (sin(t)) easing in/out:
            ///     acceleration until halfway, then deceleration.
            /// </summary>
            /// <param name="t">Current time in seconds.</param>
            /// <param name="b">Starting value.</param>
            /// <param name="c">Final value.</param>
            /// <param name="d">Duration of animation.</param>
            /// <returns>The correct value.</returns>
            public static float InOutSine(float t, float b, float c, float d)
            {
                if ((t /= d / 2) < 1)
                {
                    return c / 2 * Mathf.Sin(Mathf.PI * t / 2) + b;
                }

                return -c / 2 * (Mathf.Cos(Mathf.PI * --t / 2) - 2) + b;
            }

            /// <summary>
            ///     Easing equation function for a sinusoidal (sin(t)) easing in/out:
            ///     deceleration until halfway, then acceleration.
            /// </summary>
            /// <param name="t">Current time in seconds.</param>
            /// <param name="b">Starting value.</param>
            /// <param name="c">Final value.</param>
            /// <param name="d">Duration of animation.</param>
            /// <returns>The correct value.</returns>
            public static float OutInSine(float t, float b, float c, float d)
            {
                if (t < d / 2)
                {
                    return OutSine(t * 2, b, c / 2, d);
                }

                return InSine(t * 2 - d, b + c / 2, c / 2, d);
            }

            #endregion

            #region Cubic

            /// <summary>
            ///     Easing equation function for a cubic (t^3) easing out:
            ///     decelerating from zero velocity.
            /// </summary>
            /// <param name="t">Current time in seconds.</param>
            /// <param name="b">Starting value.</param>
            /// <param name="c">Final value.</param>
            /// <param name="d">Duration of animation.</param>
            /// <returns>The correct value.</returns>
            public static float OutCubic(float t, float b, float c, float d)
            {
                return c * ((t = t / d - 1) * t * t + 1) + b;
            }

            /// <summary>
            ///     Easing equation function for a cubic (t^3) easing in:
            ///     accelerating from zero velocity.
            /// </summary>
            /// <param name="t">Current time in seconds.</param>
            /// <param name="b">Starting value.</param>
            /// <param name="c">Final value.</param>
            /// <param name="d">Duration of animation.</param>
            /// <returns>The correct value.</returns>
            public static float InCubic(float t, float b, float c, float d)
            {
                return c * (t /= d) * t * t + b;
            }

            /// <summary>
            ///     Easing equation function for a cubic (t^3) easing in/out:
            ///     acceleration until halfway, then deceleration.
            /// </summary>
            /// <param name="t">Current time in seconds.</param>
            /// <param name="b">Starting value.</param>
            /// <param name="c">Final value.</param>
            /// <param name="d">Duration of animation.</param>
            /// <returns>The correct value.</returns>
            public static float InOutCubic(float t, float b, float c, float d)
            {
                if ((t /= d / 2) < 1)
                {
                    return c / 2 * t * t * t + b;
                }

                return c / 2 * ((t -= 2) * t * t + 2) + b;
            }

            /// <summary>
            ///     Easing equation function for a cubic (t^3) easing out/in:
            ///     deceleration until halfway, then acceleration.
            /// </summary>
            /// <param name="t">Current time in seconds.</param>
            /// <param name="b">Starting value.</param>
            /// <param name="c">Final value.</param>
            /// <param name="d">Duration of animation.</param>
            /// <returns>The correct value.</returns>
            public static float OutInCubic(float t, float b, float c, float d)
            {
                if (t < d / 2)
                {
                    return OutCubic(t * 2, b, c / 2, d);
                }

                return InCubic(t * 2 - d, b + c / 2, c / 2, d);
            }

            #endregion

            #region Quartic

            /// <summary>
            ///     Easing equation function for a quartic (t^4) easing out:
            ///     decelerating from zero velocity.
            /// </summary>
            /// <param name="t">Current time in seconds.</param>
            /// <param name="b">Starting value.</param>
            /// <param name="c">Final value.</param>
            /// <param name="d">Duration of animation.</param>
            /// <returns>The correct value.</returns>
            public static float OutQuart(float t, float b, float c, float d)
            {
                return -c * ((t = t / d - 1) * t * t * t - 1) + b;
            }

            /// <summary>
            ///     Easing equation function for a quartic (t^4) easing in:
            ///     accelerating from zero velocity.
            /// </summary>
            /// <param name="t">Current time in seconds.</param>
            /// <param name="b">Starting value.</param>
            /// <param name="c">Final value.</param>
            /// <param name="d">Duration of animation.</param>
            /// <returns>The correct value.</returns>
            public static float InQuart(float t, float b, float c, float d)
            {
                return c * (t /= d) * t * t * t + b;
            }

            /// <summary>
            ///     Easing equation function for a quartic (t^4) easing in/out:
            ///     acceleration until halfway, then deceleration.
            /// </summary>
            /// <param name="t">Current time in seconds.</param>
            /// <param name="b">Starting value.</param>
            /// <param name="c">Final value.</param>
            /// <param name="d">Duration of animation.</param>
            /// <returns>The correct value.</returns>
            public static float InOutQuart(float t, float b, float c, float d)
            {
                if ((t /= d / 2) < 1)
                {
                    return c / 2 * t * t * t * t + b;
                }

                return -c / 2 * ((t -= 2) * t * t * t - 2) + b;
            }

            /// <summary>
            ///     Easing equation function for a quartic (t^4) easing out/in:
            ///     deceleration until halfway, then acceleration.
            /// </summary>
            /// <param name="t">Current time in seconds.</param>
            /// <param name="b">Starting value.</param>
            /// <param name="c">Final value.</param>
            /// <param name="d">Duration of animation.</param>
            /// <returns>The correct value.</returns>
            public static float OutInQuart(float t, float b, float c, float d)
            {
                if (t < d / 2)
                {
                    return OutQuart(t * 2, b, c / 2, d);
                }

                return InQuart(t * 2 - d, b + c / 2, c / 2, d);
            }

            #endregion

            #region Quintic

            /// <summary>
            ///     Easing equation function for a quintic (t^5) easing out:
            ///     decelerating from zero velocity.
            /// </summary>
            /// <param name="t">Current time in seconds.</param>
            /// <param name="b">Starting value.</param>
            /// <param name="c">Final value.</param>
            /// <param name="d">Duration of animation.</param>
            /// <returns>The correct value.</returns>
            public static float OutQuint(float t, float b, float c, float d)
            {
                return c * ((t = t / d - 1) * t * t * t * t + 1) + b;
            }

            /// <summary>
            ///     Easing equation function for a quintic (t^5) easing in:
            ///     accelerating from zero velocity.
            /// </summary>
            /// <param name="t">Current time in seconds.</param>
            /// <param name="b">Starting value.</param>
            /// <param name="c">Final value.</param>
            /// <param name="d">Duration of animation.</param>
            /// <returns>The correct value.</returns>
            public static float InQuint(float t, float b, float c, float d)
            {
                return c * (t /= d) * t * t * t * t + b;
            }

            /// <summary>
            ///     Easing equation function for a quintic (t^5) easing in/out:
            ///     acceleration until halfway, then deceleration.
            /// </summary>
            /// <param name="t">Current time in seconds.</param>
            /// <param name="b">Starting value.</param>
            /// <param name="c">Final value.</param>
            /// <param name="d">Duration of animation.</param>
            /// <returns>The correct value.</returns>
            public static float InOutQuint(float t, float b, float c, float d)
            {
                if ((t /= d / 2) < 1)
                {
                    return c / 2 * t * t * t * t * t + b;
                }

                return c / 2 * ((t -= 2) * t * t * t * t + 2) + b;
            }

            /// <summary>
            ///     Easing equation function for a quintic (t^5) easing in/out:
            ///     acceleration until halfway, then deceleration.
            /// </summary>
            /// <param name="t">Current time in seconds.</param>
            /// <param name="b">Starting value.</param>
            /// <param name="c">Final value.</param>
            /// <param name="d">Duration of animation.</param>
            /// <returns>The correct value.</returns>
            public static float OutInQuint(float t, float b, float c, float d)
            {
                if (t < d / 2)
                {
                    return OutQuint(t * 2, b, c / 2, d);
                }

                return InQuint(t * 2 - d, b + c / 2, c / 2, d);
            }

            #endregion

            #region Elastic

            /// <summary>
            ///     Easing equation function for an elastic (exponentially decaying sine wave) easing out:
            ///     decelerating from zero velocity.
            /// </summary>
            /// <param name="t">Current time in seconds.</param>
            /// <param name="b">Starting value.</param>
            /// <param name="c">Final value.</param>
            /// <param name="d">Duration of animation.</param>
            /// <returns>The correct value.</returns>
            public static float OutElastic(float t, float b, float c, float d)
            {
                if ((t /= d) == 1)
                {
                    return b + c;
                }

                float p = d * .3f;
                float s = p / 4.0f;

                return c * Mathf.Pow(2, -10 * t) * Mathf.Sin((t * d - s) * (2 * Mathf.PI) / p) + c + b;
            }

            /// <summary>
            ///     Easing equation function for an elastic (exponentially decaying sine wave) easing in:
            ///     accelerating from zero velocity.
            /// </summary>
            /// <param name="t">Current time in seconds.</param>
            /// <param name="b">Starting value.</param>
            /// <param name="c">Final value.</param>
            /// <param name="d">Duration of animation.</param>
            /// <returns>The correct value.</returns>
            public static float InElastic(float t, float b, float c, float d)
            {
                if ((t /= d) == 1)
                {
                    return b + c;
                }

                float p = d * .3f;
                float s = p / 4;

                return -(c * Mathf.Pow(2, 10 * (t -= 1)) * Mathf.Sin((t * d - s) * (2 * Mathf.PI) / p)) + b;
            }

            /// <summary>
            ///     Easing equation function for an elastic (exponentially decaying sine wave) easing in/out:
            ///     acceleration until halfway, then deceleration.
            /// </summary>
            /// <param name="t">Current time in seconds.</param>
            /// <param name="b">Starting value.</param>
            /// <param name="c">Final value.</param>
            /// <param name="d">Duration of animation.</param>
            /// <returns>The correct value.</returns>
            public static float InOutElastic(float t, float b, float c, float d)
            {
                if ((t /= d / 2) == 2)
                {
                    return b + c;
                }

                float p = d * (.3f * 1.5f);
                float s = p / 4;

                if (t < 1)
                {
                    return -.5f * (c * Mathf.Pow(2, 10 * (t -= 1)) * Mathf.Sin((t * d - s) * (2 * Mathf.PI) / p)) + b;
                }

                return c * Mathf.Pow(2, -10 * (t -= 1)) * Mathf.Sin((t * d - s) * (2 * Mathf.PI) / p) * .5f + c + b;
            }

            /// <summary>
            ///     Easing equation function for an elastic (exponentially decaying sine wave) easing out/in:
            ///     deceleration until halfway, then acceleration.
            /// </summary>
            /// <param name="t">Current time in seconds.</param>
            /// <param name="b">Starting value.</param>
            /// <param name="c">Final value.</param>
            /// <param name="d">Duration of animation.</param>
            /// <returns>The correct value.</returns>
            public static float OutInElastic(float t, float b, float c, float d)
            {
                if (t < d / 2)
                {
                    return OutElastic(t * 2, b, c / 2, d);
                }

                return InElastic(t * 2 - d, b + c / 2, c / 2, d);
            }

            #endregion

            #region Bounce

            /// <summary>
            ///     Easing equation function for a bounce (exponentially decaying parabolic bounce) easing out:
            ///     decelerating from zero velocity.
            /// </summary>
            /// <param name="t">Current time in seconds.</param>
            /// <param name="b">Starting value.</param>
            /// <param name="c">Final value.</param>
            /// <param name="d">Duration of animation.</param>
            /// <returns>The correct value.</returns>
            public static float OutBounce(float t, float b, float c, float d)
            {
                if ((t /= d) < 1 / 2.75)
                {
                    return c * (7.5625f * t * t) + b;
                }

                if (t < 2 / 2.75f)
                {
                    return c * (7.5625f * (t -= 1.5f / 2.75f) * t + .75f) + b;
                }

                if (t < 2.5f / 2.75f)
                {
                    return c * (7.5625f * (t -= 2.25f / 2.75f) * t + .9375f) + b;
                }

                return c * (7.5625f * (t -= 2.625f / 2.75f) * t + .984375f) + b;
            }

            /// <summary>
            ///     Easing equation function for a bounce (exponentially decaying parabolic bounce) easing in:
            ///     accelerating from zero velocity.
            /// </summary>
            /// <param name="t">Current time in seconds.</param>
            /// <param name="b">Starting value.</param>
            /// <param name="c">Final value.</param>
            /// <param name="d">Duration of animation.</param>
            /// <returns>The correct value.</returns>
            public static float InBounce(float t, float b, float c, float d)
            {
                return c - OutBounce(d - t, 0, c, d) + b;
            }

            /// <summary>
            ///     Easing equation function for a bounce (exponentially decaying parabolic bounce) easing in/out:
            ///     acceleration until halfway, then deceleration.
            /// </summary>
            /// <param name="t">Current time in seconds.</param>
            /// <param name="b">Starting value.</param>
            /// <param name="c">Final value.</param>
            /// <param name="d">Duration of animation.</param>
            /// <returns>The correct value.</returns>
            public static float InOutBounce(float t, float b, float c, float d)
            {
                if (t < d / 2)
                {
                    return InBounce(t * 2, 0, c, d) * .5f + b;
                }

                return OutBounce(t * 2 - d, 0, c, d) * .5f + c * .5f + b;
            }

            /// <summary>
            ///     Easing equation function for a bounce (exponentially decaying parabolic bounce) easing out/in:
            ///     deceleration until halfway, then acceleration.
            /// </summary>
            /// <param name="t">Current time in seconds.</param>
            /// <param name="b">Starting value.</param>
            /// <param name="c">Final value.</param>
            /// <param name="d">Duration of animation.</param>
            /// <returns>The correct value.</returns>
            public static float OutInBounce(float t, float b, float c, float d)
            {
                if (t < d / 2)
                {
                    return OutBounce(t * 2, b, c / 2, d);
                }

                return InBounce(t * 2 - d, b + c / 2, c / 2, d);
            }

            #endregion

            #region Back

            /// <summary>
            ///     Easing equation function for a back (overshooting cubic easing: (s+1)*t^3 - s*t^2) easing out:
            ///     decelerating from zero velocity.
            /// </summary>
            /// <param name="t">Current time in seconds.</param>
            /// <param name="b">Starting value.</param>
            /// <param name="c">Final value.</param>
            /// <param name="d">Duration of animation.</param>
            /// <returns>The correct value.</returns>
            public static float OutBack(float t, float b, float c, float d)
            {
                return c * ((t = t / d - 1) * t * ((1.70158f + 1) * t + 1.70158f) + 1) + b;
            }

            /// <summary>
            ///     Easing equation function for a back (overshooting cubic easing: (s+1)*t^3 - s*t^2) easing in:
            ///     accelerating from zero velocity.
            /// </summary>
            /// <param name="t">Current time in seconds.</param>
            /// <param name="b">Starting value.</param>
            /// <param name="c">Final value.</param>
            /// <param name="d">Duration of animation.</param>
            /// <returns>The correct value.</returns>
            public static float InBack(float t, float b, float c, float d)
            {
                return c * (t /= d) * t * ((1.70158f + 1) * t - 1.70158f) + b;
            }

            /// <summary>
            ///     Easing equation function for a back (overshooting cubic easing: (s+1)*t^3 - s*t^2) easing in/out:
            ///     acceleration until halfway, then deceleration.
            /// </summary>
            /// <param name="t">Current time in seconds.</param>
            /// <param name="b">Starting value.</param>
            /// <param name="c">Final value.</param>
            /// <param name="d">Duration of animation.</param>
            /// <returns>The correct value.</returns>
            public static float InOutBack(float t, float b, float c, float d)
            {
                var s = 1.70158f;
                if ((t /= d / 2) < 1)
                {
                    return c / 2 * (t * t * (((s *= 1.525f) + 1) * t - s)) + b;
                }

                return c / 2 * ((t -= 2) * t * (((s *= 1.525f) + 1) * t + s) + 2) + b;
            }

            /// <summary>
            ///     Easing equation function for a back (overshooting cubic easing: (s+1)*t^3 - s*t^2) easing out/in:
            ///     deceleration until halfway, then acceleration.
            /// </summary>
            /// <param name="t">Current time in seconds.</param>
            /// <param name="b">Starting value.</param>
            /// <param name="c">Final value.</param>
            /// <param name="d">Duration of animation.</param>
            /// <returns>The correct value.</returns>
            public static float OutInBack(float t, float b, float c, float d)
            {
                if (t < d / 2)
                {
                    return OutBack(t * 2, b, c / 2, d);
                }

                return InBack(t * 2 - d, b + c / 2, c / 2, d);
            }

            #endregion
        }

        public interface ITween
        {
            bool Update(float dt);
            bool HasTag(object tag);
            void Stop(bool ignoreEvents = false);
            void Complete();
        }

        internal interface ITweenInternal
        {
            object GetTag();
            float GetTimer();
            float GetDelay();
            float GetDuration();
            float GetFrom();
            float GetTo();
        }

        public class Tween<T> : ITween, ITweenInternal
        {
            internal Tweener tweener;
            internal T obj;
            internal float duration;
            internal float from;
            internal float to;
            internal object tag;
            internal float delay;

            internal int loops = 1;
            internal bool reflect;

            internal Action<T> onComplete;
            internal Action onCompleteParameterless;
            internal Action<T, float> onUpdate;
            internal Action<T> onCancel;
            internal Action onCancelParameterless;

            internal float timer;
            internal float direction;

            private EaseFunction easeFunction;

            bool ITween.Update(float dt)
            {
                delay -= dt;
                if (delay <= 0f)
                {
                    timer += dt / duration * direction;

                    try
                    {
                        if (onUpdate != null)
                        {
                            float val = EaseFunctions.GetEase(easeFunction).Invoke(timer, from, to - from, 1f);
                            onUpdate.Invoke(obj, Mathf.Clamp(val, Mathf.Min(from, to), Mathf.Max(from, to)));
                        }

                        if (timer >= 1f)
                        {
                            if (reflect)
                            {
                                direction = -1f;
                                return false;
                            }

                            timer = 0f;

                            if (loops != -1)
                            {
                                --loops;
                                if (loops == 0)
                                {
                                    return true;
                                }
                            }

                            return false;
                        }

                        if (timer <= 0f)
                        {
                            if (loops != -1)
                            {
                                --loops;
                                if (loops == 0)
                                {
                                    return true;
                                }
                            }

                            if (reflect)
                            {
                                direction = 1f;
                                return false;
                            }

                            return false;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                    }

                    if (timer >= 1f)
                    {
                        return true;
                    }
                }
                else
                {
                    try
                    {
                        if (onUpdate != null)
                        {
                            onUpdate.Invoke(obj, from);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                    }
                }

                return false;
            }

            void ITween.Complete()
            {
                if (onComplete != null)
                {
                    onComplete.Invoke(obj);
                }

                if (onCompleteParameterless != null)
                {
                    onCompleteParameterless.Invoke();
                }

                delay = 0f;
                timer = direction;
                reflect = false;
                loops = 1;
            }

            void ITween.Stop(bool ignoreEvents)
            {
                if (ignoreEvents == false && timer < 1f)
                {
                    if (onCancel != null)
                    {
                        onCancel.Invoke(obj);
                    }

                    if (onCancelParameterless != null)
                    {
                        onCancelParameterless.Invoke();
                    }
                }

                delay = 0f;
                timer = direction;
                reflect = false;
                loops = 1;
            }

            bool ITween.HasTag(object tag)
            {
                return this.tag == tag;
            }

            float ITweenInternal.GetTimer()
            {
                return timer;
            }

            float ITweenInternal.GetDelay()
            {
                return delay;
            }

            float ITweenInternal.GetDuration()
            {
                return duration;
            }

            float ITweenInternal.GetFrom()
            {
                return from;
            }

            float ITweenInternal.GetTo()
            {
                return to;
            }

            object ITweenInternal.GetTag()
            {
                return tag;
            }

            public float GetDirection()
            {
                return direction;
            }

            public Tween<T> Loop(int loops = 1)
            {
                this.loops = loops;
                return this;
            }

            public Tween<T> SetValue(float timer, float direction)
            {
                this.timer = timer;
                this.direction = direction;
                return this;
            }

            public Tween<T> Reflect()
            {
                reflect = true;
                return this;
            }

            public Tween<T> Tag(object tag)
            {
                this.tag = tag;
                return this;
            }

            public Tween<T> Delay(float delay)
            {
                this.delay = delay;
                return this;
            }

            public Tween<T> Ease(EaseFunction easeFunction)
            {
                this.easeFunction = easeFunction;
                return this;
            }

            public Tween<T> OnComplete(Action<T> onResult)
            {
                onComplete = onResult;
                return this;
            }

            public Tween<T> OnComplete(Action onResult)
            {
                onCompleteParameterless = onResult;
                return this;
            }

            public Tween<T> OnCancel(Action<T> onResult)
            {
                onCancel = onResult;
                return this;
            }

            public Tween<T> OnCancel(Action onResult)
            {
                onCancelParameterless = onResult;
                return this;
            }

            public Tween<T> OnUpdate(Action<T, float> onResult)
            {
                onUpdate = onResult;
                //this.tweener.Step(this, Time.deltaTime);
                return this;
            }
        }
    }
}