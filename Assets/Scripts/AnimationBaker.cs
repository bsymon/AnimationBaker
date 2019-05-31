using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class AnimationBaker : MonoBehaviour
{
	[SerializeField]
	private Animator animator = null;

	[SerializeField]
	private AnimationClip clip = null;

	[SerializeField]
	private SkinnedMeshRenderer skinnedMesh = null;

	[SerializeField]
	private RawImage debugBakeTexture = null;

	[SerializeField]
	private bool bake = false;

	[SerializeField]
	private int animDuration = 0;

	[SerializeField]
	private int maxX = 2048;

	[SerializeField]
	private float boundsMultiplier = 1f;

	// -- //

	private Mesh bakedMesh;
	private Texture2D bakedTexture;

	// -- //

	private float InverseLerp(float a, float b, float value)
	{
		return (value - a) / (b - a);
	}

	private void Awake()
	{
		Init();

		animator.playableGraph.Stop();
		animator.playableGraph.SetTimeUpdateMode(UnityEngine.Playables.DirectorUpdateMode.Manual);
	}

	private void Update()
	{
		if(bake)
		{
			Playback();
			bake = false;
		}
	}

	// -- //

	private void Playback()
	{
		var frameRate = clip.frameRate;
		var frame     = frameRate * clip.length;
		var fixedDT   = 1f / frameRate;

		animator.playableGraph.Stop();

		var minBound = Vector3.zero;
		var maxBound = Vector3.zero;

		for(var i = 0; i < frame; ++i)
		{
			var bounds = skinnedMesh.bounds;

			if(bounds.min.sqrMagnitude > minBound.sqrMagnitude)
				minBound = bounds.min;

			if(bounds.max.sqrMagnitude > maxBound.sqrMagnitude)
				maxBound = bounds.max;

			animator.playableGraph.Evaluate(fixedDT);
		}

		animator.playableGraph.Evaluate(-((frame + 1) * fixedDT));

		for(var i = 0; i < frame; ++i)
		{
			Bake(i, minBound, maxBound);
			animator.playableGraph.Evaluate(fixedDT);
		}

		animator.playableGraph.Evaluate(-((frame + 1) * fixedDT));

		SaveToPNG();
	}

	private void SaveToPNG()
	{
		var texBytes = ImageConversion.EncodeToPNG(bakedTexture);

		using(var fs = new FileStream("C:\\Users\\Benjamin\\Desktop\\image.png", FileMode.Create, FileAccess.Write))
		{
			fs.Write(texBytes, 0, texBytes.Length);
		}
	}

	private void Init()
	{
		var vertCount = skinnedMesh.sharedMesh.vertexCount;
		var xAxis = Mathf.Min(maxX, vertCount);
		var yAxis = Mathf.RoundToInt((float) vertCount / (float) xAxis) * animDuration; // Animation duration

		bakedMesh    = new Mesh();
		bakedTexture = new Texture2D(xAxis, yAxis);

		bakedTexture.filterMode = FilterMode.Point;

		debugBakeTexture.texture = bakedTexture;

		Debug.Log(bakedTexture.width + " x " + bakedTexture.height);
		Debug.Log(maxX);
		Debug.Log(vertCount);
		Debug.Log($"Bounds : MIN {skinnedMesh.localBounds.min.ToString("0.0000000000000000")} | MAX {skinnedMesh.localBounds.max.ToString("0.0000000000000000")}");
	}

	private void Bake(int frame, Vector3 minBound, Vector3 maxBound)
	{
		skinnedMesh.BakeMesh(bakedMesh);

		var bound = skinnedMesh.localBounds;

		bound.min = minBound * boundsMultiplier;
		bound.max = maxBound * boundsMultiplier;

		Debug.Log($"Min : {(bound.min).ToString("0.0000000000")} | Max : {(bound.max).ToString("0.0000000000")}");

		for(var i = 0; i < bakedMesh.vertexCount; ++i)
		{
			var width = bakedTexture.width;
			var x = i % width;
			var y = Mathf.FloorToInt((float) i / (float) width) + frame;
			var vertex = bakedMesh.vertices[i];

			var vertXOffset = InverseLerp(bound.min.x, bound.max.x, vertex.x);
			var vertYOffset = InverseLerp(bound.min.y, bound.max.y, vertex.y);
			var vertZOffset = InverseLerp(bound.min.z, bound.max.z, vertex.z);

			var color = new Color(vertXOffset, vertYOffset, vertZOffset, 1);

			bakedTexture.SetPixel(x, y, color);
		}

		bakedTexture.Apply();
	}
}
