using System.Net.Http.Json;
using System.Web;
using Enclave.Sdk.Api.Clients.Interfaces;
using Enclave.Sdk.Api.Data;
using Enclave.Sdk.Api.Data.Pagination;
using Enclave.Sdk.Api.Data.PatchModel;
using Enclave.Sdk.Api.Data.UnaprrovedSystems;
using Enclave.Sdk.Api.Data.UnaprrovedSystems.Enum;

namespace Enclave.Sdk.Api.Clients;

/// <inheritdoc cref="IUnapprovedSystemsClient"/>
public class UnapprovedSystemsClient : ClientBase, IUnapprovedSystemsClient
{
    private string _orgRoute;

    /// <summary>
    /// This constructor is called by <see cref="EnclaveClient"/> when setting up the <see cref="UnapprovedSystemsClient"/>.
    /// It also calls the <see cref="ClientBase"/> constructor.
    /// </summary>
    /// <param name="httpClient">an instance of httpClient with a baseURL referencing the API.</param>
    /// <param name="orgRoute">The organisation API route.</param>
    public UnapprovedSystemsClient(HttpClient httpClient, string orgRoute)
    : base(httpClient)
    {
        _orgRoute = orgRoute;
    }

    /// <inheritdoc/>
    public async Task<PaginatedResponseModel<UnapprovedSystem>> GetSystemsAsync(
        int? enrolmentKeyId = null,
        string? searchTerm = null,
        UnapprovedSystemQuerySortMode? sortOrder = null,
        int? pageNumber = null,
        int? perPage = null)
    {
        var queryString = BuildQueryString(enrolmentKeyId, searchTerm, sortOrder, pageNumber, perPage);

        var model = await HttpClient.GetFromJsonAsync<PaginatedResponseModel<UnapprovedSystem>>($"{_orgRoute}/unapproved-systems?{queryString}");

        EnsureNotNull(model);

        return model;
    }

    /// <inheritdoc/>
    public async Task<int> DeclineSystems(params string[] systemIds)
    {
        using var content = CreateJsonContent(new
        {
            systemIds = systemIds,
        });

        using var request = new HttpRequestMessage
        {
            Content = content,
            Method = HttpMethod.Delete,
            RequestUri = new Uri($"{HttpClient.BaseAddress}{_orgRoute}/unapproved-systems"),
        };

        var result = await HttpClient.SendAsync(request);

        result.EnsureSuccessStatusCode();

        var model = await DeserialiseAsync<BulkUnapprovedSystemDeclineResult>(result.Content);

        EnsureNotNull(model);

        return model.SystemsDeclined;
    }

    /// <inheritdoc/>
    public async Task<UnapprovedSystemDetail> GetAsync(string systemId)
    {
        var model = await HttpClient.GetFromJsonAsync<UnapprovedSystemDetail>($"{_orgRoute}/unapproved-systems/{systemId}", Constants.JsonSerializerOptions);

        EnsureNotNull(model);

        return model;
    }

    /// <inheritdoc/>
    public async Task<UnapprovedSystemDetail> UpdateAsync(string systemId, PatchBuilder<UnapprovedSystemPatch> builder)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        using var encoded = CreateJsonContent(builder.Send());
        var result = await HttpClient.PatchAsync($"{_orgRoute}/unapproved-systems/{systemId}", encoded);

        result.EnsureSuccessStatusCode();

        var model = await DeserialiseAsync<UnapprovedSystemDetail>(result.Content);

        EnsureNotNull(model);

        return model;
    }

    /// <inheritdoc/>
    public async Task<UnapprovedSystemDetail> DeclineAsync(string systemId)
    {
        var result = await HttpClient.DeleteAsync($"{_orgRoute}/unapproved-systems/{systemId}");

        result.EnsureSuccessStatusCode();

        var model = await DeserialiseAsync<UnapprovedSystemDetail>(result.Content);

        EnsureNotNull(model);

        return model;
    }

    /// <inheritdoc/>
    public async Task ApproveAsync(string systemId)
    {
        var result = await HttpClient.PutAsync($"{_orgRoute}/unapproved-systems/{systemId}/approve", null);

        result.EnsureSuccessStatusCode();
    }

    /// <inheritdoc/>
    public async Task<int> ApproveSystemsAsync(params string[] systemIds)
    {
        using var content = CreateJsonContent(new
        {
            systemIds = systemIds,
        });

        var result = await HttpClient.PutAsync($"{_orgRoute}/unapproved-systems/approve", content);

        result.EnsureSuccessStatusCode();

        var model = await DeserialiseAsync<BulkUnapprovedSystemApproveResult>(result.Content);

        EnsureNotNull(model);

        return model.SystemsApproved;
    }

    private static string? BuildQueryString(
        int? enrolmentKeyId,
        string? searchTerm,
        UnapprovedSystemQuerySortMode? sortOrder,
        int? pageNumber,
        int? perPage)
    {
        var queryString = HttpUtility.ParseQueryString(string.Empty);
        if (enrolmentKeyId is not null)
        {
            queryString.Add("enrolment_key", enrolmentKeyId.ToString());
        }

        if (searchTerm is not null)
        {
            queryString.Add("search", searchTerm);
        }

        if (sortOrder is not null)
        {
            queryString.Add("sort", sortOrder.ToString());
        }

        if (pageNumber is not null)
        {
            queryString.Add("page", pageNumber.ToString());
        }

        if (perPage is not null)
        {
            queryString.Add("per_page", perPage.ToString());
        }

        return queryString.ToString();
    }
}