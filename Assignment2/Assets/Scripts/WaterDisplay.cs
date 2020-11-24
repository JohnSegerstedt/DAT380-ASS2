using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public interface IWaterDisplay
{
    int BlobsCount { get; }
    Vector2[] InitialPositions { get;  }
    Vector2[] Positions { get; }
    void UpdateDisplay(NativeArray<float2> positions);
    void UpdateDisplay(NativeArray<float> x, NativeArray<float> y);
}

public class WaterDisplay : MonoBehaviour, IWaterDisplay
{
    public uint2 blobs;
    public float blobSize;
    public GameObject blobPrefab;
    private List<GameObject> mBlobs = new List<GameObject>();
    private Vector2[] positions;
    
    void Awake() {
        for (var i = 0; i < blobs.x; i++) {
            for (var j = 0; j < blobs.y; j++) {
                mBlobs.Add(Instantiate(blobPrefab, transform));
            }
        }

        positions = new Vector2[mBlobs.Count];
    }

    public int BlobsCount => mBlobs.Count;
    public Vector2[] InitialPositions {
        get {
            var positions = new Vector2[BlobsCount];
            for (var i = 0; i < blobs.y; i++) {
                for (var j = 0; j < blobs.x; j++) {
                    positions[i * blobs.x + j] =
                        transform.TransformPoint(new Vector3(
                            j * 2 * blobSize + Random.value * blobSize / 2,
                            i * 2 * blobSize, 0f)
                        );
                }
            }
            return positions;
        }
    }

    public Vector2[] Positions => positions;

    public void UpdateDisplay(NativeArray<float2> positions) {
        for (var i = 0; i < positions.Length && i < mBlobs.Count; i++) {
            var prev = mBlobs[i].transform.position;
            mBlobs[i].transform.position = new Vector3(positions[i].x, positions[i].y, prev.z);
            this.positions[i] = positions[i];
        }
    }
    
    public void UpdateDisplay(NativeArray<float> x, NativeArray<float> y) {
        for (var i = 0; i < x.Length && i < mBlobs.Count; i++) {
            var prev = mBlobs[i].transform.position;
            mBlobs[i].transform.position = new Vector3(x[i], y[i], prev.z);
            positions[i] = new Vector2(x[i], y[i]);
        }
    }

    private void OnDrawGizmos() {
        if (Application.isPlaying) return;
        for (var i = 0; i < blobs.x; i++) {
            for (var j = 0; j < blobs.y; j++) {
                var pos = transform.position 
                          + new Vector3(i * 2 * blobSize, j * 2 * blobSize, 0f);
                Gizmos.DrawSphere(pos, blobSize);
            }
        }
    }
}