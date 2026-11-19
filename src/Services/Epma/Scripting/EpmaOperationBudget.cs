using System;
using System.Diagnostics;
using Jint;
namespace GeoChemistryNexus.Services.Epma;
/// <summary>Budget covers all loads, calls and serialization in a single engine operation.
/// Jint resets its standard constraints between Execute/Invoke; this budget intentionally does not reset.</summary>
internal sealed class EpmaOperationBudget : Constraint
{
    private readonly long _started=Stopwatch.GetTimestamp();
    private readonly long _allocated=GC.GetAllocatedBytesForCurrentThread();
    private int _statements;
    public override void Check()
    {
        if(++_statements>1_000_000)throw new InvalidOperationException("EPMA script exceeds 1000000 statements.");
        if(Stopwatch.GetElapsedTime(_started)>TimeSpan.FromSeconds(5))throw new InvalidOperationException("EPMA script exceeds 5 seconds.");
        if(GC.GetAllocatedBytesForCurrentThread()-_allocated>64L*1024*1024)throw new InvalidOperationException("EPMA script exceeds 64 MiB allocations.");
    }
    public override void Reset(){ }
}
