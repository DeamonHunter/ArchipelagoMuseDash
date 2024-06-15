using Archipelago.MultiClient.Net.Helpers;

namespace ArchipelagoMuseDash.Archipelago;

public class DataStorageHandler {
    private readonly IDataStorageHelper _dataStorageHelper;
    private readonly string _trapStorageIndex;
    private readonly string _feverStorageIndex;

    private readonly string _greatToPerfectIndex;
    private readonly string _missToGreatIndex;
    private readonly string _extraLifeIndex;

    public DataStorageHandler(int slotNumber, int teamNumber, IDataStorageHelper dataStorageHelper) {
        _dataStorageHelper = dataStorageHelper;
        _trapStorageIndex = $"last_trap_{slotNumber}_{teamNumber}";
        _feverStorageIndex = $"fill_fever_{slotNumber}_{teamNumber}";

        _greatToPerfectIndex = $"great_to_perfect_{slotNumber}_{teamNumber}";
        _missToGreatIndex = $"miss_to_great_{slotNumber}_{teamNumber}";
        _extraLifeIndex = $"extra_life_{slotNumber}_{teamNumber}";
    }

    public int GetHandledTrapCount() {
        return _dataStorageHelper[_trapStorageIndex];
    }

    public void SetHandledTrapCount(int count) {
        _dataStorageHelper[_trapStorageIndex] = count;
    }

    public int GetUsedGreatToPerfect() {
        return _dataStorageHelper[_greatToPerfectIndex];
    }
    public int GetUsedMissToGreat() {
        return _dataStorageHelper[_missToGreatIndex];
    }
    public int GetUsedExtraLifes() {
        return _dataStorageHelper[_extraLifeIndex];
    }

    public void SetUsedGreatToPerfect(int count) {
        _dataStorageHelper[_greatToPerfectIndex] = count;
    }
    public void SetUsedMissToGreat(int count) {
        _dataStorageHelper[_missToGreatIndex] = count;
    }
    public void SetUsedExtraLifes(int count) {
        _dataStorageHelper[_extraLifeIndex] = count;
    }
}