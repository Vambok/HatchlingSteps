using HarmonyLib;//
using OWML.Common;//
using OWML.ModHelper;//
using System.Reflection;//
using UnityEngine;//

namespace HatchlingSteps {
    public class HatchlingSteps : ModBehaviour {
        public static HatchlingSteps Instance;

        // Global variables:
        bool sceneLoaded = false;

        // Game objects:
        PlayerResources playerResources;
        PlayerCharacterController playerController;
        ProbeLauncher probeLauncher;
        ProbeLauncher shipProbeLauncher;
        DialogueBoxVer2 subtitles;
        Campfire fakeCampfire;
        ShipCockpitController shipCockpitController;

        // Data:
        const int nbSkills = 12;
        int[] skillLevel = new int[nbSkills];
        int increment = 2;
        int forced = 0;
        //(walk)
        Vector2 messVector = Vector2.one;
        Vector2 autoWalk = Vector2.zero;
        //(speak)
        int shutUpToken = 0;
        //(fly)
        Vector3 autoThrust = Vector3.zero;
        Vector2 autoRotation = Vector2.zero;
        Vector3 autoShipThrust = Vector3.zero;
        Vector2 autoShipRotation = Vector2.zero;
        //(repair)
        float repairSkillCooldown = 0;
        //(stealth)
        float stealthSkillCooldown = 0;
        bool ghostEscape = false;

        // how probable is it to randomly:   Walk     Jump    Jetpack   Scout    Fly  Constitution Speak     Read     Swim     Cook    Repair   Stealth   weight is 1/(i1 + skillLvl / i2)
        readonly (int, int)[] shitParams = [(1, 50), (1, 50), (1, 50), (1, 50), (1, 50), (1, 50), (3, 100), (1, 50), (1, 50), (8, 50), (1, 50), (1, 50)];
        enum Skills {
            Walk = 0,//
            Jump,//
            Jetpack,//
            Scout,//
            Fly,//
            Constitution,//
            Speak,
            Read,
            Swim,
            Cook,//
            Repair,
            Stealth//
        }

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
                    skillLevel = new int[nbSkills];
                    config.SetSettingsValue("Unlearn", false);
                    UpdateTripping();
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
            if(newScene != OWScene.SolarSystem) sceneLoaded = false;
            else {
                ShipLogFactSave saveData = PlayerData.GetShipLogFactSave("HatchlingSteps_currentSkill");
                if(saveData != null) skillLevel = System.Array.ConvertAll(saveData.id.Split(','), int.Parse);
                ModHelper.Events.Unity.FireInNUpdates(() => {
                    playerController = Locator.GetPlayerController();
                    playerResources = playerController._playerResources;
                    probeLauncher = Locator.GetToolModeSwapper().GetProbeLauncher();
                    subtitles = GameObject.FindWithTag("DialogueGui").GetRequiredComponent<DialogueBoxVer2>();
                    fakeCampfire = new Campfire();
                    GlobalMessenger.AddListener("ExitRoastingMode", () => ModHelper.Events.Unity.FireInNUpdates(() => { if(forced == (int)Skills.Cook) forced = 0; }, 10));
                    shipCockpitController = Locator.GetShipTransform().Find("Module_Cockpit/Systems_Cockpit/ShipCockpitController").GetComponent<ShipCockpitController>();
                    shipProbeLauncher = shipCockpitController.GetShipProbeLauncher();
                    PlayerResources._maxHealth *= 0.5f + skillLevel[(int)Skills.Constitution] / 400f;
                    playerResources._currentHealth = PlayerResources._maxHealth;
                    sceneLoaded = true;
                }, 30);
                ModHelper.Console.WriteLine("Loaded into solar system!", MessageType.Success);
            }
        }

        public void Update() {
            if(!sceneLoaded) return;
            // Walk:
            if(autoWalk.x > 0.01) autoWalk.x -= 0.1f * Time.deltaTime;
            else autoWalk.x = 0;
            if(autoWalk.y > 0.01) autoWalk.y -= 0.1f * Time.deltaTime;
            else autoWalk.y = 0;
            if(OWInput.IsNewlyPressed(InputLibrary.up, InputMode.Character | InputMode.NomaiRemoteCam) || OWInput.IsNewlyPressed(InputLibrary.down, InputMode.Character | InputMode.NomaiRemoteCam)) {
                if(Learn(Skills.Walk)) {
                    switch(Random.Range(0, 3)) {
                    case 0:
                        messVector.y = Random.Range(-1.5f, .9f);
                        ModHelper.Console.WriteLine("Walk: You mess!"); //TEST
                        break;
                    case 1:
                        messVector.y = 0;
                        ModHelper.Console.WriteLine("Walk: You shit!"); //TEST
                        DoShit(1 << (int)Skills.Jump | 1 << (int)Skills.Jetpack | 1 << (int)Skills.Scout | 1 << (int)Skills.Speak | 1 << (int)Skills.Cook);
                        break;
                    default:
                        messVector.x = Random.Range(-.5f, .5f);
                        messVector.y = Random.Range(-1.5f, .9f);
                        ModHelper.Console.WriteLine("Walk: You mess x2!"); //TEST
                        break;
                    }
                }
            } else if(OWInput.IsNewlyPressed(InputLibrary.right, InputMode.Character | InputMode.NomaiRemoteCam) || OWInput.IsNewlyPressed(InputLibrary.left, InputMode.Character | InputMode.NomaiRemoteCam))
                if(Learn(Skills.Walk)) {
                    switch(Random.Range(0, 3)) {
                    case 0:
                        messVector.x = Random.Range(-1.5f, .9f);
                        ModHelper.Console.WriteLine("Walk: You mess!"); //TEST
                        break;
                    case 1:
                        messVector.x = 0;
                        DoShit(1 << (int)Skills.Jump | 1 << (int)Skills.Jetpack | 1 << (int)Skills.Scout | 1 << (int)Skills.Speak | 1 << (int)Skills.Cook);
                        ModHelper.Console.WriteLine("Walk: You shit!"); //TEST
                        break;
                    default:
                        messVector.x = Random.Range(-1.5f, .9f);
                        messVector.y = Random.Range(-.5f, .5f);
                        ModHelper.Console.WriteLine("Walk: You mess x2!"); //TEST
                        break;
                    }
                }
            if(OWInput.IsNewlyReleased(InputLibrary.up) || OWInput.IsNewlyReleased(InputLibrary.down)) messVector.y = 1;
            if(OWInput.IsNewlyReleased(InputLibrary.right) || OWInput.IsNewlyReleased(InputLibrary.left)) messVector.x = 1;

            // Fly:
            if((OWInput.IsInputMode(InputMode.ShipCockpit | InputMode.LandingCam) && !shipCockpitController._landingManager.IsLanded()) || (OWInput.IsInputMode(InputMode.Character | InputMode.NomaiRemoteCam) && playerController._isWearingSuit && playerController._playerResources.IsJetpackUsable() && !playerController.IsGrounded() && !playerController._isTumbling)) {
                InputMode mask = InputMode.Character | InputMode.ShipCockpit | InputMode.LandingCam | InputMode.NomaiRemoteCam;
                if(OWInput.IsNewlyPressed(InputLibrary.thrustX, mask) || OWInput.IsNewlyPressed(InputLibrary.thrustZ, mask) || OWInput.IsNewlyPressed(InputLibrary.thrustUp, mask) || OWInput.IsNewlyPressed(InputLibrary.thrustDown, mask)) {
                    if(Learn(Skills.Fly)) {
                        Vector2 tempRot = Vector2.zero;
                        Vector3 tempMov = new(OWInput.GetValue(InputLibrary.thrustX), 0, OWInput.GetValue(InputLibrary.thrustZ));
                        if(tempMov.sqrMagnitude > 1) tempMov.Normalize();
                        tempMov.y = OWInput.GetValue(InputLibrary.thrustUp) - OWInput.GetValue(InputLibrary.thrustDown);
                        switch(Random.Range(0, 3)) {
                        case 0:
                            tempMov *= Random.Range(-2.5f, -.1f);
                            ModHelper.Console.WriteLine("Fly: You mess!"); //TEST
                            break;
                        case 1:
                            tempMov *= -1;
                            ModHelper.Console.WriteLine("Fly: You shit!"); //TEST
                            DoShit(1 << (int)Skills.Cook | 1 << (int)Skills.Scout | 1 << (int)Skills.Speak);
                            break;
                        default:
                            tempMov *= Random.Range(-2.5f, -.1f);
                            tempRot = new Vector2(OWInput.GetValue(InputLibrary.pitch), OWInput.GetValue(InputLibrary.yaw)) * Random.Range(-2.5f, -.1f);
                            ModHelper.Console.WriteLine("Fly: You mess x2!"); //TEST
                            break;
                        }
                        if(OWInput.IsInputMode(InputMode.ShipCockpit | InputMode.LandingCam)) {
                            autoThrust = Vector3.zero;
                            autoRotation = Vector2.zero;
                            autoShipThrust = tempMov;
                            if(tempRot.sqrMagnitude > .001f) autoShipRotation = tempRot;
                        } else {
                            autoShipThrust = Vector3.zero;
                            autoShipRotation = Vector2.zero;
                            autoThrust = tempMov;
                            if(tempRot.sqrMagnitude > .001f) autoRotation = tempRot;
                        }
                    }
                } else if(OWInput.IsNewlyPressed(InputLibrary.yaw, mask) || OWInput.IsNewlyPressed(InputLibrary.pitch, mask))
                    if(Learn(Skills.Fly)) {
                        Vector3 tempMov = Vector3.zero;
                        Vector2 tempRot = new(OWInput.GetValue(InputLibrary.pitch), OWInput.GetValue(InputLibrary.yaw));
                        switch(Random.Range(0, 3)) {
                        case 0:
                            tempRot *= Random.Range(-2.5f, -.1f);
                            ModHelper.Console.WriteLine("Fly: You mess!"); //TEST
                            break;
                        case 1:
                            tempRot *= -1;
                            ModHelper.Console.WriteLine("Fly: You shit!"); //TEST
                            DoShit(1 << (int)Skills.Cook | 1 << (int)Skills.Scout | 1 << (int)Skills.Speak);
                            break;
                        default:
                            tempRot *= Random.Range(-2.5f, -.1f);
                            tempMov = new(OWInput.GetValue(InputLibrary.thrustX), 0, OWInput.GetValue(InputLibrary.thrustZ));
                            if(tempMov.sqrMagnitude > 1) tempMov.Normalize();
                            tempMov.y = OWInput.GetValue(InputLibrary.thrustUp) - OWInput.GetValue(InputLibrary.thrustDown);
                            ModHelper.Console.WriteLine("Fly: You mess x2!"); //TEST
                            break;
                        }
                        if(OWInput.IsInputMode(InputMode.ShipCockpit | InputMode.LandingCam)) {
                            autoThrust = Vector3.zero;
                            autoRotation = Vector2.zero;
                            if(tempMov.sqrMagnitude > .001f) autoShipThrust = tempMov;
                            autoShipRotation = tempRot;
                        } else {
                            autoShipThrust = Vector3.zero;
                            autoShipRotation = Vector2.zero;
                            if(tempMov.sqrMagnitude > .001f) autoThrust = tempMov;
                            autoRotation = tempRot;
                        }
                    }
            }
            if(OWInput.IsNewlyReleased(InputLibrary.thrustX) || OWInput.IsNewlyReleased(InputLibrary.thrustZ) || OWInput.IsNewlyReleased(InputLibrary.thrustUp) || OWInput.IsNewlyReleased(InputLibrary.thrustDown))
                if(!OWInput.IsPressed(InputLibrary.thrustX) && !OWInput.IsPressed(InputLibrary.thrustZ) && !OWInput.IsPressed(InputLibrary.thrustUp) && !OWInput.IsPressed(InputLibrary.thrustDown)) {
                    autoThrust = Vector3.zero;
                    autoShipThrust = Vector3.zero;
                }
            if(OWInput.IsNewlyReleased(InputLibrary.pitch) || OWInput.IsNewlyReleased(InputLibrary.yaw))
                if(!OWInput.IsPressed(InputLibrary.pitch) && !OWInput.IsPressed(InputLibrary.yaw)) {
                    autoRotation = Vector3.zero;
                    autoShipRotation = Vector3.zero;
                }

            // Cook:
            if(OWInput.GetInputMode() == InputMode.Roasting && forced == (int)Skills.Cook && OWInput.IsNewlyPressed(InputLibrary.jump)) GlobalMessenger.FireEvent("ExitRoastingMode");

            /*/ Jetpack:
            if(OWInput.IsNewlyPressed(InputLibrary.extendStick)) ;

            // Scout:
            // Constitution:
            // Speak:
            // Read:
            // Swim:
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
            // Can you Walk or Jump?
            if(!(playerController._isAlignedToForce || playerController._isZeroGMovementEnabled) || playerController._isMovementLocked || playerController._isTumbling) {
                shitMask &= ~(1 << (int)Skills.Walk | 1 << (int)Skills.Jump);
            } else if(!playerController.HasGroundControl())
                shitMask &= ~(1 << (int)Skills.Walk);
            // Can you Jetpack?
            if(!Locator.GetPlayerSuit().IsWearingSuit(true) || !playerController._playerResources.IsJetpackUsable())
                shitMask &= ~(1 << (int)Skills.Jetpack);
            // Can you Scout? (Launch or retrieve probe)
            ProbeLauncher currentProbeLauncher = probeLauncher;
            if((shitMask >>> (int)Skills.Scout & 0x1) > 0) {
                bool shitOk = false;
                if(probeLauncher.IsEquipped()) {
                    if(probeLauncher.GetActiveProbe() == null || probeLauncher._allowRetrieval) shitOk = true;
                } else if(shipProbeLauncher.IsEquipped()) {
                    currentProbeLauncher = shipProbeLauncher;
                    if(shipProbeLauncher.GetActiveProbe() == null || shipProbeLauncher._allowRetrieval) shitOk = true;
                } // else ModHelper.Console.WriteLine("Equip probe launcher to fire! You dummy");
                if(!shitOk) shitMask &= ~(1 << (int)Skills.Scout);
            }
            // Can you Fly? (ship or jetpack)
            if((!shipCockpitController._playerAtFlightConsole || !OWInput.IsInputMode(InputMode.ShipCockpit | InputMode.LandingCam)) && (!Locator.GetPlayerSuit().IsWearingSuit() || !playerController._playerResources.IsJetpackUsable()))
                shitMask &= ~(1 << (int)Skills.Fly);
            // Can you Cook?
            if(!OWInput.IsInputMode(InputMode.Character | InputMode.NomaiRemoteCam))
                shitMask &= ~(1 << (int)Skills.Cook);

            if(shitMask <= 0) {
                ModHelper.Console.WriteLine("Can't do shit!"); //TEST
                return;
            }
            ModHelper.Console.WriteLine("6:" + shitMask); //TEST

            float chosenShit = 0;
            float[] derpSums = new float[nbSkills];
            for(int i = 0; i < nbSkills; i++) {
                if((shitMask >>> i & 0x1) > 0) chosenShit += 1 / (shitParams[i].Item1 + skillLevel[i] / shitParams[i].Item2);
                derpSums[i] = chosenShit;
            }
            chosenShit = Random.Range(0f, chosenShit);
            for(int i = 0; i < nbSkills; i++) {
                if(chosenShit < derpSums[i]) {
                    ModHelper.Console.WriteLine("You " + (Skills)i); //TEST
                    PerformShit((Skills)i);
                    break;
                }
            }
            void PerformShit(Skills skill) {
                switch(skill) {
                case Skills.Walk:
                    ModHelper.Console.WriteLine("You walk!"); //TEST
                    autoWalk += new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f));
                    break;
                case Skills.Jump:
                    ModHelper.Console.WriteLine("You jump!"); //TEST
                    forced = (int)skill;
                    playerController._jumpNextFixedUpdate = true;
                    playerController._jumpChargeTime = 1f;
                    break;
                case Skills.Jetpack:
                    ModHelper.Console.WriteLine("You boost jetpack!"); //TEST
                    forced = (int)skill;
                    playerController._jetpackModel.ActivateBoost();
                    break;
                case Skills.Scout:
                    ModHelper.Console.WriteLine("You scout!"); //TEST
                    if(currentProbeLauncher.GetActiveProbe() == null) {
                        forced = (int)skill;
                        currentProbeLauncher.LaunchProbe();
                    } else {
                        currentProbeLauncher.RetrieveProbe(true, false);
                        currentProbeLauncher._allowRetrieval = false;
                    }
                    break;
                case Skills.Fly:
                    if(OWInput.IsInputMode(InputMode.ShipCockpit | InputMode.LandingCam)) {
                        ModHelper.Console.WriteLine("You fly ship!"); //TEST
                        autoShipThrust = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f));
                        autoShipRotation = new Vector3(Random.Range(-.5f, .5f), Random.Range(-.5f, .5f), Random.Range(-.5f, .5f));
                        ModHelper.Events.Unity.FireInNUpdates(() => { autoShipThrust = Vector3.zero; autoShipRotation = Vector3.zero; }, Mathf.RoundToInt(.5f / Time.deltaTime));
                    } else {
                        ModHelper.Console.WriteLine("You fly jetpack!"); //TEST
                        autoThrust = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f));
                        autoRotation = new Vector3(Random.Range(-.5f, .5f), Random.Range(-.5f, .5f), Random.Range(-.5f, .5f));
                        ModHelper.Events.Unity.FireInNUpdates(() => { autoThrust = Vector3.zero; autoRotation = Vector3.zero; }, Mathf.RoundToInt(.5f / Time.deltaTime));
                    }
                    break;
                case Skills.Speak:
                    ModHelper.Console.WriteLine("You speak!"); //TEST
                    Speak();
                    break;
                case Skills.Cook:
                    ModHelper.Console.WriteLine("You cook!"); //TEST
                    forced = (int)skill;
                    GlobalMessenger<Campfire>.FireEvent("EnterRoastingMode", fakeCampfire);
                    break;
                default:
                    ModHelper.Console.WriteLine("Invalid shit! (" + skill + ")", MessageType.Warning);
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
            ModHelper.Events.Unity.FireInNUpdates(() => { if(localShutUpToken == shutUpToken) subtitles.SetVisible(false); }, Mathf.RoundToInt((text.Length + 40) / (Time.deltaTime * 20)));
        }

        bool Learn(Skills skill, bool skillCheck = true) {
            if(forced > 0) {
                if(forced == (int)skill && skill != Skills.Jump) forced = 0;
                return false;
            }
            switch(skill) {
            case Skills.Cook:
                break;
            case Skills.Constitution:
                playerResources._currentHealth += playerResources.GetHealthFraction() * increment / 400f;
                PlayerResources._maxHealth += increment / 400f;
                break;
            default:
                if(skillCheck && Random.Range(0, 201) <= skillLevel[(int)skill]) return false;
                break;
            }
            skillLevel[(int)skill] += increment;
            ModHelper.Console.WriteLine($"\"{skill}\" skill level increased to {skillLevel[(int)skill]}!", MessageType.Success);
            PlayerData._currentGameSave.shipLogFactSaves["HatchlingSteps_currentSkill"] = new ShipLogFactSave(string.Join(",", skillLevel));
            if(skill == Skills.Walk || skill == Skills.Constitution || skill == Skills.Jetpack || skill == Skills.Scout) UpdateTripping();
            return true;
        }
        void UpdateTripping() {
            foreach(IModBehaviour mod in ModHelper.Interaction.GetMods()) {
                if(mod.ModHelper.Manifest.UniqueName == "Owen_013.TrippingAndClumsiness") {
                    float movementFailChance = FailChance(Skills.Walk, true) / 4;
                    mod.ModHelper.Config.SetSettingsValue("Trip Duration", 2 / Mathf.Max(increment, 0.5f));
                    mod.ModHelper.Config.SetSettingsValue("Chance of Tripping Randomly", movementFailChance);
                    mod.ModHelper.Config.SetSettingsValue("Chance of Tripping per Point of Damage", FailChance(Skills.Constitution, true) / 10);
                    mod.ModHelper.Config.SetSettingsValue("Reverse Boost Chance", FailChance(Skills.Jetpack) / 2);
                    mod.ModHelper.Config.SetSettingsValue("Scout Misfire Chance", FailChance(Skills.Scout) / 4);
                    mod.ModHelper.Config.SetSettingsValue("Chance of Tripping while Sprinting", movementFailChance * 2);
                    mod.ModHelper.Config.SetSettingsValue("Emergency Boost Misfire Chance", 0);
                    mod.Configure(mod.ModHelper.Config);
                    break;
                }
            }
            float FailChance(Skills skill, bool residual = false) {
                return 1 - Mathf.Min(skillLevel[(int)skill] * 0.005f, 1) + (residual ? 0.04f - increment / 200f : 0);
            }
        }

        // PATCHES //

        [HarmonyPatch]
        public class MyPatchClass {
            // Walk:
            [HarmonyPrefix] // If walk is messed up, change walk speed
            [HarmonyPatch(typeof(OWInput), nameof(OWInput.GetAxisValue))]
            static bool OWInput_GetAxisValue_Prefix(ref Vector2 __result, IInputCommands command, InputMode mask = InputMode.All) {
                if(command == InputLibrary.moveXZ && (mask & (InputMode.Character | InputMode.NomaiRemoteCam)) > 0) __result = OWInput.SharedInputManager.GetAxisValue(command, mask) * Instance.messVector + Instance.autoWalk;
                else return true;
                return false;
            }

            // Jump:
            [HarmonyPrefix] // Check if jump is successful, if not mess up and learn
            [HarmonyPatch(typeof(PlayerCharacterController), nameof(PlayerCharacterController.ApplyJump))]
            static void PlayerCharacterController_ApplyJump_Prefix(PlayerCharacterController __instance) {
                if(__instance._jumpNextFixedUpdate) {
                    if(Instance.Learn(Skills.Jump)) {
                        switch(Random.Range(0, 3)) {
                        case 0:
                            Instance.ModHelper.Console.WriteLine("Jump: You mess!"); //TEST
                            Instance.forced = (int)Skills.Jump;
                            break;
                        case 1:
                            Instance.ModHelper.Console.WriteLine("Jump: You shit!"); //TEST
                            __instance._jumpNextFixedUpdate = false;
                            __instance._jumpChargeTime = 0f;
                            __instance._lastJumpTime = Time.time;
                            Instance.DoShit(1 << (int)Skills.Walk | 1 << (int)Skills.Jetpack | 1 << (int)Skills.Scout | 1 << (int)Skills.Speak | 1 << (int)Skills.Cook);
                            break;
                        default:
                            __instance._jumpNextFixedUpdate = false;
                            __instance._jumpChargeTime = 0f;
                            __instance._lastJumpTime = Time.time;
                            Instance.ModHelper.Console.WriteLine("Jump: You do nothing!"); //TEST
                            break;
                        }
                    }
                }
            }
            [HarmonyPrefix] // If jump is messed up, change jump speed (don't learn twice)
            [HarmonyPatch(typeof(PlayerCharacterController), nameof(PlayerCharacterController.CalculateJumpSpeed))]
            static bool PlayerCharacterController_CalculateJumpSpeed_Prefix(PlayerCharacterController __instance, ref float __result) {
                if(Instance.forced == (int)Skills.Jump) {
                    __result = __instance._maxJumpSpeed * Random.Range(.1f, 1.8f);
                    Instance.forced = 0;
                    return false;
                } else return true;
            }

            // Jetpack:
            [HarmonyPrefix] // Check if jetpack is successful, if not mess up boost and learn
            [HarmonyPatch(typeof(JetpackThrusterModel), nameof(JetpackThrusterModel.ActivateBoost))]
            static bool JetpackThrusterModel_ActivateBoost_Prefix(JetpackThrusterModel __instance) {
                if(Instance.forced == (int)Skills.Jetpack) Instance.forced = 0;
                else if(__instance.IsBoosterReadyToFire() && Instance.Learn(Skills.Jetpack)) {
                    switch(Random.Range(0, 3)) {
                    case 0:
                        Instance.ModHelper.Console.WriteLine("Jetpack: You mess!"); //TEST
                        float realBoostGroundVelocity = __instance._boostGroundVelocity;
                        __instance._boostGroundVelocity *= Random.Range(-.5f, 2.5f);
                        Instance.ModHelper.Events.Unity.FireInNUpdates(() => { __instance._boostGroundVelocity = realBoostGroundVelocity; }, 10);
                        return true;
                    case 1:
                        Instance.ModHelper.Console.WriteLine("Jetpack: You shit!"); //TEST
                        Instance.DoShit(1 << (int)Skills.Walk | 1 << (int)Skills.Jump | 1 << (int)Skills.Scout | 1 << (int)Skills.Fly | 1 << (int)Skills.Speak | 1 << (int)Skills.Cook);
                        break;
                    default:
                        Instance.ModHelper.Console.WriteLine("Jetpack: You do nothing!"); //TEST
                        break;
                    }
                    return false;
                }
                return true;
            }

            // Scout:
            [HarmonyPrefix] // Check if scout is successful, if not mess up and learn
            [HarmonyPatch(typeof(ProbeLauncher), nameof(ProbeLauncher.LaunchProbe))]
            static bool ProbeLauncher_LaunchProbe_Prefix(ProbeLauncher __instance) {
                if(Instance.Learn(Skills.Scout)) {
                    switch(Random.Range(0, 2)) {
                    case 0:
                        Instance.ModHelper.Console.WriteLine("Scout: You shit!"); //TEST
                        Instance.DoShit(1 << (int)Skills.Walk | 1 << (int)Skills.Jump | 1 << (int)Skills.Jetpack | 1 << (int)Skills.Speak | 1 << (int)Skills.Cook);
                        return false;
                    default:
                        Instance.ModHelper.Console.WriteLine("Scout: You do nothing!"); //TEST
                        Instance.ModHelper.Events.Unity.FireInNUpdates(() => { __instance.RetrieveProbe(false, true); }, 2);
                        break;
                    }
                }
                return true;
            }

            // Fly:
            [HarmonyPostfix] // When ship flight is messed up, add to movement
            [HarmonyPatch(typeof(ShipThrusterController), nameof(ShipThrusterController.ReadTranslationalInput))]
            static void ShipThrusterController_ReadTranslationalInput_Postfix(ref Vector3 __result) {
                __result += Instance.autoShipThrust;
            }
            [HarmonyPostfix] // When ship flight is messed up, add to rotation
            [HarmonyPatch(typeof(ShipThrusterController), nameof(ShipThrusterController.ReadRotationalInput))]
            static void ShipThrusterController_ReadRotationalInput_Postfix(ref Vector3 __result) {
                __result.x -= Instance.autoShipRotation.x;
                if(__result.z == 0) __result.y += Instance.autoShipRotation.y;
                else if(__result.z < 0) __result.z -= Instance.autoShipRotation.y;
                else __result.z += Instance.autoShipRotation.y;
            }
            [HarmonyPostfix] // When jetpack flight is messed up, add to movement
            [HarmonyPatch(typeof(JetpackThrusterController), nameof(JetpackThrusterController.ReadTranslationalInput))]
            static void JetpackThrusterController_ReadTranslationalInput_Postfix(ref Vector3 __result) {
                __result += Instance.autoThrust;
            }
            [HarmonyPostfix] // When jetpack flight is messed up, add to rotation
            [HarmonyPatch(typeof(JetpackThrusterController), nameof(JetpackThrusterController.ReadRotationalInput))]
            static void JetpackThrusterController_ReadRotationalInput_Postfix(ref Vector3 __result) {
                __result.x -= Instance.autoRotation.x;
                if(__result.z == 0) __result.y += Instance.autoRotation.y;
                else if(__result.z < 0) __result.z -= Instance.autoRotation.y;
                else __result.z += Instance.autoRotation.y;
            }

            // Constitution:
            [HarmonyPostfix] // Increase constitution skill when player takes damage
            [HarmonyPatch(typeof(PlayerResources), nameof(PlayerResources.ApplyInstantDamage))]
            static void PlayerResources_ApplyInstantDamage_Postfix(ref bool __result) {
                if(__result) Instance.Learn(Skills.Constitution);
            }

            // Cook:
            [HarmonyPostfix] // When marshmallow is spawned, increase cook skill
            [HarmonyPatch(typeof(Marshmallow), nameof(Marshmallow.SpawnMallow))]
            static void Marshmallow_SpawnMallow_Postfix() {
                Instance.Learn(Skills.Cook);
            }
            [HarmonyPrefix] // If cook is forced, skip ref to campfire
            [HarmonyPatch(typeof(RoastingStickController), nameof(RoastingStickController.CalculateMaxStickExtension))]
            static bool RoastingStickController_CalculateMaxStickExtension_Prefix(RoastingStickController __instance, ref float __result) {
                try { if(__instance._campfire.transform != null) return true; } catch { }
                __result = __instance._stickMaxZ;
                return false;
            }
            [HarmonyPrefix] // If cook is forced, skip ref to campfire
            [HarmonyPatch(typeof(Marshmallow), nameof(Marshmallow.UpdateRoast))]
            static bool Marshmallow_UpdateRoast_Prefix(Campfire campfire) {
                try { if(campfire.transform != null) return true; } catch { }
                return false;
            }
            [HarmonyPrefix] // If cook is forced, skip ref to campfire
            [HarmonyPatch(typeof(Campfire), nameof(Campfire.StopRoasting))]
            static bool Campfire_StopRoasting_Prefix(Campfire __instance) {
                try { if(__instance.transform != null) return true; } catch { }
                GlobalMessenger.FireEvent("ExitRoastingMode");
                return false;
            }
            [HarmonyPrefix] // Change marshmallow toasting based on cook skill
            [HarmonyPatch(typeof(Marshmallow), nameof(Marshmallow.Eat))]
            static void Marshmallow_Eat_Prefix(ref float ____toastedFraction) {
                Instance.ModHelper.Console.WriteLine("Before pref:"+____toastedFraction+" skill:" + Instance.skillLevel[(int)Skills.Cook]); //TEST
                float noobModifier = 1 - Instance.skillLevel[(int)Skills.Cook] / 150f;
                ____toastedFraction = (____toastedFraction - 0.7f) * (1 + noobModifier) + 0.7f + Mathf.Max(noobModifier, 0) * (____toastedFraction < 0.7f ? -0.5f : 0.3f);
                Instance.ModHelper.Console.WriteLine("After pref:" + ____toastedFraction); //TEST
            }
            [HarmonyPostfix] // Apply damage for very badly cooked marshmallow
            [HarmonyPatch(typeof(PlayerResources), nameof(PlayerResources.OnEatMarshmallow))]
            static void PlayerResources_OnEatMarshmallow_Postfix(PlayerResources __instance, float toastedFraction) {
                Instance.ModHelper.Console.WriteLine("Before post:" + toastedFraction); //TEST
                float damage;
                if(toastedFraction < 0f) damage = -toastedFraction / 1.2f;
                else if(toastedFraction > 1f) damage = (toastedFraction - 1f) / 0.6f;
                else return;
                damage *= 20f - Instance.skillLevel[(int)Skills.Constitution] / 12.5f;
                Instance.ModHelper.Console.WriteLine("Damage:" + damage); //TEST
                __instance.ApplyInstantDamage(damage, InstantDamageType.Puncture);
            }

            //Repair: //TODO
            [HarmonyPrefix] // Change repair speed and eventually messes up depending on repair skill, and learn
            [HarmonyPatch(typeof(RepairReceiver), nameof(RepairReceiver.RepairTick))]
            static void RepairReceiver_RepairTick_Prefix(RepairReceiver __instance, ref ShipComponent ____targetComponent, ref ShipHull ____targetHull, ref SatelliteNode ____targetSatNode) {
                if(Time.time > Instance.repairSkillCooldown) {
                    switch(__instance._type) {
                    case RepairReceiver.Type.ShipComponent:
                        ____targetComponent._repairTime = 3f;//add skill dependent modifier (can be negative for un-repair)
                        break;
                    case RepairReceiver.Type.ShipHull:
                        ____targetHull._repairTime = 5f;
                        break;
                    case RepairReceiver.Type.SatelliteNode:
                        ____targetSatNode._repairTime = 3f;
                        break;
                    }
                    Instance.repairSkillCooldown = Time.time + 2f;
                }
            }

            //Stealth:
            [HarmonyPostfix] // Change noise radius based on stealth skill (impacts anglerfish investigation distance)
            [HarmonyPatch(typeof(NoiseMaker), nameof(NoiseMaker.GetNoiseRadius))]
            static void NoiseMaker_GetNoiseRadius_Postfix(ref float __result, ref OWRigidbody ____attachedBody) {
                if(____attachedBody.CompareTag("Player") || ____attachedBody.CompareTag("Ship")) __result /= (0.3f + Instance.skillLevel[(int)Skills.Stealth] / 60f);
            }
            [HarmonyPrefix] // Change player pursue distance based on stealth skill (impacts anglerfish chase distance)
            [HarmonyPatch(typeof(AnglerfishController), nameof(AnglerfishController.OnClosestAudibleNoise))]
            static bool AnglerfishController_OnClosestAudibleNoise_Prefix(AnglerfishController __instance, NoiseMaker noiseMaker, ref OWRigidbody ____targetBody, ref Vector3 ____localDisturbancePos, ref float ____escapeDistance) {
                if(__instance._currentState == AnglerfishController.AnglerState.Consuming || (!noiseMaker.GetAttachedBody().CompareTag("Player") && !noiseMaker.GetAttachedBody().CompareTag("Ship")))
                    return true;
                float pursueDistance = 0.3f + Instance.skillLevel[(int)Skills.Stealth] / 60f;
                ____escapeDistance = 500f / pursueDistance;
                pursueDistance = __instance._pursueDistance / pursueDistance;
                if((noiseMaker.GetNoiseOrigin() - __instance.transform.position).sqrMagnitude < pursueDistance * pursueDistance) {
                    if(____targetBody != noiseMaker.GetAttachedBody()) {
                        ____targetBody = noiseMaker.GetAttachedBody();
                        if(__instance._currentState != AnglerfishController.AnglerState.Chasing)
                            __instance.ChangeState(AnglerfishController.AnglerState.Chasing);
                    }
                } else if(__instance._currentState == AnglerfishController.AnglerState.Lurking || __instance._currentState == AnglerfishController.AnglerState.Investigating) {
                    ____localDisturbancePos = __instance._brambleBody.transform.InverseTransformPoint(noiseMaker.GetNoiseOrigin());
                    if(__instance._currentState != AnglerfishController.AnglerState.Investigating)
                        __instance.ChangeState(AnglerfishController.AnglerState.Investigating);
                }
                return false;
            }
            [HarmonyPrefix] // Increase stealth skill when dodging anglerfish
            [HarmonyPatch(typeof(AnglerfishController), nameof(AnglerfishController.ChangeState))]
            static void AnglerfishController_ChangeState_Prefix(AnglerfishController __instance, AnglerfishController.AnglerState newState) {
                if(__instance._currentState != newState && newState != AnglerfishController.AnglerState.Consuming && Time.time > Instance.stealthSkillCooldown) {
                    Instance.Learn(Skills.Stealth, false);
                    Instance.stealthSkillCooldown = Time.time + 2f;
                }
            }
            [HarmonyPostfix] // Change player visibility risk based on stealth skill (impacts inhabitant's ability to see player), if seen learn stealth
            [HarmonyWrapSafe]//<- Should hopefully prevent compatibility problems when DLC is not installed
            [HarmonyPatch(typeof(GhostSensors), nameof(GhostSensors.FixedUpdate_Sensors))]
            static void GhostSensors_FixedUpdate_Sensors_Postfix(ref GhostData ____data) {
                if(____data.sensor.isPlayerVisible || ____data.sensor.isPlayerHeldLanternVisible) {
                    if(Time.time > Instance.stealthSkillCooldown) {
                        if(Instance.Learn(Skills.Stealth)) {
                            Instance.ghostEscape = false;
                            Instance.stealthSkillCooldown = Time.time + 0.75f + 2.25f / (Instance.increment + 1); // 3 1.875 1.5 1.2 1
                        } else {
                            Instance.ghostEscape = true;
                            Instance.stealthSkillCooldown = Time.time + 1f + Instance.increment / 4f; // 1 1.25 1.5 2 3
                        }
                    }
                    ____data.sensor.isPlayerVisible &= !Instance.ghostEscape;
                    ____data.sensor.isPlayerHeldLanternVisible &= !Instance.ghostEscape;
                }
            }
        }
    }
}