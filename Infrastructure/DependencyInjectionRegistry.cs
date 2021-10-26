using AzureEmailQueueTrigger.MessageSerializer;
using AzureEmailQueueTrigger.QueueConnection;
using Microsoft.Extensions.DependencyInjection;

namespace AzureEmailQueueTrigger.Infrastructure
{
	public static class DependencyInjectionRegistry
	{
		public static IServiceCollection AddAzureQueueLibrary(this IServiceCollection services, string queueConnectionString)
		{
			services.AddSingleton(new QueueConfig(queueConnectionString));
			services.AddSingleton<ICloudQueueClientFactory, CloudQueueClientFactory>();
			services.AddSingleton<IMessageSerializer, JsonMessageSerializer>();
			services.AddTransient<IQueueCommunicator, QueueCommunicator>();

			return services;
		}
	}
}
