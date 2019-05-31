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

	[SerializeField]
	private Vector3 customMinBound = Vector3.zero;

	[SerializeField]
	private Vector3 customMaxBound = Vector3.zero;

	// -- //

	private Mesh bakedMesh;
	private Texture2D bakedTexture;

	private int yPerFrame;

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

		var rot   = skinnedMesh.transform.rotation;

		// minBound = rot * minBound;
		// maxBound = rot * maxBound;

		Debug.Log("=====");
		Debug.Log($"Miniest : {(minBound).ToString("0.0000000000")} | Maxiest : {(maxBound).ToString("0.0000000000")}");

		animator.playableGraph.Evaluate(-((frame + 1) * fixedDT));

		for(var i = 0; i < frame; ++i)
		{
			// Debug.Log("===== NEW BACK =====");
			Bake(i, customMinBound, customMaxBound);
			animator.playableGraph.Evaluate(fixedDT);
			// Debug.Log("====================");
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

		yPerFrame = Mathf.RoundToInt((float) vertCount / (float) xAxis);
		var yAxis = yPerFrame * animDuration; // Animation duration

		bakedMesh    = new Mesh();
		bakedTexture = new Texture2D(xAxis, yAxis);

		bakedTexture.filterMode = FilterMode.Point;

		debugBakeTexture.texture = bakedTexture;

		Debug.Log(bakedTexture.width + " x " + bakedTexture.height);
		Debug.Log(maxX);
		Debug.Log(vertCount);
	}

	private void Bake(int frame, Vector3 minBound, Vector3 maxBound)
	{
		skinnedMesh.BakeMesh(bakedMesh);

		// var bound = skinnedMesh.localBounds;
		var rot   = skinnedMesh.transform.rotation;

		// bound.min = minBound * boundsMultiplier;
		// bound.max = maxBound * boundsMultiplier;
		minBound = minBound * boundsMultiplier;
		maxBound = maxBound * boundsMultiplier;

		for(var i = 0; i < bakedMesh.vertexCount; ++i)
		{
			var width = bakedTexture.width;
			var x = i % width;
			var y = Mathf.FloorToInt((float) i / (float) width) + (yPerFrame * frame);
			var vertex = bakedMesh.vertices[i];

			// vertex = rot * vertex;

			var vertXOffset = InverseLerp(minBound.x, maxBound.x, vertex.x);
			var vertYOffset = InverseLerp(minBound.y, maxBound.y, vertex.y);
			var vertZOffset = InverseLerp(minBound.z, maxBound.z, vertex.z);

			var color = new Color(vertXOffset, vertYOffset, vertZOffset, 1);

			bakedTexture.SetPixel(x, y, color);

			// Debug.Log($"V : {vertex.ToString("0.00000")} | C : {color.ToString("0.0000000")}");
		}

		bakedTexture.Apply();
	}
}
