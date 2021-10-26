# Evospike.AzureEmailQueueTrigger
This package is designed to use Azure Queue Storage in a very simple way to send emails.

It allows you to configure Azure Storage Queue Trigger in a very simple way, to queue an email and then be processed by an azure function

## Web Application

### `appsettings.json` configuration

The file path and other settings can be read from JSON configuration if desired.

In `appsettings.json` add a `"AzureWebJobsStorage"` properties:

```json
{
    //"AzureWebJobsStorage": "{YOUR STORAGE CONNECTION}"
    "AzureWebJobsStorage": "UseDevelopmentStorage=true"
}
```

And then pass the configuration section to the next methods:

```csharp
services.AddAzureQueueLibrary(Configuration["AzureWebJobsStorage"]);
```

Example of a controller using dependency injection services

```csharp
public class ItemsController : ControllerBase
{
    private readonly IQueueCommunicator _queueCommunicator;

    public ItemsController(IQueueCommunicator queueCommunicator)
    {
        _queueCommunicator = queueCommunicator;
    }

    [HttpPost]
	public async Task<IActionResult> ContactUs(string contactName, string emailAddress)
	{
		var thankYouEmail = new SendEmailCommand()
		{
			To = emailAddress,
			Subject = "Thank you for reaching out",
			Body = "We will contact you shortly"
		};
		await _queueCommunicator.SendAsync(thankYouEmail);

		var adminEmail = new SendEmailCommand()
		{
			To = "enmanuellopez02m@gmail.com",
			Subject = "New Contact",
			Body = $"{contactName} has reached out via contact form. Please respond back at {emailAddress}"
		};
		await _queueCommunicator.SendAsync(adminEmail);

		ViewBag.Message = "Thank you we've received your message =)";
		return View();
	}
}
```

## Azure function | Example using smtp

The file path and other settings can be read from JSON configuration if desired.

In `local.settings.json` add a `"AzureStorageSetting"` properties:

```json
{
	"IsEncrypted": false,
	  "Values": {
		"AzureWebJobsStorage": "UseDevelopmentStorage=true",
		"FUNCTIONS_WORKER_RUNTIME": "dotnet"
	  }
	"EmailHost": "smtp.gmail.com",
	"EmailPort": "587",
	"EmailSender": "{YOUR EMAIL}",
	"EmailPassword": "{YOUR PASSWORD}"
}
```

Create the following class in the new folder `Infrastructure`

```csharp
using AwesomeShop.AzureFunctions.Email;
using AwesomeShop.AzureQueueLibrary.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AzureFunctions.Infrastructure
{
	public sealed class DIContainer
	{
		private static readonly IServiceProvider _instance = Build();
		public static IServiceProvider Instance => _instance;

		static DIContainer()
		{ }

		private DIContainer()
		{ }

		private static IServiceProvider Build()
		{
			var services = new ServiceCollection();
			var configuration = new ConfigurationBuilder()
				.SetBasePath(Directory.GetCurrentDirectory())
				.AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
				.AddEnvironmentVariables()
				.Build();

			services.AddSingleton
			(
				new EmailConfig
				(
					configuration["EmailHost"],
					Convert.ToInt32(configuration["EmailPort"]),
					configuration["EmailSender"],
					configuration["EmailPassword"]
				)
			);

			services.AddSingleton<ISendEmailCommandHandler, SendEmailCommandHandler>();
			services.AddAzureQueueLibrary(configuration["AzureWebJobsStorage"]);
			return services.BuildServiceProvider();
		}
	}
}
```

Create the following class in the new folder `Email`

```csharp
namespace AzureFunctions.Email
{
	public class EmailConfig
	{
		public string Host { get; set; }
		public int Port { get; set; }
		public string Sender { get; set; }
		public string Password { get; set; }

		public EmailConfig(string host, int port, string sender, string password)
		{
			if (host == null)
			{
				throw new ArgumentNullException(nameof(host));
			}

			if (sender == null)
			{
				throw new ArgumentNullException(nameof(sender));
			}

			if (password == null)
			{
				throw new ArgumentNullException(nameof(password));
			}

			Host = host;
			Port = port;
			Sender = sender;
			Password = password;
		}
	}
}
```

```csharp
using Evosiple.AzureEmailQueueTrigger.Messages;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace AwesomeShop.AzureFunctions.Email
{
	public interface ISendEmailCommandHandler
	{
		Task Handle(SendEmailCommand command);
	}
	public class SendEmailCommandHandler : ISendEmailCommandHandler
	{
		private readonly EmailConfig _emailConfig;

		public SendEmailCommandHandler(EmailConfig emailConfig)
		{
			_emailConfig = emailConfig;
		}

		public async Task Handle(SendEmailCommand command)
		{
			using (var client = new SmtpClient(_emailConfig.Host, _emailConfig.Port)
			{
				Credentials = new NetworkCredential(_emailConfig.Sender, _emailConfig.Password),
				EnableSsl = true
			})
			using(var message = new MailMessage(_emailConfig.Sender, command.To, command.Subject, command.Body))
			{
				await client.SendMailAsync(message);
				await Task.CompletedTask;
			}
		}
	}
}
```

```csharp
using System;
using System.Threading.Tasks;
using AzureFunctions.Infrastructure;
using Evosiple.AzureEmailQueueTrigger.Infrastructure;
using Evosiple.AzureEmailQueueTrigger.QueueConnection;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Evosiple.AzureEmailQueueTrigger.Messages;

namespace AzureFunctions.Email
{
    public static class EmailQueueTrigger
    {
        [FunctionName("EmailQueueTrigger")]
        public static async Task Run(
			[QueueTrigger(RouteNames.EmailBox, Connection = "AzureWebJobsStorage")]
			string message,
			ILogger log)
        {
			try
			{
				var queueCommunicator = DIContainer.Instance.GetService<IQueueCommunicator>();
				var command = queueCommunicator.Read<SendEmailCommand>(message);

				var handler = DIContainer.Instance.GetService<ISendEmailCommandHandler>();
				await handler.Handle(command);
			}
			catch (Exception ex)
			{
				log.LogError(ex, $"Something went wrong with the EmailQueueTrigger {message}");
				throw;
			}
        }
    }
}
```