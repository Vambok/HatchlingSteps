using Epic.OnlineServices;
using HarmonyLib;//
using OWML.Common;//
using OWML.ModHelper;//
using System.Reflection;//
using UnityEngine;//

namespace HatchlingSteps {
    public class HatchlingSteps : ModBehaviour {
        public static HatchlingSteps Instance;

        int[] skillLevel = new int[12];
        int increment = 2;
        Vector2 messVector = new(1, 1);

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

            ModHelper.Console.WriteLine("Loaded into solar system!", MessageType.Success);
        }

        public void Update() {
            // Walk:
            if(OWInput.IsNewlyPressed(InputLibrary.up) || OWInput.IsNewlyPressed(InputLibrary.down)) if(Learn(Skills.Walk)) messVector.y = Random.Range(-1.5f, .5f);
            if(OWInput.IsNewlyPressed(InputLibrary.right) || OWInput.IsNewlyPressed(InputLibrary.left)) if(Learn(Skills.Walk)) messVector.x = Random.Range(-1.5f, .5f);
            if(OWInput.IsNewlyReleased(InputLibrary.up) || OWInput.IsNewlyReleased(InputLibrary.down)) messVector.y = 1;
            if(OWInput.IsNewlyReleased(InputLibrary.right) || OWInput.IsNewlyReleased(InputLibrary.left)) messVector.x = 1;
            /*/ Jump:
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

        bool Learn(Skills skill) {
            if(skillLevel[(int)skill]<Random.Range(0,201)) {
                skillLevel[(int)skill] += increment;
                ModHelper.Console.WriteLine($"\"{skill}\" skill level increased to {skillLevel[(int)skill]}!", MessageType.Success);
                PlayerData._currentGameSave.shipLogFactSaves["HatchlingSteps_currentSkill"] = new ShipLogFactSave(string.Join(",", skillLevel));
                switch (skill) {
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
                        __instance._jumpNextFixedUpdate = false;
                        __instance._jumpChargeTime = 0f;
                        __instance._lastJumpTime = Time.time;
                    }
                }
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(OWInput), nameof(OWInput.GetAxisValue))]
            static bool OWInput_GetAxisValue_Prefix(ref Vector2 __result, IInputCommands command, InputMode mask = InputMode.All) {
                if(command == InputLibrary.moveXZ && (mask & (InputMode.Character | InputMode.NomaiRemoteCam)) > 0) __result = OWInput.SharedInputManager.GetAxisValue(command, mask) * Instance.messVector;
                else return true;
                return false;
            }
        }
    }
}