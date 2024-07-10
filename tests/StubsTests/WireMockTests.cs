using FluentAssertions;

using Microsoft.AspNetCore.Http;

using System.Net;
using System.Net.Http.Headers;

using WireMock.Admin.Mappings;
using WireMock.Matchers;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using WireMock.Settings;

namespace WireMockTests;

/// <summary>
/// Para maiores informações consulta pagina do projeto <see href="https://github.com/WireMock-Net/WireMock.Net/tree/master">WireMock.Net</see>
/// </summary>
public class WireMockTests
{
    /// <summary>
    /// <para>Configuração para o Serviço do WireMock.</para>
    /// <para></para>
    /// <para>A propriedade <see cref="WireMockServerSettings.StartAdminInterface"/> somente deve ser definida como <c>True</c>
    /// Quando estiver em situação de Debug de não ocorrer match da request que foi feita.
    /// Para esse cenário recomenda-se habitar essa propriedade para <c>True</c> e utilizar as urls: <see href="http://localhost:9876/__admin/requests">Requests</see>
    /// e <see href="http://localhost:9876/__admin/mappings/PartialMappingGuid">PartialMappingGuid</see>  para facilitar a identificação do problema.</para>
    /// <para>Observe que a porta definina na url acima deve ser a mesma definida na propriedade <c>Port</c></para>
    /// 
    /// <para>Na segunda url informada tem uma parametro chamado <c>PartialMappingGuid</c> esse valor deve ser obtido apartir da primeira url informada.
    /// Para maior inofrmação acesso o <see href="https://github.com/WireMock-Net/WireMock.Net/wiki/Request-Matching-Tips">WireMock.Net Github</see> e 
    /// <see href="https://cezarypiatek.github.io/post/mocking-outgoing-http-requests-p2/">Cezary Piątek</see> </para>
    /// </summary>
    private WireMockServerSettings _wireMockSettings;
    private WireMockServer _wireMockServer;
    private Dictionary<string, string> _requestParams;
    private readonly HttpClient _httpClient;

    public WireMockTests()
    {
        _wireMockSettings ??= new()
        {
            Port = null, //quando é definido null na porta o wiremock seta um aleatório. Dessa forma não vai dar erro de socket falando que já existe uma url para porta especificada.
            AllowPartialMapping = false,
            StartAdminInterface = true
        };

        _wireMockServer ??= WireMockServer.Start(_wireMockSettings);
        _httpClient = new HttpClient() { BaseAddress = new Uri($"http://Localhost:{_wireMockServer.Port}/") };

        _requestParams = new Dictionary<string, string>
        {
            { "startDate", "2024-07-10"},
            { "endDate", "2024-07-25"},
            { "subSellerId", "700046620" }
        };
    }

    [Fact]
    public async Task GivenValidPath_WithExtachMatch_ToDoGet_Should_ReturnSuccess()
    {
        //arrange
        const string path = "v1/ShowUr/Summary?subSellerId=700046620&startDate=2024-07-10&endDate=2024-07-25";

        var paramModel = _requestParams?.Select(item => new ParamModel()
        {
            Name = item.Key,
            Matchers = [new() { Name = "ExactMatcher", Pattern = item.Value }]
        }).ToList();

        var requestModel = new RequestModel()
        {
            Methods = ["Get"],
            Path = "/v1/ShowUr/Summary",
            Params = paramModel
        };

        var response = new ResponseModel()
        {
            StatusCode = StatusCodes.Status200OK,
            Headers = new Dictionary<string, object>
            {
                ["Content-Type"] = "application/json; charset=utf-8"
            }
        };

        var mappingModel = new MappingModel() { Request = requestModel, Response = response };

        _wireMockServer.WithMapping(mappingModel);

        //act
        var responseMessage = await _httpClient.GetAsync(path);
        var jsonContent = await responseMessage.Content.ReadAsStringAsync();

        responseMessage.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GivenValidPath_WithRequestBuild_ToDoGet_Should_ReturnSuccess()
    {
        //arrange
        const string path = "/v1/ShowUr/Summary?subSellerId=700046620";
        object content = new { value = 100 };

        var request = Request.Create()
            .UsingMethod("Get")
            .WithPath("/v1/ShowUr/Summary")
            .WithParam(key: "subSellerId", ignoreCase: true, values: ["700046620"])
            ;

        var response = Response.Create()
            .WithBody("{\"Message\": \"Success Message\", \"Errors\": []}")
            .WithStatusCode(HttpStatusCode.OK)
            ;

        _wireMockServer.Given(request).RespondWith(response);

        //act
        var responseMessage = await _httpClient.GetAsync(path);
        var jsonContent = await responseMessage.Content.ReadAsStringAsync();

        //assert
        responseMessage.StatusCode.Should().Be(HttpStatusCode.OK);
        jsonContent.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GivenInValidPath_WithExtachMatch_ToDoGet_Should_ReturnNotFound()
    {
        //arrange
        const string path = "v1/ShowUr/Summary?subSellerId=700046620&startDate=2024-07-10&endDate=2024-07-25";

        var paramModel = _requestParams?.Select(item => new ParamModel()
        {
            Name = item.Key,
            Matchers = [new() { Name = "ExactMatcher", Pattern = item.Value }]
        }).ToList();

        var requestModel = new RequestModel()
        {
            Methods = ["Get"],
            //Sem barra inicial
            Path = "v1/ShowUr/Summary",
            Params = paramModel
        };

        var response = new ResponseModel()
        {
            StatusCode = StatusCodes.Status200OK,
            Headers = new Dictionary<string, object>
            {
                ["Content-Type"] = "application/json; charset=utf-8"
            }
        };

        var mappingModel = new MappingModel() { Request = requestModel, Response = response };

        _wireMockServer.WithMapping(mappingModel);

        //act
        var responseMessage = await _httpClient.GetAsync(path);
        var jsonContent = await responseMessage.Content.ReadAsStringAsync();

        responseMessage.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GivenValidPath_WithRequestBuild_ToDoPost_Should_ReturnSuccess()
    {
        //arrange
        const string path = "/v1/ShowUr/Summary?subSellerId=700046620";
        object content = new { value = 100 };

        var request = Request.Create()
            .UsingMethod("Post")
            .WithPath("/v1/ShowUr/Summary")
            .WithParam(key: "subSellerId", ignoreCase: true, values: ["700046620"])
            .WithBody(new JsonMatcher(value: content, ignoreCase: true, regex: false)
            );

        var response = Response.Create()
            .WithBody("{\"Message\": \"Success Message\", \"Errors\": []}")
            .WithStatusCode(HttpStatusCode.Created)
            ;

        _wireMockServer.Given(request).RespondWith(response);

        //act
        var responseMessage = await _httpClient.PostAsync(path, content.ToStringContent());
        var jsonContent = await responseMessage.Content.ReadAsStringAsync();

        //assert
        responseMessage.StatusCode.Should().Be(HttpStatusCode.Created);
        jsonContent.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GivenValidRequest_WithoutParams_ToDoPost_Should_ReturnSuccess()
    {
        //arrange
        const string path = "/v1/ShowUr/Summary";
        object content = new { value = 100 };

        var request = Request.Create()
            .UsingMethod("Post")
            .WithPath("/v1/ShowUr/Summary")
            .WithBody(new JsonMatcher(value: content, ignoreCase: true, regex: false)
            );

        var response = Response.Create()
            .WithBody("{\"Message\": \"Success Message\", \"Errors\": []}")
            .WithStatusCode(HttpStatusCode.Created)
            ;

        _wireMockServer.Given(request).RespondWith(response);

        //act
        var responseMessage = await _httpClient.PostAsync(path, content.ToStringContent());
        var jsonContent = await responseMessage.Content.ReadAsStringAsync();

        //assert
        responseMessage.StatusCode.Should().Be(HttpStatusCode.Created);
        jsonContent.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GivenValidRequest_WithoutParams_ToDoPath_Should_ReturnSuccess()
    {
        //arrange
        const string path = "/v1/ShowUr/Summary";
        object content = new { value = 100 };

        var request = Request.Create()
            .UsingMethod("Patch")
            .WithPath("/v1/ShowUr/Summary")
            .WithBody(new JsonMatcher(value: content, ignoreCase: true, regex: false)
            );

        var response = Response.Create()
            .WithBody("{\"Message\": \"Success Message\", \"Errors\": []}")
            .WithStatusCode(HttpStatusCode.Accepted)
            ;

        _wireMockServer.Given(request).RespondWith(response);

        //act
        var responseMessage = await _httpClient.PatchAsync(path, content.ToStringContent());
        var jsonContent = await responseMessage.Content.ReadAsStringAsync();

        //assert
        responseMessage.StatusCode.Should().Be(HttpStatusCode.Accepted);
        jsonContent.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GivenInValidPathWithoutParamsInUrl_WithRequestBuild_ToDoPost_Should_ReturnNotFound()
    {
        //arrange
        const string path = "/v1/ShowUr/Summary"; //path sem parametros na url
        object content = new { value = 100 };

        var request = Request.Create()
            .UsingMethod("Post")
            .WithPath("/v1/ShowUr/Summary")
            .WithParam(key: "subSellerId", ignoreCase: true, values: ["700046620"])
            .WithBody(new JsonMatcher(value: content, ignoreCase: true, regex: false)
            );

        var response = Response.Create()
            .WithBody("{\"Message\": \"Success Message\", \"Errors\": []}")
            .WithStatusCode(HttpStatusCode.Created)
            ;

        _wireMockServer.Given(request).RespondWith(response);

        //act
        var responseMessage = await _httpClient.PostAsync(path, content.ToStringContent());
        var jsonContent = await responseMessage.Content.ReadAsStringAsync();

        //assert
        responseMessage.StatusCode.Should().Be(HttpStatusCode.NotFound);
        jsonContent.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GivenValid_WithMultPartImagePng_ToDoPost_Should_ReturnCreated()
    {
        //https://github.com/WireMock-Net/WireMock.Net/blob/master/test/WireMock.Net.Tests/WireMockServerTests.WithMultiPart.cs

        //arrange
        var imagePngBytes = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAIAAAACAgMAAAAP2OW3AAAADFBMVEX/tID/vpH/pWX/sHidUyjlAAAADElEQVR4XmMQYNgAAADkAMHebX3mAAAAAElFTkSuQmCC");

        var imagePngMatcher = new MimePartMatcher(matchBehaviour: MatchBehaviour.AcceptOnMatch,
            contentTypeMatcher: new ContentTypeMatcher("image/png"),
            contentDispositionMatcher: null,
            contentTransferEncodingMatcher: null,
            contentMatcher: new ExactObjectMatcher(imagePngBytes));

        var matchers = new IMatcher[] { imagePngMatcher };

        var request = Request.Create()
            .UsingMethod("Post")
            .WithPath("/multipart")
            .WithMultiPart(matchers);

        var response = Response.Create()
            .WithStatusCode(HttpStatusCode.Created);

        _wireMockServer.Given(request).RespondWith(response);

        //act
        var fileContent = new ByteArrayContent(imagePngBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");

        var formDataContent = new MultipartFormDataContent();
        formDataContent.Add(content: fileContent, name: "somefile", fileName: "image.png");

        var responseMessage = await _httpClient.PostAsync("/multipart", formDataContent);

        //assert
        responseMessage.StatusCode.Should().Be(HttpStatusCode.Created);

    }

}
