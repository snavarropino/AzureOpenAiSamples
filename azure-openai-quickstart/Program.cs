using Azure.AI.OpenAI;
using Azure;
using System.Text.Json;

//https://learn.microsoft.com/en-us/dotnet/api/overview/azure/ai.openai-readme?view=azure-dotnet-preview
var getWeatherTool = new ChatCompletionsFunctionToolDefinition()
{
    Name = "get_current_weather",
    Description = "Get the current weather in a given location",
    Parameters = BinaryData.FromObjectAsJson(
        new
        {
            Type = "object",
            Properties = new
            {
                Location = new
                {
                    Type = "string",
                    Description = "The city and state, e.g. San Francisco, CA",
                },
                Unit = new
                {
                    Type = "string",
                    Enum = new[] { "celsius", "fahrenheit" },
                }
            },
            Required = new[] { "location" },
        },
        new JsonSerializerOptions() {  PropertyNamingPolicy = JsonNamingPolicy.CamelCase }),
};

var getReplenishment = new ChatCompletionsFunctionToolDefinition()
{
    Name = "get_replenishment",
    Description = "Get replenishment for point of sale and product",
    Parameters = BinaryData.FromObjectAsJson(
        new
        {
            Type = "object",
            Properties = new
            {
                PointOfSale = new
                {
                    Type = "string",
                    Description = "point of sale code",
                },
                Product = new
                {
                    Type = "string",
                    Description = "Product code",
                },
            },
            Required = new[]
            {
                "PointOfSale", "Product"
            },
        },
        new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }),
};

var getProduct = new ChatCompletionsFunctionToolDefinition()
{
    Name = "get_product",
    Description = "Get product code by description",
    Parameters = BinaryData.FromObjectAsJson(
        new
        {
            Type = "object",
            Properties = new
            {
                ProductDescription = new
                {
                    Type = "string",
                    Description = "Product description",
                },
            },
            Required = new[]
            {
                "ProductDescription"
            },
        },
        new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }),
};


//string endpoint = "https://oa-fc.openai.azure.com/";
//string key = "a2125c6833684d85ad7b7f8a09eefd9d";

string endpoint = "https://aoi-03042024.openai.azure.com";
string key = "61daaf33a29e428fbde39ec8e3ad02ac";

var client = new OpenAIClient(new Uri(endpoint), new AzureKeyCredential(key));

var chatCompletionsOptions = new ChatCompletionsOptions
{
    //DeploymentName = "gpt-35t",
    DeploymentName = "gpt-4-0613-2",
    //Messages = { new ChatRequestUserMessage("What's the weather like in Boston?") },
    //Messages = { new ChatRequestUserMessage("Dame la reposición para la tienda 005 y el producto PAN7XL") },
    Messages = { new ChatRequestUserMessage("Dame la reposición para la tienda 005 y el producto pantalones azules") },
    Tools = { getWeatherTool, getReplenishment, getProduct },
    //Tools = { getWeatherTool },
    ToolChoice =  ChatCompletionsToolChoice.Auto
};
var finished = false;
Response<ChatCompletions> chatResponse = await client.GetChatCompletionsAsync(chatCompletionsOptions);
ChatChoice responseChoice = chatResponse.Value.Choices[0];
do
{
    if (responseChoice.FinishReason == CompletionsFinishReason.ToolCalls)
    {
        // Add the assistant message with tool calls to the conversation history
        ChatRequestAssistantMessage toolCallHistoryMessage = new(responseChoice.Message);
        chatCompletionsOptions.Messages.Add(toolCallHistoryMessage);

        // Add a new tool message for each tool call that is resolved
        foreach (ChatCompletionsToolCall toolCall in responseChoice.Message.ToolCalls)
        {
            chatCompletionsOptions.Messages.Add(GetToolCallResponseMessage(toolCall));
        }

        // Now make a new request with all the messages thus far, including the original
        Response<ChatCompletions> toolResponse = await client.GetChatCompletionsAsync(chatCompletionsOptions);
        responseChoice = toolResponse.Value.Choices[0];

    }
    else
    {
        finished = true;
        Console.WriteLine(responseChoice.Message.Content);
    }
} while (!finished);

return;

// Purely for convenience and clarity, this standalone local method handles tool call responses.
ChatRequestToolMessage GetToolCallResponseMessage(ChatCompletionsToolCall toolCall)
{
    var functionToolCall = toolCall as ChatCompletionsFunctionToolCall;
    if (functionToolCall?.Name == getWeatherTool.Name)
    {
        // Validate and process the JSON arguments for the function call
        string unvalidatedArguments = functionToolCall.Arguments;
        var functionResultData = (object)null; // GetYourFunctionResultData(unvalidatedArguments);
        // Here, replacing with an example as if returned from "GetYourFunctionResultData"
        functionResultData = "31 celsius";
        return new ChatRequestToolMessage(functionResultData.ToString(), toolCall.Id);
    }
    else if (functionToolCall?.Name == getProduct.Name)
    {
        return new ChatRequestToolMessage("AB45", toolCall.Id);
    }
    else if (functionToolCall?.Name == getReplenishment.Name)
    {
        return new ChatRequestToolMessage(45.ToString(), toolCall.Id);
    }
    else
    {
        // Handle other or unexpected calls
        throw new NotImplementedException();
    }
}



CompletionsOptions completionsOptions = new()
{
    DeploymentName = "gpt-35t", 
    Prompts = { "When was Microsoft founded?" },
};
Response<Completions> completionsResponse = client.GetCompletions(completionsOptions);
var completion = completionsResponse.Value.Choices[0].Text;
Console.WriteLine($"Chatbot: {completion}");


