using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ThunderWire.Utility
{
    public static class Tools
    {
        public static Camera MainCamera()
        {
            return Object.FindObjectsOfType<Camera>().Where(x => x.gameObject.tag == "MainCamera").SingleOrDefault();
        }

#if UNITY_EDITOR
        public static List<T> FindAllSceneObjects<T>() where T : Object
        {
            return Resources.FindObjectsOfTypeAll<T>().Where(x => !EditorUtility.IsPersistent(x)).ToList();
        }
#endif

        public static void PlayOneShot2D(Vector3 position, AudioClip clip, float volume = 1f)
        {
            GameObject go = new GameObject("OneShotAudio");
            go.transform.position = position;
            AudioSource source = go.AddComponent<AudioSource>();
            source.spatialBlend = 0f;
            source.clip = clip;
            source.volume = volume;
            source.Play();
            Object.Destroy(go, clip.length);
        }

        public static string GameObjectPath(this GameObject obj)
        {
            return string.Join("/", obj.GetComponentsInParent<Transform>().Select(t => t.name).Reverse().ToArray());
        }

        public static string GetBetween(this string str, char start, char end)
        {
            int m_start = str.IndexOf(start);
            int m_end = str.IndexOf(end);
            string old = str.Substring(start, end - start + 1);
            return old.Substring(1, old.Length - 2);
        }

        public static string ReplacePart(this string str, char start, char end, string replace)
        {
            int m_start = str.IndexOf(start);
            int m_end = str.IndexOf(end);
            string old = str.Substring(start, end - start + 1);
            string result = old.Substring(1, old.Length - 2);
            return str.Replace(old, replace);
        }

        public static string TitleCase(this string str)
        {
            CultureInfo cultureInfo = Thread.CurrentThread.CurrentCulture;
            TextInfo textInfo = cultureInfo.TextInfo;
            return textInfo.ToTitleCase(str);
        }

        public static Vector3 Clamp(this Vector3 vec, float max)
        {
            return new Vector3(Mathf.Clamp(vec.x, 0, 1), Mathf.Clamp(vec.y, 0, 1), Mathf.Clamp(vec.z, 0, 1));
        }
    }
}
