using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

namespace THPS.Episode13
{
    public struct QuadTreeHelper
    {
        private NativeArray<QuadTree> _leaves;
        private int _index;
        
        public QuadTreeHelper(int capacity)
        {
            _leaves = new NativeArray<QuadTree>(capacity, Allocator.Persistent);
            _index = 0;
        }
        
        public void SetNextLeaf(float2 center, float2 extents, int capacity)
        {
            var newQt = new QuadTree(center, extents, capacity);
            _leaves[_index] = newQt;
            _index++;
        }

        public unsafe QuadTree* GetPrevLeafPtr()
        {
            var arrayPtr = (QuadTree*)_leaves.GetUnsafePtr();
            var leafPtr = arrayPtr + _index - 1;
            return leafPtr;
        }

        public void Reset()
        {
            _index = 0;
        }
    }
    
    public unsafe struct QuadTree
    {
        private readonly float2 _center;
        private readonly float2 _extents;
        private readonly int _capacity;
        private FixedList64Bytes<float2> _positions;
        private bool _isDivided;

        private QuadTree* _subdivisionPtr;

        private int _entityCount;
        
        private float MinX => _center.x - _extents.x;
        private float MaxX => _center.x + _extents.x;
        private float MinY => _center.y - _extents.y;
        private float MaxY => _center.y + _extents.y;

        public QuadTree(float2 center, float2 extents, int capacity)
        {
            _center = center;
            _extents = extents;
            _capacity = capacity;
            _positions = new FixedList64Bytes<float2>();
            _isDivided = false;
            _subdivisionPtr = null;
            _entityCount = 0;
        }

        public int Count(int curDepth, ref int maxDepth)
        {
            var count = 1;
            curDepth += 1;
            if (curDepth > maxDepth)
            {
                maxDepth = curDepth;
            }
            if (!_isDivided) return count;

            for (var i = 0; i < 4; i++)
            {
                var ptr = _subdivisionPtr + i;
                UnsafeUtility.CopyPtrToStructure<QuadTree>(ptr, out var subdivision);
                count += subdivision.Count(curDepth, ref maxDepth);
            }

            return count;
        }
        
        public bool InsertEntity(float2 position, ref QuadTreeHelper quadTreeHelper)
        {
            if(!ContainsPoint(position)) return false;

            if (_entityCount < _capacity)
            {
                _positions.Add(position);
                _entityCount++;
                return true;
            }

            if (!_isDivided)
            {
                Subdivide(ref quadTreeHelper);
            }

            for (var i = 0; i < 4; i++)
            {
                var ptr = _subdivisionPtr + i;
                UnsafeUtility.CopyPtrToStructure<QuadTree>(ptr, out var subdivision);
                if (subdivision.InsertEntity(position, ref quadTreeHelper))
                {
                    UnsafeUtility.CopyStructureToPtr(ref subdivision, ptr);
                    return true;
                }
            }
            
            return false;
        }

        public void GetNearbyPositions(float2 origin, float range, ref NativeList<float2> nearbyEntities)
        {
            if (!Intersects(origin, range)) return;
            foreach (var position in _positions)
            {
                if (ContainsPoint(position, origin, range))
                {
                    nearbyEntities.Add(position);
                }
            }

            if (!_isDivided) return;
            for (var i = 0; i < 4; i++)
            {
                var ptr = _subdivisionPtr + i;
                UnsafeUtility.CopyPtrToStructure<QuadTree>(ptr, out var subdivision);
                subdivision.GetNearbyPositions(origin, range, ref nearbyEntities);
            }
        }

        private bool ContainsPoint(float2 point)
        {
            return (point.x >= MinX && point.x < MaxX && point.y >= MinY && point.y < MaxY);
        }

        private bool ContainsPoint(float2 point, float2 origin, float range)
        {
            var minX = origin.x - range;
            var maxX = origin.x + range;
            var minY = origin.y - range;
            var maxY = origin.y + range;
            return (point.x >= minX && point.x < maxX && point.y >= minY && point.y < maxY);
        }
        
        private bool Intersects(float2 origin, float range)
        {
            return !(origin.x - range > MaxX || 
                     origin.x + range < MinX || 
                     origin.y - range > MaxY ||
                     origin.y + range < MinY);
        }
        
        private void Subdivide(ref QuadTreeHelper quadTreeHelper)
        {
            var halfExtents = _extents / 2f;
            
            quadTreeHelper.SetNextLeaf(new float2(_center.x + halfExtents.x, _center.y + halfExtents.y), halfExtents, _capacity);
            _subdivisionPtr = quadTreeHelper.GetPrevLeafPtr();

            quadTreeHelper.SetNextLeaf(new float2(_center.x - halfExtents.x, _center.y + halfExtents.y), halfExtents, _capacity);
            quadTreeHelper.SetNextLeaf(new float2(_center.x + halfExtents.x, _center.y - halfExtents.y), halfExtents, _capacity);
            quadTreeHelper.SetNextLeaf(new float2(_center.x - halfExtents.x, _center.y - halfExtents.y), halfExtents, _capacity);
            
            _isDivided = true;
        }

        public void Draw()
        {
            var color = _isDivided ? Color.red : Color.green;
            Debug.DrawLine(new Vector3(MinX, MinY), new Vector3(MinX, MaxY), color);
            Debug.DrawLine(new Vector3(MinX, MaxY), new Vector3(MaxX, MaxY), color);
            Debug.DrawLine(new Vector3(MaxX, MaxY), new Vector3(MaxX, MinY), color);
            Debug.DrawLine(new Vector3(MaxX, MinY), new Vector3(MinX, MinY), color);

            if (!_isDivided) return;
            
            for (var i = 0; i < 4; i++)
            {
                var ptr = _subdivisionPtr + i;
                UnsafeUtility.CopyPtrToStructure<QuadTree>(ptr, out var subdivision);
                subdivision.Draw();
            }
        }
    }
}