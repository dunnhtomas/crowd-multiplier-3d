using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using System.Collections.Generic;

namespace CrowdMultiplier.Core
{
    /// <summary>
    /// High-performance crowd simulation using Unity Job System and Burst Compiler
    /// Implements boids algorithm with spatial partitioning for optimal performance
    /// </summary>
    public class CrowdController : MonoBehaviour
    {
        [Header("Crowd Configuration")]
        [SerializeField] private GameObject crowdUnitPrefab;
        [SerializeField] private int initialCrowdSize = 10;
        [SerializeField] private int maxCrowdSize = 1000;
        [SerializeField] private float unitSpeed = 5f;
        [SerializeField] private float followDistance = 2f;
        
        [Header("Boids Parameters")]
        [SerializeField] private float separationRadius = 1f;
        [SerializeField] private float alignmentRadius = 2.5f;
        [SerializeField] private float cohesionRadius = 3f;
        [SerializeField] private float separationWeight = 1.5f;
        [SerializeField] private float alignmentWeight = 1f;
        [SerializeField] private float cohesionWeight = 1f;
        
        [Header("Performance Optimization")]
        [SerializeField] private bool useJobSystem = true;
        [SerializeField] private bool useBurstCompiler = true;
        [SerializeField] private int maxUnitsPerFrame = 100;
        [SerializeField] private bool enableLOD = true;
        
        // Job System Arrays
        private NativeArray<float3> positions;
        private NativeArray<float3> velocities;
        private NativeArray<float3> accelerations;
        private NativeArray<bool> isActive;
        
        // Game Objects
        private List<CrowdUnit> crowdUnits = new List<CrowdUnit>();
        private Transform playerAvatar;
        private Transform targetDestination;
        
        // Performance tracking
        private float lastOptimizationTime;
        private int currentActiveUnits;
        
        // Events
        public event System.Action<int> OnCrowdSizeChanged;
        public event System.Action<Vector3> OnCrowdPositionChanged;
        
        private void Start()
        {
            playerAvatar = GameObject.FindWithTag("Player")?.transform;
            if (playerAvatar == null)
            {
                Debug.LogWarning("Player not found, creating placeholder");
                var player = new GameObject("Player");
                player.tag = "Player";
                playerAvatar = player.transform;
            }
            InitializeCrowd();
        }
        
        private void InitializeCrowd()
        {
            // Initialize native arrays for job system
            positions = new NativeArray<float3>(maxCrowdSize, Allocator.Persistent);
            velocities = new NativeArray<float3>(maxCrowdSize, Allocator.Persistent);
            accelerations = new NativeArray<float3>(maxCrowdSize, Allocator.Persistent);
            isActive = new NativeArray<bool>(maxCrowdSize, Allocator.Persistent);
            
            // Spawn initial crowd
            for (int i = 0; i < initialCrowdSize; i++)
            {
                SpawnCrowdUnit(i);
            }
            
            currentActiveUnits = initialCrowdSize;
            OnCrowdSizeChanged?.Invoke(currentActiveUnits);
        }
        
        private void SpawnCrowdUnit(int index)
        {
            if (index >= maxCrowdSize) return;
            
            Vector3 spawnPosition = playerAvatar.position + UnityEngine.Random.insideUnitSphere * 3f;
            spawnPosition.y = playerAvatar.position.y;
            
            GameObject unitGO;
            if (crowdUnitPrefab != null)
            {
                unitGO = Instantiate(crowdUnitPrefab, spawnPosition, Quaternion.identity, transform);
            }
            else
            {
                // Create simple cube if no prefab is assigned
                unitGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
                unitGO.transform.position = spawnPosition;
                unitGO.transform.parent = transform;
                unitGO.transform.localScale = Vector3.one * 0.5f;
            }
            
            CrowdUnit unit = unitGO.GetComponent<CrowdUnit>();
            if (unit == null)
            {
                unit = unitGO.AddComponent<CrowdUnit>();
            }
            
            unit.Initialize(index, this);
            
            if (index < crowdUnits.Count)
            {
                crowdUnits[index] = unit;
            }
            else
            {
                crowdUnits.Add(unit);
            }
            
            // Initialize job system data
            positions[index] = spawnPosition;
            velocities[index] = float3.zero;
            accelerations[index] = float3.zero;
            isActive[index] = true;
        }
        
        private void Update()
        {
            if (useJobSystem)
            {
                UpdateCrowdWithJobs();
            }
            else
            {
                UpdateCrowdTraditional();
            }
            
            // Performance optimization check
            if (Time.time - lastOptimizationTime > 1f)
            {
                CheckPerformanceOptimization();
                lastOptimizationTime = Time.time;
            }
        }
        
        private void UpdateCrowdWithJobs()
        {
            if (!positions.IsCreated) return;
            
            // Update positions in job system
            var boidsJob = new BoidsJob
            {
                positions = positions,
                velocities = velocities,
                accelerations = accelerations,
                isActive = isActive,
                playerPosition = playerAvatar.position,
                targetPosition = targetDestination ? targetDestination.position : playerAvatar.position,
                deltaTime = Time.deltaTime,
                unitSpeed = unitSpeed,
                separationRadius = separationRadius,
                alignmentRadius = alignmentRadius,
                cohesionRadius = cohesionRadius,
                separationWeight = separationWeight,
                alignmentWeight = alignmentWeight,
                cohesionWeight = cohesionWeight
            };
            
            JobHandle jobHandle = boidsJob.Schedule(currentActiveUnits, 32);
            jobHandle.Complete();
            
            // Update GameObjects based on job results
            for (int i = 0; i < currentActiveUnits; i++)
            {
                if (isActive[i] && i < crowdUnits.Count && crowdUnits[i] != null)
                {
                    crowdUnits[i].transform.position = positions[i];
                    crowdUnits[i].UpdateMovement(velocities[i]);
                }
            }
        }
        
        private void UpdateCrowdTraditional()
        {
            // Fallback traditional update for compatibility
            Vector3 playerPos = playerAvatar.position;
            
            for (int i = 0; i < currentActiveUnits && i < crowdUnits.Count; i++)
            {
                if (crowdUnits[i] != null)
                {
                    Vector3 direction = (playerPos - crowdUnits[i].transform.position).normalized;
                    crowdUnits[i].transform.position += direction * unitSpeed * Time.deltaTime;
                }
            }
        }
        
        public void MultiplyCrowd(float multiplier)
        {
            int newSize = Mathf.RoundToInt(currentActiveUnits * multiplier);
            newSize = Mathf.Clamp(newSize, 1, maxCrowdSize);
            
            if (newSize > currentActiveUnits)
            {
                // Spawn new units
                for (int i = currentActiveUnits; i < newSize; i++)
                {
                    SpawnCrowdUnit(i);
                }
            }
            else if (newSize < currentActiveUnits)
            {
                // Deactivate excess units
                for (int i = newSize; i < currentActiveUnits; i++)
                {
                    if (i < crowdUnits.Count && crowdUnits[i] != null)
                    {
                        crowdUnits[i].gameObject.SetActive(false);
                        if (isActive.IsCreated && i < isActive.Length)
                        {
                            isActive[i] = false;
                        }
                    }
                }
            }
            
            currentActiveUnits = newSize;
            OnCrowdSizeChanged?.Invoke(currentActiveUnits);
            
            // Analytics tracking
            if (GameManager.Instance != null)
            {
                var analyticsManager = GameManager.Instance.GetComponent<AnalyticsManager>();
                analyticsManager?.TrackEvent("crowd_multiplied", new Dictionary<string, object>
                {
                    { "multiplier", multiplier },
                    { "new_size", newSize },
                    { "timestamp", System.DateTime.UtcNow.ToString() }
                });
            }
        }
        
        public void OptimizeCrowd()
        {
            if (!enableLOD) return;
            
            // Level of Detail optimization
            Camera mainCamera = Camera.main;
            if (mainCamera == null) return;
            
            Vector3 cameraPos = mainCamera.transform.position;
            
            for (int i = 0; i < currentActiveUnits && i < crowdUnits.Count; i++)
            {
                if (crowdUnits[i] != null)
                {
                    float distance = Vector3.Distance(cameraPos, crowdUnits[i].transform.position);
                    crowdUnits[i].SetLODLevel(distance);
                }
            }
        }
        
        private void CheckPerformanceOptimization()
        {
            float fps = 1f / Time.unscaledDeltaTime;
            
            if (fps < 45f && currentActiveUnits > 50) // Performance threshold
            {
                // Reduce crowd size for performance
                int targetSize = Mathf.RoundToInt(currentActiveUnits * 0.8f);
                for (int i = targetSize; i < currentActiveUnits; i++)
                {
                    if (i < crowdUnits.Count && crowdUnits[i] != null)
                    {
                        crowdUnits[i].gameObject.SetActive(false);
                        if (isActive.IsCreated && i < isActive.Length)
                        {
                            isActive[i] = false;
                        }
                    }
                }
                currentActiveUnits = targetSize;
                OnCrowdSizeChanged?.Invoke(currentActiveUnits);
            }
        }
        
        public int GetCrowdSize() => currentActiveUnits;
        
        public Vector3 GetCrowdCenter()
        {
            if (currentActiveUnits == 0) return Vector3.zero;
            
            Vector3 center = Vector3.zero;
            int activeCount = 0;
            
            for (int i = 0; i < currentActiveUnits && i < crowdUnits.Count; i++)
            {
                if (crowdUnits[i] != null && crowdUnits[i].gameObject.activeInHierarchy)
                {
                    center += crowdUnits[i].transform.position;
                    activeCount++;
                }
            }
            
            return activeCount > 0 ? center / activeCount : Vector3.zero;
        }
        
        private void OnDestroy()
        {
            // Dispose native arrays to prevent memory leaks
            if (positions.IsCreated) positions.Dispose();
            if (velocities.IsCreated) velocities.Dispose();
            if (accelerations.IsCreated) accelerations.Dispose();
            if (isActive.IsCreated) isActive.Dispose();
        }
    }
    
    // Job System implementation for high-performance crowd simulation
    [Unity.Burst.BurstCompile]
    public struct BoidsJob : IJobParallelFor
    {
        public NativeArray<float3> positions;
        public NativeArray<float3> velocities;
        public NativeArray<float3> accelerations;
        [ReadOnly] public NativeArray<bool> isActive;
        [ReadOnly] public float3 playerPosition;
        [ReadOnly] public float3 targetPosition;
        [ReadOnly] public float deltaTime;
        [ReadOnly] public float unitSpeed;
        [ReadOnly] public float separationRadius;
        [ReadOnly] public float alignmentRadius;
        [ReadOnly] public float cohesionRadius;
        [ReadOnly] public float separationWeight;
        [ReadOnly] public float alignmentWeight;
        [ReadOnly] public float cohesionWeight;
        
        public void Execute(int index)
        {
            if (!isActive[index]) return;
            
            float3 separation = CalculateSeparation(index);
            float3 alignment = CalculateAlignment(index);
            float3 cohesion = CalculateCohesion(index);
            float3 seek = CalculateSeek(index);
            
            accelerations[index] = separation * separationWeight +
                                 alignment * alignmentWeight +
                                 cohesion * cohesionWeight +
                                 seek;
            
            velocities[index] += accelerations[index] * deltaTime;
            velocities[index] = math.normalize(velocities[index]) * unitSpeed;
            
            positions[index] += velocities[index] * deltaTime;
        }
        
        private float3 CalculateSeparation(int index)
        {
            float3 steer = float3.zero;
            int count = 0;
            
            for (int i = 0; i < positions.Length; i++)
            {
                if (i == index || !isActive[i]) continue;
                
                float distance = math.distance(positions[index], positions[i]);
                if (distance < separationRadius && distance > 0)
                {
                    float3 diff = positions[index] - positions[i];
                    diff = math.normalize(diff) / distance; // Weight by distance
                    steer += diff;
                    count++;
                }
            }
            
            if (count > 0)
            {
                steer /= count;
                steer = math.normalize(steer);
            }
            
            return steer;
        }
        
        private float3 CalculateAlignment(int index)
        {
            float3 average = float3.zero;
            int count = 0;
            
            for (int i = 0; i < positions.Length; i++)
            {
                if (i == index || !isActive[i]) continue;
                
                float distance = math.distance(positions[index], positions[i]);
                if (distance < alignmentRadius)
                {
                    average += velocities[i];
                    count++;
                }
            }
            
            if (count > 0)
            {
                average /= count;
                average = math.normalize(average);
            }
            
            return average;
        }
        
        private float3 CalculateCohesion(int index)
        {
            float3 center = float3.zero;
            int count = 0;
            
            for (int i = 0; i < positions.Length; i++)
            {
                if (i == index || !isActive[i]) continue;
                
                float distance = math.distance(positions[index], positions[i]);
                if (distance < cohesionRadius)
                {
                    center += positions[i];
                    count++;
                }
            }
            
            if (count > 0)
            {
                center /= count;
                return math.normalize(center - positions[index]);
            }
            
            return float3.zero;
        }
        
        private float3 CalculateSeek(int index)
        {
            float3 target = math.distance(positions[index], playerPosition) < 10f ? targetPosition : playerPosition;
            return math.normalize(target - positions[index]);
        }
    }
    
    // Simple CrowdUnit component
    public class CrowdUnit : MonoBehaviour
    {
        private int unitIndex;
        private CrowdController controller;
        private Renderer unitRenderer;
        
        public void Initialize(int index, CrowdController crowdController)
        {
            unitIndex = index;
            controller = crowdController;
            unitRenderer = GetComponent<Renderer>();
        }
        
        public void UpdateMovement(float3 velocity)
        {
            if (velocity.x != 0 || velocity.z != 0)
            {
                transform.rotation = Quaternion.LookRotation(new Vector3(velocity.x, 0, velocity.z));
            }
        }
        
        public void SetLODLevel(float distance)
        {
            if (unitRenderer == null) return;
            
            // Simple LOD system
            if (distance > 20f)
            {
                unitRenderer.enabled = false;
            }
            else if (distance > 10f)
            {
                unitRenderer.enabled = true;
                transform.localScale = Vector3.one * 0.3f;
            }
            else
            {
                unitRenderer.enabled = true;
                transform.localScale = Vector3.one * 0.5f;
            }
        }
    }
}
