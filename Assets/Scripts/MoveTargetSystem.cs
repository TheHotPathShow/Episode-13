using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace THPS.Episode13
{
    public partial class MoveTargetSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            var horizontal = Input.GetAxis("Horizontal");
            var vertical = Input.GetAxis("Vertical");

            foreach (var transform in SystemAPI.Query<RefRW<LocalTransform>>().WithAll<BoidTargetTag>())
            {
                var movementVector = new float3(horizontal, vertical, 0f) * 5f * deltaTime;
                transform.ValueRW.Position += movementVector;
            }
        }
    }
}