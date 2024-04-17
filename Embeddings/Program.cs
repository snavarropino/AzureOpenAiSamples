using Azure;
using Azure.AI.OpenAI;
using static Qdrant.Client.Grpc.Conditions;
using Qdrant.Client;
using Qdrant.Client.Grpc;

//https://github.com/qdrant/qdrant-dotnet
string endpoint = "https://oa-fc.openai.azure.com/";
string key = "a2125c6833684d85ad7b7f8a09eefd9d";

AzureKeyCredential credentials = new (key);

OpenAIClient openAIClient = new (new Uri(endpoint), credentials);
var qdrantClient = new QdrantClient("localhost",6334);
await qdrantClient.CreateCollectionAsync("c1",
    new VectorParams { Size = 1536, Distance = Distance.Cosine });

await InsertEmbeddings(openAIClient, qdrantClient, "backup");

//var parrafo1 = @"
//La famosa banda de rock Nirvana se separó, en 1990, al menos por un tiempo. 
//Los integrantes declararon en diversos medios que no hubo una pelea, 
//sino que quieren trabajar un tiempo como solitas. Además, en una rueda de prensa, 
//el representante dijo que es probable que el año próximo los músicos se vuelvan a 
//juntar para hacer una gira";
//await InsertEmbedding(1,openAIClient, qdrantClient,parrafo1,"Nirvana");
//var parrafo2 = @"
//Los derechos humanos son derechos inherentes a todas las personas, es decir, 
//que una persona tiene estos derechos por el hecho de ser persona. 
//Algunos de estos derechos son el derecho a la vida, el derecho a la libertad y 
//el derecho a la libre expresión.";
//await InsertEmbedding(2,openAIClient, qdrantClient,parrafo2,"DH");

//QUERY
EmbeddingsOptions embeddingOptions = new()
{
    DeploymentName = "emb",
    //Input = { "¿en qué año se separó Nirvana?"  },
    Input = { "¿hacemos backups de bases de datos de parámetros?"  },
};

var queryEmb = openAIClient.GetEmbeddings(embeddingOptions);

var queryResult = await qdrantClient.SearchAsync(
    "c1",
    queryEmb.Value.Data[0].Embedding.ToArray(),
    //filter: MatchKeyword("tag", "Nirvana"),
    limit: 5);

var chatClient = new OpenAIClient(new Uri(endpoint), new AzureKeyCredential(key));

//var prompt = @$"
//Un usuario ha preguntado acerca del grupo Nirvana.

//Respondele usando la siguiente referencia, sin inventarte información:

//{queryResult.First().Payload}
//";

var prompt = @$"
Un usuario ha preguntado ¿hacemos backups de bases de datos de parámetros?.

Respondele usando la siguiente referencia, sin inventarte información:

{queryResult.First().Payload}
";

var chatCompletionsOptions = new ChatCompletionsOptions
{
    DeploymentName = "gpt-35t",
    Messages = { new ChatRequestUserMessage(prompt) },
};
Response<ChatCompletions> chatResponse = await chatClient.GetChatCompletionsAsync(chatCompletionsOptions);
ChatChoice responseChoice = chatResponse.Value.Choices[0];
Console.WriteLine(responseChoice.Message.Content);
return;    

async Task InsertEmbeddings(OpenAIClient openAiClient, QdrantClient qdrantClient, string tag)
{
    //read all markdown files in the current directory and insert embeddings
    ulong id = 1;
    foreach (var markdownFile in MarkdownReader.ReadAll())
    {
        await InsertEmbedding(id++, openAiClient, qdrantClient, markdownFile.Item2, markdownFile.Item1);
    }
}

async Task InsertEmbedding(ulong id,OpenAIClient openAiClient, QdrantClient qdrantClient, string text, string tag)
{
    EmbeddingsOptions embeddingOptions = new()
    {
        DeploymentName = "emb",
        Input = { text  }
    };

    var returnValue = openAiClient.GetEmbeddings(embeddingOptions);

    var vectors = new List<PointStruct>();
    foreach (var data in returnValue.Value.Data)
    {
        //Console.WriteLine(data.Embedding.ToArray());
        var point= new PointStruct
        {
            Id = id,
            Vectors = data.Embedding.ToArray(),
            Payload = { 
                ["original"] = text , 
                ["reference"] = tag
            } 
        }; 
        vectors.Add(point);
    }
    await qdrantClient.UpsertAsync("c1", vectors);
}