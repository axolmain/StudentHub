using System.Collections.Concurrent;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.SemanticFunctions;
using Microsoft.SemanticKernel.SkillDefinition;
using Newtonsoft.Json.Linq;

namespace Backend.Services.AiServices;

public class MultipleChoiceService
{
    private readonly IConfiguration _configuration;
    private readonly KernelService _kernelService;
    private IKernel _kernel;
    private List<ISKFunction> _multipleChoiceFunctions;
    private readonly TextEmbeddingService _textEmbeddingService;
    private readonly IUserAuthService _userAuthService;

    public MultipleChoiceService(IConfiguration configuration,
        KernelService kernelService, TextEmbeddingService textEmbeddingService, IUserAuthService userAuthService)
    {
        _configuration = configuration;
        _kernelService = kernelService;
        _textEmbeddingService = textEmbeddingService;
        _userAuthService = userAuthService;
    }

    private static List<ISKFunction> RegisterMultiplechoiceFunctions(IKernel kernel)
    {
        const string skPrompt = @"
Please make comprehensive and detailed multiple-choice test questions based on the most important parts of the following information. 
Each question should have four options, with only one being the correct answer. Make the questions range from easy to hard and format 
the questions in JSON, where each question includes four nested answers. Send only the JSON response; NO OTHER TEXT
{
  ""questions"": ""answer""
        {
            ""question"": ""...,""
                ""options"": [
                ""..."",
                ""..."",
                ""..."",
                ""...""
                ],
                ""answer"": ""...""
        },
        {...}
        ]
    }
```INFORMATION
{{$INFORMATION}}
```
";
        const string skPrompt2 = @"
Here is a comprehensive and detailed multiple-choice test questions where each question has four options, but 
only one is the correct answer. Check for duplicate questions or options within questions, and remove it so that there 
are only unique questions and options left. Send only the cleaned version of the JSON response; NO OTHER TEXT

```JSON
{{$INFORMATION}}
```
";

        PromptTemplateConfig promptConfig = new()
        {
            Completion =
            {
                MaxTokens = 5000,
                Temperature = 0.1,
                TopP = 0.1
            }
        };
        
        PromptTemplate basePromptTemplate = new(skPrompt, promptConfig, kernel);
        PromptTemplate duplicatePromptTemplate = new(skPrompt2, promptConfig, kernel);

        SemanticFunctionConfig baseFunctionConfig = new(promptConfig, basePromptTemplate);
        SemanticFunctionConfig duplicateFunctionConfig = new(promptConfig, duplicatePromptTemplate);

        List<ISKFunction> functions = new List<ISKFunction>()
        {
            kernel.RegisterSemanticFunction("CreateMultipleChoices", "ImportantInfoMultipleChoices", baseFunctionConfig),
            kernel.RegisterSemanticFunction("CreateMultipleChoices", "ClearDuplicateQuestions", duplicateFunctionConfig)
        };
        

        return functions;
    }
    
    static string MergeJsonObjects(IEnumerable<string> jsonStrings)
    {
        JObject mergedObject = new JObject();

        foreach (string jsonString in jsonStrings)
        {
            JObject jsonObject = JObject.Parse(jsonString);
            mergedObject.Merge(jsonObject, new JsonMergeSettings
            {
                MergeArrayHandling = MergeArrayHandling.Union
            });
        }

        return mergedObject.ToString();
    }

    public async Task<string> Execute(string studySessionId)
    {
        string? userId = _userAuthService.GetUserUuid();
        _kernel = await _kernelService.GetKernel(userId, studySessionId);
        _multipleChoiceFunctions = RegisterMultiplechoiceFunctions(_kernel);
        IEnumerable<Chunk> chunks =
            await _textEmbeddingService.GetChunks(userId, studySessionId);
        IEnumerable<Chunk> enumerable = chunks.ToList();
        ConcurrentBag<string> response = new();

        // Start and await all tasks for getting the multiple-choice questions
        var tasks = enumerable.Select(async c =>
        {
            await GetMultipleChoiceResponse(_kernel, _multipleChoiceFunctions[0], c.Text, response);
        });
        await Task.WhenAll(tasks);

        // Merge and clean the generated questions
        string mergedJson = MergeJsonObjects(response);
        SKContext kernelContext = _kernel.CreateNewContext();
        kernelContext.Variables["INFORMATION"] = mergedJson;
        string cleanedQuestions = (await _multipleChoiceFunctions[1].InvokeAsync(kernelContext)).Result;
    
        // Here you can decide what to do with the cleaned questions, for example add it to a list and return
        return cleanedQuestions ;
    }

    private static async Task GetMultipleChoiceResponse(IKernel kernel, ISKFunction multipleChoiceFunction, string section, ConcurrentBag<string> responseBag)
    {
        SKContext kernelContext = kernel.CreateNewContext();
        kernelContext.Variables["INFORMATION"] = section;
        string result = (await multipleChoiceFunction.InvokeAsync(kernelContext)).Result;
        responseBag.Add(result);
    }
    
    private static async Task<string> CleanMultipleChoiceResponse(IKernel kernel,
        ISKFunction cleanMultipleChoiceFunction, Task<List<string>> baseResponseTask)
    {
        SKContext kernelContext = kernel.CreateNewContext();

        List<string> baseResponse = await baseResponseTask;
        List<string> newResponse = new();
        foreach (string response in baseResponse)
        {
            kernelContext.Variables["INFORMATION"] = response;
            newResponse.Add((await cleanMultipleChoiceFunction.InvokeAsync(kernelContext)).Result);
        }

        return MergeJsonObjects(newResponse);;
    }
}