using Azure.Identity;
using NServiceBus;

namespace LearnerDataStorybook.Services;

public static class NServiceBusService
{
    public static async Task<IEndpointInstance> CreateEndpointAsync(string serviceBusNamespace)
    {
        var config = new EndpointConfiguration("learner-data-storybook");
        config.EnableInstallers();
        config.SendOnly();

        var transport = config.UseTransport<AzureServiceBusTransport>();
        transport.CustomTokenCredential(serviceBusNamespace, new DefaultAzureCredential());

        config.UseSerialization<SystemJsonSerializer>();

        config.Conventions()
            .DefiningEventsAs(t => t.Name.EndsWith("Event"))
            .DefiningCommandsAs(t => t.Namespace != null && t.Name.EndsWith("Command"));

        return await Endpoint.Start(config).ConfigureAwait(false);
    }
}
