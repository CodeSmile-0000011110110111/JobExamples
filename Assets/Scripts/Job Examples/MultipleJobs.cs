using System.Collections.Generic;
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

public class MultipleJobs : MonoBehaviour
{
	private const int jobCount = 1000;
	private NativeArray<JobHandle> jobHandles;

	[SerializeField] private int JobInputValue;
	private List<NativeArray<int>> outputValues;

	// Notes about scheduling:
	// - calling Complete() right after Schedule() will essentially block the main thread and void any benefits of using jobs
	// - it is important where you call Schedule() (S) and Complete() (C), here are some good examples:
	//    S: Awake/Start/OnEnable / C: Coroutine after some frames/seconds => spread some heavy init code over the first couple updates/frames
	//    S: Update/FixedUpdate / C: LateUpdate/OnPreRender => spread job's work over the course of the current frame 
	//    S: LateUpdate / Update/FixedUpdate => spread job's work over to the next frame, ie perform work during rendering the current frame
	private void Update()
	{
		jobHandles = new NativeArray<JobHandle>(jobCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
		outputValues = new List<NativeArray<int>>();

		for (var i = 0; i < jobCount; i++)
		{
			// create the native array needed to get the output value(s) from a job
			// Note: even though we only need a single value as output we still have to use a native collection!
			var outputValue = new NativeArray<int>(1, Allocator.TempJob);
			outputValues.Add(outputValue);

			jobHandles[i] = new BackgroundJobWithInputOutputParams
			{
				InputValue = JobInputValue,
				OutputValue = outputValue
			}.Schedule();
		}

		// make sure the jobs are going to start now
		JobHandle.ScheduleBatchedJobs();

		// there are now several independent jobs running in the background in parallel ...
	}

	private void LateUpdate()
	{
		// complete all jobs at once
		JobHandle.CompleteAll(jobHandles);
		jobHandles.Dispose();

		// execution on main thread continues here after both jobs have completed

		// read the output value and do something with it:
		Debug.Log("MultipleJobs: Job output value = " + outputValues[0][0]);

		// do not forget to dispose of the temp arrays
		for (var i = 0; i < jobCount; i++) outputValues[i].Dispose();
		outputValues = null;
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

			OutputValue[0] = InputValue + 999;
		}
	}
}