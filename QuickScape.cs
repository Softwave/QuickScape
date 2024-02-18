// QuickScape
// A simple terrain generator for the Godot game engine

using Godot;
using System;

[Tool]
public partial class QuickScape : Node3D
{
	private FastNoiseLite fNoise = new FastNoiseLite();
	[Export]
	public Material GroundMaterial { get; set;  }


	private bool _build = false;
	[Export]
	public bool Build
	{
		get => _build;
		set
		{
			_build = value;
			if (_build)
			{
				RequestTerrainGeneration();
			}
		}
	}

	private void RequestTerrainGeneration()
	{
		// Check if we're in the editor and the build flag is true
		if (Engine.IsEditorHint() && _build)
		{
			GenerateTerrain();
		}
	}



	// Private variables
	private int _period = 50;
	private int _octaves = 6;
	private int _yScale = 10;
	private int _subWidth = 100;
	private int _subHeight = 100;
	private int _terrainWidth = 200;
	private int _terrainHeight = 200;


	// Public getters and setters
	[Export]
	public int Period
	{
		get => _period;
		set => _period = value;
	}

	[Export]
	public int Octaves
	{
		get => _octaves;
		set => _octaves = value;
	}

	[Export]
	public int YScale
	{
		get => _yScale;
		set => _yScale = value;
	}

	[Export]
	public int SubWidth
	{
		get => _subWidth;
		set => _subWidth = value;
	}
	[Export]
	public int SubHeight
	{
		get => _subHeight;
		set => _subHeight = value;
	}

	[Export]
	public int TerrainWidth
	{
		get => _terrainWidth;
		set => _terrainWidth = value;
	}

	[Export]
	public int TerrainHeight
	{
		get => _terrainHeight;
		set => _terrainHeight = value;
	}


	public override void _Ready()
	{
		//
		if (!Engine.IsEditorHint())
			return;
	}

	// The terrain generation function
	private void GenerateTerrain()
	{
		// Don't generate terrain if we're not in the editor
		if (!Engine.IsEditorHint())
			return;

		if (GetChildCount() > 0)
			GetChild(0).QueueFree();

		// Setup the noise
		fNoise.NoiseType = FastNoiseLite.NoiseTypeEnum.SimplexSmooth;
		fNoise.Seed = new Random().Next();
		fNoise.FractalOctaves = Octaves;
		fNoise.Frequency = 1.0f / Period;

		// Create the terrain mesh
		var planeMesh = new PlaneMesh
		{
			Size = new Vector2(TerrainWidth, TerrainHeight),
			SubdivideDepth = SubWidth,
			SubdivideWidth = SubHeight
		};

		// SurfaceTool to create the terrain
		var surfaceTool = new SurfaceTool();
		surfaceTool.CreateFrom(planeMesh, 0);
		var arrayPlane = surfaceTool.Commit();
		var dataTool = new MeshDataTool();
		dataTool.CreateFromSurface(arrayPlane, 0);

		// Noisify the terrain
		for (int i = 0; i < dataTool.GetVertexCount(); i++)
		{
			var vertex = dataTool.GetVertex(i);
			vertex.Y = fNoise.GetNoise3D(vertex.X, vertex.Z, 0) * YScale;
			dataTool.SetVertex(i, vertex);
		}

		for (int i = 0; i < arrayPlane.GetSurfaceCount(); i++)
		{
			// There is no SurfaceRemove in Godot 4.0+ so we have to remove it manually
			arrayPlane.ClearSurfaces();
		}

		// Commit and generate normals
		dataTool.CommitToSurface(arrayPlane);
		// Generate with SurfaceTool
		surfaceTool.Begin(Mesh.PrimitiveType.Triangles);
		surfaceTool.CreateFrom(arrayPlane, 0);
		surfaceTool.GenerateNormals();

		// Set the mesh of the current node
		this.Set("mesh", surfaceTool.Commit());
		this.Set("material_override", GroundMaterial);

	}
}
