using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

[BurstCompile]
public struct NewJob : IJob
{
	[ReadOnly] public int InputValue;
	public NativeArray<int> OutputValue;

	public void Execute()
	{
	}
}