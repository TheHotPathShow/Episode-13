using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace THPS.Episode13
{
    public partial struct BoidSpawnSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BoidSpawnData>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Enabled = false;
            var random = Random.CreateFromIndex(1000u);
            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);
            var boidSpawnData = SystemAPI.GetSingleton<BoidSpawnData>();
            
            for (var i = 0; i < boidSpawnData.SpawnCount; i++)
            {
                var position = random.NextFloat2InsideCircle(boidSpawnData.SpawnRadius);
                var newBoid = ecb.Instantiate(boidSpawnData.Prefab);
                var newTransform = LocalTransform.FromPosition(position.x, position.y, 0f);
                ecb.SetComponent(newBoid, newTransform);
            }
            
            ecb.Playback(state.EntityManager);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }
    
    public static class RandomExtents
    {
        public static float2 NextFloat2InsideCircle(this ref Random random, float radius)
        {
            var angle = random.NextFloat(math.PI * 2f);
            var distance = random.NextFloat(radius);
            var x = distance * math.cos(angle);
            var y = distance * math.sin(angle);
            return new float2(x, y);
        }
    }
}