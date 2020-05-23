using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

#pragma warning disable 0649

/*
    Required packages:
        - Burst (dependency: Mathematics)
        - Jobs (dependencies: Collections, Mathematics)
 */

public class InputOutputParamBackgroundJob : MonoBehaviour
{
	private JobHandle backgroundJobHandleA;
	private JobHandle backgroundJobHandleB;

	[SerializeField] private int JobInputValue;
	private NativeArray<int> outputValueA;
	private NativeArray<int> outputValueB;

	// Notes about scheduling:
	// - calling Complete() right after Schedule() will essentially block the main thread and void any benefits of using jobs
	// - it is important where you call Schedule() (S) and Complete() (C), here are some good examples:
	//    S: Awake/Start/OnEnable / C: Coroutine after some frames/seconds => spread some heavy init code over the first couple updates/frames
	//    S: Update/FixedUpdate / C: LateUpdate/OnPreRender => spread job's work over the course of the current frame 
	//    S: LateUpdate / Update/FixedUpdate => spread job's work over to the next frame, ie perform work during rendering the current frame
	private void Update()
	{
		// create the native array needed to get the output value(s) from a job
		// Note: even though we only need a single value as output we still have to use a native collection!
		outputValueA = new NativeArray<int>(1, Allocator.TempJob);
		// schedule the job here, it will begin its work immediately on a background thread
		backgroundJobHandleA = new BackgroundJobWithInputOutputParams
		{
			InputValue = JobInputValue,
			OutputValue = outputValueA
		}.Schedule();


		// create the native array needed to get the output value(s) from a job
		// Note: even though we only need a single value as output we still have to use a native collection!
		outputValueB = new NativeArray<int>(1, Allocator.TempJob);
		// this is the same as above but without struct initializer syntax - use whichever style suits you better
		var job = new BackgroundJobWithInputOutputParams();
		job.InputValue = JobInputValue;
		job.OutputValue = outputValueB;
		backgroundJobHandleB = job.Schedule();

		// there are now two independent jobs running in the background in parallel ...
	}

	private void LateUpdate()
	{
		// complete ensures that the job is done by now, if not, this will wait on the main thread for the job to complete
		backgroundJobHandleA.Complete();
		backgroundJobHandleB.Complete();

		// execution on main thread continues here after both jobs have completed

		// read the output value and do something with it:
		Debug.Log("InputOutputParamBackgroundJob: Job A output value = " + outputValueA[0]);
		Debug.Log("InputOutputParamBackgroundJob: Job B output value = " + outputValueB[0]);

		// do not forget to dispose of the temp arrays
		outputValueA.Dispose();
		outputValueB.Dispose();
	}

	// Attributing jobs with BurstCompile allows potentially significant speed improviements, but also imposes some restrictions
	[BurstCompile]
	// This job is declared inline in this class for convenience and because it's probably tightly coupled.
	// You can also define jobs outside classes as public, perhaps even placing it in its own file.
	private struct BackgroundJobWithInputOutputParams : IJob
	{
		[ReadOnly] public int InputValue;
		public NativeArray<int> OutputValue;

		// automatically called after scheduling the job
		public void Execute()
		{
			// perform background task here
			// Note: you cannot access game objects & components from within here!

			OutputValue[0] = InputValue + 123;
		}
	}
}