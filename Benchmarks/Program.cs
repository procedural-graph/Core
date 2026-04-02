using BenchmarkDotNet.Running;
using ProceduralGraph.Mathematics;
using System;

const int SampleSize = 10_000_000;
var random = new Random(42);

Console.WriteLine($"Running error analysis across {SampleSize:N0} samples...\n");

AnalyzeFloatReciprocal(SampleSize, random);
AnalyzeDoubleReciprocal(SampleSize, random);
AnalyzeFloatReciprocalSqrt(SampleSize, random);
AnalyzeDoubleReciprocalSqrt(SampleSize, random);

Console.ReadKey();

BenchmarkRunner.Run(typeof(Program).Assembly);

static void AnalyzeFloatReciprocal(int samples, Random random)
{
    double maxRelError = 0.0;
    double sumRelError = 0.0;

    for (int i = 0; i < samples; i++)
    {
        // Generate numbers across different magnitudes (e.g., 0.001 to 100,000)
        float value = (float)(random.NextDouble() * Math.Pow(10, random.Next(-3, 6)));
        if (value == 0) continue;

        float exact = 1f / value;
        float approx = FastMath.ReciprocalEstimate(value);

        double relError = Math.Abs((exact - approx) / exact);

        if (relError > maxRelError) maxRelError = relError;
        sumRelError += relError;
    }

    PrintResults("Float Reciprocal", maxRelError, sumRelError / samples);
}

static void AnalyzeDoubleReciprocal(int samples, Random random)
{
    double maxRelError = 0.0;
    double sumRelError = 0.0;

    for (int i = 0; i < samples; i++)
    {
        double value = random.NextDouble() * Math.Pow(10, random.Next(-3, 6));
        if (value == 0) continue;

        double exact = 1.0 / value;
        double approx = FastMath.ReciprocalEstimate(value);

        double relError = Math.Abs((exact - approx) / exact);

        if (relError > maxRelError) maxRelError = relError;
        sumRelError += relError;
    }

    PrintResults("Double Reciprocal", maxRelError, sumRelError / samples);
}

static void AnalyzeFloatReciprocalSqrt(int samples, Random random)
{
    double maxRelError = 0.0;
    double sumRelError = 0.0;

    for (int i = 0; i < samples; i++)
    {
        float value = (float)(random.NextDouble() * Math.Pow(10, random.Next(-3, 6)));
        if (value <= 0) continue;

        float exact = 1f / (float)Math.Sqrt(value);
        float approx = FastMath.ReciprocalSqrtEstimate(value);

        double relError = Math.Abs((exact - approx) / exact);

        if (relError > maxRelError) maxRelError = relError;
        sumRelError += relError;
    }

    PrintResults("Float Reciprocal Sqrt", maxRelError, sumRelError / samples);
}

static void AnalyzeDoubleReciprocalSqrt(int samples, Random random)
{
    double maxRelError = 0.0;
    double sumRelError = 0.0;

    for (int i = 0; i < samples; i++)
    {
        double value = random.NextDouble() * Math.Pow(10, random.Next(-3, 6));
        if (value <= 0) continue;

        double exact = 1.0 / Math.Sqrt(value);
        double approx = FastMath.ReciprocalSqrtEstimate(value);

        double relError = Math.Abs((exact - approx) / exact);

        if (relError > maxRelError) maxRelError = relError;
        sumRelError += relError;
    }

    PrintResults("Double Reciprocal Sqrt", maxRelError, sumRelError / samples);
}

static void PrintResults(string name, double maxError, double avgError)
{
    Console.WriteLine($"--- {name} ---");
    // Format as percentage with scientific notation fallback for very small numbers
    Console.WriteLine($"Max Relative Error: {maxError:P6}  ({maxError:E3})");
    Console.WriteLine($"Avg Relative Error: {avgError:P6}  ({avgError:E3})\n");
}