using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

namespace Kairos.EditorTools
{
    public static class AudioMixerBuilder
    {
        public static void TestAudioMixer()
        {
            var audioImporterAssembly = typeof(AudioImporter).Assembly;
            var mixerControllerType = audioImporterAssembly.GetType("UnityEditor.Audio.AudioMixerController");
            if (mixerControllerType != null)
            {
                Debug.Log("AudioMixerController found: " + mixerControllerType.FullName);
                var methods = mixerControllerType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                foreach (var m in methods)
                {
                    Debug.Log("Method: " + m.Name);
                }
            }
            else
            {
                Debug.LogWarning("AudioMixerController NOT found in " + audioImporterAssembly.FullName);
            }
        }
    }
}
