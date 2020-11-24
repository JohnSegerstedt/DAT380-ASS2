using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
public class WaterCounter : MonoBehaviour
{
    public Text countDisplayText;
    public Text countDisplayTextShadow;
    private IWaterDisplay waterDisplay;
    private EdgeCollider2D collider;
    private List<Vector2> normals;
    private float insideFactor;
	private float timeEmpty = 0f;
	private float maxTimeEmpty = 1f;

    void Awake() {
        var waterDisplays = FindObjectsOfType<MonoBehaviour>()
            .Where(m => m.enabled)
            .OfType<IWaterDisplay>();
        var enumerable = waterDisplays as IWaterDisplay[] ?? waterDisplays.ToArray();
        if (enumerable.Any()) {
            waterDisplay = enumerable[0];
        }

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
        var total = waterDisplay.BlobsCount;
        var inside = total;
        var points = collider.points;
        var collTransform = collider.transform;
        foreach (var blob in waterDisplay.Positions) {
            for (var i = 1; i < collider.pointCount; i++) {
                Vector2 p0 = ((float3)collTransform.TransformPoint(points[i])).xy;
                var fromP0 = blob - p0 + normals[i - 1] * .2f;
                if (math.dot(fromP0, normals[i - 1]) < 0) {
                    inside -= 1;
                    break;
                }
            }
        }
		if(timeEmpty <= maxTimeEmpty){
			insideFactor = inside / (float)total;
			countDisplayText.text = $"{GetPercentageInside()}%";
			countDisplayTextShadow.text = $"{GetPercentageInside()}%";
			if(insideFactor < 0.01){
				timeEmpty += Time.deltaTime;
				if(timeEmpty > maxTimeEmpty){
					FindObjectOfType<GameManagerScript>().SetGameOver();
					countDisplayText.color = Color.red;
					countDisplayText.text = "0%";
					countDisplayTextShadow.text = "0%";
				}
			}else{
				timeEmpty = 0f;
			}
		}
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