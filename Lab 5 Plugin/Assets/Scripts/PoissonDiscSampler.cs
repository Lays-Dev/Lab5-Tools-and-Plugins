using System.Collections.Generic;
using UnityEngine;

public class PoissonDiscSampler
{
    // How many candidates to try around a point before giving up on it.
    private const int k = 30;

    private readonly Rect rect;
    private readonly float radius2;
    private readonly float cellSize;
    private readonly Vector2[,] grid;
    private readonly List<Vector2> activeSamples = new List<Vector2>();

    // width and height bound samples to [0, width] and [0, height].
    // radius is the minimum distance between samples, not the grid cell size.
    public PoissonDiscSampler(float width, float height, float radius)
    {
        rect = new Rect(0, 0, width, height);
        radius2 = radius * radius;
        // One point per cell, so a nearby point is only a few cells away.
        cellSize = radius / Mathf.Sqrt(2);
        grid = new Vector2[Mathf.CeilToInt(width / cellSize),
                           Mathf.CeilToInt(height / cellSize)];
    }

    public IEnumerable<Vector2> Samples()
    {
        // Start from one random point, then grow outward from points that still have room nearby.
        yield return AddSample(new Vector2(Random.value * rect.width, Random.value * rect.height));

        while (activeSamples.Count > 0)
        {
            int i = (int)(Random.value * activeSamples.Count);
            Vector2 sample = activeSamples[i];

            bool found = false;
            for (int j = 0; j < k; ++j)
            {
                float angle = 2 * Mathf.PI * Random.value;
                // Uniform point in the annulus [radius, 2 * radius].
                float r = Mathf.Sqrt(Random.value * 3 * radius2 + radius2);
                Vector2 candidate = sample + r * new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

                if (rect.Contains(candidate) && IsFarEnough(candidate))
                {
                    found = true;
                    yield return AddSample(candidate);
                    break;
                }
            }

            // No open spot around this point, so stop trying to grow from it.
            if (!found)
            {
                activeSamples[i] = activeSamples[activeSamples.Count - 1];
                activeSamples.RemoveAt(activeSamples.Count - 1);
            }
        }
    }

    private bool IsFarEnough(Vector2 sample)
    {
        GridPos pos = new GridPos(sample, cellSize);

        int xmin = Mathf.Max(pos.x - 2, 0);
        int ymin = Mathf.Max(pos.y - 2, 0);
        int xmax = Mathf.Min(pos.x + 2, grid.GetLength(0) - 1);
        int ymax = Mathf.Min(pos.y + 2, grid.GetLength(1) - 1);

        for (int y = ymin; y <= ymax; y++)
        {
            for (int x = xmin; x <= xmax; x++)
            {
                Vector2 s = grid[x, y];
                // Vector2.zero marks an empty cell.
                if (s != Vector2.zero)
                {
                    Vector2 d = s - sample;
                    if (d.x * d.x + d.y * d.y < radius2)
                        return false;
                }
            }
        }

        return true;
    }

    private Vector2 AddSample(Vector2 sample)
    {
        activeSamples.Add(sample);
        GridPos pos = new GridPos(sample, cellSize);
        grid[pos.x, pos.y] = sample;
        return sample;
    }

    private struct GridPos
    {
        public int x;
        public int y;

        public GridPos(Vector2 sample, float cellSize)
        {
            x = (int)(sample.x / cellSize);
            y = (int)(sample.y / cellSize);
        }
    }
}
