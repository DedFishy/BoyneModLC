using BepInEx;
using BepInEx.Logging;
using DunGen;
using GameNetcodeStuff;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using Unity;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace BoyneMod
{
    [BepInPlugin(modGUID, modName, modVersion)] // Creating the plugin
    [BepInDependency(LethalLib.Plugin.ModGUID)]
    [BepInDependency(MoreCompany.PluginInformation.PLUGIN_GUID)]
    public class BoyneMod : BaseUnityPlugin // MODNAME : BaseUnityPlugin
    {

        public static AssetBundle Rocktopus;

        public const string modGUID = "dedfishy.boynemod"; // a unique name for your mod
        public const string modName = "BoyneMod"; // the name of your mod
        public const string modVersion = "1.0.0.0"; // the version of your mod

        private readonly Harmony harmony = new Harmony(modGUID); // Creating a Harmony instance which will run the mods

        void Awake() // runs when Lethal Company is launched
        {
            var BepInExLogSource = BepInEx.Logging.Logger.CreateLogSource("BoyneMod Startup Sequence"); // creates a logger for the BepInEx console
            BepInExLogSource.LogMessage(@"
    ____                         __  __           _ 
   |  _ \                       |  \/  |         | |
   | |_) | ___  _   _ _ __   ___| \  / | ___   __| |
   |  _ < / _ \| | | | '_ \ / _ \ |\/| |/ _ \ / _` |
   | |_) | (_) | |_| | | | |  __/ |  | | (_) | (_| |
   |____/ \___/ \__, |_| |_|\___|_|  |_|\___/ \__,_|
                 __/ |                              
                |___/                               
It may be bad, but at least it's not the thick woman mod!
");
            List<Type> classesToPatch = new List<Type> {
                typeof(menuModifications),
                typeof(playerModifications)
            };
            BepInExLogSource.LogMessage("Registering Harmony patches...");
            for (var i = 0; i < classesToPatch.Count; i++)
            {
                BepInExLogSource.LogMessage("Installing patch for " +  classesToPatch[i].Name + "... [" + (i+1) + "/" + classesToPatch.Count + "]");
                harmony.PatchAll(classesToPatch[i]);
            }

            BepInExLogSource.LogMessage("Registering MonoMod patches...");

            On.DunGen.DungeonGenerator.Generate += DungeonGenerator_Generate; // Doesn't seem to work :(
            

            BepInExLogSource.LogMessage("Installing MoreCompany cosmetics...");

            string targetCosmeticFile = assetMurderer.getMoreCompanyCosmeticsPath() + "boyne.cosmetics";
            if (!File.Exists(targetCosmeticFile)) {
                File.Copy(assetMurderer.getAssetPath("boyne.cosmetics"), targetCosmeticFile);
            }

            BepInExLogSource.LogMessage("Loading asset bundles...");
            Rocktopus = AssetBundle.LoadFromFile(assetMurderer.getAssetPath("rocktopus"));
            
            

        BepInExLogSource.LogMessage("BoyneMod startup complete!");
        }

        public static void DungeonGenerator_Generate(On.DunGen.DungeonGenerator.orig_Generate orig, DungeonGenerator self)
        {
            orig(self);
            assetMurderer.setGameObjectMeshTexture(assetMurderer.findGameObject("EclipseObject"), "sunTexture.png");
            RenderSettings.skybox = assetMurderer.TextureToBaseMaterial(assetMurderer.CreateTextureFromPath("skybox.png"));
            DynamicGI.UpdateEnvironment();
        }
    }

    public class assetMurderer
    {
        static ManualLogSource BepInExLogSource = BepInEx.Logging.Logger.CreateLogSource("BoyneMod Asset Murderer");

        public static Material TextureToBaseMaterial(Texture texture)
        {
            Material material = new Material(Shader.Find("Standard"));
            material.mainTexture = texture;
            return material;
        }

        public static string getAssetWebPath(string path)
        {
            return "file://" + BepInEx.Paths.PluginPath + "/BoyneMod/assets/" + path;
        }
        public static string getAssetPath(string path)
        {
            return BepInEx.Paths.PluginPath + "/BoyneMod/assets/" + path;
        }

        public static string getMoreCompanyCosmeticsPath()
        {
            string path = BepInEx.Paths.PluginPath + "/MoreCompanyCosmetics/";
            System.IO.Directory.CreateDirectory(path);
            return path;
        }

        public static void UpdateAudioClip(string assetPath, ref AudioClip audioClip)
        {
            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(getAssetWebPath(assetPath), AudioType.MPEG))
            {
                www.SendWebRequest();

                while (!www.isDone) { }

                if (www.result == UnityWebRequest.Result.ConnectionError)
                {
                    BepInExLogSource.LogError("Failed to retrieve audio asset: " + assetPath);
                }
                else
                {
                    audioClip = DownloadHandlerAudioClip.GetContent(www);
                }
            }
        }

        public static void UpdateTextureImage(string assetPath, Texture2D oldTexture)
        {
            if (oldTexture == null)
            {
                BepInExLogSource.LogWarning("Texture was null! Assuming this is okay...");
                return;
            }
            oldTexture.LoadImage(File.ReadAllBytes(getAssetPath(assetPath)));
        }

        public static GameObject getChildGameObject(GameObject parent, string name)
        {
            Transform[] ts = parent.transform.GetComponentsInChildren<Transform>(true);
            foreach (Transform t in ts) if (t.gameObject.name == name) return t.gameObject;
            return null;
        }

        public static GameObject getRootGameObject(string name)
        {
            foreach (var item in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (item.name == name)
                {
                    return item;
                }
            }
            return null;
        }

        public static Texture CreateTextureFromPath(string path)
        {
            Texture2D newTexture = new Texture2D(0, 0);
            UpdateTextureImage(path, newTexture);
            return newTexture;
        }

        public static void setGameObjectMeshTexture(GameObject gameObject, string path) { 
            MeshRenderer meshRenderer = gameObject.GetComponent<MeshRenderer>();
            
            meshRenderer.material.mainTexture = CreateTextureFromPath(path);
        }

        public static GameObject findGameObject(string name)
        {
            return GameObject.Find(name);

        }

        public static string reverseString(string str)
        {
            char[] charArray = str.ToCharArray();
            Array.Reverse(charArray);
            return new string(charArray);
        }
    }

    [HarmonyPatch(typeof(PlayerControllerB))]
    [HarmonyPatch("Update")]
    class playerModifications
    {

        static bool hasDoneInitialSetup = false;

        [HarmonyPostfix]
        static void Postfix(ref PlayerControllerB __instance)
        {
            __instance.sprintMeter = 1f;
            if (!hasDoneInitialSetup)
            {
                hasDoneInitialSetup = true; // If this method errors out before this line, an infinite loop will occur and obliterate the FPS

                assetMurderer.setGameObjectMeshTexture(assetMurderer.findGameObject("Plane.001"), "posters.png");
                //assetMurderer.setGameObjectMeshTexture(assetMurderer.findGameObject("SunTexture"), "sunTexture.png");

            }
            //BepInExLogSource.LogMessage("Updated texture of posters: " + posterObject.ToString());
        }
    }

    /*
    [HarmonyPatch(typeof(InsertClassHere))]
    [HarmonyPatch("InsertMethodHere")]
    class insertClassNameHere
    {
        [HarmonyPostfix]
        static void Postfix(ref InsertClassHere __instance) 
        {

        }
    }
    */

    [HarmonyPatch(typeof(MenuManager))]
    [HarmonyPatch("Awake")]
    class menuModifications
    {
        static ManualLogSource BepInExLogSource = BepInEx.Logging.Logger.CreateLogSource("BoyneMod Menu Modifications");


        [HarmonyPostfix]
        static void Postfix(ref MenuManager __instance)
        {

            

            assetMurderer.UpdateAudioClip("menuMusic.mp3", ref __instance.menuMusic);

            GameObject headerImageObj = assetMurderer.getChildGameObject(__instance.menuButtons, "HeaderImage");
            if (headerImageObj != null) {

                assetMurderer.UpdateTextureImage("headerImage.png", headerImageObj.GetComponent<UnityEngine.UI.Image>().sprite.texture);
                headerImageObj.name = "boynestolethisheaderimage"; // This prevents More Company from changing the header image because I'm petty

            }

            foreach (var item in assetMurderer.getRootGameObject("Canvas").GetComponentsInChildren<TextMeshProUGUI>())
            {
                item.text = assetMurderer.reverseString(item.text);
            };

            

            //__instance.DisplayMenuNotification("You've successfully loaded BoyneMod, the greatest mod to have ever been developed. Boyne is not legally responsible for any psychadellic damage caused by this software.", "Okay pookie");

            
        }
    }

    // Boyne's crazy notes
    /*
     * The posters are contained in the gameobject Plane.001 as a mesh
     * 
     */
}