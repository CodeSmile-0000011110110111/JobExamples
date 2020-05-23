using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Profiling;

#pragma warning disable 0649

/// <summary>
/// Note: due to the overhead of setup and copying values after the job completes, this version takes twice as long as without jobs.
/// However that means once you have at least three of them you start to see benefits.
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class JobbedMeshGen : MonoBehaviour
{
	[SerializeField] int xSize = 20;
	[SerializeField] int ySize = 15;
	
	private Mesh mesh;
	private MeshFilter meshFilter;

	private NativeArray<Vector3> vertices;
	private NativeArray<int> triangles;
	private NativeArray<Vector2> uv;
	private NativeArray<Vector4> tangents;

	private int vertexCount;
	private int triangleCount;
	
	private MeshGenJob job;
	private JobHandle jobHandle;
	
	[BurstCompile]
	private struct MeshGenJob : IJob
	{
		[ReadOnly] public int xSize;
		[ReadOnly] public int ySize;
		public NativeArray<Vector3> vertices;
		public NativeArray<int> triangles;
		public NativeArray<Vector2> uv;
		public NativeArray<Vector4> tangents;
		
		public void Execute()
		{
			var tangent = new Vector4(1f, 0f, 0f, -1f);
		
			for (int i = 0, y = 0; y <= ySize; y++)
			{
				for (var x = 0; x <= xSize; x++, i++)
				{
					vertices[i] = new Vector3(x, y);
					uv[i] = new Vector2(x / (float)xSize, y / (float)ySize);
					tangents[i] = tangent;
				}
			}

			for (int ti = 0, vi = 0, y = 0; y < ySize; y++, vi++)
			{
				for (var x = 0; x < xSize; x++, ti += 6, vi++)
				{
					triangles[ti] = vi;
					triangles[ti + 3] = triangles[ti + 2] = vi + 1;
					triangles[ti + 4] = triangles[ti + 1] = vi + xSize + 1;
					triangles[ti + 5] = vi + xSize + 2;
				}
			}
		}
	}
	
	private void Start()
	{
		var startTime = Time.realtimeSinceStartup;
		Profiler.BeginSample("MeshGen NativeArray");

		vertexCount = (xSize + 1) * (ySize + 1);
		triangleCount = xSize * ySize * 6;
		vertices = new NativeArray<Vector3>(vertexCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
		triangles = new NativeArray<int>(triangleCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
		uv = new NativeArray<Vector2>(vertexCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
		tangents = new NativeArray<Vector4>(vertexCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
		
		job = new MeshGenJob
		{
			xSize = xSize,
			ySize = ySize,
			vertices = vertices,
			triangles = triangles,
			uv = uv,
			tangents = tangents,
		};
		
		jobHandle = job.Schedule();

		StartCoroutine(FinishTheJob());
		
		Profiler.EndSample();
		var endTime = Time.realtimeSinceStartup;
		Debug.LogError("MeshGen NativeArray: " + (endTime - startTime) * 1000f + " ms");
	}

	private IEnumerator FinishTheJob()
	{
		// 1 frame delay is acceptable in this case
		yield return new WaitForEndOfFrame();

		var startTime = Time.realtimeSinceStartup;
		Profiler.BeginSample("MeshGen NativeArray Complete()");

		jobHandle.Complete();
		
		mesh = new Mesh();
		meshFilter = GetComponent<MeshFilter>();
		meshFilter.mesh = mesh;

		mesh.name = "Created by JobbedMeshGen(TM)";
		
		// ToArray() was slow, I was hoping CopyTo() might be faster
		// it isn't, at least not in the editor
		var tempVertices = new Vector3[vertexCount];
		vertices.CopyTo(tempVertices);
		mesh.vertices = tempVertices;

		var tempTriangles = new int[triangleCount];
		triangles.CopyTo(tempTriangles);
		mesh.triangles = tempTriangles;

		var tempUV = new Vector2[vertexCount];
		uv.CopyTo(tempUV);
		mesh.uv = tempUV;
		
		var tempTangents = new Vector4[vertexCount];
		tangents.CopyTo(tempTangents);
		mesh.tangents = tempTangents;
		
		mesh.RecalculateNormals();

		vertices.Dispose();
		triangles.Dispose();
		uv.Dispose();
		tangents.Dispose();

		Profiler.EndSample();
		var endTime = Time.realtimeSinceStartup;
		Debug.LogError("MeshGen NativeArray Complete(): " + (endTime - startTime) * 1000f + " ms");
	}
}
