using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using VLab;

#if UNITY_EDITOR
namespace VLabEditor
{
    public class CreateTexture2DArray
    {
        static string texdir = "ExampleImageset";
        static int startindex = 1;
        static int numofimage = 10;

        [MenuItem("Assets/Create/Texture2DArray")]
        public static void Create()
        {
            var texturearray = texdir.LoadImageSet(startindex, numofimage, true);
            if (texturearray == null)
            {
                Debug.Log("No Texture2DArray Created.");
            }
            else
            {
                var path = "Assets/Resources/" + texdir + ".asset";
                AssetDatabase.CreateAsset(texturearray, path);
                Debug.Log("Texture2DArray asset saved to: " + path);
            }
        }
    }
}
#endif