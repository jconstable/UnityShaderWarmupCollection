using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ShaderPrewarm
{
    [CreateAssetMenu(fileName = "Data", menuName = "Shader/ShaderWarmupCollection", order = 1)]
    public class ShaderWarmupCollection : ScriptableObject
    {
        // Small class such that Dictionary-like structure can be added from the inspector
        [System.Serializable]
        public class ShaderKeywords
        {
            public string ShaderName;
            public List<string> ShaderKeywordList = new List<string>();
        }
        
        // ScriptableObject properties
        public string Name;
        public List<ShaderKeywords> ShaderSets;
        public List<Mesh> AdditionalWarmupGeometry;

        [System.NonSerialized]
        private bool IsInit = false;
        [System.NonSerialized]
        private List<Mesh> RuntimeGeneratedWarmupGeometry;
        
        public void WarmUp()
        {
            Init();
            
            List<Mesh> AllWarmupGeometry = new List<Mesh>(AdditionalWarmupGeometry);
            AllWarmupGeometry.AddRange(RuntimeGeneratedWarmupGeometry);
            
            
            RenderTexture tempRT = RenderTexture.GetTemporary(4,4);
            CommandBuffer buffer = new CommandBuffer();
            buffer.name = string.Format("WarmupShaderCollection {0}", Name);
            buffer.SetRenderTarget(tempRT);
            
            foreach (var set in ShaderSets)
            {
                string shaderName = set.ShaderName;
                Shader shader = Shader.Find(shaderName);
                if (shader == null)
                {
                    Debug.LogError(string.Format("WarmupShaderCollection: Unable to locate shader {0}", shaderName));
                    continue;
                }
                foreach (string keywordList in set.ShaderKeywordList)
                {
                    Material mat = new Material(shader);
                    foreach(var keyword in keywordList.Split(' '))
                    {
                        mat.EnableKeyword(keyword);
                    }

                    foreach (Mesh m in AllWarmupGeometry)
                    {
                        buffer.DrawMesh(m, Matrix4x4.identity, mat);
                    }
                }
            }

            Graphics.ExecuteCommandBuffer(buffer);
            
            buffer.Release();
            RenderTexture.ReleaseTemporary(tempRT);
        }

        private void Init()
        {
            if (!IsInit)
            {
                RuntimeGeneratedWarmupGeometry = new List<Mesh>();
                
                RuntimeGeneratedWarmupGeometry.Add(GenerateTriangleFromScripting(MeshGenerationOptions.GeometryOnly));
//                RuntimeGeneratedWarmupGeometry.Add(GenerateTriangleFromScripting(MeshGenerationOptions.UV0));
//                RuntimeGeneratedWarmupGeometry.Add(GenerateTriangleFromScripting(MeshGenerationOptions.UV0 | MeshGenerationOptions.UV1));
//                RuntimeGeneratedWarmupGeometry.Add(GenerateTriangleFromScripting(MeshGenerationOptions.UV0 | MeshGenerationOptions.UV1 | MeshGenerationOptions.Normals));
                
                IsInit = true;
            }
        }

        [Flags]
        private enum MeshGenerationOptions
        {
            GeometryOnly,
            Normals,
            UV0,
            UV1,
            UV2,
            UV3
        }

        // Create a triangle using the precision and compression offered by scripting. Typically only 32bit precision 
        // and no stripped channels
        private Mesh GenerateTriangleFromScripting(MeshGenerationOptions options)
        {
            Mesh scriptedTriangle = new Mesh();
            scriptedTriangle.vertices = new Vector3[]
            {
                Vector3.up,
                Vector3.left,
                Vector3.right
            };
            scriptedTriangle.SetIndices(new int[] {0, 1, 2}, MeshTopology.Triangles, 0);

            List<Vector2> uvs = new List<Vector2>()
            {
                Vector2.zero, Vector2.right, Vector2.down
            };
            if ((options & MeshGenerationOptions.UV0) == MeshGenerationOptions.UV0)
                scriptedTriangle.SetUVs(0, uvs);
            if ((options & MeshGenerationOptions.UV1) == MeshGenerationOptions.UV1)
                scriptedTriangle.SetUVs(1, uvs);
            if ((options & MeshGenerationOptions.UV2) == MeshGenerationOptions.UV2)
                scriptedTriangle.SetUVs(2, uvs);
            if ((options & MeshGenerationOptions.UV3) == MeshGenerationOptions.UV3)
                scriptedTriangle.SetUVs(3, uvs);

            if ((options & MeshGenerationOptions.Normals) == MeshGenerationOptions.Normals)
            {
                scriptedTriangle.SetNormals(new List<Vector3>()
                {
                    Vector3.up, Vector3.up, Vector3.up
                });
            }
            return scriptedTriangle;
        }
        
#if UNITY_EDITOR
        public void AddVariant(Shader shader, string[] keywordList)
        {
            AddVariant(shader.name, keywordList);
        }

        public void AddVariant(string shader, string[] keywordList)
        {
            string keywordsString = string.Join(" ",keywordList);
            foreach (var set in ShaderSets)
            {
                if (set.ShaderName == shader)
                {
                    foreach (var keywords in set.ShaderKeywordList)
                    {
                        if (keywords == keywordsString)
                            return;
                    }
                    set.ShaderKeywordList.Add(keywordsString);
                    Save();
                    return;
                }
            }

            ShaderSets.Add(new ShaderKeywords()
            {
                ShaderName = shader,
                ShaderKeywordList = new List<string>()
                {
                    keywordsString
                }
            });
            Save();
        }

        private void Save()
        {
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        #endif
    }
}