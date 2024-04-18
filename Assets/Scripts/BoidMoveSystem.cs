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
        private ComponentLookup<LocalTransform> _transformLookup;
        private EntityQuery _boidQuery;
        private float2 _minCorner;
        private float2 _maxCorner;
        private QuadTreeHelper _helper;

        private Entity _schoolTargetEntity;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _transformLookup = SystemAPI.GetComponentLookup<LocalTransform>();
            state.RequireForUpdate<BoidSchoolTargetTag>();
            _boidQuery = SystemAPI.QueryBuilder().WithAll<BoidData, BoidVelocity, BoidAcceleration>().Build();
            _helper = new QuadTreeHelper(16384);
        }

        public void OnStartRunning(ref SystemState state)
        {
            _schoolTargetEntity = SystemAPI.GetSingletonEntity<BoidSchoolTargetTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _helper.Reset();
            var deltaTime = SystemAPI.Time.DeltaTime;

            var schoolTargetPosition = SystemAPI.GetComponent<LocalTransform>(_schoolTargetEntity).Position;
            var target = new float2(schoolTargetPosition.x, schoolTargetPosition.y);
            
            _transformLookup.Update(ref state);

            var otherBoids = _boidQuery.ToEntityArray(state.WorldUpdateAllocator);
            var positionContainer = new NativeArray<float2>(otherBoids.Length, state.WorldUpdateAllocator);

            var qtCenter = SystemAPI.GetSingleton<BoidSchoolCenter>().Value;
            var qtRadius = SystemAPI.GetSingleton<BoidSchoolRadius>().Value * 2f;
            
            var quadTree = new QuadTree(qtCenter, qtRadius, 4);

            for (var i = 0; i < otherBoids.Length; i++)
            {
                var boid = otherBoids[i];
                var otherPosition = _transformLookup[boid].Position;
                var pos = new float2(otherPosition.x, otherPosition.y);
                quadTree.InsertEntity(pos, ref _helper);
                positionContainer[i] = pos;
            }

            var averagePos = double2.zero;

            foreach (var position in positionContainer)
            {
                averagePos += position;
            }
            
            averagePos /= positionContainer.Length;

            var newCenter = new BoidSchoolCenter { Value = (float2)averagePos };

            SystemAPI.SetSingleton(newCenter);
            
            otherBoids.Dispose();
            positionContainer.Dispose();
            
            //quadTree.Draw();
            
            new FlappyBoidsJob { TargetPosition = target, TheQuadTree = quadTree}.ScheduleParallel();
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
        [ReadOnly, NativeDisableUnsafePtrRestriction] public QuadTree TheQuadTree;
        
        [BurstCompile]
        private void Execute(ref BoidAcceleration acceleration, in BoidData boidData, in LocalTransform transform)
        {
            var myPosition = new float2(transform.Position.x, transform.Position.y);
            var desired = TargetPosition - myPosition;
            if (math.lengthsq(desired) > 0f)
            {
                desired = math.normalize(desired);
                desired *= boidData.TargetWeight;
                acceleration.Value += desired;
            }

            var steering = new float2();
            var nearbyCount = 0;
            
            var otherPositions = new NativeList<float2>(0, Allocator.Temp);
            TheQuadTree.GetNearbyPositions(myPosition, 10f, ref otherPositions);
            
            foreach (var otherPos in otherPositions)
            {
                if (myPosition.Equals(otherPos)) continue;
                
                var distSq = math.distancesq(myPosition, otherPos);
                if(distSq >= boidData.SightRadiusSq || distSq <= 0f) continue;

                var diff = myPosition - otherPos;
                steering += diff;
                nearbyCount++;
            }

            otherPositions.Dispose();
            
            if (nearbyCount > 0)
            {
                steering /= nearbyCount;
                steering = math.normalize(steering);
                steering *= boidData.SeparationWeight;
            }
            
            acceleration.Value += steering;
        }
    }
    
    [BurstCompile]
    public partial struct BoidMoveJob : IJobEntity
    {
        public float DeltaTime;
        
        [BurstCompile]
        private void Execute(LocalTransform transform, ref BoidVelocity velocity, ref BoidAcceleration acceleration, in BoidData boidData)
        {
            velocity.Value += acceleration.Value;
            if (math.lengthsq(velocity.Value) > boidData.MaxSpeed * boidData.MaxSpeed)
            {
                velocity.Value = math.normalize(velocity.Value) * boidData.MaxSpeed;
            }
            var moveThisFrame = new float3(velocity.Value.x, velocity.Value.y, 0f) * DeltaTime;
            transform.Translate(moveThisFrame);

            acceleration.Value = float2.zero;
        }
    }
}