using Unity.Entities;
using UnityEngine;

namespace THPS.Episode13
{
    public class BoidSpawnDataAuthoring : MonoBehaviour
    {
        public GameObject Prefab;
        public float SpawnRadius;
        public int SpawnCount;

        public class BoidSpawnDataBaker : Baker<BoidSpawnDataAuthoring>
        {
            public override void Bake(BoidSpawnDataAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new BoidSpawnData
                {
                    Prefab = GetEntity(authoring.Prefab, TransformUsageFlags.Dynamic),
                    SpawnRadius = authoring.SpawnRadius,
                    SpawnCount = authoring.SpawnCount
                });
            }
        }
    }
}