using HarmonyLib;//
using OWML.Common;//
using OWML.ModHelper;//
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
        public bool forced = false;
        float shutUpTimer = 0;

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
                chosenShit += (shitMask >>> i & 0x1) / (skillLevel[i] + 4f);
                derpSums[i] = chosenShit;
            }
            chosenShit = Random.Range(0f, chosenShit);
            if(chosenShit < derpSums[(int)Skills.Walk]) {
                autoWalk += new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f));
            } else if(chosenShit < derpSums[(int)Skills.Jump]) {
                playerController._jumpNextFixedUpdate = true;
                playerController._jumpChargeTime = 1f;
                forced = true;
            } else if(chosenShit < derpSums[(int)Skills.Jetpack]) {
                playerController._jetpackModel.ActivateBoost();
            } else if(chosenShit < derpSums[(int)Skills.Scout]) {
                if(probeLauncher.GetActiveProbe() == null) probeLauncher.LaunchProbe();
                else {
                    probeLauncher.RetrieveProbe(true, false);
                    probeLauncher._allowRetrieval = false;
                }
            } else if(chosenShit < derpSums[(int)Skills.Fly]) {
                //TODO fly
            } else if(chosenShit < derpSums[(int)Skills.Constitution]) {
            } else if(chosenShit < derpSums[(int)Skills.Speak]) {
                ModHelper.Console.WriteLine("You: Blablabla!"); //TODO speak
            } else if(chosenShit < derpSums[(int)Skills.Read]) {
            } else if(chosenShit < derpSums[(int)Skills.Swim]) {
                //TODO swim
            } else if(chosenShit < derpSums[(int)Skills.Cook]) {
                ModHelper.Console.WriteLine("You cook!"); //TODO cook
            }/* else if(chosenShit < derpSums[(int)Skills.Repair]) {
            } else if(chosenShit < derpSums[(int)Skills.Stealth]) {
            } else ModHelper.Console.WriteLine("Invalid skill!", MessageType.Warning);//*/
        }

        void Speak(string text) {
            switch(skillLevel[(int)Skills.Speak]) {
            case <5:
                text = "...";
                break;
            case <10:
                text = "aaaaa!";
                text = "ooooo!";
                break;
            case <20:
                text = "Aaaaah!";
                text = "OooOo!";
                text = "Uuuuuh!";
                text = "Iiiii!";
                break;
            case <30:
                text = "Muh muh!";
                text = "Bah bah!";
                text = "Dah dah!";
                text = "Beh deh!";
                break;
            case <40:
                text = "Dee dee dah!";
                text = "Bah BAH bah!";
                text = "Da ba dee da ba dah!";
                break;
            case <60:
                text = "peak!";
                text = "space!";
                text = "woket!";
                text = "sky!";
                text = "blablabla!";
                break;
            case <100:
                text = "I speak!";
                text = "Can say things!";
                text = "Me hatchling!";
                text = "Hello!";
                text = "Friends!";
                text = "Adventure!";
                break;
            default:
                break;
            }

            subtitles._potentialOptions = null;
            subtitles.ResetAllText();
            subtitles.SetNameFieldVisible(false);
            subtitles.SetMainFieldDialogueText(text);
            subtitles._buttonPromptElement.gameObject.SetActive(false);
            subtitles._mainFieldTextEffect?.StartTextEffect();
            float shutUpTime = Time.time;
            ModHelper.Events.Unity.FireInNUpdates(() => {
                ShutUp(shutUpTime);
            }, Mathf.RoundToInt((text.Length+20)/(Time.deltaTime*20)));
            shutUpTimer = shutUpTime;
        }
        void ShutUp(float timer) {
            if(timer == shutUpTimer) subtitles.SetVisible(false);
        }

        bool Learn(Skills skill) {
            if(!forced && skillLevel[(int)skill] < Random.Range(0, 201)) {
                skillLevel[(int)skill] += increment;
                ModHelper.Console.WriteLine($"\"{skill}\" skill level increased to {skillLevel[(int)skill]}!", MessageType.Success);
                PlayerData._currentGameSave.shipLogFactSaves["HatchlingSteps_currentSkill"] = new ShipLogFactSave(string.Join(",", skillLevel));
                switch(skill) {
                case Skills.Walk:
                    //config.SetSettingsValue("Chance of Tripping Randomly", 0.53-skillLevel[(int)skill]*0.0025); //TODO
                    break;
                default:
                    break;
                }
                return true;
            }
            return false;
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
        }
    }
}