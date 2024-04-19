using Unity.Entities;
using UnityEngine;

namespace THPS.Episode13
{
    public class BoidTargetAuthoring : MonoBehaviour
    {
        public class BoidTargetBaker : Baker<BoidTargetAuthoring>
        {
            public override void Bake(BoidTargetAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<BoidTargetTag>(entity);
            }
        }
    }
}