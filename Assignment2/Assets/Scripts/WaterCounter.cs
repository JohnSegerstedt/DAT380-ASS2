using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
public class WaterCounter : MonoBehaviour
{
    public WaterDisplay waterDisplay;
    public Text countDisplay;
    private EdgeCollider2D collider;
    private List<Vector2> normals;
    private float insideFactor;

    void Awake() {
        collider = GetComponent<EdgeCollider2D>();
        normals = new List<Vector2>(collider.edgeCount);
        var points = collider.points;
        var collTransform = collider.transform;
        for (var i = 1; i < collider.pointCount; i++) {
            var edge = collTransform.TransformPoint(points[i]) - collTransform.TransformPoint(points[i - 1]);
            var normal = new Vector2(-edge.y, edge.x);
            normals.Add(normal.normalized);
        }
    }

    public int GetPercentageInside() {
        return Mathf.CeilToInt(100 * insideFactor);
    }

    void Update() {
        if (!Application.isPlaying) return;
        var total = waterDisplay.Blobs.Count;
        var inside = total;
        var points = collider.points;
        var collTransform = collider.transform;
        foreach (var blob in waterDisplay.Blobs) {
            for (var i = 1; i < collider.pointCount; i++) {
                var p0 = collTransform.TransformPoint(points[i]);
                var fromP0 = blob.transform.position - p0;
                var fromP02D = new Vector2(fromP0.x, fromP0.y) + normals[i - 1] * .1f;
                if (math.dot(fromP02D, normals[i - 1]) < 0) {
                    inside -= 1;
                    break;
                }
            }
        }

        insideFactor = inside / (float)total;
        countDisplay.text = $"{GetPercentageInside()} %";
    }

    private void OnDrawGizmos() {
        if (collider == null || normals == null) return;
        if (!Selection.Contains(gameObject)) return;
        var points = collider.points;
        var collTransform = collider.transform;
        for (var i = 1; i < collider.pointCount; i++) {
            var sum = collTransform.TransformPoint(points[i]) + collTransform.TransformPoint(points[i - 1]);
            var middle = new Vector2(sum.x, sum.y) / 2;
            Gizmos.DrawRay(middle, normals[i - 1]);
        }
    }
}