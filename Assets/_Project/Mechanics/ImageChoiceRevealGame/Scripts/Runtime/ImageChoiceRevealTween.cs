using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ImageChoiceRevealGame
{
    public static class ImageChoiceRevealTween
    {
        public static IEnumerator Fade(CanvasGroup group, float from, float to, float duration)
        {
            if (group == null) yield break;
            if (duration <= 0f) { group.alpha = to; yield break; }
            float time = 0f;
            group.alpha = from;
            while (time < duration)
            {
                time += Time.deltaTime;
                group.alpha = Mathf.Lerp(from, to, EaseOutCubic(time / duration));
                yield return null;
            }
            group.alpha = to;
        }

        public static IEnumerator Scale(RectTransform target, Vector3 from, Vector3 to, float duration, bool backEase = true)
        {
            if (target == null) yield break;
            if (duration <= 0f) { target.localScale = to; yield break; }
            float time = 0f;
            target.localScale = from;
            while (time < duration)
            {
                time += Time.deltaTime;
                float t = backEase ? EaseOutBack(time / duration) : EaseOutCubic(time / duration);
                target.localScale = Vector3.LerpUnclamped(from, to, t);
                yield return null;
            }
            target.localScale = to;
        }

        public static IEnumerator Move(RectTransform target, Vector2 from, Vector2 to, float duration)
        {
            if (target == null) yield break;
            if (duration <= 0f) { target.anchoredPosition = to; yield break; }
            float time = 0f;
            target.anchoredPosition = from;
            while (time < duration)
            {
                time += Time.deltaTime;
                target.anchoredPosition = Vector2.LerpUnclamped(from, to, EaseOutCubic(time / duration));
                yield return null;
            }
            target.anchoredPosition = to;
        }

        public static IEnumerator Color(Image image, Color from, Color to, float duration)
        {
            if (image == null) yield break;
            if (duration <= 0f) { image.color = to; yield break; }
            float time = 0f;
            image.color = from;
            while (time < duration)
            {
                time += Time.deltaTime;
                image.color = UnityEngine.Color.Lerp(from, to, EaseOutCubic(time / duration));
                yield return null;
            }
            image.color = to;
        }

        public static IEnumerator Shake(RectTransform target, float strength, float duration)
        {
            if (target == null) yield break;
            Vector2 start = target.anchoredPosition;
            float time = 0f;
            while (time < duration)
            {
                time += Time.deltaTime;
                float damping = 1f - Mathf.Clamp01(time / duration);
                target.anchoredPosition = start + new Vector2(Mathf.Sin(time * 70f) * strength * damping, 0f);
                yield return null;
            }
            target.anchoredPosition = start;
        }

        public static IEnumerator Parallel(MonoBehaviour runner, params IEnumerator[] routines)
        {
            int running = 0;
            for (int i = 0; i < routines.Length; i++)
            {
                if (routines[i] == null) continue;
                running++;
                runner.StartCoroutine(Run(routines[i], () => running--));
            }
            while (running > 0) yield return null;
        }

        private static IEnumerator Run(IEnumerator routine, Action done)
        {
            yield return routine;
            if (done != null) done();
        }

        private static float EaseOutCubic(float t)
        {
            t = Mathf.Clamp01(t);
            return 1f - Mathf.Pow(1f - t, 3f);
        }

        private static float EaseOutBack(float t)
        {
            t = Mathf.Clamp01(t);
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }
    }
}
