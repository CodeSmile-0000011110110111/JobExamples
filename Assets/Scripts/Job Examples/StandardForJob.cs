using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

#pragma warning disable 0649

public class StandardForJob : MonoBehaviour
{
	private const int arraySize = 5000;

	private JobHandle backgroundJobHandle;
	private NativeArray<int> inputValue;
	private NativeArray<int> outputValue;

	private void Update()
	{
		inputValue = new NativeArray<int>(arraySize, Allocator.TempJob);
		outputValue = new NativeArray<int>(arraySize, Allocator.TempJob);
		var job = new BackgroundForJobWithInputOutputParams
		{
			InputValue = inputValue,
			OutputValue = outputValue
		};

		// Schedule entire for job on a single background thread
		backgroundJobHandle = job.Schedule(inputValue.Length, new JobHandle());
	}

	private void LateUpdate()
	{
		backgroundJobHandle.Complete();

		// read the output value and do something with it:
		Debug.Log("StandardForJob: Job output value = " + outputValue[0] + ", " + outputValue[1] + ", " + outputValue[2] + ", " + outputValue[3] + ", ..");

		// do not forget to dispose of the temp arrays
		outputValue.Dispose();
		inputValue.Dispose();
	}

	[BurstCompile]
	private struct BackgroundForJobWithInputOutputParams : IJobFor
	{
		[ReadOnly] public NativeArray<int> InputValue;
		public NativeArray<int> OutputValue;

		// The index parameter is new - it's essentially the 'i' of a for loop ;)
		public void Execute(int index)
		{
			// perform the array lookup / assignment task here
			OutputValue[index] = InputValue[index] + index;
			OutputValue[0] = OutputValue[0] + index;
		}
	}
}