using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

[BurstCompile]
public struct NewJobFor : IJobFor
{
	[ReadOnly] public NativeArray<int> InputValue;
	public NativeArray<int> OutputValue;

	public void Execute(int index)
	{
	}
}