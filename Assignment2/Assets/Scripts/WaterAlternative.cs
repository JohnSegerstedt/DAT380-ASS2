using System.Collections.Generic;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public struct WaterState
{
    [NativeDisableParallelForRestriction] public NativeArray<float> x;
    [NativeDisableParallelForRestriction] public NativeArray<float> y;
    public NativeArray<float> oldX;
    public NativeArray<float> oldY;
    public NativeArray<float> vx;
    public NativeArray<float> vy;
    public NativeArray<float> p;
    public NativeArray<float> pNear;

    public void Dispose() {
        if (x.IsCreated) x.Dispose();
        if (y.IsCreated) y.Dispose();
        if (oldX.IsCreated) oldX.Dispose();
        if (oldY.IsCreated) oldY.Dispose();
        if (vx.IsCreated) vx.Dispose();
        if (vy.IsCreated) vy.Dispose();
        if (p.IsCreated) p.Dispose();
        if (pNear.IsCreated) pNear.Dispose();
    }
}

public struct GridInfo
{
    public int xCells;
    public int yCells;
    public float halfXSize;
    public float halfYSize;
    public float cellSide;
    public int cellSpace;
}

[BurstCompile(CompileSynchronously = true)]
public struct UpdateParticles : IJobParallelFor
{
    public WaterState waterState;
    [ReadOnly] public float dt;
    [ReadOnly] public float2 gravity;

    public void Execute(int i) {
        waterState.oldX[i] = waterState.x[i];
        waterState.oldY[i] = waterState.y[i];

        waterState.vx[i] += gravity.x * dt;
        waterState.vy[i] += gravity.y * dt;

        // Update positions
        waterState.x[i] += waterState.vx[i] * dt;
        waterState.y[i] += waterState.vy[i] * dt;
    }
}

[BurstCompile(CompileSynchronously = true)]
public struct GridSort : IJobParallelFor
{
    [ReadOnly] public NativeArray<float> x;
    [ReadOnly] public NativeArray<float> y;
    [ReadOnly] public GridInfo gridInfo;

    [NativeDisableParallelForRestriction] public NativeArray<int> grid;

    public void Execute(int key) {
        // Place key in the grid
        var gridX = (int) math.floor((x[key] + gridInfo.halfXSize) / gridInfo.cellSide);
        gridX = math.clamp(gridX, 0, gridInfo.xCells - 1);
        var gridY = (int) math.floor((y[key] + gridInfo.halfYSize) / gridInfo.cellSide);
        gridY = math.clamp(gridY, 0, gridInfo.yCells - 1);
        var cellStart = (gridY * gridInfo.xCells + gridX) * gridInfo.cellSpace;
        var stored = grid[cellStart];
        if (stored < gridInfo.cellSpace - 1) {
            grid[cellStart + 1 + stored] = key;
            grid[cellStart] = stored + 1;
        }
    }
}


[BurstCompile(CompileSynchronously = true)]
public struct UpdatePressures : IJobParallelFor
{
    public WaterState waterState;
    [ReadOnly] public float interactionRadius;
    [ReadOnly] public float interactionRadiusSq;
    [ReadOnly] public float stiffness;
    [ReadOnly] public float restDensity;
    [ReadOnly] public float stiffnessNear;

    [ReadOnly] public GridInfo gridInfo;
    [ReadOnly] public NativeArray<int> grid;

    public void Execute(int i) {
        // Getting the position in the grid for this blob
        var gridX = (int) math.floor((waterState.x[i] + gridInfo.halfXSize) / gridInfo.cellSide);
        var gridY = (int) math.floor((waterState.y[i] + gridInfo.halfYSize) / gridInfo.cellSide);

        // We loop over a square of 3x3 cells around the cell
        var startX = math.clamp(gridX - 1, 0, gridInfo.xCells - 1);
        var endX = math.clamp(gridX + 1, 0, gridInfo.xCells - 1);
        var startY = math.clamp(gridY - 1, 0, gridInfo.yCells - 1);
        var endY = math.clamp(gridY + 1, 0, gridInfo.yCells - 1);

        var position = new float2(waterState.x[i], waterState.y[i]);
        float density = 0;
        float nearDensity = 0;

        for (var x = startX; x <= endX; x++) {
            for (var y = startY; y <= endY; y++) {
                var cellStart = (y * gridInfo.xCells + x) * gridInfo.cellSpace;
                var stored = grid[cellStart];

                for (var cellIdx = 1; cellIdx <= stored; cellIdx++) {
                    var n = grid[cellStart + cellIdx];
                    if (i == n) continue;

                    var neighbour = new float2(waterState.x[n], waterState.y[n]);

                    var lsq = math.distancesq(neighbour, position);
                    if (lsq > interactionRadiusSq) continue;

                    var g = 1 - math.sqrt(lsq) / interactionRadius;

                    // Update density
                    density += g * g;
                    nearDensity += g * g * g;
                }
            }
        }

        waterState.p[i] = stiffness * (density - restDensity);
        waterState.pNear[i] = stiffnessNear * nearDensity;
    }
}

[BurstCompile(CompileSynchronously = true)]
public struct ApplyRelaxation : IJobParallelFor
{
    public WaterState waterState;
    [ReadOnly] public float dt;
    [ReadOnly] public float interactionRadius;
    [ReadOnly] public float interactionRadiusSq;

    [ReadOnly] public GridInfo gridInfo;
    [ReadOnly] public NativeArray<int> grid;

    public void Execute(int i) {
        var gridX = (int) math.floor((waterState.x[i] + gridInfo.halfXSize) / gridInfo.cellSide);
        var gridY = (int) math.floor((waterState.y[i] + gridInfo.halfYSize) / gridInfo.cellSide);

        // We loop over a square of 3x3 cells around the cell
        var startX = math.clamp(gridX - 1, 0, gridInfo.xCells - 1);
        var endX = math.clamp(gridX + 1, 0, gridInfo.xCells - 1);
        var startY = math.clamp(gridY - 1, 0, gridInfo.yCells - 1);
        var endY = math.clamp(gridY + 1, 0, gridInfo.yCells - 1);

        var position = new float2(waterState.x[i], waterState.y[i]);

        // Apply relaxation
        for (var x = startX; x <= endX; x++) {
            for (var y = startY; y <= endY; y++) {
                var cellStart = (y * gridInfo.xCells + x) * gridInfo.cellSpace;
                var stored = grid[cellStart];

                for (var cellIdx = 1; cellIdx <= stored; cellIdx++) {
                    var n = grid[cellStart + cellIdx];
                    if (i == n) continue;

                    var nPos = new float2(waterState.x[n], waterState.y[n]);

                    var lsq = math.distancesq(nPos, position);
                    if (lsq > interactionRadiusSq || lsq == 0) continue;

                    var g = 1 - math.sqrt(lsq) / interactionRadius;

                    var magnitude = waterState.p[i] * g + waterState.pNear[i] * g * g;

                    var direction = math.normalize(nPos - position);
                    var force = direction * magnitude;
                    var d = force * (dt * dt);

                    // if (math.any(math.isnan(d))) {
                    //     Debug.Log($"Found nan! {d} {force} {direction} {magnitude} {g} {lsq}");
                    // }

                    waterState.x[i] += d.x * -.5f;
                    waterState.y[i] += d.y * -.5f;
                    waterState.x[n] += d.x * .5f;
                    waterState.y[n] += d.y * .5f;
                }
            }
        }
    }
}

[BurstCompile(CompileSynchronously = true)]
public struct ColliderInteraction : IJobParallelFor
{
    public WaterState waterState;

    [ReadOnly] public float dt;
    [ReadOnly] public NativeArray<float4> fansCorners;
    [ReadOnly] public NativeArray<float2> fansDirections;
    [ReadOnly] public NativeArray<int> colliders;
    [ReadOnly] public NativeArray<float2> colliderPoints;
    [ReadOnly] public NativeArray<float2> colliderTranslation;

    // Determines if the lines AB and CD intersect.
    static bool DoSegmentsIntersect(float2 colliderA, float2 colliderB, float2 blobA, float2 blobB) {
        var CmP = blobA - colliderA;
        var r = colliderB - colliderA;
        var s = blobB - blobA;

        var CmPxr = CmP.x * r.y - CmP.y * r.x;
        var CmPxs = CmP.x * s.y - CmP.y * s.x;
        var rxs = r.x * s.y - r.y * s.x;

        if (math.abs(CmPxr) <= math.EPSILON) {
            return ((blobA.x - colliderA.x < 0f) != (blobA.x - colliderB.x < 0f))
                   || ((blobA.y - colliderA.y < 0f) != (blobA.y - colliderB.y < 0f));
        }

        if (math.abs(rxs) <= math.EPSILON)
            return false; // Lines are parallel.

        var rxsr = 1f / rxs;
        var t = CmPxs * rxsr;
        var u = CmPxr * rxsr;

        return (t > -math.EPSILON) && (t < 1f + math.EPSILON) && (u > -math.EPSILON) && (u < 1f + math.EPSILON);
    }

    // Determines if and where the lines AB and CD intersect.
    static float2 SegmentsIntersect(float2 colliderA, float2 colliderB, float2 blobA, float2 blobB) {
        var CmP = blobA - colliderA;
        var r = colliderB - colliderA;
        var s = blobB - blobA;

        var CmPxr = CmP.x * r.y - CmP.y * r.x;
        var CmPxs = CmP.x * s.y - CmP.y * s.x;
        var rxs = r.x * s.y - r.y * s.x;

        if (math.abs(CmPxr) <= math.EPSILON) {
            // Lines are collinear
            if (math.dot(blobA - colliderA, blobA - colliderB) < math.EPSILON) {
                // blobA inside colliderA->colliderB
                return blobA;
            }

            if (math.mul(CmP, colliderA - blobB) < math.EPSILON) {
                // colliderA inside blobA->blobB
                if (math.dot(CmP, r) < 0f) {
                    return colliderA;
                }

                return colliderB;
            }

            // no collision, just in line
            return math.NAN;
        }

        if (math.abs(rxs) <= math.EPSILON)
            return math.NAN; // Lines are parallel.

        var rxsr = 1f / rxs;
        var t = CmPxs * rxsr;
        var u = CmPxr * rxsr;

        if ((t > -math.EPSILON) && (t < 1f + math.EPSILON) && (u > -math.EPSILON) && (u < 1f + math.EPSILON)) {
            return colliderA + t * r;
        }

        return math.NAN;
    }

    public void Execute(int i) {
        var currentStart = 0;
        var hasCollided = false;
        var position = new float2(waterState.x[i], waterState.y[i]);
        var oldPosition = new float2(waterState.oldX[i], waterState.oldY[i]);

        for (var c = 0; c < fansCorners.Length; c++) {
            var corner = fansCorners[c];
            if (!(corner.x < position.x && corner.y < position.y
                                        && corner.z > position.x && corner.w > position.y))
                continue;
            // inside collider
            // check if edge collider in the middle
            var fanDir = fansDirections[c];
            // choose opposite side to fanDir (we assume AABB and Axis-Aligned fanDir)
            var collision = new float2(oldPosition.x, corner.w); // top
            if (fanDir.y > math.EPSILON) collision = new float2(oldPosition.x, corner.y); // bottom
            else if (fanDir.x < -math.EPSILON) collision = new float2(corner.z, oldPosition.y); // right
            else if (fanDir.x > math.EPSILON) collision = new float2(corner.x, oldPosition.y); // left
            // check dist to box side
            var dist = math.distance(position, collision);
            var screened = false;
            var accumulated = 0;
            for (var colliderIdx = 0; colliderIdx < colliders.Length; colliderIdx++) {
                var points = colliders[colliderIdx];
                for (var p = 1; p < points; p++) {
                    if (DoSegmentsIntersect(
                        colliderPoints[accumulated + p - 1],
                        colliderPoints[accumulated + p],
                        oldPosition,
                        oldPosition - fanDir * dist
                    )) {
                        screened = true;
                        break;
                    }
                }

                if (screened)
                    break;
                accumulated += points;
            }

            if (!screened) {
                waterState.vx[i] += fanDir.x * 10 * dt;
                waterState.vy[i] += fanDir.y * 10 * dt;
            }
        }

        var minLengthSq = math.INFINITY;
        var edge0 = float2.zero;
        var edge1 = float2.zero;
        var firstIntersection = float2.zero;
        for (var c = 0; c < colliders.Length; c++) {
            var points = colliders[c];
            var prevPos = oldPosition + colliderTranslation[c];
            for (var p = 1; p < points; p++) {
                var p0 = colliderPoints[currentStart + p - 1];
                var p1 = colliderPoints[currentStart + p];

                var dir = math.normalize(position - oldPosition);
                var intersection = SegmentsIntersect(
                    p0, p1, prevPos, position);

                if (math.any(math.isnan(intersection)))
                    continue;

                hasCollided = true;

                var lengthSq = math.lengthsq(intersection - prevPos);
                if (lengthSq < minLengthSq) {
                    firstIntersection = intersection;
                    minLengthSq = lengthSq;
                    edge0 = p0;
                    edge1 = p1;
                }
            }

            currentStart += points;
        }

        if (hasCollided) {
            var fromIntersectionToNew = position - firstIntersection;
            var edgeDir = edge1 - edge0;
            var normal = new float2(edgeDir.y, -edgeDir.x);
            if (math.dot(normal, fromIntersectionToNew) < 0)
                normal *= -1f;
            normal = math.normalize(normal);
            var pointOutOfCollision = firstIntersection - normal * 0.02f;
            var vel = normal * math.dot(new float2(waterState.vx[i], waterState.vy[i]), normal) * 1.2f;

            waterState.x[i] = pointOutOfCollision.x;
            waterState.y[i] = pointOutOfCollision.y;
            waterState.vx[i] -= vel.x;
            waterState.vy[i] -= vel.y;
        }
    }
}

public class WaterAlternative : MonoBehaviour
{
    private WaterState waterState;
    private GridInfo gridInfo;

    private UpdateParticles updateParticles;
    private GridSort gridSort;
    private UpdatePressures updatePressures;
    private ApplyRelaxation applyRelaxation;
    private ColliderInteraction colliderInteraction;

    private WaterDisplay waterDisplay;

    private NativeArray<float2> collidersPoints;
    private NativeArray<int> collidersPointsNum;
    private NativeArray<float2> oldColliderPositions;
    private NativeArray<float4> fansCorners;
    private NativeArray<float2> fansDirections;

    private NativeArray<int> grid;

    private EdgeCollider2D[] colliders;

    public float interactionRadius;
    public float2 gravity;
    public float stiffness;
    public float stiffnessNear;
    public float restDensity;

    public Vector2 gridHalfSize;

    void Start() {
        waterDisplay = GetComponent<WaterDisplay>();
        colliders = FindObjectsOfType<EdgeCollider2D>();
        var boxColliders = FindObjectsOfType<BoxCollider2D>();
        var fans = boxColliders.Where(boxCollider => boxCollider.CompareTag("FanAir")).ToList();

        var blobs = waterDisplay.InitialPositions;
        var waterBlobsNum = waterDisplay.BlobsCount;

        waterState = new WaterState() {
            x = new NativeArray<float>(waterBlobsNum, Allocator.Persistent),
            y = new NativeArray<float>(waterBlobsNum, Allocator.Persistent),
            oldX = new NativeArray<float>(waterBlobsNum, Allocator.Persistent),
            oldY = new NativeArray<float>(waterBlobsNum, Allocator.Persistent),
            vx = new NativeArray<float>(waterBlobsNum, Allocator.Persistent),
            vy = new NativeArray<float>(waterBlobsNum, Allocator.Persistent),
            p = new NativeArray<float>(waterBlobsNum, Allocator.Persistent),
            pNear = new NativeArray<float>(waterBlobsNum, Allocator.Persistent)
        };

        var gridSize = 2 * gridHalfSize;
        var cellSide = interactionRadius;
        gridInfo = new GridInfo() {
            cellSide = cellSide,
            cellSpace = 10,
            xCells = (ushort) Mathf.CeilToInt(gridSize.x / cellSide),
            yCells = (ushort) Mathf.CeilToInt(gridSize.y / cellSide),
            halfXSize = gridSize.x / 2,
            halfYSize = gridSize.y / 2
        };
        grid = new NativeArray<int>(
            gridInfo.xCells * gridInfo.yCells * gridInfo.cellSpace, Allocator.Persistent);

        for (var i = 0; i < waterBlobsNum; i++) {
            waterState.oldX[i] = waterState.x[i] = blobs[i].x;
            waterState.oldY[i] = waterState.y[i] = blobs[i].y;
        }

        var collidersPointsCount = 0;
        for (var c = 0; c < colliders.Length; c++) {
            collidersPointsCount += colliders[c].pointCount;
        }

        collidersPointsNum = new NativeArray<int>(colliders.Length, Allocator.Persistent);
        oldColliderPositions = new NativeArray<float2>(colliders.Length, Allocator.Persistent);
        collidersPoints = new NativeArray<float2>(collidersPointsCount, Allocator.Persistent);
        var collidersPointsAccumulated = 0;
        for (var c = 0; c < colliders.Length; c++) {
            var points = colliders[c].pointCount;
            collidersPointsNum[c] = points;
            for (var i = 0; i < points; i++) {
                var collider = colliders[c];
                var point = collider.transform.TransformPoint(
                    new Vector3(collider.points[i].x, collider.points[i].y, 0));
                collidersPoints[collidersPointsAccumulated + i] = new float2(point.x, point.y);
            }

            collidersPointsAccumulated += points;
        }

        fansCorners = new NativeArray<float4>(fans.Count, Allocator.Persistent);
        fansDirections = new NativeArray<float2>(fans.Count, Allocator.Persistent);
        for (var i = 0; i < fans.Count; i++) {
            var fan = fans[i];
            var bounds = fan.bounds;
            fansCorners[i] = new float4(
                bounds.min.x,
                bounds.min.y,
                bounds.max.x,
                bounds.max.y
            );
            // left because in the prefab the original direction of the air seems to be to the left
            fansDirections[i] = ((float3) fan.transform.TransformDirection(Vector3.left)).xy;
        }

        updateParticles = new UpdateParticles() {
            gravity = gravity,
            waterState = waterState
        };
        gridSort = new GridSort() {
            grid = grid,
            x = waterState.x,
            y = waterState.y,
            gridInfo = gridInfo
        };
        updatePressures = new UpdatePressures() {
            grid = grid,
            stiffness = stiffness,
            stiffnessNear = stiffnessNear,
            interactionRadius = interactionRadius,
            restDensity = restDensity,

            interactionRadiusSq = interactionRadius * interactionRadius,

            waterState = waterState,
            gridInfo = gridInfo
        };
        applyRelaxation = new ApplyRelaxation() {
            grid = grid,
            interactionRadius = interactionRadius,
            interactionRadiusSq = interactionRadius * interactionRadius,
            waterState = waterState,
            gridInfo = gridInfo
        };
        colliderInteraction = new ColliderInteraction() {
            waterState = waterState,
            colliders = collidersPointsNum,
            colliderPoints = collidersPoints,
            fansCorners = fansCorners,
            fansDirections = fansDirections
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

        for (var c = 0; c < fansCorners.Length; c++) {
            var corner = fansCorners[c];
            Debug.DrawLine(
                new Vector3(corner.x, corner.y, 0),
                new Vector3(corner.x, corner.w, 0),
                Color.magenta
            );
            Debug.DrawLine(
                new Vector3(corner.x, corner.y, 0),
                new Vector3(corner.z, corner.y, 0),
                Color.magenta
            );
            Debug.DrawLine(
                new Vector3(corner.z, corner.y, 0),
                new Vector3(corner.z, corner.w, 0),
                Color.magenta
            );
            Debug.DrawLine(
                new Vector3(corner.x, corner.w, 0),
                new Vector3(corner.z, corner.w, 0),
                Color.magenta
            );
        }

        Debug.DrawLine(
            new Vector3(-gridInfo.halfXSize, -gridInfo.halfYSize, 0),
            new Vector3(-gridInfo.halfXSize, gridInfo.halfYSize, 0),
            Color.green
        );
        Debug.DrawLine(
            new Vector3(-gridInfo.halfXSize, gridInfo.halfYSize, 0),
            new Vector3(gridInfo.halfXSize, gridInfo.halfYSize, 0),
            Color.green
        );
        Debug.DrawLine(
            new Vector3(gridInfo.halfXSize, gridInfo.halfYSize, 0),
            new Vector3(gridInfo.halfXSize, -gridInfo.halfYSize, 0),
            Color.green
        );
        Debug.DrawLine(
            new Vector3(gridInfo.halfXSize, -gridInfo.halfYSize, 0),
            new Vector3(-gridInfo.halfXSize, -gridInfo.halfYSize, 0),
            Color.green
        );
    }

    private void FixedUpdate() {
        updateParticles.dt = Time.fixedDeltaTime;
        applyRelaxation.dt = Time.fixedDeltaTime;
        colliderInteraction.dt = Time.fixedDeltaTime;

        var updateHandle = updateParticles.Schedule(waterState.x.Length, 256);

        for (var i = 0; i < gridInfo.xCells * gridInfo.yCells; i += 1) {
            grid[i * gridInfo.cellSpace] = 0;
        }


        var colliderMovement = new NativeArray<float2>(colliders.Length, Allocator.TempJob);
        var accumulated = 0;
        for (var i = 0; i < colliders.Length; i++) {
            var collider = colliders[i];
            var pos = ((float3) collider.transform.position).xy;
            colliderMovement[i] = pos - oldColliderPositions[i];
            oldColliderPositions[i] = pos;
            for (var j = 0; j < collidersPointsNum[i]; j++) {
                var point = collider.transform.TransformPoint(collider.points[j]);
                collidersPoints[accumulated + j] = new float2(point.x, point.y);
            }

            accumulated += collidersPointsNum[i];
        }

        colliderInteraction.colliderTranslation = colliderMovement;

        updateHandle.Complete(); // Need update to complete first
        gridSort.Schedule(waterState.x.Length, 256).Complete();
        updatePressures.Schedule(waterState.x.Length, 256).Complete();
        applyRelaxation.Schedule(waterState.x.Length, 256).Complete();
        colliderInteraction.Schedule(waterState.x.Length, 256).Complete();

        colliderMovement.Dispose();
        waterDisplay.UpdateDisplay(waterState.x, waterState.y);
    }

    private void CheckNans() {
        for (var i = 0; i < waterState.x.Length; i++) {
            if (math.isnan(waterState.x[i])) {
                Debug.Log("NANS here first!");
                break;
            }
        }
    }

    private void OnDestroy() {
        waterState.Dispose();
        if (grid.IsCreated) grid.Dispose();
        if (collidersPoints.IsCreated)
            collidersPoints.Dispose();

        if (collidersPointsNum.IsCreated)
            collidersPointsNum.Dispose();

        if (oldColliderPositions.IsCreated)
            oldColliderPositions.Dispose();
        if (fansCorners.IsCreated)
            fansCorners.Dispose();
        if (fansDirections.IsCreated)
            fansDirections.Dispose();
    }
}