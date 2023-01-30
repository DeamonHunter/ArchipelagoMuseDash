﻿using System;
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
            ArchipelagoStatic.ArchLogger.Log("DeathLink", $"Player Died. Current Status: {(_deathLinkService != null ? "Active" : "Deactive")}, {_killingPlayer}");
            if (_deathLinkService == null || _killingPlayer)
                return;

            var alias = _session.Players.GetPlayerAlias(_slotID);
            _deathLinkService.SendDeathLink(new DeathLink(alias, $"{alias} ran out of Rhythm."));
            ArchipelagoStatic.ArchLogger.Log("DeathLink", "Sent Link");
        }

        void OnDeathLinkReceived(DeathLink deathLink) {
            ArchipelagoStatic.ArchLogger.Log("DeathLink", $"Received DeathLink: {deathLink.Source}: {deathLink.Cause}");
            _killingPlayer = true;
        }

        public void Update() {
            if (!_killingPlayer)
                return;

            var battleStage = ArchipelagoStatic.BattleComponent;
            if (battleStage == null || battleStage.isDead || battleStage.isSucceed)
                return;

            try {
                BattleRoleAttributeComponent.instance.Hurt(-BattleRoleAttributeComponent.instance.GetHp(), false);
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