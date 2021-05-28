using System.Collections.Generic;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.Models.LifeCycleEvents;

namespace SimpleWfTest
{
	public class SynchronousWorkflowExecutionResult
	{
		public string WorkflowId { get; set; }
		public string WorkflowInstanceId { get; set; }
		public string Reference { get; set; }
		public LifeCycleEvent LastLifeCycleEvent { get; set; }
	}

	public class SynchronousWorkflowExecutionUtility
	{
		private readonly IWorkflowHost _host;
		private readonly ILifeCycleEventHub _hub;

		private readonly Dictionary<string, TaskCompletionSource<SynchronousWorkflowExecutionResult>>
			_completionSources = new Dictionary<string, TaskCompletionSource<SynchronousWorkflowExecutionResult>>();
			
		public SynchronousWorkflowExecutionUtility(IWorkflowHost host, ILifeCycleEventHub hub)
		{
			_host = host;

			_hub = hub;
			_hub.Subscribe(HandleWorkflowEvent);
		}

		private void HandleWorkflowEvent(LifeCycleEvent @event)
		{
			switch (@event) {
				case WorkflowCompleted completed:
				case WorkflowTerminated terminated:
				case WorkflowError error:
					if (_completionSources.ContainsKey(@event.WorkflowInstanceId)) {
						var completionSource = _completionSources[@event.WorkflowInstanceId];
						var result = (SynchronousWorkflowExecutionResult) completionSource.Task.AsyncState;
						result.LastLifeCycleEvent = @event;
						completionSource.SetResult(result);
					}

					break;
			}
		}

		public async Task<SynchronousWorkflowExecutionResult> StartWorkflowAndWait(string workflowId,
			int? version = null, object data = null, string reference = null) 
		{
			var result = new SynchronousWorkflowExecutionResult() {
				WorkflowId = workflowId,
				Reference = reference
			};

			var completionSource = new TaskCompletionSource<SynchronousWorkflowExecutionResult>(result);
			var instanceId = await _host.StartWorkflow(workflowId, version, data, reference);
			result.WorkflowInstanceId = instanceId;

			_completionSources.Add(instanceId, completionSource);
			return await completionSource.Task;
		}
	}
}