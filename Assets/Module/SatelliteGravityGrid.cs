using System;
using System.Collections.Generic;
using System.Linq;

public class SatelliteGravityGrid
{
    public SatelliteVector[,] VectorGrid;
    public int Size;

    public SatelliteGravityGrid(int size)
    {
        Size = size;
        VectorGrid = new SatelliteVector[size * 2 + 1, size * 2 + 1];
        for (int i = 0; i < size * 2 + 1; i++)
        {
            for (int j = 0; j < size * 2 + 1; j++)
            {
                VectorGrid[i, j] = new SatelliteVector();
            }
        }
    }

    public SatelliteVector GetForce(SatelliteVector coordinate)
    {
        if (coordinate.RingLength() > Size)
            return null;
        return VectorGrid[Size + coordinate.Values[0] - coordinate.Values[2], Size + coordinate.Values[1] - coordinate.Values[2]];
    }

    public void AddForce(SatelliteVector coordinate, SatelliteVector force)
    {
        if (coordinate.RingLength() > Size)
            return;

        VectorGrid[Size + coordinate.Values[0] - coordinate.Values[2], Size + coordinate.Values[1] - coordinate.Values[2]] += force;
    }

    public void AddGravityWell(SatelliteVector coordinate, bool strong)
    {
        List<SatelliteVector> vectors = new List<SatelliteVector>();
        for (int i = -2; i <= 2; i++)
        {
            for (int j = -2; j <= 2; j++)
            {
                vectors.Add(new SatelliteVector(i, j, 0));
            }
        }

        foreach (SatelliteVector vector in vectors)
        {
            SatelliteVector pull = new SatelliteVector();
            int distance = vector.RingLength();
            if (distance == 1)
                pull -= vector;
            if (distance <= 2 && strong)
                pull -= vector.Primitive();
            AddForce(coordinate + vector, pull);
        }
    }

    public string Log()
    {
        return Enumerable.Range(0, Size * 2 + 1).Select(x => Enumerable.Range(0, Size * 2 + 1).Select(y => VectorGrid[x, y]).Join(",")).Join("; ");
    }
}