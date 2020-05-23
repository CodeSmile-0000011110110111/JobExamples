using System.Linq;
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

public class PersistentInputOutputParamBackgroundJob : MonoBehaviour
{
	private JobHandle backgroundJobHandleA;
	private JobHandle backgroundJobHandleB;

	// if jobs run permanently (ie every frame) it makes sense (avoid frequent alloc/dealloc) to create and assign
	// both jobs and required native collections as fields, allocating them in Awake/Start and disposing them in OnDestroy
	private BackgroundJobWithInputOutputParams jobA;
	private BackgroundJobWithInputOutputParams jobB;

	[SerializeField] private int JobInputValue;
	private NativeArray<int> outputValueA;
	private NativeArray<int> outputValueB;

	private void Awake()
	{
		// persistent allocator allows us to re-use the same collection indefinetely (but don't forget to Dispose() them!)
		// Note: native collections cannot be used as field initializers! You have to create them in Awake/Start or any other method.
		Debug.Log("BackgroundJobWithInputOutputParams: allocating persistent native collections");
		outputValueA = new NativeArray<int>(1, Allocator.Persistent);
		outputValueB = new NativeArray<int>(1, Allocator.Persistent);
		jobA.OutputValue = outputValueA;
		jobB.OutputValue = outputValueB;
		jobA.InputValueToAddEveryFrame = 123;
		jobB.InputValueToAddEveryFrame = 1;
	}

	private void OnDestroy()
	{
		// ensure the jobs complete before destroying the instance
		Debug.Log("BackgroundJobWithInputOutputParams: waiting for jobs to complete");
		backgroundJobHandleA.Complete();
		backgroundJobHandleB.Complete();

		// disposing native collections in OnDestroy is a good fit for native collections with Allocator.Persistent
		Debug.Log("BackgroundJobWithInputOutputParams: disposing native collections");
		outputValueA.Dispose();
		outputValueB.Dispose();
	}

	// Notes about scheduling:
	// - calling Complete() right after Schedule() will essentially block the main thread and void any benefits of using jobs
	// - it is important where you call Schedule() (S) and Complete() (C), here are some good examples:
	//    S: Awake/Start/OnEnable / C: Coroutine after some frames/seconds => spread some heavy init code over the first couple updates/frames
	//    S: Update/FixedUpdate / C: LateUpdate/OnPreRender => spread job's work over the course of the current frame 
	//    S: LateUpdate / Update/FixedUpdate => spread job's work over to the next frame, ie perform work during rendering the current frame
	private void Update()
	{
		// assign the new input value, output value has already been assigned and is persistent
		jobA.InputValue = JobInputValue;
		// must add itself as dependency to write-protect the now-persistent outputValueA collection
		backgroundJobHandleA = jobA.Schedule(backgroundJobHandleA);


		jobB.InputValue = JobInputValue;
		jobB.InputValueToAddEveryFrame = outputValueB[0]; // use output as another input in order to increment values
		// must add itself as dependency to write-protect the now-persistent outputValueA collection
		backgroundJobHandleB = jobB.Schedule(backgroundJobHandleB);


		// there are now two independent jobs running in the background in parallel ...
	}

	private void LateUpdate()
	{
		// complete ensures that the job is done by now, if not, this will wait on the main thread for the job to complete
		// it shouldn't matter in which order you complete the job handles
		backgroundJobHandleA.Complete();
		backgroundJobHandleB.Complete();

		// execution on main thread continues here after both jobs have completed

		// read the output value and do something with it:
		Debug.Log("PersistentInputOutputParamBackgroundJob: Job A persistent output value = " + outputValueA[0]);
		Debug.Log("PersistentInputOutputParamBackgroundJob: Job B persistent output value = " +
		          outputValueB.First()); // LINQ also works, though this will be slower
	}

	// Attributing jobs with BurstCompile allows potentially significant speed improviements, but also imposes some restrictions
	[BurstCompile]
	// This job is declared inline in this class for convenience and because it's probably tightly coupled.
	// You can also define jobs outside classes as public, perhaps even placing it in its own file.
	private struct BackgroundJobWithInputOutputParams : IJob
	{
		[ReadOnly] public int InputValue;
		public int InputValueToAddEveryFrame;
		public NativeArray<int> OutputValue;

		// automatically called after scheduling the job
		public void Execute()
		{
			// perform background task here
			// Note: you cannot access game objects & components from within here!

			OutputValue[0] = InputValue + InputValueToAddEveryFrame;

			// Note: even though the job instance is stored in a field, we cannot keep track of internal job state
			// by modifying the job's fields - in order to do that, assign any changed input values before re-scheduling the job
			InputValueToAddEveryFrame = InputValueToAddEveryFrame - 111;
		}
	}
}