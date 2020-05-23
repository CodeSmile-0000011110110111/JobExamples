using Unity.Burst;
using Unity.Jobs;
using UnityEngine;

#pragma warning disable 0649

/*
    Required packages:
        - Burst (dependency: Mathematics)
        - Jobs (dependencies: Collections, Mathematics)
 */

// Example of a background job that takes neither input nor returns an output. Probably not very useful but results in the cleanest example.
public class NoParamsBackgroundJob : MonoBehaviour
{
	private JobHandle backgroundJobHandle;

	// Notes about scheduling:
	// - calling Complete() right after Schedule() will essentially block the main thread and void any benefits of using jobs
	// - it is important where you call Schedule() (S) and Complete() (C), here are some good examples:
	//    S: Awake/Start/OnEnable / C: Coroutine after some frames/seconds => spread some heavy init code over the first couple updates/frames
	//    S: Update/FixedUpdate / C: LateUpdate/OnPreRender => spread job's work over the course of the current frame 
	//    S: LateUpdate / Update/FixedUpdate => spread job's work over to the next frame, ie perform work during rendering the current frame
	private void Update()
	{
		// schedule the job here, it will begin its work immediately on a background thread
		backgroundJobHandle = new BackgroundJobWithoutParams().Schedule();
	}

	private void LateUpdate()
	{
		// complete ensures that the job is done by now, if not, this will wait on the main thread for the job to complete
		backgroundJobHandle.Complete();

		// execution on main thread continues ...
	}

	// Attributing jobs with BurstCompile allows potentially significant speed improviements, but also imposes some restrictions. 
	[BurstCompile]
	// This job is declared inline in this class for convenience and because it's probably tightly coupled.
	// You can also define jobs outside classes as public, perhaps even placing it in its own file.
	private struct BackgroundJobWithoutParams : IJob
	{
		// automatically called after scheduling the job
		public void Execute()
		{
			// perform background task here
			// Note: you cannot access game objects & components from within here!
			Debug.Log("BackgroundJobWithoutParams: Execute()");
		}
	}
}