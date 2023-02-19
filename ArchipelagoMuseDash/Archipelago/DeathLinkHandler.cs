using System;
using System.Collections.Generic;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using Assets.Scripts.GameCore.HostComponent;

namespace ArchipelagoMuseDash.Archipelago {
    /// <summary>
    /// Handles DeathLink for the Archipelago Randomiser
    /// </summary>
    public class DeathLinkHandler {
        ArchipelagoSession _session;
        int _slotID;
        DeathLinkService _deathLinkService;

        bool _killingPlayer;
        string _deathLinkReason;

        Random _random = new Random();

        List<string> _deathReasons = new List<string>() {
            //Generic
            "{0} has ran out of rhythm.",
            "{0} took one too many to the face.",
            "{0} forgot they shouldn't use Little Devil Marija on an easy song.",

            //Stage references
            "{0} ate too much candy.",
            "{0} got shot.",
            "{0} got charmed.",
            "{0} got run over by a limousine.",
            "{0} should've been playing Groove Coaster",
            "{0} should've been playing Touhou",

            //Song references
            "{0} spilled their MilK.",
            "{0} went bankrupt.",
            "{0} lost all their BrainPower.",
            "{0} ran into a grass snake.",
            "{0} dove straight into the ground.",
            "{0} stopped their tape tonight.",
            "{0} ran out of energy for their synergy matrix."
        };

        public DeathLinkHandler(ArchipelagoSession session, int slotID, Dictionary<string, object> slotData) {
            _session = session;
            _slotID = slotID;

            if (!slotData.TryGetValue("deathLink", out object deathLinkEnabled) || ((long)deathLinkEnabled) != 1)
                return;

            _deathLinkService = session.CreateDeathLinkService();
            _deathLinkService.EnableDeathLink();
            _deathLinkService.OnDeathLinkReceived += OnDeathLinkReceived;
        }

        public void PlayerDied() {
            ArchipelagoStatic.ArchLogger.LogDebug("DeathLink", $"Player Died. Current Status: {(_deathLinkService != null ? "Active" : "Deactive")}, {_killingPlayer}");
            if (_deathLinkService == null || _killingPlayer)
                return;

            var alias = _session.Players.GetPlayerAlias(_slotID);

            var reasonIndex = _random.Next(_deathReasons.Count);
            var chosenReason = string.Format(_deathReasons[reasonIndex], alias);

            ArchipelagoStatic.ArchLogger.Log("DeathLink", $"Sending deathlink: {chosenReason}");
            _deathLinkService.SendDeathLink(new DeathLink(alias, chosenReason));
        }

        void OnDeathLinkReceived(DeathLink deathLink) {
            ArchipelagoStatic.ArchLogger.Log("DeathLink", $"Received DeathLink: {deathLink.Source}: {deathLink.Cause}");
            _deathLinkReason = $"Killed By {deathLink.Source}\n\"{deathLink.Cause}\"";
            _killingPlayer = true;
        }

        public string GetDeathLinkReason() {
            var reason = _deathLinkReason;
            _deathLinkReason = null;
            return reason;
        }

        public void Update() {
            if (!_killingPlayer)
                return;

            var battleStage = ArchipelagoStatic.BattleComponent;
            if (battleStage == null || battleStage.isDead || battleStage.isPause || battleStage.isSucceed)
                return;

            try {
                BattleRoleAttributeComponent.instance.Hurt(-9999, false);
            }
            catch (Exception e) {
                ArchipelagoStatic.ArchLogger.Error("DeathLink", e);
            }
            finally {
                _killingPlayer = false;
            }
        }
    }
}
