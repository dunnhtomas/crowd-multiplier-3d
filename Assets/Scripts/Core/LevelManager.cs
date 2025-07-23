using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace CrowdMultiplier.Core
{
    /// <summary>
    /// Comprehensive level management system with procedural generation and analytics
    /// Handles level progression, difficulty scaling, and performance optimization
    /// </summary>
    public class LevelManager : MonoBehaviour
    {
        [Header("Level Configuration")]
        [SerializeField] private int currentLevel = 1;
        [SerializeField] private int maxLevels = 100;
        [SerializeField] private float levelLength = 50f;
        [SerializeField] private bool enableProceduralGeneration = true;
        
        [Header("Difficulty Scaling")]
        [SerializeField] private AnimationCurve difficultyProgressionCurve;
        [SerializeField] private float baseDifficulty = 1f;
        [SerializeField] private float maxDifficulty = 5f;
        [SerializeField] private int difficultyIncreaseInterval = 5;
        
        [Header("Gate Configuration")]
        [SerializeField] private GameObject[] gatePrefabs;
        [SerializeField] private int minGatesPerLevel = 5;
        [SerializeField] private int maxGatesPerLevel = 15;
        [SerializeField] private float minGateSpacing = 8f;
        [SerializeField] private float maxGateSpacing = 12f;
        
        [Header("Obstacle Configuration")]
        [SerializeField] private GameObject[] obstaclePrefabs;
        [SerializeField] private int maxObstaclesPerLevel = 8;
        [SerializeField] private float obstacleSpacing = 15f;
        
        [Header("Level Assets")]
        [SerializeField] private Transform playerStartPoint;
        [SerializeField] private Transform levelEndPoint;
        [SerializeField] private Transform levelContainer;
        
        [Header("Performance Optimization")]
        [SerializeField] private bool enableObjectPooling = true;
        [SerializeField] private bool enableLODSystem = true;
        [SerializeField] private float cullingDistance = 100f;
        
        // Level state
        private List<GameObject> currentLevelObjects = new List<GameObject>();
        private List<Gameplay.Gate> currentGates = new List<Gameplay.Gate>();
        private Dictionary<string, Queue<GameObject>> objectPools = new Dictionary<string, Queue<GameObject>>();
        
        // Level progression
        private float levelStartTime;
        private int gatesTriggered = 0;
        private int obstaclesHit = 0;
        private bool levelCompleted = false;
        
        // Events
        public event System.Action<int> OnLevelStarted;
        public event System.Action<int, LevelResults> OnLevelCompleted;
        public event System.Action<int> OnLevelFailed;
        public event System.Action<float> OnLevelProgressUpdated;
        
        // Properties
        public int CurrentLevel => currentLevel;
        public float CurrentDifficulty => CalculateDifficulty(currentLevel);
        public bool IsLevelActive => !levelCompleted;
        public Vector3 PlayerStartPosition => playerStartPoint != null ? playerStartPoint.position : Vector3.zero;
        
        private void Start()
        {
            InitializeLevel();
            StartLevel();
        }
        
        private void Update()
        {
            if (IsLevelActive)
            {
                UpdateLevelProgress();
                CheckLevelCompletion();
                OptimizePerformance();
            }
        }
        
        private void InitializeLevel()
        {
            // Initialize object pools
            if (enableObjectPooling)
            {
                InitializeObjectPools();
            }
            
            // Setup level container
            if (levelContainer == null)
            {
                GameObject container = new GameObject("Level_" + currentLevel);
                levelContainer = container.transform;
            }
            
            // Initialize difficulty curve if not set
            if (difficultyProgressionCurve.keys.Length == 0)
            {
                difficultyProgressionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
            }
        }
        
        private void InitializeObjectPools()
        {
            // Pool gate prefabs
            foreach (var gatePrefab in gatePrefabs)
            {
                if (gatePrefab != null)
                {
                    string poolKey = gatePrefab.name;
                    objectPools[poolKey] = new Queue<GameObject>();
                    
                    // Pre-populate pool
                    for (int i = 0; i < 10; i++)
                    {
                        GameObject pooledObject = Instantiate(gatePrefab);
                        pooledObject.SetActive(false);
                        objectPools[poolKey].Enqueue(pooledObject);
                    }
                }
            }
            
            // Pool obstacle prefabs
            foreach (var obstaclePrefab in obstaclePrefabs)
            {
                if (obstaclePrefab != null)
                {
                    string poolKey = obstaclePrefab.name;
                    objectPools[poolKey] = new Queue<GameObject>();
                    
                    // Pre-populate pool
                    for (int i = 0; i < 5; i++)
                    {
                        GameObject pooledObject = Instantiate(obstaclePrefab);
                        pooledObject.SetActive(false);
                        objectPools[poolKey].Enqueue(pooledObject);
                    }
                }
            }
        }
        
        private void StartLevel()
        {
            levelStartTime = Time.time;
            levelCompleted = false;
            gatesTriggered = 0;
            obstaclesHit = 0;
            
            // Clear previous level
            ClearCurrentLevel();
            
            // Generate new level
            if (enableProceduralGeneration)
            {
                GenerateProceduralLevel();
            }
            else
            {
                LoadPredefinedLevel();
            }
            
            // Notify systems
            OnLevelStarted?.Invoke(currentLevel);
            
            // Track analytics
            TrackLevelStart();
        }
        
        private void GenerateProceduralLevel()
        {
            float currentZ = playerStartPoint.position.z + 10f; // Start ahead of player
            float difficulty = CurrentDifficulty;
            
            // Calculate number of gates based on difficulty
            int gateCount = Mathf.RoundToInt(Mathf.Lerp(minGatesPerLevel, maxGatesPerLevel, difficulty / maxDifficulty));
            
            // Generate gates
            for (int i = 0; i < gateCount; i++)
            {
                // Determine gate type based on difficulty and position
                GateType gateType = DetermineGateType(i, gateCount, difficulty);
                GameObject gatePrefab = GetGatePrefabForType(gateType);
                
                if (gatePrefab != null)
                {
                    Vector3 gatePosition = new Vector3(
                        Random.Range(-3f, 3f), // Random X position
                        0f,
                        currentZ
                    );
                    
                    GameObject gate = SpawnObject(gatePrefab, gatePosition);
                    ConfigureGate(gate, gateType, difficulty);
                    
                    currentLevelObjects.Add(gate);
                    
                    var gateComponent = gate.GetComponent<Gameplay.Gate>();
                    if (gateComponent != null)
                    {
                        currentGates.Add(gateComponent);
                        gateComponent.OnGateTriggered += OnGateTriggered;
                    }
                }
                
                currentZ += Random.Range(minGateSpacing, maxGateSpacing);
            }
            
            // Generate obstacles
            GenerateObstacles(difficulty);
            
            // Set level end position
            if (levelEndPoint != null)
            {
                levelEndPoint.position = new Vector3(0f, 0f, currentZ + 10f);
            }
        }
        
        private void GenerateObstacles(float difficulty)
        {
            if (obstaclePrefabs.Length == 0) return;
            
            int obstacleCount = Mathf.RoundToInt(difficulty * 2f);
            obstacleCount = Mathf.Clamp(obstacleCount, 0, maxObstaclesPerLevel);
            
            for (int i = 0; i < obstacleCount; i++)
            {
                GameObject obstaclePrefab = obstaclePrefabs[Random.Range(0, obstaclePrefabs.Length)];
                
                Vector3 obstaclePosition = new Vector3(
                    Random.Range(-4f, 4f),
                    0f,
                    Random.Range(20f, levelLength - 10f)
                );
                
                GameObject obstacle = SpawnObject(obstaclePrefab, obstaclePosition);
                currentLevelObjects.Add(obstacle);
            }
        }
        
        private GateType DetermineGateType(int gateIndex, int totalGates, float difficulty)
        {
            // Early gates are more likely to be multipliers
            if (gateIndex < totalGates * 0.3f)
            {
                return Random.value < 0.8f ? GateType.Multiplier : GateType.Bonus;
            }
            // Middle gates have mixed types
            else if (gateIndex < totalGates * 0.7f)
            {
                float rand = Random.value;
                if (rand < 0.4f) return GateType.Multiplier;
                else if (rand < 0.7f) return GateType.Enemy;
                else return GateType.Bonus;
            }
            // Later gates are more challenging
            else
            {
                return Random.value < 0.6f ? GateType.Enemy : GateType.Multiplier;
            }
        }
        
        private GameObject GetGatePrefabForType(GateType gateType)
        {
            // For now, return a random gate prefab
            // In a full implementation, you'd have specific prefabs for each type
            if (gatePrefabs.Length > 0)
            {
                return gatePrefabs[Random.Range(0, gatePrefabs.Length)];
            }
            return null;
        }
        
        private void ConfigureGate(GameObject gate, GateType gateType, float difficulty)
        {
            var gateComponent = gate.GetComponent<Gameplay.Gate>();
            if (gateComponent != null)
            {
                gateComponent.SetGateType((Gameplay.GateType)gateType);
                
                switch (gateType)
                {
                    case GateType.Multiplier:
                        float multiplier = Random.Range(1.5f, 3f + difficulty * 0.5f);
                        gateComponent.SetMultiplierValue(multiplier);
                        break;
                        
                    case GateType.Enemy:
                        int enemySize = Mathf.RoundToInt(20f + difficulty * 15f);
                        gateComponent.SetEnemyCrowdSize(enemySize);
                        break;
                        
                    case GateType.Bonus:
                        gateComponent.SetMultiplierValue(Random.Range(1.2f, 2f));
                        break;
                }
            }
        }
        
        private GameObject SpawnObject(GameObject prefab, Vector3 position)
        {
            GameObject obj;
            
            if (enableObjectPooling && objectPools.ContainsKey(prefab.name))
            {
                obj = GetPooledObject(prefab.name);
                if (obj != null)
                {
                    obj.transform.position = position;
                    obj.SetActive(true);
                    return obj;
                }
            }
            
            // Fallback to instantiation
            obj = Instantiate(prefab, position, Quaternion.identity, levelContainer);
            return obj;
        }
        
        private GameObject GetPooledObject(string poolKey)
        {
            if (objectPools.ContainsKey(poolKey) && objectPools[poolKey].Count > 0)
            {
                return objectPools[poolKey].Dequeue();
            }
            return null;
        }
        
        private void LoadPredefinedLevel()
        {
            // Load level from ScriptableObject or scene data
            // This would be implemented based on your level design workflow
            Debug.Log($"Loading predefined level {currentLevel}");
        }
        
        private void UpdateLevelProgress()
        {
            if (playerStartPoint == null || levelEndPoint == null) return;
            
            // Calculate progress based on player position
            var player = FindObjectOfType<PlayerController>();
            if (player != null)
            {
                float totalDistance = Vector3.Distance(playerStartPoint.position, levelEndPoint.position);
                float currentDistance = Vector3.Distance(playerStartPoint.position, player.Position);
                float progress = Mathf.Clamp01(currentDistance / totalDistance);
                
                OnLevelProgressUpdated?.Invoke(progress);
            }
        }
        
        private void CheckLevelCompletion()
        {
            var player = FindObjectOfType<PlayerController>();
            if (player != null && levelEndPoint != null)
            {
                float distanceToEnd = Vector3.Distance(player.Position, levelEndPoint.position);
                
                if (distanceToEnd < 5f && !levelCompleted)
                {
                    CompleteLevel();
                }
            }
        }
        
        private void CompleteLevel()
        {
            levelCompleted = true;
            float levelTime = Time.time - levelStartTime;
            
            var results = new LevelResults
            {
                LevelNumber = currentLevel,
                CompletionTime = levelTime,
                GatesTriggered = gatesTriggered,
                ObstaclesHit = obstaclesHit,
                FinalCrowdSize = FindObjectOfType<CrowdController>()?.GetCrowdSize() ?? 0,
                Score = CalculateScore(levelTime, gatesTriggered, obstaclesHit)
            };
            
            OnLevelCompleted?.Invoke(currentLevel, results);
            TrackLevelCompletion(results);
            
            // Auto-advance to next level after delay
            Invoke(nameof(LoadNextLevel), 2f);
        }
        
        private int CalculateScore(float time, int gates, int obstacles)
        {
            int baseScore = 1000;
            int timeBonus = Mathf.Max(0, 500 - Mathf.RoundToInt(time * 10));
            int gateBonus = gates * 100;
            int obstaclePenalty = obstacles * 50;
            
            return baseScore + timeBonus + gateBonus - obstaclePenalty;
        }
        
        private void LoadNextLevel()
        {
            currentLevel++;
            if (currentLevel <= maxLevels)
            {
                StartLevel();
            }
            else
            {
                // Game completed
                Debug.Log("All levels completed!");
            }
        }
        
        public void RestartLevel()
        {
            StartLevel();
        }
        
        public void LoadLevel(int levelNumber)
        {
            currentLevel = Mathf.Clamp(levelNumber, 1, maxLevels);
            StartLevel();
        }
        
        private void ClearCurrentLevel()
        {
            // Return objects to pools or destroy them
            foreach (var obj in currentLevelObjects)
            {
                if (obj != null)
                {
                    if (enableObjectPooling)
                    {
                        ReturnToPool(obj);
                    }
                    else
                    {
                        Destroy(obj);
                    }
                }
            }
            
            currentLevelObjects.Clear();
            currentGates.Clear();
        }
        
        private void ReturnToPool(GameObject obj)
        {
            if (objectPools.ContainsKey(obj.name.Replace("(Clone)", "")))
            {
                obj.SetActive(false);
                objectPools[obj.name.Replace("(Clone)", "")].Enqueue(obj);
            }
            else
            {
                Destroy(obj);
            }
        }
        
        private float CalculateDifficulty(int level)
        {
            float normalizedLevel = (float)(level - 1) / (maxLevels - 1);
            return baseDifficulty + (maxDifficulty - baseDifficulty) * difficultyProgressionCurve.Evaluate(normalizedLevel);
        }
        
        private void OptimizePerformance()
        {
            if (!enableLODSystem) return;
            
            var player = FindObjectOfType<PlayerController>();
            if (player == null) return;
            
            // Disable objects that are too far from player
            foreach (var obj in currentLevelObjects)
            {
                if (obj != null)
                {
                    float distance = Vector3.Distance(obj.transform.position, player.Position);
                    obj.SetActive(distance < cullingDistance);
                }
            }
        }
        
        private void OnGateTriggered(Gameplay.Gate gate, int resultingCrowdSize)
        {
            gatesTriggered++;
            
            // Track gate interaction analytics
            if (GameManager.Instance != null)
            {
                var analyticsManager = GameManager.Instance.GetComponent<AnalyticsManager>();
                analyticsManager?.TrackEvent("gate_triggered", new Dictionary<string, object>
                {
                    { "level", currentLevel },
                    { "gate_type", gate.GetGateType().ToString() },
                    { "crowd_size_result", resultingCrowdSize },
                    { "gates_triggered_total", gatesTriggered }
                });
            }
        }
        
        private void TrackLevelStart()
        {
            if (GameManager.Instance != null)
            {
                var analyticsManager = GameManager.Instance.GetComponent<AnalyticsManager>();
                analyticsManager?.TrackLevelEvent("start", currentLevel, new Dictionary<string, object>
                {
                    { "difficulty", CurrentDifficulty },
                    { "gate_count", currentGates.Count },
                    { "generation_type", enableProceduralGeneration ? "procedural" : "predefined" }
                });
            }
        }
        
        private void TrackLevelCompletion(LevelResults results)
        {
            if (GameManager.Instance != null)
            {
                var analyticsManager = GameManager.Instance.GetComponent<AnalyticsManager>();
                analyticsManager?.TrackLevelEvent("complete", currentLevel, new Dictionary<string, object>
                {
                    { "completion_time", results.CompletionTime },
                    { "gates_triggered", results.GatesTriggered },
                    { "obstacles_hit", results.ObstaclesHit },
                    { "final_crowd_size", results.FinalCrowdSize },
                    { "score", results.Score }
                });
            }
        }
        
        public Vector3 GetPlayerStartPosition()
        {
            return playerStartPoint != null ? playerStartPoint.position : Vector3.zero;
        }
        
        public float GetLevelProgress()
        {
            var player = FindObjectOfType<PlayerController>();
            if (player != null && playerStartPoint != null && levelEndPoint != null)
            {
                float totalDistance = Vector3.Distance(playerStartPoint.position, levelEndPoint.position);
                float currentDistance = Vector3.Distance(playerStartPoint.position, player.Position);
                return Mathf.Clamp01(currentDistance / totalDistance);
            }
            return 0f;
        }
    }
    
    [System.Serializable]
    public class LevelResults
    {
        public int LevelNumber;
        public float CompletionTime;
        public int GatesTriggered;
        public int ObstaclesHit;
        public int FinalCrowdSize;
        public int Score;
    }
    
    public enum GateType
    {
        Multiplier,
        Enemy,
        Obstacle,
        Bonus,
        Shield
    }
}
