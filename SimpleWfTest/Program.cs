using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Models.LifeCycleEvents;
using WorkflowCore.Services;

namespace SimpleWfTest
{
	class Program
	{
		static async Task Main(string[] args)
		{
			await WfCoreMain();
		}

		private static async Task WfCoreMain()
		{
			IServiceProvider serviceProvider = ConfigureServices();

			//start the workflow host
			IWorkflowHost host = serviceProvider.GetService<IWorkflowHost>();
			host.RegisterWorkflow<HelloWorldWorkflow>();
			host.Start();

			var hub = serviceProvider.GetService<ILifeCycleEventHub>();
			var syncWorkflowExecUtility = new SynchronousWorkflowExecutionUtility(host, hub);

			hub.Subscribe(@event => {
				if (@event is WorkflowCompleted) { // why does this never trigger?
					Console.WriteLine($"{nameof(HelloWorldWorkflow)} is ending");
				}
			});

			//var syncWfResult = await syncWorkflowExecUtility.StartWorkflowAndWait(nameof(HelloWorldWorkflow));
			var id = await host.StartWorkflow(nameof(HelloWorldWorkflow));

			//Console.WriteLine($"{nameof(HelloWorldWorkflow)} should've ended.");
			Console.ReadLine();
			host.Stop();
		}

		private static IServiceProvider ConfigureServices()
		{
			//setup dependency injection
			IServiceCollection services = new ServiceCollection();
			services.AddLogging();
			services.AddWorkflow();
			services.AddTransient<HelloWorldStep>();
			services.AddTransient<GoodbyeWorldStep>();

			var serviceProvider = services.BuildServiceProvider();

			return serviceProvider;
		}
	}

	class HelloWorldWorkflow : IWorkflow
	{
		public void Build(IWorkflowBuilder<object> builder)
		{
			IStepBuilder<object, HelloWorldStep> stepBuilder = builder.StartWith<HelloWorldStep>();
			IStepBuilder<object, GoodbyeWorldStep> then = stepBuilder.Then<GoodbyeWorldStep>();
			IStepBuilder<object, GoodbyeWorldStep> endWorkflow = then.EndWorkflow();
		}

		public string Id { get; } = nameof(HelloWorldWorkflow);
		public int Version { get; } = 1;
	}

	class HelloWorldStep : StepBody
	{
		public override ExecutionResult Run(IStepExecutionContext context)
		{
			Console.WriteLine($"Hello world! {context.Workflow.Data}");
			return ExecutionResult.Next();
		}
	}

	class GoodbyeWorldStep : StepBody
	{
		public override ExecutionResult Run(IStepExecutionContext context)
		{
			Console.WriteLine($"Goodbye world!");
			return ExecutionResult.Next();
		}
	}
}