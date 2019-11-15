using System.Collections;
using System.Collections.Generic;
using ShaderPrewarm;
using UnityEditor;
using UnityEngine;

namespace ShaderPrewarmSample
{
    public class SampleWarmupBehavior : MonoBehaviour
    {
        public ShaderWarmupCollection Collection;
        public bool Warm;

        public string ShaderNameToAdd;
        public string[] KeywordsToAdd;
        public bool AddNew;

        // Update is called once per frame
        void Update()
        {
            if (AddNew && Collection != null)
            {
                Collection.AddVariant(ShaderNameToAdd, KeywordsToAdd);
                ShaderNameToAdd = null;
                KeywordsToAdd = new string[0];
                AddNew = false;
                return;
            }

            if (Warm && Collection != null)
            {
                Collection.WarmUp();
                Warm = false;
                return;
            }

            if (Warm || AddNew)
            {
                Warm = false;
                AddNew = false;
                Debug.LogError("A ShaderPrewarmCollectionObject must be set to use the Warm or AddNew operations.");
            }
        }
    }
}
