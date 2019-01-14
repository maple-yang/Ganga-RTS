﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AccidentalNoise;
using System;

public class TerrainGenerator : MonoBehaviour
{
    public List<GameObject> treePrefabs;
    public List<GameObject> goldPrefabs;
    public List<GameObject> animalsPrefabs;

    public int layersCount = 1;

    [System.Serializable]
    public class GenerateInfo
    {
        public FractalType fractalType = FractalType.MULTI;
        public BasisTypes basisType = BasisTypes.SIMPLEX;
        public InterpTypes interpType = InterpTypes.QUINTIC;

        public int octaves = 3;
        public double frequency = 2.0;
        public double lacunarity = 2.0;
        public uint seed = 1;
        public double scale = 1.0;
    }
    public GenerateInfo mainMapGenerateInfo;

    public Texture2D mapTexture;

    public bool generate = false;
    float[,] mapData;

    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        if (generate)
        {
            generate = false;
            Generate((int)mainMapGenerateInfo.seed);
        }
    }

    public ModuleBase GetFractal()
    {
        Fractal ground_shape_fractal = new Fractal(mainMapGenerateInfo.fractalType,
                                            mainMapGenerateInfo.basisType,
                                            mainMapGenerateInfo.interpType,
                                            mainMapGenerateInfo.octaves,
                                            mainMapGenerateInfo.frequency,
                                            mainMapGenerateInfo.seed);
        ground_shape_fractal.SetLacunarity(mainMapGenerateInfo.lacunarity);
        return ground_shape_fractal as ModuleBase;
    }

    private float KeepPositive(float value)
    {
        if (value < 0) return 0.0f;
        if (value > 1) return 1.0f;
        return value;
    }

    public Color ParseHEX(string hexString)
    {
        Color newColor = new Color();
        ColorUtility.TryParseHtmlString(hexString, out newColor);
        return newColor;
    }

    private Vector2 CalculatePositionToTerrain(Vector2 position)
    {
        Terrain t = Terrain.activeTerrain;
        float scaleX = t.terrainData.alphamapHeight / t.terrainData.size.x;
        float scaleY = t.terrainData.alphamapWidth / t.terrainData.size.z;
        return new Vector2(position.x * scaleX, position.y * scaleY);
    }

    private Vector3 CalculateTerrainToPosition(int x, int y)
    {
        Terrain t = Terrain.activeTerrain;
        float scaleX = t.terrainData.size.x / t.terrainData.alphamapHeight;
        float scaleY = t.terrainData.size.z / t.terrainData.alphamapWidth;
        return new Vector3(y * scaleY, 0.0f, x * scaleX);
    }

    public void SetTextureOnTerrain(Vector2 position, Vector2 size, int layer, int value)
    {
        Terrain t = Terrain.activeTerrain;
        float[,,] map = new float[(int)size.x, (int)size.y, layersCount];
        for (int y = 0; y < (int)size.x; y++)
        {
            for (int x = 0; x < (int)size.y; x++)
            {
                map[y, x, layer] = value;
            }
        }
        Vector2 terrainPosition = CalculatePositionToTerrain(position);
        t.terrainData.SetAlphamaps((int)terrainPosition.x, (int)terrainPosition.y, map);
    }

    public void RemoveGrassOnTerrain(Vector2 position, Vector2 size)
    {
        Terrain t = Terrain.activeTerrain;
        int[,] grassLayers = new int[(int)size.x, (int)size.y];
        for (int y = 0; y < (int)size.x; y++)
        {
            for (int x = 0; x < (int)size.y; x++)
            {
                grassLayers[y, x] = 0;
            }
        }
        Vector2 terrainPosition = CalculatePositionToTerrain(position);
        for (int i = 0; i <= 3; i++)
            t.terrainData.SetDetailLayer((int)terrainPosition.x, (int)terrainPosition.y, i, grassLayers);
    }

    public List<Vector3> GetCoordinates(float minValue, int step = 1, int destCount = 0, float randomOffset = 0.0f, float offsetFromBorder = 0.01f)
    {
        List<Vector3> positions = new List<Vector3>();
        int sizeX = Terrain.activeTerrain.terrainData.alphamapWidth;
        int sizeY = Terrain.activeTerrain.terrainData.alphamapHeight;

        int offsetFromBorderMap = 0;
        if (offsetFromBorder > 0.0f)
            offsetFromBorderMap = (int)(sizeX * offsetFromBorder * 2.0f);

        int countPositions = 0;
        int newStep = step;
        if (destCount > 0)
        {
            for (int y = offsetFromBorderMap; y < sizeX - offsetFromBorderMap; y += step)
                for (int x = offsetFromBorderMap; x < sizeY - offsetFromBorderMap; x += step)
                    if (mapData[x, y] > minValue)
                        countPositions += 1;
            newStep = (int)Math.Ceiling(Math.Sqrt(countPositions / destCount));
        }

        for (int y = offsetFromBorderMap; y < sizeX - offsetFromBorderMap; y += newStep)
        {
            for (int x = offsetFromBorderMap; x < sizeY - offsetFromBorderMap; x += newStep)
            {
                if (mapData[x, y] > minValue)
                {
                    Vector3 newPos = CalculateTerrainToPosition(x, y);
                    if (randomOffset > 0.0f)
                        newPos += new Vector3(5 - x % 10, 0, 5 - y % 10);

                    positions.Add(newPos);
                }
            }
        }
        return positions;
    }

    public List<Vector3> GetCoordinatesInBorder(float maxValue, float offsetPos)
    {
        List<Vector3> positions = new List<Vector3>();
        int sizeX = Terrain.activeTerrain.terrainData.alphamapWidth;
        int sizeY = Terrain.activeTerrain.terrainData.alphamapHeight;
        int offset = (int)(sizeX * offsetPos);

        for (int y = offset; y < sizeY - offset; y++)
            if (mapData[offset, y] < maxValue)
                positions.Add(CalculateTerrainToPosition(offset, y));

        for (int x = offset; x < sizeX - offset; x++)
            if (mapData[x, sizeY - offset] < maxValue)
                positions.Add(CalculateTerrainToPosition(x, sizeY - offset));

        for (int y = sizeY - offset; y > offset; y--)
            if (mapData[sizeX - offset, y] < maxValue)
                positions.Add(CalculateTerrainToPosition(sizeX - offset, y));

        for (int x = sizeX - offset; x > offset; x--)
            if (mapData[x, offset] < maxValue)
                positions.Add(CalculateTerrainToPosition(x, offset));

        return positions;
    }

    public List<Vector3> GetCoordinatesInBorderNested(int rows, int countOnRow, int randValue)
    {
        List<Vector3> positions = new List<Vector3>();
        for (int row = 1; row <= rows; row++)
        {
            float offsetPos = 0.49f / rows * row * 0.8f;
            List<Vector3> avalibleGoldPositions = GetCoordinatesInBorder(maxValue: 0.7f, offsetPos: offsetPos);
            int count = avalibleGoldPositions.Count;
           
            int limit = countOnRow - (row * 2);
            for (int i = 1; i <= limit; i++)
            {
                float step = (count - 1) / limit;
                int positionIndex = (int)((randValue + step * i) % count);

                positions.Add(avalibleGoldPositions[positionIndex]);
            }
        }
        return positions;
    }

    public Dictionary<string, List<Vector3>> GetSpawnData(int spawnCount, int maxTrees, int goldCountOnRow, int goldRows, int animalsCountOnRow, int animalsRows)
    {
        Dictionary<string, List<Vector3>> newData = new Dictionary<string, List<Vector3>>();
        
        // Spawn coordinates
        List<Vector3> avalibleSpawnPositions = GetCoordinatesInBorder(maxValue: 0.3f, offsetPos: 0.15f);
        
        newData["spawn"] = new List<Vector3>();
        for (int i = 1; i <= spawnCount; i++)
        {
            int positionIndex = (int)((avalibleSpawnPositions.Count - 1) / spawnCount * i);
            newData["spawn"].Add(avalibleSpawnPositions[positionIndex]);
        }

        newData["trees"] = GetCoordinates(minValue: 0.9f, destCount: maxTrees, randomOffset: 1.0f);

        newData["gold"] = GetCoordinatesInBorderNested(goldRows, goldCountOnRow, (int)mainMapGenerateInfo.seed);

        newData["animals"] = GetCoordinatesInBorderNested(animalsRows, animalsCountOnRow, (int)(mainMapGenerateInfo.seed / 2));

        return newData;
    }

    public void Generate(int mapSeed, int newSize = 3)
    {
        mainMapGenerateInfo.seed = (uint)mapSeed;
        Terrain t = Terrain.activeTerrain;

        int newTerrainSize = (int)(newSize * 128 * 1.5f);
        t.terrainData.SetDetailResolution(newTerrainSize, 32);
        t.terrainData.alphamapResolution = newTerrainSize;
        t.terrainData.heightmapResolution = newTerrainSize;
        t.terrainData.size = new Vector3(100 * newSize, 100, 100 * newSize);
        mainMapGenerateInfo.scale = newSize / 2.0f;

        // Debug.Log("Terrain Generate: size:" + newSize + " " + t.terrainData.size);

        int sizeX = t.terrainData.alphamapWidth;
        int sizeY = t.terrainData.alphamapHeight;
        if (t.terrainData.detailHeight != t.terrainData.alphamapWidth)
            Debug.Log("detailHeight and alphamapWidth must be equal :" + t.terrainData.alphamapWidth + " " + t.terrainData.alphamapHeight);
        mapData = new float[sizeX, sizeY];

        ModuleBase moduleBase = GetFractal();
        mapTexture = new Texture2D(sizeX, sizeY);
        SMappingRanges ranges = new SMappingRanges();

        float[,,] map = new float[sizeX, sizeY, layersCount];
        int[][,] grassLayers = new int[][,] { new int[sizeX, sizeY], new int[sizeX, sizeY], new int[sizeX, sizeY], new int[sizeX, sizeY] };
        for (int y = 0; y < sizeX; y++)
        {
            for (int x = 0; x < sizeY; x++)
            {
                double p = (double)x / (double)sizeX;
                double q = (double)y / (double)sizeY;
                double nx, ny = 0.0;
                nx = ranges.mapx0 + p * (ranges.mapx1 - ranges.mapx0);
                ny = ranges.mapy0 + q * (ranges.mapy1 - ranges.mapy0);

                float val = (float)moduleBase.Get(nx * mainMapGenerateInfo.scale, ny * mainMapGenerateInfo.scale);

                float textureScale = (val + 1.0f);
                mapData[x, y] = textureScale;

                if (textureScale > 0.89f)
                     mapTexture.SetPixel(y, x, ParseHEX("#005C01"));
                else if (textureScale > 0.7f)
                    mapTexture.SetPixel(y, x, ParseHEX("#007501"));
                else if (textureScale < 0.2f)
                    mapTexture.SetPixel(y, x, ParseHEX("#3F8541"));
                else if (textureScale < 0.1f)
                    mapTexture.SetPixel(y, x, ParseHEX("#5F8560"));
                else
                    mapTexture.SetPixel(y, x, ParseHEX("#008501"));
                // mapTexture.SetPixel(x, y, new Color(val, val, val));

                float grassValue = 0.0f;
                if(textureScale > 0.75)
                    grassValue = KeepPositive((textureScale - 0.75f) * 4.0f);
                map[x, y, 1] = grassValue;

                float stoneValue = 0.0f;
                if (textureScale < 0.25)
                {
                    stoneValue = 1.0f - KeepPositive(textureScale * 4.0f);
                }
                map[x, y, 3] = stoneValue;

                map[x, y, 0] = KeepPositive(textureScale + 0.2f) - grassValue - stoneValue / 2.0f;
                map[x, y, 2] = KeepPositive(1.0f - textureScale - 0.2f) - grassValue - stoneValue / 2.0f;

                if (textureScale > 0.9f)
                {
                    grassLayers[3][x, y] = 1;
                }
                if (textureScale > 0.7f)
                {
                    grassLayers[2][x, y] = 2;
                }
                else if (textureScale > 0.5f)
                {
                    grassLayers[1][x, y] = 2;
                }   
                else if (textureScale > 0.15f)
                {
                    grassLayers[1][x, y] = 1;
                }
            }
        }
        mapTexture.Apply();
        t.terrainData.SetAlphamaps(0, 0, map);
        for (int i = 0; i < grassLayers.Length; i++)
            t.terrainData.SetDetailLayer(0, 0, i, grassLayers[i]);
    }

    public static double DoubleLerp(double start, double end, double amount)
    {
        double difference = end - start;
        double adjusted = difference * amount;
        return start + adjusted;
    }

    public static Color ColorLerp(Color colour, Color to, double amount)
    {
        // start colours as lerp-able floats
        double sr = colour.r, sg = colour.g, sb = colour.b;

        // end colours as lerp-able floats
        double er = to.r, eg = to.g, eb = to.b;

        // lerp the colours to get the difference
        float r = (float)(DoubleLerp(sr, er, amount) / 255.0),
             g = (float)(DoubleLerp(sg, eg, amount) / 255.0),
             b = (float)(DoubleLerp(sb, eb, amount) / 255.0);

        // return the new colour
        return new Color(r, g, b);
    }
}