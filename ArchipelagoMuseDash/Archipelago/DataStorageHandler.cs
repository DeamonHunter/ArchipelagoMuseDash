using Archipelago.MultiClient.Net.Helpers;

namespace ArchipelagoMuseDash.Archipelago
{
    public class DataStorageHandler
    {
        private readonly DataStorageHelper _dataStorageHelper;
        private readonly string _trapStorageIndex;
        private readonly string _feverStorageIndex;

        public DataStorageHandler(int slotNumber, int teamNumber, DataStorageHelper dataStorageHelper)
        {
            _dataStorageHelper = dataStorageHelper;
            _trapStorageIndex = $"last_trap_{slotNumber}_{teamNumber}";
            _feverStorageIndex = $"fill_fever_{slotNumber}_{teamNumber}";
        }

        public int GetHandledTrapCount() => _dataStorageHelper[_trapStorageIndex];
        public int GetHandledFeverCount() => _dataStorageHelper[_feverStorageIndex];
        public void SetHandledTrapCount(int count) => _dataStorageHelper[_trapStorageIndex] = count;
        public void SetHandledFeverCount(int count) => _dataStorageHelper[_trapStorageIndex] = count;
    }
}