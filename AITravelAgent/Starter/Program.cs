using AITravelAgent;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;

string filePath = Path.GetFullPath("appsettings.json");
IConfigurationRoot config = new ConfigurationBuilder()
    .AddJsonFile(filePath)
    .Build();

// Set your values in appsettings.json
string modelId = config["modelId"]!;
string endpoint = config["endpoint"]!;
string apiKey = config["apiKey"]!;

// Create a kernel with Azure OpenAI chat completion
IKernelBuilder builder = Kernel.CreateBuilder();
builder.AddAzureOpenAIChatCompletion(modelId, endpoint, apiKey);

// Build the kernel
Kernel kernel = builder.Build();
IChatCompletionService chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
kernel.Plugins.AddFromType<FlightBookingPlugin>("FlightBooking");
kernel.ImportPluginFromType<CurrencyConverterPlugin>();
kernel.FunctionInvocationFilters.Add(new PermissionFilter());

OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new() {
    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
};

ChatHistory history = [];
history.AddSystemMessage("Before providing destination recommendations, ask the user if they have a budget for their trip.");

async Task GetReply() {
    ChatMessageContent reply = await chatCompletionService.GetChatMessageContentAsync(
        history,
        executionSettings: openAIPromptExecutionSettings,
        kernel: kernel
    );

    Console.WriteLine("Assistant: " + reply.ToString());
    history.AddAssistantMessage(reply.ToString());
}

string hbprompt = """
<message role="system">Instructions: Before providing the the user with a travel itinerary, ask how many days their trip is.</message>
<message role="user">I'm going to Rome. Can you create an itinerary for me?</message>
<message role="assistant">Sure, how many days is your trip?</message>
<message role="user">{{input}}</message>
""";

HandlebarsPromptTemplateFactory templateFactory = new();
PromptTemplateConfig promptTemplateConfig = new() {
    Template = hbprompt,
    TemplateFormat = "handlebars",
    Name = "CreateItinerary",
};

KernelFunction function = kernel.CreateFunctionFromPrompt(promptTemplateConfig, templateFactory);
KernelPlugin plugin = kernel.CreatePluginFromFunctions("TravelItinerary", [function]);
kernel.Plugins.Add(plugin);

Console.WriteLine("Press enter to exit");
Console.WriteLine("Assistant: How may I help you?");
Console.Write("User: ");

string input = Console.ReadLine()!;

while (input != "") {
    history.AddUserMessage(input);
    await GetReply();
    Console.Write("User: ");
    input = Console.ReadLine()!;
}

Console.ReadLine();
