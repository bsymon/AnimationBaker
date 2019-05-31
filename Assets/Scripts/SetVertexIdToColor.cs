using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetVertexIdToColor : MonoBehaviour
{
	[SerializeField]
	private MeshFilter meshFilter = null;

	[SerializeField]
	private MeshRenderer meshRenderer = null;

	// -- //

	private void Awake()
	{
		Set();
	}

	// -- //

	private void Set()
	{
		var mesh   = meshFilter.sharedMesh;
		var colors = new Color[mesh.vertexCount];

		for(var i = 0; i < mesh.vertexCount; ++i)
		{
			var id    = i;
			// var id    = (float) i / (float) mesh.vertexCount;
			var color = new Color(id, id, id, 1);

			colors[i] = color;
		}

		mesh.colors = colors;

		var mpb = new MaterialPropertyBlock();
		mpb.SetFloat("_VertexCount", mesh.vertexCount);

		meshRenderer.SetPropertyBlock(mpb);
	}
}
