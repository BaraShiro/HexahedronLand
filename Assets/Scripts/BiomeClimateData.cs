using System;
using UnityEngine;

[Serializable]
public class BiomeClimateData
{
    [SerializeField] private int matrixRows;
    [SerializeField] private int matrixColumns;
    [SerializeField] private SurfaceBiomeGenerator[] biomeDataMatrix;

    public int MatrixRows
    {
        get => matrixRows;
        set
        {
            if (value != matrixRows)
            {
                matrixRows = value < 1 ? 1 : value;
                ReshapeMatrix();
            }
        } 
    }

    public int MatrixColumns
    {
        get => matrixColumns;
        set 
        {
            if (value != matrixColumns)
            {
                matrixColumns = value < 1 ? 1 : value;
                ReshapeMatrix();
            }
        }
    }

    public BiomeClimateData(int rows, int columns)
    {
        matrixRows = rows < 1 ? 1 : rows;
        matrixColumns = columns < 1 ? 1 : columns;
        biomeDataMatrix = new SurfaceBiomeGenerator[matrixRows * matrixColumns];
    }

    public ref SurfaceBiomeGenerator GetBiome(int row, int column)
    {
        // Clamp to avoid index out of bounds
        row = Mathf.Clamp(row, 0, matrixRows - 1); 
        column = Mathf.Clamp(column, 0, matrixColumns - 1);
        return ref biomeDataMatrix[(row * matrixColumns) + column];
    }
    public ref SurfaceBiomeGenerator GetBiome(float precipitation, float temperature)
    {
        precipitation = Mathf.Clamp01(precipitation);
        temperature = Mathf.Clamp01(temperature);

        // If precipitation or temperature is equal to 1.0f, row or column will be one over bounds,
        // but this is fine as we clamp them in GetBiome(int, int)
        int row = Mathf.FloorToInt(precipitation * matrixRows);
        int column = Mathf.FloorToInt(temperature * matrixColumns);

        // Debug.Log($"{row} {column}");
        
        return ref GetBiome(row, column);
    }

    public void AddBiomeData(SurfaceBiomeGenerator data, int row, int col)
    {
        // Clamp to avoid index out of bounds
        row = Mathf.Clamp(row, 0, matrixRows - 1); 
        col = Mathf.Clamp(col, 0, matrixColumns - 1);
        biomeDataMatrix[(row * matrixColumns) + col] = data;
    }

    public string SayHi()
    {
        return "Hi!";
    }

    private void ReshapeMatrix()
    {
        SurfaceBiomeGenerator[] newBiomeDataMatrix = new SurfaceBiomeGenerator[matrixRows * matrixColumns];
        int shortestLength = Mathf.Min(biomeDataMatrix.Length, newBiomeDataMatrix.Length);
        for (int i = 0; i < shortestLength; i++)
        {
            newBiomeDataMatrix[i] = biomeDataMatrix[i];
        }

        biomeDataMatrix = newBiomeDataMatrix;
    }
    
}
