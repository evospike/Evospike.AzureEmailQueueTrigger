namespace AzureEmailQueueTrigger.Infrastructure
{
	public class QueueConfig
	{
		public string QueueConnectionString { get; set; }

		public QueueConfig() {}

		public QueueConfig(string queueConnectionString)
		{
			QueueConnectionString = queueConnectionString;
		}
	}
}
