using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;



namespace MKP2___Template
{
    // enum type  determines the type of a patch
    public enum type
    {
        BILINEAR,  // bilinear
        BICUBIC, // bicubic
        ARCH // arch
    };

    // enum placement determines the position of a patch
    public enum placement 
    {
        LEFT,
        MIDDLE,
        RIGHT
    }

    class Patch{
        // sampled patch (points computed using the de Casteljau algorithm)
        public class Sampl
        {
            public List<int> Indices; 
            public List<Vector3> Coordinates;

            public Sampl()
            {
                Indices = new List<int>();
                Coordinates = new List<Vector3>();
            }
        }

        public type TypeOfPatch; 
        public placement Place;
        public int NumberOfSamples, DegreeM, DegreeN;

        public Sampl Sampling;
        public float[] Color;

        public List<Vector3>[] Curves = new List<Vector3>[4];

        // Initialization of a patch
        public Patch(type _TypeOfPatch, int _DegreeM, int _DegreeN, int _NumberOfSamples, float[] _Color, placement _Place)
        {
            TypeOfPatch = _TypeOfPatch;
            NumberOfSamples = _NumberOfSamples;
            DegreeM = _DegreeM;
            DegreeN = _DegreeN;
            Color = _Color;
            Place = _Place;

            // --------------- !!! TODO !!! -------------------
            //
            // sample the control polygons of the curves C0, C1, D0, D1, do not forget to initialize the respective lists
            // use the list "Curves" for these boundary curves as follows (otherwise the modificiation of control vertices for these curves won't work):
            // Curves[0] is C0
            // Curves[1] is C1
            // Curves[2] is D0
            // Curves[3] is D1
            //
            // Replace the following lines with the initial control vertices of the curves
            // --------------------------------------------------

            Curves[0] = new List<Vector3>();
            for (int j = 0; j < DegreeN + 1; j++)
                Curves[0].Add(new Vector3(-1, (float)2 * j / DegreeN - 1, 0));
            Curves[1] = new List<Vector3>();
            for (int j = 0; j < DegreeN + 1; j++)
                Curves[1].Add(new Vector3(1, (float)2 * j / DegreeN - 1, 0));
            Curves[2] = new List<Vector3>();
            for (int j = 0; j < DegreeM + 1; j++)
                Curves[2].Add(new Vector3((float)2 * j / DegreeM - 1, -1, 0));
            Curves[3] = new List<Vector3>();
            for (int j = 0; j < DegreeM + 1; j++)
                Curves[3].Add(new Vector3((float)2 * j / DegreeM - 1, 1, 0));



            Sampling = new Sampl();


            // Initial sampling of a patch
            Sampling.Coordinates = Sample(TypeOfPatch, NumberOfSamples, NumberOfSamples);
            Sampling.Indices = GetIndices(TypeOfPatch, NumberOfSamples, NumberOfSamples, true);         
            
        }
    

        // sampling of the initial patch with the given number of samples eU, eV in the direction u, v, respectively
        private List<Vector3> Sample(type _TypeOfPatch, int eU, int eV)
        {
            List<Vector3> SampleList = new List<Vector3>();
            
                for (int i = 0; i <= eV; i++)
                    for (int j = 0; j <= eU; j++)
                        SampleList.Add(new Vector3(-1.0f + 2.0f * i / eV, -1.0f + 2.0f * j / eU, 0.0f));                    

            return SampleList;
        }

        // getting indicies for a patch with given number of samples eU, eV in the direction u, v, respectively 
        private List<int> GetIndices(type _TypeOfPatch, int eU, int eV, bool DrawAll)
        {
            List<int> IndList = new List<int>();
            
                // indices for rectangles - quadruples  
                if (eU <= 0)
                {
                    for (int i = 0; i < eV; i++)
                    {
                        IndList.Add(i);
                        IndList.Add(i);
                        IndList.Add(i + 1);
                        IndList.Add(i + 1);
                    }
                }
                else if (eV <= 0)
                {
                    for (int i = 0; i < eU; i++)
                    {
                        IndList.Add(i);
                        IndList.Add(i + 1);
                        IndList.Add(i + 1);
                        IndList.Add(i);
                    }
                }
                else
                {
                    for (int i = 0; i < eV; i++)
                        for (int j = 0; j < eU; j++)
                        {
                            IndList.Add(i * (eU + 1) + j);
                            IndList.Add(i * (eU + 1) + j + 1);
                            IndList.Add((i + 1) * (eU + 1) + j + 1);
                            IndList.Add((i + 1) * (eU + 1) + j);
                        }
                }            
            return IndList;
        }

        // simple curve casteljau
        private Vector3 Casteljau(int index, float t)
        {
            int deg = index < 2 ? DegreeN : DegreeM;
            if (t == 0) return Curves[index][0];
            else if(t == 1) return Curves[index][deg];
            List<Vector3> tmp = new List<Vector3>(Curves[index]);
            for (int i = 0; i<deg; ++i)
            {
                for(int j = 0; j<deg-i; ++j)
                {
                    tmp[j] = (1 - t) * tmp[j] + t * tmp[j + 1];
                }
            }
            return tmp[0];
        }

        private Vector3 BilinearSc(float s, float t)
        {
            return (1 - s) * Casteljau(0, t) + s * Casteljau(1, t);
        }
        private Vector3 BilinearSd(float s, float t)
        {
            return (1 - t) * Casteljau(2, s) + t * Casteljau(3, s);
        }
        private Vector3 BilinearScd(float s, float t)
        {
            // (1 - s) * (1 - t) * A + (1 - s) * t * B + s * (1 - t) * C + s * t * D 
            return (1 - s) * (1 - t) * Curves[0][0] + (1 - s) * t * Curves[3][0] + (1 - t) * s * Curves[1][0] + s * t * Curves[1][DegreeN];
        }

        
        // Bicubic interpolation of tangnets
        private Vector3 tangent(int i, float t)
        {
            if(i == 0)
            {
                // tangent of d0 in 0
                Vector3 d_d0_0 = Curves[2][1] - Curves[2][0];
                // tangent of d1 in 0
                Vector3 d_d1_0 = Curves[3][1] - Curves[3][0];
                return H3(0, t) * d_d0_0 + H3(3, t) * d_d1_0;
            }
            if(i == 1)
            {
                // tangent of d0 in 1
                Vector3 d_d0_1 = Curves[2][DegreeM] - Curves[2][DegreeM-1];
                // tangent of d1 in 1
                Vector3 d_d1_1 = Curves[3][DegreeM] - Curves[3][DegreeM-1];
                return H3(0, t) * d_d0_1 + H3(3, t) * d_d1_1;
            }
            if(i == 2)
            {
                // tangent of c0 in 0
                Vector3 d_c0_0 = Curves[0][1] - Curves[0][0];
                // tangent of c1 in 0
                Vector3 d_c1_0 = Curves[1][1] - Curves[1][0];
                return H3(0, t) * d_c0_0 + H3(3, t) * d_c1_0;
            }
            if (i == 3)
            {
                // tangent of c0 in 1
                Vector3 d_c0_1 = Curves[0][DegreeN] - Curves[0][DegreeN - 1];
                // tangent of c1 in 1
                Vector3 d_c1_1 = Curves[1][DegreeN] - Curves[1][DegreeN - 1];
                return H3(0, t) * d_c0_1 + H3(3, t) * d_c1_1;
            }
            else return Vector3.Zero;
        }
        // Hermite polynomials
        private float H3(int i, float t)
        {
            if (i == 0) return (1 + 2 * t) * (1 - t) * (1 - t);
            if (i == 1) return t * (1 - t) * (1 - t);
            if (i == 2) return t * t * (t - 1);
            else return t * t * (3 - 2 * t);
        }

        private Vector3 BicubicSc(float s, float t)
        {
            return H3(0, s) * Casteljau(0, t) + H3(1, s) * tangent(0, t) + H3(2, s) * tangent(1, t) + H3(3, s) * Casteljau(1, t);
        }
        private Vector3 BicubicSd(float s, float t)
        {
            return H3(0, t) * Casteljau(2, s) + H3(1, t) * tangent(2, s) + H3(2, t) * tangent(3, s) + H3(3, t) * Casteljau(3, s);
        }
        private Vector3 BicubicScd(float s, float t)
        {
            Vector3[,] matrix = new Vector3[4,4];
            
            matrix[0,0] = Curves[0][0]; // A
            matrix[0,1] = tangent(2, 0); // f0(0)
            matrix[0,2] = tangent(3, 0); // f1(0)
            matrix[0,3] = Curves[3][0]; // B

            matrix[1,0] = tangent(0, 0); // e0(0)
            matrix[1,1] = Vector3.Zero; // twist
            matrix[1,2] = Vector3.Zero; // twist
            matrix[1,3] = tangent(0, 1); // eo(1)

            matrix[2,0] = tangent(1, 0); // e1(0)
            matrix[2,1] = Vector3.Zero; // twist
            matrix[2,2] = Vector3.Zero; // twist
            matrix[2,3] = tangent(1, 1); // e1(1)

            matrix[3,0] = Curves[1][0]; // C
            matrix[3,1] = tangent(2, 1); // f0(1)
            matrix[3,2] = tangent(3, 1); // f1(1)
            matrix[3,3] = Curves[1].Last(); // D

            // B3(s) * matrix * B3(t)
            Vector3 res = Vector3.Zero;
            for (int j = 0; j < 4; j++)
            {
                Vector3 tmp = Vector3.Zero;
                for (int k = 0; k < 4; k++) tmp = tmp + H3(k , s) * matrix[k, j];
                res += H3(j, t) * tmp;
            }
            return res;
        }
        // Indices of the sampled grid are in the following order (example is the patch with degM = 3, degN = 2
        //
        //      8 --- 9 -- 10 -- 11
        //      |     |     |     |
        //      4 --- 5 --- 6 --- 7
        //      |     |     |     |
        //      0 --- 1 --- 2 --- 3
        //
        //

        // Computation of points of the patch 
        public void RecomputePatch()
        {            
            if (TypeOfPatch == type.BILINEAR)
            {
                Sampling.Coordinates.Clear();
                for (int i = 0; i<NumberOfSamples+1; i++)
                {
                    for(int j = 0; j<NumberOfSamples+1; j++)
                    {
                        float s = (float)i / (NumberOfSamples);
                        float t = (float)j / (NumberOfSamples);
                        Sampling.Coordinates.Add(BilinearSc(s, t) + BilinearSd(s, t) - BilinearScd(s, t));
                    }
                }

                // --------------- !!! TODO !!! -------------------
                //
                // Compute the samples of the bilinear patch
                //
                // Do not forget to clear the vector "Sampling.Coordinates" beforehand
                //
                // ------------------------------------------------
            }

            if (TypeOfPatch == type.BICUBIC)
            {
                Sampling.Coordinates.Clear();
                for (int i = 0; i < NumberOfSamples + 1; i++)
                {
                    for (int j = 0; j < NumberOfSamples + 1; j++)
                    {
                        float s = (float)i / (NumberOfSamples);
                        float t = (float)j / (NumberOfSamples);
                        Sampling.Coordinates.Add(BicubicSc(s, t) + BicubicSd(s, t) - BicubicScd(s, t));
                    }
                }
                // --------------- !!! TODO !!! -------------------
                //
                // Compute the samples of the bicubic patch
                //
                // Do not forget to clear the vector "Sampling.Coordinates" beforehand
                //
                // ------------------------------------------------
            }

            if (TypeOfPatch == type.ARCH)
            {
                // --------------- !!! TODO !!! -------------------
                //
                // Compute the samples of the arch as the linear patch
                //
                // Do not forget to clear the vector "Sampling.Coordinates" beforehand
                //
                // ------------------------------------------------
                Sampling.Coordinates.Clear();
                for (int i = 0; i < NumberOfSamples + 1; i++)
                {
                    for (int j = 0; j < NumberOfSamples + 1; j++)
                    {
                        float s = (float)i / (NumberOfSamples);
                        float t = (float)j / (NumberOfSamples);

                        Vector3 res = Vector3.Zero;
                        Vector3 A = new Vector3(-1, 1, 0);
                        Vector3 B = new Vector3(-1, -1, 0);
                        Vector3 C = new Vector3(1, 1, 0);
                        Vector3 D = new Vector3(1, -1, 0);

                        // res = (1-s)*c0(t) + s*c1(t) + (1-t)*d0(s) + t*d1(s) - bilinearABCD

                        // (1-s)*c0(t)
                        res += (1 - s) * ((1 - t) * A + t * B);
                        // s*c1(t)
                        res += s * ((1 - t) * C + t * D);
                        // (1-t)*d0(s)
                        res += (1-t)*((1- s) * A + s * C);
                        // t*d1(s)
                        res += t * new Vector3(-(float)Math.Cos(s * Math.PI), -1,(float)Math.Sin(s * Math.PI));
                        // (1 - s) * (1 - t) * A + (1 - s) * t * B + s * (1 - t) * C + s * t * D 
                        res -= (1 - s) * (1 - t) * A + (1 - s) * t * B + s * (1 - t) * C + s * t * D;
                        Sampling.Coordinates.Add(res);
                    }
                }

            }

        }        
        
        
    }
}
