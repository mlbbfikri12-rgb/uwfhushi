using Hotel.Api.Controllers;
using Microsoft.AspNetCore.Authorization;
using System.Reflection;

namespace Hotel.Api.Tests;

public class EndpointAuthorizationTests
{
    [Theory]
    [InlineData(nameof(RoomTypesController.CreateRoomType))]
    [InlineData(nameof(RoomTypesController.UpdateRoomType))]
    public void RoomTypeWriteEndpoints_AreSpvOnly(string methodName)
    {
        var roles = GetAuthorizeRoles(typeof(RoomTypesController), methodName);

        Assert.Equal("SPV", roles);
    }

    [Theory]
    [InlineData(nameof(RoomsController.CreateRoom))]
    [InlineData(nameof(RoomsController.UpdateRoom))]
    [InlineData(nameof(RoomsController.UpdateRoomStatus))]
    [InlineData(nameof(RoomsController.AddRoomImage))]
    [InlineData(nameof(RoomsController.DeleteRoomImage))]
    [InlineData(nameof(RoomsController.SetAvailability))]
    public void RoomWriteEndpoints_AreSpvAndFoOnly(string methodName)
    {
        var roles = GetAuthorizeRoles(typeof(RoomsController), methodName);

        Assert.Equal("SPV,FO", roles);
        Assert.DoesNotContain("SUPER_ADMIN", roles);
    }

    [Fact]
    public void BranchController_IsSuperAdminOnly()
    {
        var attribute = typeof(BranchesController).GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(attribute);
        Assert.Equal("SUPER_ADMIN", attribute.Roles);
    }

    [Fact]
    public void StaffController_IsSuperAdminOnly()
    {
        var attribute = typeof(StaffController).GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(attribute);
        Assert.Equal("SUPER_ADMIN", attribute.Roles);
    }

    private static string? GetAuthorizeRoles(Type controllerType, string methodName)
    {
        var method = controllerType.GetMethods()
            .Single(m => m.Name == methodName);

        return method.GetCustomAttribute<AuthorizeAttribute>()?.Roles;
    }
}
