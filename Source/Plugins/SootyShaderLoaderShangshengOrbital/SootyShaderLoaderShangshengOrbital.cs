using System;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace SootyShaderLoaderShangshengOrbital
{
    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    public class SootyShaderLoaderShangshengOrbital : MonoBehaviour
    {
        private const string shadersAssetName = "ShangshengOrbitalShader";
        private const string winShaderName = "-windows.ksp";
        private const string linuxShaderName = "-linux.ksp";
        private const string macShaderName = "-macosx.ksp";

        private static bool loaded;
        
        public static Shader SootyShader { get; private set; }

        [SuppressMessage("Code Quality", "IDE0051", Justification = "Called by Unity")]
        private void Start()
        {
            if (loaded)
                return;

            LoadBundle();
        }

        //This code is borrowed from Textures Unlimited: https://github.com/shadowmage45/TexturesUnlimited/blob/master/Plugin/SSTUTools/KSPShaderTools/Addon/TexturesUnlimitedLoader.cs#L198-L254
        private void LoadBundle()
        {
            string shaderPath;

            if (Application.platform == RuntimePlatform.WindowsPlayer && SystemInfo.graphicsDeviceVersion.StartsWith("OpenGL"))
                shaderPath = shadersAssetName + linuxShaderName;
            else if (Application.platform == RuntimePlatform.WindowsPlayer)
                shaderPath = shadersAssetName + winShaderName;
            else if (Application.platform == RuntimePlatform.LinuxPlayer)
                shaderPath = shadersAssetName + linuxShaderName;
            else
                shaderPath = shadersAssetName + macShaderName;

            shaderPath = KSPUtil.ApplicationRootPath + "GameData/ShangshengOrbital/Resources/" + shaderPath;

            // KSP-PartTools built AssetBunldes are in the Web format, 
            // and must be loaded using a WWW reference; you cannot use the
            // AssetBundle.CreateFromFile/LoadFromFile methods unless you 
            // manually compiled your bundles for stand-alone use

            AssetBundle bundle;

            using (WWW www = CreateWWW(shaderPath))
            {
                if (!string.IsNullOrEmpty(www.error))
                {
                    print("[ShangshengOrbital] - Error while loading shader AssetBundle: " + www.error);
                    return;
                }
                else if (www.assetBundle == null)
                {
                    print("[ShangshengOrbital] - Could not load AssetBundle from WWW - " + www);
                    return;
                }

                bundle = www.assetBundle;
            }

            string[] assetNames = bundle.GetAllAssetNames();
            int len = assetNames.Length;
            Shader shader;
            for (int i = 0; i < len; i++)
            {
                if (assetNames[i].EndsWith(".shader"))
                {
                    shader = bundle.LoadAsset<Shader>(assetNames[i]);
                    print("[ShangshengOrbital] - Loaded Shader: " + shader.name + " :: " + assetNames[i] + " from path: " + shaderPath);
                    if (shader == null || string.IsNullOrEmpty(shader.name))
                    {
                        print("[ShangshengOrbital] - ERROR: Shader did not load properly for asset name: " + assetNames[i]);
                    }
                    GameDatabase.Instance.databaseShaders.AddUnique(shader);
                    SootyShader = shader;
                }
            }
            //this unloads the compressed assets inside the bundle, but leaves any instantiated shaders in-place
            bundle.Unload(false);

            loaded = true;
        }

        private static WWW CreateWWW(string bundlePath)
        {
            try
            {
                string name = Application.platform == RuntimePlatform.WindowsPlayer ? "file:///" + bundlePath : "file://" + bundlePath;
                return new WWW(Uri.EscapeUriString(name));
            }
            catch (Exception e)
            {
                print("[ShangshengOrbital] - Error while creating AssetBundle request: " + e);
                return null;
            }
        }
    }
}
