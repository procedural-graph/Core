using BenchmarkDotNet.Attributes;
using Microsoft.VSDiagnostics;
using System;

namespace ProceduralGraph.Benchmarks;

[CPUUsageDiagnoser, DisassemblyDiagnoser(printSource: true)]
public class FastMath
{
    private const int N = 10000;
    private float[] _floatData;
    private double[] _doubleData;

    [GlobalSetup]
    public void Setup()
    {
        var random = new Random(42);
        _floatData = new float[N];
        _doubleData = new double[N];

        for (int i = 0; i < N; i++)
        {
            // Avoid zero to prevent DivideByZero / Infinity skews in benchmarks
            _floatData[i] = (float)(random.NextDouble() * 100.0) + 0.1f;
            _doubleData[i] = (random.NextDouble() * 100.0) + 0.1;
        }
    }

    // --- Float Benchmarks ---

    [Benchmark(Baseline = true)]
    public float StandardReciprocalFloat()
    {
        float result = 0;
        var data = _floatData;
        for (int i = 0; i < data.Length; i++)
        {
            result += 1f / data[i];
        }
        return result;
    }

    [Benchmark]
    public float EstimateReciprocalFloat()
    {
        float result = 0;
        var data = _floatData;
        for (int i = 0; i < data.Length; i++)
        {
            result += Mathematics.FastMath.ReciprocalEstimate(data[i]);
        }
        return result;
    }

    [Benchmark]
    public float StandardReciprocalSqrtFloat()
    {
        float result = 0;
        var data = _floatData;
        for (int i = 0; i < data.Length; i++)
        {
            result += 1f / (float)Math.Sqrt(data[i]);
        }
        return result;
    }

    [Benchmark]
    public float EstimateReciprocalSqrtFloat()
    {
        float result = 0;
        var data = _floatData;
        for (int i = 0; i < data.Length; i++)
        {
            result += Mathematics.FastMath.ReciprocalSqrtEstimate(data[i]);
        }
        return result;
    }

    // --- Double Benchmarks ---

    [Benchmark]
    public double StandardReciprocalDouble()
    {
        double result = 0;
        var data = _doubleData;
        for (int i = 0; i < data.Length; i++)
        {
            result += 1.0 / data[i];
        }
        return result;
    }

    [Benchmark]
    public double EstimateReciprocalDouble()
    {
        double result = 0;
        var data = _doubleData;
        for (int i = 0; i < data.Length; i++)
        {
            result += Mathematics.FastMath.ReciprocalEstimate(data[i]);
        }
        return result;
    }

    [Benchmark]
    public double StandardReciprocalSqrtDouble()
    {
        double result = 0;
        var data = _doubleData;
        for (int i = 0; i < data.Length; i++)
        {
            result += 1.0 / Math.Sqrt(data[i]);
        }
        return result;
    }

    [Benchmark]
    public double EstimateReciprocalSqrtDouble()
    {
        double result = 0;
        var data = _doubleData;
        for (int i = 0; i < data.Length; i++)
        {
            result += Mathematics.FastMath.ReciprocalSqrtEstimate(data[i]);
        }
        return result;
    }
}
