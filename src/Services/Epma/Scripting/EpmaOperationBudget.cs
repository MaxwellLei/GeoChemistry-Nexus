using System;
using System.Diagnostics;
using Jint;
namespace GeoChemistryNexus.Services.Epma;
/// <summary>Budget covers all loads, calls and serialization in a single engine operation.
/// Jint resets its standard constraints between Execute/Invoke; this budget intentionally does not reset.
/// The per-row default is small; the once-per-run <c>prepare</c> entry gets a larger one because it sees every row.</summary>
internal sealed class EpmaOperationBudget(int maxStatements = EpmaOperationBudget.RowStatements, int maxSeconds = EpmaOperationBudget.RowSeconds) : Constraint
{
    public const int RowStatements = 1_000_000;
    public const int RowSeconds = 5;
    public const int PrepareStatements = 20_000_000;
    public const int PrepareSeconds = 20;
    private readonly long _started=Stopwatch.GetTimestamp();
    private readonly long _allocated=GC.GetAllocatedBytesForCurrentThread();
    private int _statements;
    public override void Check()
    {
        if(++_statements>maxStatements)throw new InvalidOperationException($"EPMA script exceeds {maxStatements} statements.");
        if(Stopwatch.GetElapsedTime(_started)>TimeSpan.FromSeconds(maxSeconds))throw new InvalidOperationException($"EPMA script exceeds {maxSeconds} seconds.");
        if(GC.GetAllocatedBytesForCurrentThread()-_allocated>64L*1024*1024)throw new InvalidOperationException("EPMA script exceeds 64 MiB allocations.");
    }
    public override void Reset(){ }
}
