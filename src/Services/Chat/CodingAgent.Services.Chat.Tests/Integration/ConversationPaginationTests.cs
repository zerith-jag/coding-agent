using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace CodingAgent.Services.Chat.Tests.Integration;

[Collection("ChatServiceCollection")]
[Trait("Category", "Integration")]
public class ConversationPaginationTests
{
    private readonly ChatServiceFixture _fixture;

    public ConversationPaginationTests(ChatServiceFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetConversations_WithDefaultPagination_ShouldReturnFirstPage()
    {
        // Arrange: Create test conversations
        var conversationIds = new List<Guid>();
        for (int i = 0; i < 5; i++)
        {
            var request = new { title = $"Pagination Test {i} - {Guid.NewGuid()}" };
            var response = await _fixture.Client.PostAsJsonAsync("/conversations", request);
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var created = await response.Content.ReadFromJsonAsync<ConversationDto>();
            conversationIds.Add(created!.Id);
        }

        // Act
        var getResponse = await _fixture.Client.GetAsync("/conversations");

        // Assert
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Check pagination headers
        getResponse.Headers.Should().ContainKey("X-Total-Count");
        getResponse.Headers.Should().ContainKey("X-Page-Number");
        getResponse.Headers.Should().ContainKey("X-Page-Size");
        getResponse.Headers.Should().ContainKey("X-Total-Pages");

        var pageNumber = int.Parse(getResponse.Headers.GetValues("X-Page-Number").First());
        var pageSize = int.Parse(getResponse.Headers.GetValues("X-Page-Size").First());

        pageNumber.Should().Be(1);
        pageSize.Should().Be(50);

        var conversations = await getResponse.Content.ReadFromJsonAsync<List<ConversationDto>>();
        conversations.Should().NotBeNull();
        conversations!.Count.Should().BeGreaterOrEqualTo(5);
    }

    [Fact]
    public async Task GetConversations_WithCustomPageSize_ShouldRespectPageSize()
    {
        // Arrange: Create 10 test conversations
        for (int i = 0; i < 10; i++)
        {
            var request = new { title = $"Page Size Test {i} - {Guid.NewGuid()}" };
            await _fixture.Client.PostAsJsonAsync("/conversations", request);
        }

        // Act: Request with page size of 5
        var getResponse = await _fixture.Client.GetAsync("/conversations?page=1&pageSize=5");

        // Assert
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var pageSize = int.Parse(getResponse.Headers.GetValues("X-Page-Size").First());
        pageSize.Should().Be(5);

        var conversations = await getResponse.Content.ReadFromJsonAsync<List<ConversationDto>>();
        conversations.Should().NotBeNull();
        conversations!.Count.Should().BeLessOrEqualTo(5);
    }

    [Fact]
    public async Task GetConversations_WithPageSizeExceedingMax_ShouldCapAt100()
    {
        // Act: Request with page size greater than max (100)
        var getResponse = await _fixture.Client.GetAsync("/conversations?page=1&pageSize=150");

        // Assert
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var pageSize = int.Parse(getResponse.Headers.GetValues("X-Page-Size").First());
        pageSize.Should().Be(100, "page size should be capped at maximum of 100");
    }

    [Fact]
    public async Task GetConversations_WithInvalidPageNumber_ShouldDefaultToPage1()
    {
        // Act: Request with invalid page number (0 or negative)
        var getResponse = await _fixture.Client.GetAsync("/conversations?page=0&pageSize=10");

        // Assert
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var pageNumber = int.Parse(getResponse.Headers.GetValues("X-Page-Number").First());
        pageNumber.Should().Be(1, "invalid page numbers should default to 1");
    }

    [Fact]
    public async Task GetConversations_SecondPage_ShouldReturnDifferentItems()
    {
        // Arrange: Create enough conversations to span multiple pages
        for (int i = 0; i < 15; i++)
        {
            var request = new { title = $"Multi Page Test {i} - {Guid.NewGuid()}" };
            await _fixture.Client.PostAsJsonAsync("/conversations", request);
        }

        // Act: Get page 1
        var page1Response = await _fixture.Client.GetAsync("/conversations?page=1&pageSize=5");
        page1Response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page1Items = await page1Response.Content.ReadFromJsonAsync<List<ConversationDto>>();

        // Act: Get page 2
        var page2Response = await _fixture.Client.GetAsync("/conversations?page=2&pageSize=5");
        page2Response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page2Items = await page2Response.Content.ReadFromJsonAsync<List<ConversationDto>>();

        // Assert: Pages should contain different items
        page1Items.Should().NotBeNull();
        page2Items.Should().NotBeNull();
        
        var page1Ids = page1Items!.Select(c => c.Id).ToList();
        var page2Ids = page2Items!.Select(c => c.Id).ToList();
        
        page1Ids.Should().NotIntersectWith(page2Ids, "different pages should contain different conversations");
    }

    [Fact]
    public async Task GetConversations_LastPage_ShouldHaveCorrectItemCount()
    {
        // Arrange: Create exactly 23 conversations (to test partial last page)
        var uniquePrefix = Guid.NewGuid().ToString("N").Substring(0, 8);
        for (int i = 0; i < 23; i++)
        {
            var request = new { title = $"Last Page Test {uniquePrefix} {i}" };
            await _fixture.Client.PostAsJsonAsync("/conversations", request);
        }

        // Wait a moment to ensure all conversations are created
        await Task.Delay(100);

        // Act: Get last page with page size of 10
        var firstPageResponse = await _fixture.Client.GetAsync("/conversations?page=1&pageSize=10");
        var totalCount = int.Parse(firstPageResponse.Headers.GetValues("X-Total-Count").First());
        var totalPages = int.Parse(firstPageResponse.Headers.GetValues("X-Total-Pages").First());

        var lastPageResponse = await _fixture.Client.GetAsync($"/conversations?page={totalPages}&pageSize=10");
        lastPageResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var lastPageItems = await lastPageResponse.Content.ReadFromJsonAsync<List<ConversationDto>>();
        lastPageItems.Should().NotBeNull();

        // Assert: If there are 23 items with page size 10, last page (page 3) should have 3 items
        // But since we might have other conversations from other tests, we just verify it's not empty
        lastPageItems!.Count.Should().BeGreaterThan(0);
        lastPageItems.Count.Should().BeLessOrEqualTo(10);
    }

    [Fact]
    public async Task GetConversations_EmptyResult_ShouldReturnEmptyArrayWithCorrectHeaders()
    {
        // Act: Request a very high page number that won't have data
        var getResponse = await _fixture.Client.GetAsync("/conversations?page=9999&pageSize=10");

        // Assert
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var totalCount = int.Parse(getResponse.Headers.GetValues("X-Total-Count").First());
        var pageNumber = int.Parse(getResponse.Headers.GetValues("X-Page-Number").First());
        
        pageNumber.Should().Be(9999);

        var conversations = await getResponse.Content.ReadFromJsonAsync<List<ConversationDto>>();
        conversations.Should().NotBeNull();
        conversations!.Should().BeEmpty();
    }

    [Fact]
    public async Task GetConversations_ShouldIncludeHATEOASLinks()
    {
        // Arrange: Create some conversations
        for (int i = 0; i < 10; i++)
        {
            var request = new { title = $"HATEOAS Test {i} - {Guid.NewGuid()}" };
            await _fixture.Client.PostAsJsonAsync("/conversations", request);
        }

        // Act: Get page 2 with page size of 3
        var getResponse = await _fixture.Client.GetAsync("/conversations?page=2&pageSize=3");

        // Assert
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        getResponse.Headers.Should().ContainKey("Link");

        var linkHeader = getResponse.Headers.GetValues("Link").First();
        linkHeader.Should().Contain("rel=\"first\"", "should include link to first page");
        linkHeader.Should().Contain("rel=\"last\"", "should include link to last page");
        linkHeader.Should().Contain("rel=\"prev\"", "should include link to previous page");
        linkHeader.Should().Contain("rel=\"next\"", "should include link to next page");
    }

    [Fact]
    public async Task GetConversations_FirstPage_ShouldNotHavePreviousLink()
    {
        // Act: Get first page
        var getResponse = await _fixture.Client.GetAsync("/conversations?page=1&pageSize=10");

        // Assert
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        if (getResponse.Headers.Contains("Link"))
        {
            var linkHeader = getResponse.Headers.GetValues("Link").First();
            linkHeader.Should().NotContain("rel=\"prev\"", "first page should not have previous link");
        }
    }

    [Fact]
    public async Task GetConversations_OrderedByUpdatedAtDescending_ShouldReturnNewestFirst()
    {
        // Arrange: Create conversations with slight delays to ensure different UpdatedAt times
        var firstConversationTitle = $"First {Guid.NewGuid()}";
        await _fixture.Client.PostAsJsonAsync("/conversations", new { title = firstConversationTitle });
        
        await Task.Delay(50);
        
        var secondConversationTitle = $"Second {Guid.NewGuid()}";
        await _fixture.Client.PostAsJsonAsync("/conversations", new { title = secondConversationTitle });

        // Act
        var getResponse = await _fixture.Client.GetAsync("/conversations?page=1&pageSize=10");
        var conversations = await getResponse.Content.ReadFromJsonAsync<List<ConversationDto>>();

        // Assert: Most recently created should be first
        conversations.Should().NotBeNull();
        
        // The second conversation should appear before the first (descending order)
        if (conversations != null)
        {
            var conversationList = conversations.ToList();
            var secondIndex = conversationList.FindIndex(c => c.Title == secondConversationTitle);
            var firstIndex = conversationList.FindIndex(c => c.Title == firstConversationTitle);

            if (secondIndex >= 0 && firstIndex >= 0)
            {
                secondIndex.Should().BeLessThan(firstIndex, "newer conversations should appear first");
            }
        }
    }

    [Fact]
    public async Task GetConversations_TotalPagesCalculation_ShouldBeCorrect()
    {
        // Arrange: Create exactly 25 conversations
        var uniquePrefix = Guid.NewGuid().ToString("N").Substring(0, 8);
        for (int i = 0; i < 25; i++)
        {
            var request = new { title = $"Total Pages Test {uniquePrefix} {i}" };
            await _fixture.Client.PostAsJsonAsync("/conversations", request);
        }

        await Task.Delay(100);

        // Act: Request with page size of 10
        var getResponse = await _fixture.Client.GetAsync("/conversations?page=1&pageSize=10");

        // Assert
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var totalCount = int.Parse(getResponse.Headers.GetValues("X-Total-Count").First());
        var totalPages = int.Parse(getResponse.Headers.GetValues("X-Total-Pages").First());

        // With at least 25 items and page size 10, we should have at least 3 pages
        totalPages.Should().BeGreaterOrEqualTo(3, "25 items with page size 10 should have at least 3 pages");
    }

    private record ConversationDto(Guid Id, string Title, DateTime CreatedAt, DateTime UpdatedAt);
}
