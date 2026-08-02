using UnityEngine;
using System.Collections.Generic;

namespace NyghtshadeHollow.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Subsystems")]
        public PrisonStateManager PrisonState;
        public FactionManager Factions;
        public DialogueManager Dialogue;
        public QuestManager Quests;
        public InventoryManager Inventory;
        public JournalManager Journal;
        public SaveManager Save;

        [Header("Player")]
        public GameObject PlayerPrefab;
        private GameObject _player;

        public static event System.Action OnGameReady;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            BootSubsystems();
        }

        private void BootSubsystems()
        {
            PrisonState = gameObject.AddComponent<PrisonStateManager>();
            Factions    = gameObject.AddComponent<FactionManager>();
            Dialogue    = gameObject.AddComponent<DialogueManager>();
            Quests      = gameObject.AddComponent<QuestManager>();
            Inventory   = gameObject.AddComponent<InventoryManager>();
            Journal     = gameObject.AddComponent<JournalManager>();
            Save        = gameObject.AddComponent<SaveManager>();
        }

        private void Start()
        {
            Save.Load();
            OnGameReady?.Invoke();
        }

        public GameObject GetPlayer() => _player;

        public void SpawnPlayer(Vector3 position)
        {
            if (_player != null) Destroy(_player);
            _player = Instantiate(PlayerPrefab, position, Quaternion.identity);
        }

        private void OnApplicationQuit()
        {
            Save.Save();
        }
    }
}
