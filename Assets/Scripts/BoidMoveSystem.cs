using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace THPS.Episode13
{
    public partial struct BoidMoveSystem : ISystem, ISystemStartStop
    {
        private EntityQuery _boidQuery;
        private QuadTreeHelper _helper;
        private Entity _boidTargetEntity;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BoidTargetTag>();
            _boidQuery = SystemAPI.QueryBuilder().WithAll<BoidData, BoidVelocity, BoidAcceleration>().Build();
            _helper = new QuadTreeHelper(16384);
        }

        public void OnStartRunning(ref SystemState state)
        {
            _boidTargetEntity = SystemAPI.GetSingletonEntity<BoidTargetTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _helper.Reset();
            var deltaTime = SystemAPI.Time.DeltaTime;
            var targetPosition = SystemAPI.GetComponent<LocalTransform>(_boidTargetEntity).Position.xy;
            var boidEntityArray = _boidQuery.ToEntityArray(state.WorldUpdateAllocator);
            var quadTree = new QuadTree(float2.zero, 28f, 4);

            for (var i = 0; i < boidEntityArray.Length; i++)
            {
                var boid = boidEntityArray[i];
                var boidPosition = SystemAPI.GetComponent<LocalTransform>(boid).Position.xy;
                quadTree.InsertEntity(boidPosition, ref _helper);
            }

            quadTree.Draw();
            new FlappyBoidsJob { TargetPosition = targetPosition, QuadTree = quadTree}.ScheduleParallel();
            new BoidMoveJob { DeltaTime = deltaTime }.ScheduleParallel();
        }

        public void OnStopRunning(ref SystemState state)
        {
        }

        public void OnDestroy(ref SystemState state)
        {
        }
    }
    
    [BurstCompile]
    public partial struct FlappyBoidsJob : IJobEntity
    {
        [ReadOnly] public float2 TargetPosition;
        [ReadOnly, NativeDisableUnsafePtrRestriction] public QuadTree QuadTree;
        
        [BurstCompile]
        private void Execute(ref BoidAcceleration acceleration, in BoidData boidData, in LocalTransform transform)
        {
            var myPosition = new float2(transform.Position.x, transform.Position.y);
            var vectorToTarget = TargetPosition - myPosition;
            if (math.lengthsq(vectorToTarget) > 0f)
            {
                vectorToTarget = math.normalize(vectorToTarget);
                vectorToTarget *= boidData.TargetWeight;
            }

            var steering = new float2();
            var nearbyCount = 0;
            
            var otherPositions = new NativeList<float2>(0, Allocator.Temp);
            QuadTree.GetNearbyPositions(myPosition, 10f, ref otherPositions);
            
            foreach (var otherPos in otherPositions)
            {
                if (myPosition.Equals(otherPos)) continue;
                
                var distSq = math.distancesq(myPosition, otherPos);
                if(distSq >= boidData.SightRadiusSq || distSq <= 0f) continue;

                var diff = myPosition - otherPos;
                diff = 1 / diff;
                steering += diff;
                nearbyCount++;
            }

            if (nearbyCount > 0 && math.length(steering) > 0f)
            {
                steering = math.normalize(steering);
                steering *= boidData.SeparationWeight;
            }

            var curAcceleration = vectorToTarget + steering;

            acceleration.Value = math.clamp(curAcceleration, new float2(-100f, -100f), new float2(100f, 100f));
        }
    }
    
    [BurstCompile]
    public partial struct BoidMoveJob : IJobEntity
    {
        public float DeltaTime;
        
        [BurstCompile]
        private void Execute(ref LocalTransform transform, ref BoidVelocity velocity, in BoidAcceleration acceleration, 
            in BoidData boidData)
        {
            velocity.Value += acceleration.Value;
            if (math.lengthsq(velocity.Value) > boidData.MaxSpeed * boidData.MaxSpeed)
            {
                velocity.Value = math.normalize(velocity.Value) * boidData.MaxSpeed;
            }
            var moveThisFrame = new float3(velocity.Value.x, velocity.Value.y, 0f) * DeltaTime;
            transform.Position += moveThisFrame;
        }
    }
}