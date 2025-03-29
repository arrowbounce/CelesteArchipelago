using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.MessageLog.Messages;
using Archipelago.MultiClient.Net.Packets;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;

using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Monocle;

namespace Celeste.Mod.CelesteArchipelago
{
    public class ArchipelagoController : DrawableGameComponent
    {
        public static ArchipelagoController Instance { get; private set; }
        public IProgressionSystem ProgressionSystem { get; set; }
        public ArchipelagoSession Session
        {
            get
            {
                return Connection.Session;
            }
        }
        public ArchipelagoSlotData SlotData
        {
            get
            {
                return Connection.SlotData;
            }
        }
        public PlayState PlayState
        {
            get
            {
                return new PlayState(Session.DataStorage[Scope.Slot, "CelestePlayState"]);
            }
            set
            {
                Session.DataStorage[Scope.Slot, "CelestePlayState"] = value.ToString();
            }
        }

        private CheckpointState _checkpointState;
        public CheckpointState CheckpointState
        {
            get
            {
                if (_checkpointState == null)
                {
                    _checkpointState = new CheckpointState(unchecked((ulong)(Session.DataStorage[Scope.Slot, "CelesteCheckpointState"].To<long>() - long.MinValue)), Session.DataStorage);
                }
                return _checkpointState;
            }
        }
        public bool BlockMessages { get; set; } = false;
        public bool IsConnected
        {
            get
            {
                return Connection != null && Connection.IsConnected;
            }
        }

        public DeathLinkService DeathLinkService { get; private set; }
        public List<string> DeathLinkPool { get; private set; } = new();
        public bool IsLocalDeath = true;
        public long DeathAmnestyCount { get; private set; } = 0;
        private ChatHandler ChatHandler { get; set; }
        private Connection Connection { get; set; }
        public VictoryConditionOptions VictoryCondition
        {
            get { return (VictoryConditionOptions)SlotData.VictoryCondition; }
        }
        private List<IPatchable> patchObjects = new List<IPatchable>
        {
            new PatchedCassette(),
            new PatchedHeartGem(),
            new PatchedHeartGemDoor(),
            new PatchedLevel(),
            new PatchedLevelLoader(),
            new PatchedLevelSetStats(),
            new PatchedOuiChapterPanel(),
            new PatchedOuiChapterSelect(),
            new PatchedOuiMainMenu(),
            new PatchedOuiJournal(),
            new PatchedSaveData(),
            new PatchedPlayer(),
            new PatchedStrawberry(),
            new PatchedBerryCounter(),
        };

        public ArchipelagoController(Game game) : base(game)
        {
            UpdateOrder = 9000;
            DrawOrder = 9000;
            Enabled = false;
            Instance = this;
            game.Components.Add(this);
            ChatHandler = new ChatHandler(Game);
            game.Components.Add(ChatHandler);
            ProgressionSystem = new NullProgression();
        }

        public void Init()
        {
            Enabled = true;
            ChatHandler.Init();
        }

        public void DeInit()
        {
            ChatHandler.DeInit();
        }

        public override void Update(GameTime gameTime)
        {

        }

        public void LoadPatches()
        {
            foreach (var patch in patchObjects)
            {
                patch.Load();
            }
        }

        public void UnloadPatches()
        {
            foreach (var patch in patchObjects)
            {
                patch.Unload();
            }
        }

        public void StartSession(Action<LoginResult> onLogin)
        {
            var parameters = new ConnectionParameters(
                game: "Celeste",
                server: CelesteArchipelagoModule.Settings.Server,
                port: CelesteArchipelagoModule.Settings.Port,
                name: CelesteArchipelagoModule.Settings.Name,
                flags: ItemsHandlingFlags.AllItems,
                version: new Version(0, 5, 1), // Needs hotfix aswell
                tags: null,
                uuid: null,
                password: CelesteArchipelagoModule.Settings.Password,
                slotData: true
            );

            Connection = new Connection(Celeste.Instance, parameters, (loginResult) =>
            {
                if (loginResult.Successful)
                {
                    Session.Items.ItemReceived += ReceiveItemCallback;
                    if (SlotData.ProgressionSystem == (int)ProgressionSystemOptions.DEFAULT_PROGRESSION)
                    {
                        ProgressionSystem = new DefaultProgression(SlotData);
                    }
                    Session.DataStorage[Scope.Slot, "CelestePlayState"].Initialize("1;0;0;dotutorial");
                    Session.DataStorage[Scope.Slot, "CelesteCheckpointState"].Initialize(long.MinValue);
                    Session.DataStorage[Scope.Slot, "CelesteDeathAmnestyState"].Initialize(0);

                    CelesteArchipelagoModule.Settings.DeathLink = SlotData.DeathLink == 1;
                    DeathLinkService = Session.CreateDeathLinkService();
                    DeathLinkService.OnDeathLinkReceived += ReceiveDeathLinkCallback;

                    DeathAmnestyCount = Session.DataStorage["CelesteDeathAmnestyState"];

                    if (CelesteArchipelagoModule.Settings.DeathLink)
                    {
                        DeathLinkService.EnableDeathLink();
                    }
                    else
                    {
                        DeathLinkService.DisableDeathLink();
                    }

                    CleanPreviousSaveData();

                    Connection.Disposed += (sender, args) =>
                    {
                        Session.MessageLog.OnMessageReceived -= HandleMessage;
                        Session.Items.ItemReceived -= ReceiveItemCallback;
                        ProgressionSystem = new NullProgression();
                        DeathLinkService = null;
                    };
                }
                else
                {
                    Connection?.Dispose();
                }
                onLogin(loginResult);
            });
        }

        public void DisconnectSession()
        {
            Connection?.Dispose();
        }

        private void CleanPreviousSaveData()
        {
            CheckpointState.CleanSaveDataCheckpoints();
        }

        public void ReceiveItemCallback(IReceivedItemsHelper receivedItemsHelper)
        {
            while (receivedItemsHelper.Any())
            {
                // Receive latest uncollected item
                Logger.Log("CelesteArchipelago", $"Received item {receivedItemsHelper.PeekItem().ItemName} with ID {receivedItemsHelper.PeekItem().ItemId}");
                if (receivedItemsHelper.PeekItem().ItemName == "Victory (Celeste)")
                {
                    receivedItemsHelper.DequeueItem();
                    continue;
                }
                var itemID = receivedItemsHelper.PeekItem().ItemId;
                ArchipelagoNetworkItem item = new ArchipelagoNetworkItem(itemID);

                // Collect received item via chosen progression system
                ProgressionSystem.OnCollectedServer(item.areaKey, item.type, item.strawberry);
                receivedItemsHelper.DequeueItem();
            }
        }

        public void SendLocationCallback(ArchipelagoNetworkItem location)
        {
            Logger.Log("CelesteArchipelago", $"Checking location {Session.Locations.GetLocationNameFromId(location.ID) ?? location.ID.ToString()}");
            Session.Locations.CompleteLocationChecks(location.ID);

            var goalLevel = ProgressionSystem.GetGoalLevel();
            bool isVictory = location.type == CollectableType.COMPLETION
                && location.area == goalLevel.ID
                && location.mode == (int)goalLevel.Mode;

            if (isVictory)
            {
                Logger.Log("CelesteArchipelago", "Sending Victory Condition.");
                var statusUpdatePacket = new StatusUpdatePacket();
                statusUpdatePacket.Status = ArchipelagoClientState.ClientGoal;
                Session.Socket.SendPacket(statusUpdatePacket);
            }
        }

        public void ReplayClientCollected()
        {
            ArchipelagoNetworkItem item;
            foreach (var loc in Session.Locations.AllLocationsChecked)
            {
                Logger.Log("CelesteArchipelago", $"Replaying location {Session.Locations.GetLocationNameFromId(loc) ?? loc.ToString()}");
                item = new ArchipelagoNetworkItem(loc);
                ProgressionSystem.OnCollectedClient(item.areaKey, item.type, item.strawberry, true);
            }
        }

        public void ReceiveDeathLinkCallback(DeathLink deathLink)
        {
            string completeMessage;
            completeMessage = $"{Dialog.Clean("archipelago_messages_deathlink_recieved")} {deathLink.Source}";
            completeMessage += string.IsNullOrEmpty(deathLink.Cause) ? "" : $": {deathLink.Cause}";

            DeathLinkPool.Add(completeMessage);
            IsLocalDeath = false;
        }

        public void FlushDeathLinkMessage()
        {
            ChatHandler.HandleMessage(DeathLinkPool[0], Color.PaleVioletRed);
            DeathLinkPool.RemoveAt(0);
        }

        private string TryGetMessage(string baseMessage, int message_count = 1)
        {
            // Message count number is inclusive
            message_count = Math.Max(1, message_count);

            Random randomNum = new Random();

            int attempts = 3;
            while (attempts > 0)
            {
                string label = baseMessage + randomNum.Range(1, message_count).ToString();
                if (Dialog.Has(label))
                {
                    return Dialog.Clean(label);
                }
                attempts--;
            }

            return baseMessage + "1"; // Fallback to first message
        }

        private string ChooseDeathMessage()
        {
            Random randomNum = new Random();
            if (randomNum.Range(0, 6) == 0)
            {
                return TryGetMessage("archipelago_messages_deathlink_random_", 5);
            }

            switch (PlayState.AreaKey.GetSID()) // Chapter
            {
                case "Celeste/0-Intro": // Prologue
                    return TryGetMessage("archipelago_messages_deathlink_intro_", 3);
                case "Celeste/1-ForsakenCity":
                    return TryGetMessage("archipelago_messages_deathlink_forsaken_city_", 2);
                case "Celeste/2-OldSite":
                    return TryGetMessage("archipelago_messages_deathlink_old_site_", 2);
                case "Celeste/3-CelestialResort":
                    return TryGetMessage("archipelago_messages_deathlink_celestial_resort_", 4);
                case "Celeste/4-GoldenRidge":
                    return TryGetMessage("archipelago_messages_deathlink_golden_ridge_", 4);
                case "Celeste/5-MirrorTemple":
                    return TryGetMessage("archipelago_messages_deathlink_mirror_temple_", 2);
                case "Celeste/6-Reflection":
                    return TryGetMessage("archipelago_messages_deathlink_reflection_", 4);
                case "Celeste/7-Summit":
                    return TryGetMessage("archipelago_messages_deathlink_summit_", 3);
                case "Celeste/8-Epilogue":
                    return TryGetMessage("archipelago_messages_deathlink_epilogue_", 2);
                case "Celeste/9-Core":
                    return TryGetMessage("archipelago_messages_deathlink_core_", 3);
                case "Celeste/LostLevels": // Farewell
                    return TryGetMessage("archipelago_messages_deathlink_farewell_", 1);
                default:
                    Logger.Log(LogLevel.Debug, "CelesteArchipelago", $"Could not find cause {PlayState.AreaKey.GetSID()}");
                    return Dialog.Clean("archipelago_messages_deathlink_default");
            }
        }

        private string SetDeathCause(string player)
        {
            // Feel free to add messages (within Dialog)
            // Note: To refer to the player, use "player" in the message
            // People who's messages have been added: .realityy, the_magic_left_to_rot

            string message = ChooseDeathMessage();
            Logger.Log(LogLevel.Debug, "CelesteArchipelago", $"Death cause: {message.Replace("player", player)}");
            return message.Replace("player", player);   
        }

        public void SendDeathLinkCallback()
        {
            if (!CelesteArchipelagoModule.Settings.DeathLink || !IsLocalDeath)
            {
                return;
            }

            if (DeathAmnestyCount >= SlotData.DeathAmnestyMax - 1)
            {
                ChatHandler.HandleMessage(Dialog.Clean("archipelago_messages_deathlink_sent"), Color.PaleVioletRed);
                string sourcePlayer = Session.Players.GetPlayerAlias(Session.ConnectionInfo.Slot);
                DeathLink deathLink = new DeathLink(sourcePlayer, SetDeathCause(sourcePlayer));
                DeathLinkService.SendDeathLink(deathLink);

                DeathAmnestyCount = 0;
            }
            else
            {
                DeathAmnestyCount++;
            }

            Session.DataStorage["CelesteDeathAmnestyState"] = DeathAmnestyCount;
        }

        public void HandleMessage(LogMessage message)
        {
            if (!BlockMessages) ChatHandler.HandleMessage(message);
        }

        public void HandleMessage(string text, Color color)
        {
            if (!BlockMessages) ChatHandler.HandleMessage(text, color);
        }

        protected override void Dispose(bool disposing)
        {
            Connection?.Dispose();
            ChatHandler?.Dispose();
            base.Dispose(disposing);
        }
    }
}