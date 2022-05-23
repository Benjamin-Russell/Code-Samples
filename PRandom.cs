using System;
using System.Collections.Generic;
using UnityEngine;

// Class for facilitating predictable psuedo-random functionality
// such that the same sequences of RNG can be replicated.

public static class PRandom
{
    public enum RNGIndex
    { 
        //...

        NUM_INDEXES
    }

    static Dictionary<RNGIndex, System.Random> sRNGs = null;
    public static Dictionary<RNGIndex, bool> sRNGsEnabled = null;

    public static void InitIfNeeded()
    {
        if (sRNGs == null)
        {
            sRNGs = new Dictionary<RNGIndex, System.Random>();
            sRNGsEnabled = new Dictionary<RNGIndex, bool>();

            for (int i = 0; i < (int)RNGIndex.NUM_INDEXES; ++i)
            {
                sRNGs.Add((RNGIndex)i, null);
                sRNGsEnabled.Add((RNGIndex)i, false);
            }
        }
    }

    public static void SetSeed(RNGIndex rngIndex, int seed)
    {
        sRNGs[rngIndex] = new System.Random(seed);
    }

    public static float GetFloat(RNGIndex rngIndex)
    {
        if (sRNGsEnabled[rngIndex])
        {
            return (float)sRNGs[rngIndex].NextDouble();
        }
        else
        {
            // This PRNG is disabled, default to Unity Random
            return UnityEngine.Random.value;
        }
    }

    // min and max inclusive float
    public static float GetRange(RNGIndex rngIndex, float min, float max)
    {
        return Mathf.Lerp(min, max, GetFloat(rngIndex));
    }

    // min inclusive, max exclusive integer
    public static int GetRange(RNGIndex rngIndex, int min, int max)
    {
        if (sRNGsEnabled[rngIndex])
        {
            return sRNGs[rngIndex].Next(min, max);
        }
        else
        {
            // This PRNG is disabled, default to Unity Random
            return UnityEngine.Random.Range(min, max);
        }
    }
}