using Unity.Entities;
using UnityEngine;

namespace THPS.Episode13
{
    public class BoidDataAuthoring : MonoBehaviour
    {
        public float SightRadiusSq;
        public float MaxSpeed;
        public float TargetWeight;
        public float SeparationWeight;

        public class BoidDataBaker : Baker<BoidDataAuthoring>
        {
            public override void Bake(BoidDataAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new BoidData
                {
                    SightRadiusSq = authoring.SightRadiusSq,
                    MaxSpeed = authoring.MaxSpeed,
                    TargetWeight = authoring.TargetWeight,
                    SeparationWeight = authoring.SeparationWeight
                });
            }
        }
    }
}