﻿using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;

namespace ArchipelagoMuseDash.Archipelago.Items;

public class ExternalItem : IMuseDashItem {

    private readonly string _receivingPlayerName;
    private readonly string _songUid;

    public ExternalItem(long itemId, string itemName, string receivingPlayer) {
        SongText = itemName.Replace('_', ' ');
        _receivingPlayerName = receivingPlayer;

        if (ArchipelagoStatic.AlbumDatabase.TryGetSongFromItemId(itemId, out var info))
            _songUid = info.uid;
        else if (ArchipelagoStatic.AlbumDatabase.TryGetMusicInfo(itemName, out info))
            _songUid = info.uid;
    }
    public ItemInfo Item { get; set; }

    public string UnlockSongUid => _songUid ?? ArchipelagoStatic.AlbumDatabase.GetMusicInfo("Magical Wonderland").uid;
    public bool UseArchipelagoLogo => _songUid == null;

    public string TitleText => "Sending a New Item!!";
    public string SongText { get; }
    public string AuthorText => $"Sent to {_receivingPlayerName}.";

    public string PreUnlockBannerText => "A new item?";
    public string PostUnlockBannerText => GetPostUnlockBannerText();

    public void UnlockItem(ItemHandler handler, bool immediate) { }

    private string GetPostUnlockBannerText() {
        if (_songUid != null)
            return "It ain't yours.";

        if ((Item.Flags & ItemFlags.Advancement) != 0)
            return "Looks good!";

        if ((Item.Flags & ItemFlags.NeverExclude) != 0)
            return "Hope it's useful!";

        if ((Item.Flags & ItemFlags.Trap) != 0)
            return "...";

        return "Looks useless.";
    }
}