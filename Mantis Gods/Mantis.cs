﻿using HutongGames.PlayMaker.Actions;
using Modding;
using System.Collections;
using GlobalEnums;
using UnityEngine;
using UnityEngine.SceneManagement;
using Vasi;
using Logger = Modding.Logger;
using Random = System.Random;
using USceneManager = UnityEngine.SceneManagement.SceneManager;

namespace Mantis_Gods
{
    internal class Mantis : MonoBehaviour
    {
        public static Color FloorColor;
        public static bool RainbowFloor;
        public static int RainbowUpdateDelay;
        public static bool NormalArena;
        public static bool KeepSpikes;
        public int CurrentDelay;
        public int RainbowPos;

        private GameObject _lord2, _lord3, _lord1;
        private GameObject _mantisBattle;
        private GameObject _plane;

        private IEnumerator BattleBeat()
        {
            Log("Started Battle Beat Coroutine");

            yield return new WaitForSeconds(13);

            Destroy(_plane);

            yield return new WaitForSeconds(6);

            ReflectionHelper.SetField(GameManager.instance, "entryDelay", 1f);
            ReflectionHelper.SetField(GameManager.instance, "targetScene", "Fungus2_14");
            GameManager.instance.entryGateName = "bot3";

            // Go to Fungus2_15 from the bottom 3 entrance
            // This is where you would fall down from where claw is into the arena
            GameManager.instance.BeginSceneTransition(new GameManager.SceneLoadInfo
            {
                AlwaysUnloadUnusedAssets = true,
                EntryGateName = "bot3",
                PreventCameraFadeOut = false,
                SceneName = "Fungus2_14",
                Visualization = GameManager.SceneLoadVisualizations.Dream
            });

            Log("Finished Coroutine");
        }

        private Color GetNextRainbowColor()
        {
            var c = new Color();

            // the cycle repeats every 768
            int realCyclePos = RainbowPos % 768;
            
            c.a = 1.0f;
            
            if (realCyclePos < 256)
            {
                c.b = 0;
                c.r = (256 - realCyclePos) / 256f;
                c.g = realCyclePos / 256f;
            }
            else if (realCyclePos < 512)
            {
                c.b = (realCyclePos - 256) / 256f;
                c.r = 0;
                c.g = (512 - realCyclePos) / 256f;
            }
            else
            {
                c.b = (768 - realCyclePos) / 256f;
                c.r = (realCyclePos - 512) / 256f;
                c.g = 0;
            }

            RainbowPos++;
            
            return c;
        }

        private static void Log(string str)
        {
            Logger.Log("[Mantis Gods]: " + str);
        }

        public void OnDestroy()
        {
            // So that they don't remain after you quit out
            USceneManager.sceneLoaded -= ResetScene;
            ModHooks.LanguageGetHook -= LangGet;
            ModHooks.GetPlayerBoolHook -= GetBool;
            ModHooks.SetPlayerBoolHook -= SetBool;
        }

        public void Start()
        {
            USceneManager.sceneLoaded += ResetScene;
            ModHooks.LanguageGetHook += LangGet;
            ModHooks.GetPlayerBoolHook += GetBool;
            ModHooks.SetPlayerBoolHook += SetBool;
            ModHooks.ObjectPoolSpawnHook += ShotHandler;
        }

        private static GameObject ShotHandler(GameObject go)
        {
            // if you haven't defeated the normal lords do nothing
            if (!PlayerData.instance.defeatedMantisLords) return go;

            // check for spikes
            if (!go.name.Contains("Shot Mantis Lord")) return go;

            PlayMakerFSM shotFsm = go.LocateMyFSM("Control");
            if (shotFsm == null) return go;
            // 40, 40, 20, 20
            shotFsm.GetAction<SetFloatValue>("Set L", 0).floatValue = -48f;
            shotFsm.GetAction<SetFloatValue>("Set R", 0).floatValue = 48f;
            shotFsm.GetAction<SetFloatValue>("Set L", 1).floatValue = -20f;
            shotFsm.GetAction<SetFloatValue>("Set R", 1).floatValue = 20f;

            return go;
        }

        public void Update()
        {
            if (RainbowFloor && _plane != null)
            {
                CurrentDelay++;
                if (CurrentDelay >= RainbowUpdateDelay)
                {
                    Texture2D tex = new Texture2D(1, 1);
                    tex.SetPixel(0, 0, GetNextRainbowColor());
                    tex.Apply();
                    _plane.GetComponent<MeshRenderer>().material.mainTexture = tex;
                    CurrentDelay = 0;
                }
            }

            if (!PlayerData.instance.defeatedMantisLords) return;
            if (_lord1 != null && _lord2 != null && _lord3 != null) return;

            if (_mantisBattle == null)
            {
                _mantisBattle = GameObject.Find("Mantis Battle");
            }

            if (_lord1 == null)
            {
                _lord1 = _mantisBattle.FindGameObjectInChildren("Mantis Lord");
                if (_lord1 != null) _lord1.AddComponent<Lord>();
            }

            if (_lord3 != null && _lord2 != null) return;

            _lord2 = _mantisBattle.FindGameObjectInChildren("Mantis Lord S1");
            _lord3 = _mantisBattle.FindGameObjectInChildren("Mantis Lord S2");

            if (_lord3 == null || _lord2 == null) return;
            
            _lord2.AddComponent<Lord>();
            _lord3.AddComponent<Lord>();
        }

        private static Mesh CreateMesh(float width, float height)
        {
            Mesh m = new Mesh
            {
                name = "ScriptedMesh",
                vertices = new Vector3[]
                {
                    new Vector3(-width, -height, 0.01f),
                    new Vector3(width, -height, 0.01f),
                    new Vector3(width, height, 0.01f),
                    new Vector3(-width, height, 0.01f)
                },
                uv = new Vector2[]
                {
                    new Vector2(0, 0),
                    new Vector2(0, 1),
                    new Vector2(1, 1),
                    new Vector2(1, 0)
                },
                triangles = new int[] {0, 1, 2, 0, 2, 3}
            };
            m.RecalculateNormals();
            return m;
        }

        private bool GetBool(string originalSet, bool orig)
        {
            if (originalSet == "defeatedMantisLords"
                && PlayerData.instance.defeatedMantisLords
                && !MantisGods.Instance.Settings.DefeatedGods
                && HeroController.instance.hero_state == ActorStates.no_input)
            {
                return false;
            }

            return orig;
        }


        // Used to override the text for Mantis Lords
        // Mantis Lords => Mantis Gods
        private static string LangGet(string key, string sheetTitle, string orig)
        {
            if (key == "MANTIS_LORDS_MAIN" && PlayerData.instance.defeatedMantisLords)
                return "Gods";

            return orig;
        }

        private void ResetScene(Scene arg0, LoadSceneMode arg1)
        {
            _lord1 = _lord2 = _lord3 = null;

            if (arg0.name != "Fungus2_15_boss") return;
            if (!PlayerData.instance.defeatedMantisLords) return;

            // Set the mapZone to White Palace so when you die
            // you don't spawn a shade and don't lose geo
            GameManager.instance.sm.mapZone = MapZone.WHITE_PALACE;

            // Destroy all game objects that aren't the BossLoader
            // BossLoader unloads the mantis lord stuff after you die
            foreach (GameObject go in USceneManager.GetSceneByName("Fungus2_15").GetRootGameObjects())
            {
                if (NormalArena)
                {
                    if 
                    (
                        !KeepSpikes 
                        && go.name.Contains("Deep Spikes")
                        || go.GetComponent<HealthManager>() != null
                    )
                    {
                        Destroy(go);
                    }
                }
                else
                {
                    if (go.name == "BossLoader") continue;
                    Destroy(go);
                }
            }

            GameObject[] floors =
            {
                GameObject.Find("Mantis Battle/mantis_lord_opening_floors"),
                GameObject.Find("Mantis Battle/mantis_lord_opening_floors (1)")
            };

            if (NormalArena)
            {
                if (KeepSpikes) return;

                foreach (GameObject go in floors)
                {
                    go.LocateMyFSM("Floor Control")
                      .ChangeTransition("Extended", "MLORD FLOOR RETRACT", "Extended");
                }

                return;
            }

            // Remove the brown floor thingies
            foreach (GameObject go in floors)
            {
                Destroy(go);
            }

            // Remove any particles
            var spc = FindObjectOfType<SceneParticlesController>();
            spc.DisableParticles();

            // Make the floor plane
            _plane = new GameObject("Plane")
            {
                // Make it able to be walked on
                tag = "HeroWalkable",
                layer = 8
            };

            // Dimensions
            var meshFilter = _plane.AddComponent<MeshFilter>();
            meshFilter.mesh = CreateMesh(200, 6.03f);
            var renderer = _plane.AddComponent<MeshRenderer>();
            renderer.material.shader = Shader.Find("Particles/Additive");

            // Color
            if (RainbowFloor)
            {
                var rand = new Random();
                RainbowPos = rand.Next(0, 767);
                FloorColor = GetNextRainbowColor();
                CurrentDelay = 0;
            }

            // Texture
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, FloorColor);
            tex.Apply();

            // Renderer
            renderer.material.mainTexture = tex;
            renderer.material.color = Color.white;

            // Collider
            var col = _plane.AddComponent<BoxCollider2D>();
            col.isTrigger = false;

            // Make it exist.
            _plane.SetActive(true);
        }

        private bool SetBool(string originalSet, bool value)
        {
            // If you've already defeated mantis lords and you're defeating them again
            // then you've just beat mantis gods
            if (originalSet != "defeatedMantisLords" || !PlayerData.instance.defeatedMantisLords || !value) 
                return value;
            
            MantisGods.Instance.Settings.DefeatedGods = true;
            StartCoroutine(BattleBeat());

            return true;
        }
    }
}