using System.Text.Json.Nodes;

namespace RestSQL.Domain.Tests;

public class EndpointResultTests
{
    [Fact]
    public void Success_WithNull_ReturnsEmptyResult()
    {
        var result = EndpointResult.Success(null);
        
        Assert.Null(result.Data);
        Assert.False(result.HasData);
    }

    [Fact]
    public void Success_WithEmptyArray_ReturnsEmptyResult()
    {
        var emptyArray = new JsonArray();
        var result = EndpointResult.Success(emptyArray);
        
        Assert.NotNull(result.Data);
        Assert.False(result.HasData);
    }

    [Fact]
    public void Success_WithPopulatedArray_ReturnsDataResult()
    {
        var array = new JsonArray();
        array.Add(JsonValue.Create("test"));
        var result = EndpointResult.Success(array);
        
        Assert.NotNull(result.Data);
        Assert.True(result.HasData);
    }

    [Fact]
    public void Success_WithObject_ReturnsDataResult()
    {
        var obj = new JsonObject { ["test"] = "value" };
        var result = EndpointResult.Success(obj);
        
        Assert.NotNull(result.Data);
        Assert.True(result.HasData);
    }

    [Fact]
    public void Empty_ReturnsEmptyResult()
    {
        var result = EndpointResult.Empty();
        
        Assert.Null(result.Data);
        Assert.False(result.HasData);
    }
}