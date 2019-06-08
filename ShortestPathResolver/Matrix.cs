using System;
using System.IO;

namespace ShortestPathResolver
{
    class Matrix
    {
        public int[] CalculateRanges(int clients, int vertices, int n)
        {
            int[] ar = new int[2];
            ar[0] = n * (vertices / clients);
            ar[1] = (ar[0] + (vertices / clients)) - 1;
            if (n + 1 == clients) ar[1] += vertices % clients;
            return ar;
        }

        public int[,] GenerateMatrix(int nodes)
        {
            int[,] m = new int[nodes, nodes];
            Random rnd = new Random();
            int tempValue;
            int connectivity = 0;
            int cst = 99999;
            for (int i = 0; i < nodes; i++)
            {
                for (int j = 0; j < nodes; j++)
                {
                    if (i > j) continue;
                    if (i == j)
                    {
                        m[i, j] = 0;
                    }
                    else
                    {
                        if (connectivity == 0 || rnd.Next(0, 3) != 0)
                        {
                            connectivity = 1;
                            tempValue = rnd.Next(1, 50);
                            m[i, j] = tempValue;
                            m[j, i] = tempValue;
                        }
                        else
                        {
                            m[i, j] = cst;
                            m[j, i] = cst;
                        }
                    }
                }
                connectivity = 0;
            }
            return m;
        }

        public void SaveMatrixToFile(int[,] matrix, string name)
        {
            using (StreamWriter file = new StreamWriter(@name, false))
            {
                for (int j = 0; j < matrix.GetLength(0); j++)
                {
                    for (int i = 0; i < matrix.GetLength(1); i++)
                    {
                        file.Write("{0}\t", matrix[i, j]);
                    }
                    file.Write("\n");
                }
            }
        }

        public int[,] Initialize(int length)
        {
            int[,] dist = new int[length, length];
            for (int i = 0; i < length; ++i)
            {
                for (int j = 0; j < length; ++j)
                {
                    dist[i, j] = 0;
                }
            }
            return dist;
        }

    }
}
