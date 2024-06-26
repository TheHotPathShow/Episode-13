﻿using Unity.Entities;
using Unity.Mathematics;

namespace THPS.Episode13
{
    public struct BoidTargetTag : IComponentData {}

    public struct BoidData : IComponentData
    {
        public float SightRadiusSq;
        public float MaxSpeed;
        public float TargetWeight;
        public float SeparationWeight;
    }

    public struct BoidVelocity : IComponentData
    {
        public float2 Value;
    }

    public struct BoidAcceleration : IComponentData
    {
        public float2 Value;
    }

    public struct BoidSpawnData : IComponentData
    {
        public Entity Prefab;
        public float SpawnRadius;
        public int SpawnCount;
    }
}