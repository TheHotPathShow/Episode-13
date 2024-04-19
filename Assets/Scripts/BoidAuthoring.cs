using Unity.Entities;
using UnityEngine;

namespace THPS.Episode13
{
    public class BoidAuthoring : MonoBehaviour
    {
        public float SightRadius;
        public float MaxSpeed;
        public float TargetWeight;
        public float SeparationWeight;

        public class BoidBaker : Baker<BoidAuthoring>
        {
            public override void Bake(BoidAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new BoidData
                {
                    SightRadiusSq = authoring.SightRadius * authoring.SightRadius,
                    MaxSpeed = authoring.MaxSpeed,
                    TargetWeight = authoring.TargetWeight,
                    SeparationWeight = authoring.SeparationWeight
                });
                AddComponent<BoidVelocity>(entity);
                AddComponent<BoidAcceleration>(entity);
            }
        }
    }
}