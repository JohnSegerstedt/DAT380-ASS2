using System.Collections.Generic;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[RequireComponent(typeof(WaterDisplay))]
public class Water : MonoBehaviour
{
    [Min(0)] public float gravity = 8;
    [Min(0.1f)] public float forceRadius = 3f;
    [Min(0)] public float waterForce = 20f;
    [Min(0)] public float waterDamping = 0.2f;
    [Min(0)] public float collidersThickness = 0.5f;

    NativeArray<float2> velocities, positions, newVelocities, newPositions, collidersPoints;
    private NativeArray<int> collidersPointsNum;

    private NativeArray<float2> _oldColliderPositions;

    private ushort xCells, yCells;
    private ushort cellSpace;
    private float cellSide;
    private Vector2 gridSize;
    private NativeArray<int> grid;

    private WaterDisplay _waterDisplayDisplay;
    private EdgeCollider2D[] _colliders;

    private WaterJob _waterJob;
    private SortJob _sortJob;

    private void Start() {
        _waterDisplayDisplay = GetComponent<WaterDisplay>();
        _colliders = FindObjectsOfType<EdgeCollider2D>();
        var blobs = _waterDisplayDisplay.Blobs;
        var waterBlobsNum = blobs.Count();

        velocities = new NativeArray<float2>(waterBlobsNum, Allocator.Persistent);
        positions = new NativeArray<float2>(waterBlobsNum, Allocator.Persistent);
        newVelocities = new NativeArray<float2>(waterBlobsNum, Allocator.Persistent);
        newPositions = new NativeArray<float2>(waterBlobsNum, Allocator.Persistent);

        cellSide = forceRadius;
        var cameraWidth = Camera.main.orthographicSize * 2;
        var cameraHeight = cameraWidth / Camera.main.aspect;
        gridSize = new Vector2(cameraWidth, cameraHeight);
        xCells = (ushort) Mathf.CeilToInt(gridSize.x / cellSide);
        yCells = (ushort) Mathf.CeilToInt(gridSize.y / cellSide);
        cellSpace = 10;
        grid = new NativeArray<int>(xCells * yCells * cellSpace, Allocator.Persistent);

        for (var i = 0; i < xCells * yCells; i++) {
            grid[i * cellSpace] = 0;
        }

        for (var i = 0; i < positions.Length; i++) {
            var pos = blobs[i].transform.position;
            positions[i] = new float2(pos.x, pos.y);
            velocities[i] = 0;
        }

        var collidersPointsCount = 0;
        for (var c = 0; c < _colliders.Length; c++) {
            collidersPointsCount += _colliders[c].pointCount;
        }

        collidersPointsNum = new NativeArray<int>(_colliders.Length, Allocator.Persistent);
        _oldColliderPositions = new NativeArray<float2>(_colliders.Length, Allocator.Persistent);
        collidersPoints = new NativeArray<float2>(collidersPointsCount, Allocator.Persistent);
        var collidersPointsAccumulated = 0;
        for (var c = 0; c < _colliders.Length; c++) {
            var points = _colliders[c].pointCount;
            collidersPointsNum[c] = points;
            for (var i = 0; i < points; i++) {
                var collider = _colliders[c];
                var point = new Vector3(collider.points[i].x, collider.points[i].y, 0);
                collidersPoints[collidersPointsAccumulated + i] = new float2(point.x, point.y);
            }

            collidersPointsAccumulated += points;
        }

        for (var key = 0; key < positions.Length; key++) {
            // Place key in the grid
            var gridX = (int) math.floor((positions[key].x + gridSize.x / 2) / cellSide);
            if (gridX < 0) gridX = 0;
            else if (gridX > xCells - 1) gridX = xCells - 1;
            var gridY = (int) math.floor((positions[key].y + gridSize.y / 2) / cellSide);
            if (gridY < 0) gridY = 0;
            else if (gridY > yCells - 1) gridY = yCells - 1;
            var cellStart = (gridY * xCells + gridX) * cellSpace;
            var stored = grid[cellStart];
            if (stored < cellSpace - 1) {
                grid[cellStart + 1 + stored] = key;
                grid[cellStart] = stored + 1;
            }
        }

        _sortJob = new SortJob {
            grid = grid,
            xCells = xCells,
            yCells = yCells,
            halfXSize = gridSize.x / 2,
            halfYSize = gridSize.y / 2,
            cellSpace = cellSpace,
            cellSide = cellSide
        };

        _waterJob = new WaterJob {
            gravity = gravity,
            forceRadius = forceRadius,
            waterForce = waterForce,
            waterDamping = waterDamping,

            colliders = collidersPointsNum,
            colliderPoints = collidersPoints,
            collidersThickness = collidersThickness,

            gridSizeX = gridSize.x,
            gridSizeY = gridSize.y,

            grid = grid,
            xCells = xCells,
            yCells = yCells,
            cellSpace = cellSpace,
            cellSide = cellSide
        };
    }

    private void Update() {
        var startNum = 0;
        for (var c = 0; c < collidersPointsNum.Length; c++) {
            var points = collidersPointsNum[c];
            for (var p = 1; p < points; p++) {
                var p0 = collidersPoints[startNum + p - 1];
                var p1 = collidersPoints[startNum + p];
                Debug.DrawLine(new Vector3(p0.x, p0.y, 0), new Vector3(p1.x, p1.y, 0), Color.red);
            }

            startNum += points;
        }
    }

    void FixedUpdate() {
        for (var i = 0; i < xCells * yCells; i += 1) {
            grid[i * cellSpace] = 0;
        }

        _sortJob.positions = positions;
        _sortJob
            .Schedule(positions.Length, 256)
            .Complete();

        var colliderMovement = new NativeArray<float2>(_colliders.Length, Allocator.TempJob);
        var accumulated = 0;
        for (var i = 0; i < _colliders.Length; i++) {
            float2 pos = ((float3) _colliders[i].transform.position).xy;
            colliderMovement[i] = pos - _oldColliderPositions[i];
            _oldColliderPositions[i] = pos;
            for (var j = 0; j < collidersPointsNum[i]; j++) {
                collidersPoints[accumulated + j] += colliderMovement[i];
            }

            accumulated += collidersPointsNum[i];
        }

        _waterJob.dt = Time.fixedDeltaTime;
        _waterJob.velocities = velocities;
        _waterJob.positions = positions;
        _waterJob.newVelocities = newVelocities;
        _waterJob.newPositions = newPositions;
        _waterJob.colliderMovement = colliderMovement;
        _waterJob
            .Schedule(positions.Length, 256)
            .Complete();

        _waterDisplayDisplay.UpdateDisplay(newPositions);

        colliderMovement.Dispose();

        //Swap buffers for next frame:
        var tmpPositions = newPositions;
        newPositions = positions;
        positions = tmpPositions;

        var tmpVelocities = newVelocities;
        newVelocities = velocities;
        velocities = tmpVelocities;
    }

    private void OnDestroy() {
        if (velocities.IsCreated)
            velocities.Dispose();

        if (positions.IsCreated)
            positions.Dispose();

        if (newVelocities.IsCreated)
            newVelocities.Dispose();

        if (newPositions.IsCreated)
            newPositions.Dispose();

        if (collidersPoints.IsCreated)
            collidersPoints.Dispose();

        if (collidersPointsNum.IsCreated)
            collidersPointsNum.Dispose();

        if (_oldColliderPositions.IsCreated)
            _oldColliderPositions.Dispose();

        grid.Dispose();
    }

    [BurstCompile(CompileSynchronously = true)]
    private struct SortJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<float2> positions;
        [ReadOnly] public int xCells;
        [ReadOnly] public int yCells;
        [ReadOnly] public float halfXSize;
        [ReadOnly] public float halfYSize;
        [ReadOnly] public float cellSide;
        [ReadOnly] public int cellSpace;

        [NativeDisableParallelForRestriction] public NativeArray<int> grid;

        public void Execute(int key) {
            // Place key in the grid
            var gridX = (int) math.floor((positions[key].x + halfXSize) / cellSide);
            if (gridX < 0) gridX = 0;
            else if (gridX > xCells - 1) gridX = xCells - 1;
            var gridY = (int) math.floor((positions[key].y + halfYSize) / cellSide);
            if (gridY < 0) gridY = 0;
            else if (gridY > yCells - 1) gridY = yCells - 1;
            var cellStart = (gridY * xCells + gridX) * cellSpace;
            var stored = grid[cellStart];
            if (stored < cellSpace - 1) {
                grid[cellStart + 1 + stored] = key;
                grid[cellStart] = stored + 1;
            }
        }
    }

    // Using BurstCompile to compile a Job with burst
    // Set CompileSynchronously to true to make sure that the method will not be compiled asynchronously
    // but on the first schedule
    [BurstCompile(CompileSynchronously = true)]
    private struct WaterJob : IJobParallelFor
    {
        [ReadOnly] public float dt;
        [ReadOnly] public float gravity;
        [ReadOnly] public float forceRadius;
        [ReadOnly] public float waterForce;
        [ReadOnly] public float waterDamping;
        [ReadOnly] public float boundaryForce;
        [ReadOnly] public float gridSizeX;
        [ReadOnly] public float gridSizeY;


        [ReadOnly] public int xCells;
        [ReadOnly] public int yCells;
        [ReadOnly] public float cellSide;
        [ReadOnly] public int cellSpace;

        [ReadOnly] public NativeArray<int> grid;

        [ReadOnly] public NativeArray<float2> velocities;

        [ReadOnly] public NativeArray<float2> positions;

        [WriteOnly] public NativeArray<float2> newVelocities;

        [WriteOnly] public NativeArray<float2> newPositions;

        [ReadOnly] public NativeArray<int> colliders;
        [ReadOnly] public NativeArray<float2> colliderPoints;
        [ReadOnly] public NativeArray<float2> colliderMovement;
        [ReadOnly] public float collidersThickness;

        private static float3 FindClosestPoint(float2 to, float2 p0, float2 p1) {
            var edge = p1 - p0;
            var edgeDir = math.normalize(edge);
            var p0to = to - p0;
            var p1to = to - p1;

            if (math.mul(p1to, edge) >= 0) // p1 is closest
                return new float3(p1.x, p1.y, math.length(p1to));
            var p0toProjectedToEdge = math.mul(p0to, edgeDir);
            if (p0toProjectedToEdge < 0) // p0 is closest
                return new float3(p0.x, p0.y, math.length(p0to));
            var point = p0 + edgeDir * p0toProjectedToEdge;
            var delta = to - point;

            return new float3(point.x, point.y, math.length(delta));
        }

        private static float2 IntersectionPointRays(float2 ao, float2 ad, float2 bo, float2 bd) {
            var denom = (ad.x * bd.y - ad.y * bd.x);
            if (math.abs(denom) <= math.EPSILON) return new float2(math.NAN);
            var u = (ao.y * bd.x + bd.y * bo.x - bo.y * bd.x - bd.y * ao.x) / denom;
            return ao + ad * u;
        }

        public void Execute(int i) {
            var totalForce = new float2(0, -gravity * dt);

            // Getting the position in the grid for this blob
            var gridX = (int) math.floor((positions[i].x + gridSizeX / 2) / cellSide);
            var gridY = (int) math.floor((positions[i].y + gridSizeY / 2) / cellSide);

            // We loop over a square of 3x3 cells around the cell
            var startX = math.clamp(gridX - 1, 0, xCells - 1);
            var endX = math.clamp(gridX + 1, 0, xCells - 1);
            var startY = math.clamp(gridY - 1, 0, yCells - 1);
            var endY = math.clamp(gridY + 1, 0, yCells - 1);

            for (var x = startX; x <= endX; x++) {
                for (var y = startY; y <= endY; y++) {
                    var cellStart = (y * xCells + x) * cellSpace;
                    var stored = grid[cellStart];
                    for (var _j = 1; _j <= stored; _j++) {
                        var j = grid[cellStart + _j];

                        var otherPos = positions[j];
                        var deltaPos = otherPos - positions[i];
                        var distance = math.length(deltaPos);
                        if (distance <= 0 || distance > forceRadius) {
                            continue;
                        }

                        var direction = -deltaPos / distance;
                        var forceAmount01 = math.clamp((forceRadius - distance) / forceRadius, 0, 1);
                        var force = (direction * math.pow(forceAmount01, 0.5f) * waterForce) * dt;

                        totalForce += force;
                    }
                }
            }

            var damping = velocities[i] * math.length(velocities[i]) * waterDamping * dt;
            var velocity = velocities[i] + totalForce - damping;

            // Contain above Y 0
            if (positions[i].y < 0) {
                velocity.y = -positions[i].y / dt;
            }

            var position = positions[i] + velocity * dt;

            var currentStart = 0;
            for (var c = 0; c < colliders.Length; c++) {
                var points = colliders[c];
                var positionChange = colliderMovement[c];
                for (var p = 1; p < points; p++) {
                    var p0 = colliderPoints[currentStart + p - 1];
                    var p1 = colliderPoints[currentStart + p];
                    var pointAndDist = FindClosestPoint(
                        position,
                        p0,
                        p1
                    );
                    var point = pointAndDist.xy;
                    var distance = pointAndDist.z;

                    if (distance == 0f || distance >= collidersThickness) {
                        continue;
                    }

                    var normal = math.normalize(position - point);

                    var pointOutOfCollision = IntersectionPointRays(
                        point + normal * (collidersThickness + .01f),
                        p1 - p0,
                        position,
                        velocity
                    );
                    if (!math.any(math.isnan(pointOutOfCollision))) {
                        position = pointOutOfCollision + positionChange;
                        velocity -= normal * math.dot(velocity, normal);

                        // At least have the same velocity in each axis as the collider
                        if (math.abs(positionChange.x) > math.EPSILON) {
                            velocity.x = positionChange.x > 0
                                ? math.max(velocity.x, positionChange.x / dt)
                                : math.min(velocity.x, positionChange.x / dt);
                        }

                        if (math.abs(positionChange.y) > math.EPSILON) {
                            velocity.y = positionChange.y > 0
                                ? math.max(velocity.y, positionChange.y / dt)
                                : math.min(velocity.y, positionChange.y / dt);
                        }
                    }
                }

                currentStart += points;
            }

            newVelocities[i] = velocity;
            newPositions[i] = position;
        }
    }
}