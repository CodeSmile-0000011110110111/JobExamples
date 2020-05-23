using UnityEngine;
using UnityEngine.Profiling;

#pragma warning disable 0649

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class MeshGen : MonoBehaviour
{
	[SerializeField] int xSize = 20;
	[SerializeField] int ySize = 15;
	
	private Mesh mesh;
	private MeshFilter meshFilter;
	
	private Vector4[] tangents;
	private int[] triangles;
	private Vector2[] uv;
	private Vector3[] vertices;

	private void Start()
	{
		var startTime = Time.realtimeSinceStartup;
		Profiler.BeginSample("MeshGen");
		Generate();
		Profiler.EndSample();
		var endTime = Time.realtimeSinceStartup;
		Debug.LogError("MeshGen: " + (endTime - startTime) * 1000f + " ms");
	}

	private void Generate()
	{
		vertices = new Vector3[(xSize + 1) * (ySize + 1)];
		triangles = new int[xSize * ySize * 6];
		uv = new Vector2[vertices.Length];
		tangents = new Vector4[vertices.Length];
		var tangent = new Vector4(1f, 0f, 0f, -1f);
		
		for (int i = 0, y = 0; y <= ySize; y++)
		{
			for (var x = 0; x <= xSize; x++, i++)
			{
				vertices[i] = new Vector3(x, y);
				uv[i] = new Vector2(x / (float) xSize, y / (float) ySize);
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

		mesh = new Mesh();
		meshFilter = GetComponent<MeshFilter>();
		meshFilter.mesh = mesh;

		mesh.name = "Created by MeshGen(TM)";
		mesh.vertices = vertices;
		mesh.triangles = triangles;
		mesh.uv = uv;
		mesh.tangents = tangents;
		mesh.RecalculateNormals();
	}
}