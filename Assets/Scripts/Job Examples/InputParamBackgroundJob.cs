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

public class InputParamBackgroundJob : MonoBehaviour
{
	private JobHandle backgroundJobHandleA;

	[SerializeField] private int JobInputValue;

	// Notes about scheduling:
	// - calling Complete() right after Schedule() will essentially block the main thread and void any benefits of using jobs
	// - it is important where you call Schedule() (S) and Complete() (C), here are some good examples:
	//    S: Awake/Start/OnEnable / C: Coroutine after some frames/seconds => spread some heavy init code over the first couple updates/frames
	//    S: Update/FixedUpdate / C: LateUpdate/OnPreRender => spread job's work over the course of the current frame 
	//    S: LateUpdate / Update/FixedUpdate => spread job's work over to the next frame, ie perform work during rendering the current frame
	private void LateUpdate()
	{
		// schedule the job here, it will begin its work immediately on a background thread
		backgroundJobHandleA = new BackgroundJobWithInputParam {InputValue = JobInputValue}.Schedule();

		// job is now running in background ...
	}

	private void FixedUpdate()
	{
		// complete ensures that the job is done by now, if not, this will wait on the main thread for the job to complete
		backgroundJobHandleA.Complete();

		// execution on main thread continues here after job has completed ...
	}

	// Attributing jobs with BurstCompile allows potentially significant speed improviements, but also imposes some restrictions
	[BurstCompile]
	// This job is declared inline in this class for convenience and because it's probably tightly coupled.
	// You can also define jobs outside classes as public, perhaps even placing it in its own file.
	private struct BackgroundJobWithInputParam : IJob
	{
		// ReadOnly tells the compiler that we will not change this value, allowing it to better optimise the code
		[ReadOnly] public int InputValue;

		// automatically called after scheduling the job
		public void Execute()
		{
			// perform background task here
			// Note: you cannot access game objects & components from within here!
			// You also cannot concatenate strings, hence the InputValue is purposefully not logged here 
			Debug.Log("BackgroundJobWithInputParam: Execute()");

			// Note: due to BurstCompile these lines will raise a runtime error as they allocate managed memory (a new string)
			//Debug.Log(InputValue);
			//Debug.Log(InputValue.ToString());
		}
	}
}