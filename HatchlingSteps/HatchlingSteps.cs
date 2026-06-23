using HarmonyLib;//
using OWML.Common;//
using OWML.ModHelper;//
using Steamworks;
using System.Reflection;//
using UnityEngine;//

namespace HatchlingSteps {
    public class HatchlingSteps : ModBehaviour {
        public static HatchlingSteps Instance;

        PlayerCharacterController playerController;
        ProbeLauncher probeLauncher;
        DialogueBoxVer2 subtitles;
        int[] skillLevel = new int[12];
        int increment = 2;
        Vector2 messVector = new(1, 1);
        Vector2 autoWalk = new(0, 0);
        bool forced = false;
        int shutUpToken = 0;

        public void Awake() {
            Instance = this;
            // You won't be able to access OWML's mod helper in Awake.
            // So you probably don't want to do anything here.
            // Use Start() instead.
        }

        public void Start() {
            // Starting here, you'll have access to OWML's mod helper.
            ModHelper.Console.WriteLine($"My mod {nameof(HatchlingSteps)} is loaded!", MessageType.Success);

            new Harmony("Vambok.HatchlingSteps").PatchAll(Assembly.GetExecutingAssembly());

            // Example of accessing game code.
            OnCompleteSceneLoad(OWScene.TitleScreen, OWScene.TitleScreen); // We start on title screen
            LoadManager.OnCompleteSceneLoad += OnCompleteSceneLoad;
        }

        public override void Configure(IModConfig config) {
            if(LoadManager.GetCurrentScene() == OWScene.SolarSystem) {
                if(config.GetSettingsValue<bool>("Unlearn")) {
                    skillLevel = new int[12];
                    config.SetSettingsValue("Unlearn", false);
                }
                increment = config.GetSettingsValue<string>("Skill") switch {
                    "Rock" => 0,
                    "Clumsy" => 1,
                    "Fast learner" => 4,
                    "Prodigy" => 8,
                    _ => 2
                };
            }
        }

        public void OnCompleteSceneLoad(OWScene previousScene, OWScene newScene) {
            if(newScene != OWScene.SolarSystem) return;

            ShipLogFactSave saveData = PlayerData.GetShipLogFactSave("HatchlingSteps_currentSkill");
            if(saveData != null) skillLevel = System.Array.ConvertAll(saveData.id.Split(','), int.Parse);
            ModHelper.Events.Unity.FireInNUpdates(() => {
                playerController = Locator.GetPlayerController();
                probeLauncher = Locator.GetToolModeSwapper()._probeLauncher;
                subtitles = GameObject.FindWithTag("DialogueGui").GetRequiredComponent<DialogueBoxVer2>();
            }, 30);
            ModHelper.Console.WriteLine("Loaded into solar system!", MessageType.Success);
        }

        public void Update() {
            // Walk:
            if(autoWalk.x > 0.01) autoWalk.x -= 0.1f * Time.deltaTime;
            else autoWalk.x = 0;
            if(autoWalk.y > 0.01) autoWalk.y -= 0.1f * Time.deltaTime;
            else autoWalk.y = 0;
            if(OWInput.IsNewlyPressed(InputLibrary.up) || OWInput.IsNewlyPressed(InputLibrary.down))
                if(Learn(Skills.Walk)) {
                    switch(Random.Range(0, 3)) {
                    case 0:
                        messVector.y = Random.Range(-1.5f, .5f);
                        break;
                    case 1:
                        messVector.y = 0;
                        DoShit(1 << (int)Skills.Jump | 1 << (int)Skills.Jetpack | 1 << (int)Skills.Scout | 1 << (int)Skills.Speak | 1 << (int)Skills.Cook);
                        break;
                    default:
                        messVector.y = 0;
                        break;
                    }
                }
            if(OWInput.IsNewlyPressed(InputLibrary.right) || OWInput.IsNewlyPressed(InputLibrary.left))
                if(Learn(Skills.Walk)) {
                    switch(Random.Range(0, 3)) {
                    case 0:
                        messVector.x = Random.Range(-1.5f, .5f);
                        break;
                    case 1:
                        messVector.x = 0;
                        DoShit(1 << (int)Skills.Jump | 1 << (int)Skills.Jetpack | 1 << (int)Skills.Scout | 1 << (int)Skills.Speak | 1 << (int)Skills.Cook);
                        break;
                    default:
                        messVector.x = 0;
                        break;
                    }
                }
            if(OWInput.IsNewlyReleased(InputLibrary.up) || OWInput.IsNewlyReleased(InputLibrary.down)) messVector.y = 1;
            if(OWInput.IsNewlyReleased(InputLibrary.right) || OWInput.IsNewlyReleased(InputLibrary.left)) messVector.x = 1;
            /*/ Jump://thrustUp
            if(OWInput.IsNewlyPressed(InputLibrary.jump)) ;
            // Jetpack:
            if(OWInput.IsNewlyPressed(InputLibrary.extendStick)) ;

            // Scout:
            // Fly:
            // Constitution:
            // Speak:
            // Read:
            // Swim:
            // Cook:
            // Repair:
            // Stealth:
            if(OWInput.IsNewlyPressed(InputLibrary.boost)) ;
            if(OWInput.IsNewlyPressed(InputLibrary.up)) ;
            if(OWInput.IsNewlyPressed(InputLibrary.right)) ;
            if(OWInput.IsNewlyPressed(InputLibrary.down)) ;
            if(OWInput.IsNewlyPressed(InputLibrary.left)) ;
            if(OWInput.IsNewlyPressed(InputLibrary.jump)) ;
            if(OWInput.IsNewlyPressed(InputLibrary.extendStick)) ;
            if(OWInput.IsNewlyPressed(InputLibrary.probeLaunch)) ;
            if(OWInput.IsNewlyPressed(InputLibrary.probeRetrieve)) ;
            if(OWInput.IsNewlyPressed(InputLibrary.thrustDown)) ;
            if(OWInput.IsNewlyPressed(InputLibrary.thrustUp)) ;
            if(OWInput.IsNewlyPressed(InputLibrary.thrustX)) ;
            if(OWInput.IsNewlyPressed(InputLibrary.thrustZ)) ;*/
        }

        void DoShit(int shitMask) {
            bool shitOk = false;
            if((shitMask >>> (int)Skills.Scout & 0x1) > 0) {
                if(probeLauncher.IsEquipped()) {
                    if(probeLauncher.GetActiveProbe() == null) {
                        if(probeLauncher.AllowLaunchMode()) shitOk = true;
                        // else ModHelper.Console.WriteLine("Probe launcher not ready! You dummy");
                    } else if(probeLauncher._allowRetrieval) {
                        shitOk = true;
                    } // else ModHelper.Console.WriteLine("Probe not ready to be retrieved! You dummy");
                } // else ModHelper.Console.WriteLine("Equip probe launcher to fire! You dummy");
                if(!shitOk) shitMask -= 1 << (int)Skills.Scout;
            }
            if((shitMask >>> (int)Skills.Jetpack & 0x1) > 0 && !playerController._playerResources.IsJetpackUsable()) shitMask -= 1 << (int)Skills.Jetpack;

            int nbSkills = skillLevel.Length;
            float chosenShit = 0;
            float[] derpSums = new float[nbSkills];
            for(int i = 0;i < nbSkills;i++) {
                chosenShit += (shitMask >>> i & 0x1) / (1 + skillLevel[i] / 50f);
                derpSums[i] = chosenShit;
            }
            chosenShit = Random.Range(0f, chosenShit);
            for(int i = 0;i < nbSkills;i++) {
                if(chosenShit < derpSums[i]) {
                    PerformShit((Skills)i);
                    break;
                }
            }
            void PerformShit(Skills skill) {
                switch(skill) {
                case Skills.Walk:
                    autoWalk += new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f));
                    break;
                case Skills.Jump:
                    playerController._jumpNextFixedUpdate = true;
                    playerController._jumpChargeTime = 1f;
                    forced = true;
                    break;
                case Skills.Jetpack:
                    playerController._jetpackModel.ActivateBoost();
                    break;
                case Skills.Scout:
                    if(probeLauncher.GetActiveProbe() == null) probeLauncher.LaunchProbe();
                    else {
                        probeLauncher.RetrieveProbe(true, false);
                        probeLauncher._allowRetrieval = false;
                    }
                    break;
                case Skills.Fly:
                    ModHelper.Console.WriteLine("You fly!"); //TODO fly
                    break;
                case Skills.Speak:
                    Speak();
                    break;
                case Skills.Cook:
                    ModHelper.Console.WriteLine("You cook!"); //TODO cook
                    break;
                default:
                    ModHelper.Console.WriteLine("Invalid shit! ("+skill+")", MessageType.Warning);
                    break;
                }
            }
        }

        void Speak() {
            string[] text = skillLevel[(int)Skills.Speak] switch {
                < 5 => ["..."],
                < 10 => ["aaaaa!", "ooooo!", "aa?", "mmm!", "oh!"],
                < 20 => ["Aaaaah!", "OooOo!", "Uuuuuh!", "Iiiii!", "Aaa?", "Ooooh!", "Hmmm!", "Eeeep!"],
                < 30 => ["Muh muh!", "Bah bah!", "Dah dah!", "Beh deh!", "Buh buh!", "Mah mah!", "Deh doh!", "Hah bah!"],
                < 40 => ["Dee dee dah!", "Bah BAH bah!", "Da ba dee da ba dah!", "Ba da bum!", "Dee dah doo!", "Buh dee bah!", "Mah na mah!"],
                < 60 => ["peak!", "space!", "woket!", "sky!", "blablabla!", "suit!", "ship!", "fire!", "stars!", "mashmaloo!", "nomai?"],
                < 100 => ["I words!", "Can talk!", "Me explorer!", "Ship go!", "Stars big!", "Helmet good!", "I speak!", "Can say things!", "Me hatchling!", "Hello!", "Friends!", "Adventure!"],
                < 150 => ["That was almost intentional.", "Yup, I meant to do that.", "Wait, was that speaking?", "Gravity is rude.", "Miss the campfire.", "I wonder if trees get dizzy.", "Oops! Failed again.", "No idea of what I'm doing.", "That wasn't based on anything.", "Space is amazing!", "I'm getting distracted.", "Do planets know they're planets?", "There sure is a lot of space in space.", "The universe is a strange place.", "I wonder where all the stars end."],
                _ => ["Through perseverance, I'll eventually succeed.", "If Feldspar can do it, so can I.", "I have absolutely no idea what I'm doing.", "Everything is better with a campfire.", "Maybe we just got tired of swimming.", "A single Eye? Not even close to half a view.", "Sometimes I forget how small I am.", "If I keep going, I might even find my long lost pet stone.", "I wonder what the stars might think of us.", "I should probably not say this out loud.", "The universe is very big, and I am very throwable."],
            };
            subtitles._potentialOptions = null;
            subtitles.ResetAllText();
            subtitles.SetNameFieldVisible(false);
            subtitles.SetMainFieldDialogueText(text[Random.Range(0, text.Length)]);
            subtitles._buttonPromptElement.gameObject.SetActive(false);
            subtitles._mainFieldTextEffect?.StartTextEffect();
            int localShutUpToken = ++shutUpToken;
            ModHelper.Events.Unity.FireInNUpdates(() => { if(localShutUpToken == shutUpToken) subtitles.SetVisible(false); }, Mathf.RoundToInt((text.Length + 20) / (Time.deltaTime * 20)));
        }

        bool Learn(Skills skill) {
            if(skill == Skills.Cook) skillLevel[(int)skill] += increment;
            else if(!forced && skillLevel[(int)skill] < Random.Range(0, 201)) {
                skillLevel[(int)skill] += increment;
                ModHelper.Console.WriteLine($"\"{skill}\" skill level increased to {skillLevel[(int)skill]}!", MessageType.Success);
                PlayerData._currentGameSave.shipLogFactSaves["HatchlingSteps_currentSkill"] = new ShipLogFactSave(string.Join(",", skillLevel));
                if(skill == Skills.Walk || skill == Skills.Constitution || skill == Skills.Jetpack || skill == Skills.Scout) UpdateTripping();
                return true;
            }
            return false;
        }
        void UpdateTripping() {
            foreach(IModBehaviour mod in ModHelper.Interaction.GetMods()) {
                if(mod.ModHelper.Manifest.UniqueName == "Owen_013.TrippingAndClumsiness") {
                    float movementFailChance = FailChance(Skills.Walk, true);
                    mod.ModHelper.Config.SetSettingsValue("Trip Duration", 2 / Mathf.Max(increment, 0.5f));
                    mod.ModHelper.Config.SetSettingsValue("Chance of Tripping Randomly", movementFailChance / 2);
                    mod.ModHelper.Config.SetSettingsValue("Chance of Tripping per Point of Damage", FailChance(Skills.Constitution, true) / 10);
                    mod.ModHelper.Config.SetSettingsValue("Reverse Boost Chance", FailChance(Skills.Jetpack) / 2);
                    mod.ModHelper.Config.SetSettingsValue("Scout Misfire Chance", FailChance(Skills.Scout) / 4);
                    mod.ModHelper.Config.SetSettingsValue("Chance of Tripping while Sprinting", movementFailChance);
                    mod.Configure(mod.ModHelper.Config);
                    break;
                }
            }

            float FailChance(Skills skill, bool residual = false) {
                return 1 - Mathf.Min(skillLevel[(int)skill] * 0.005f, 1) + (residual ? 0.08f - increment / 100f : 0);
            }
        }

        enum Skills {
            Walk = 0,
            Jump,
            Jetpack,
            Scout,
            Fly,
            Constitution,
            Speak,
            Read,
            Swim,
            Cook,
            Repair,
            Stealth
        }


        [HarmonyPatch]
        public class MyPatchClass {
            [HarmonyPrefix]
            [HarmonyPatch(typeof(PlayerCharacterController), nameof(PlayerCharacterController.ApplyJump))]
            static void PlayerCharacterController_ApplyJump_Prefix(PlayerCharacterController __instance) {
                if(__instance._jumpNextFixedUpdate) {
                    if(Instance.Learn(Skills.Jump)) {
                        switch(Random.Range(0, 3)) {
                        case 0:
                            Instance.forced = true;
                            break;
                        case 1:
                            __instance._jumpNextFixedUpdate = false;
                            __instance._jumpChargeTime = 0f;
                            __instance._lastJumpTime = Time.time;
                            Instance.DoShit(1 << (int)Skills.Walk | 1 << (int)Skills.Jetpack | 1 << (int)Skills.Scout | 1 << (int)Skills.Speak | 1 << (int)Skills.Cook);
                            break;
                        default:
                            __instance._jumpNextFixedUpdate = false;
                            __instance._jumpChargeTime = 0f;
                            __instance._lastJumpTime = Time.time;
                            break;
                        }
                    }
                }
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(PlayerCharacterController), nameof(PlayerCharacterController.CalculateJumpSpeed))]
            static bool PlayerCharacterController_CalculateJumpSpeed_Prefix(PlayerCharacterController __instance, ref float __result) {
                if(Instance.forced) {
                    __result = __instance._maxJumpSpeed * Random.Range(.1f, 1.8f);
                    Instance.forced = false;
                    return false;
                } else return true;
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(OWInput), nameof(OWInput.GetAxisValue))]
            static bool OWInput_GetAxisValue_Prefix(ref Vector2 __result, IInputCommands command, InputMode mask = InputMode.All) {
                if(command == InputLibrary.moveXZ && (mask & (InputMode.Character | InputMode.NomaiRemoteCam)) > 0) __result = OWInput.SharedInputManager.GetAxisValue(command, mask) * Instance.messVector + Instance.autoWalk;
                else return true;
                return false;
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(Campfire), nameof(Campfire.StartRoasting))]
            static void Campfire_StartRoasting_Postfix() {
                Instance.Learn(Skills.Cook);
            }
            [HarmonyPrefix]
            [HarmonyPatch(typeof(PlayerResources), nameof(PlayerResources.OnEatMarshmallow))]
            static void PlayerResources_OnEatMarshmallow_Prefix(float toastedFraction) {
                Instance.ModHelper.Console.WriteLine("Before pref:"+toastedFraction); //TEST
                float noobModifier = 1 - Instance.skillLevel[(int)Skills.Cook] / 150f;
                toastedFraction = (toastedFraction - 0.7f) * (1 + noobModifier) + 0.7f + Mathf.Max(noobModifier, 0) * (toastedFraction < 0.7f ? -0.5f : 0.3f);
                Instance.ModHelper.Console.WriteLine("After pref:" + toastedFraction); //TEST
            }
            [HarmonyPostfix]
            [HarmonyPatch(typeof(PlayerResources), nameof(PlayerResources.OnEatMarshmallow))]
            static void PlayerResources_OnEatMarshmallow_Postfix(float toastedFraction) {
                Instance.ModHelper.Console.WriteLine("Before post:" + toastedFraction); //TEST
                float damage;
                if(toastedFraction < 0f) damage = -toastedFraction / 1.2f;
                else if(toastedFraction > 1f) damage = (toastedFraction - 1f) / 0.6f;
                else return;
                Instance.ModHelper.Console.WriteLine("Damage:" + damage * 50f); //TEST
                Locator.GetPlayerBody().GetComponent<PlayerResources>().ApplyInstantDamage(damage * 50f, InstantDamageType.Puncture);
            }
        }
    }
}