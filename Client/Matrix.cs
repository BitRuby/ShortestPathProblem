using System;

namespace ShortestPathResolver
{
    class Matrix
    {
        private static int[,] Calculate(int[,] dist, int start, int end)
        {
            Console.WriteLine("Calculating...");
            int V = end;
            int L = (int)Math.Floor((double)Math.Pow(dist.Length, 0.5));
            for (int k = 0; k <= L - 1; ++k)
            {
                for (int i = 0; i <= L - 1; ++i)
                {
                    for (int j = start; j <= end; ++j)
                    {
                        if (dist[i, j] > dist[i, k] + dist[k, j])
                            dist[i, j] = dist[i, k] + dist[k, j];
                    }
                }
            }
            return dist;
        }
    }
}
