using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public class WaterDisplay : MonoBehaviour
{
    public uint2 blobs;
    public float blobSize;
    public GameObject blobPrefab;

    private List<GameObject> mBlobs = new List<GameObject>();
    public List<GameObject> Blobs => mBlobs;

    void Awake() {
        for (var i = 0; i < blobs.x; i++) {
            for (var j = 0; j < blobs.y; j++) {
                mBlobs.Add(Instantiate(
                    blobPrefab, transform.position
                                + new Vector3(i * 2 * blobSize + Random.value * blobSize / 2, j * 2 * blobSize, 0f),
                    Quaternion.identity,
                    transform));
            }
        }
    }

    public void UpdateDisplay(NativeArray<float2> positions) {
        for (var i = 0; i < positions.Length && i < mBlobs.Count; i++) {
            mBlobs[i].transform.position = new Vector3(positions[i].x, positions[i].y, 0);
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