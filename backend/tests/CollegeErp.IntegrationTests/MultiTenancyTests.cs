using System.Net;
using System.Net.Http.Json;

namespace CollegeErp.IntegrationTests;

public sealed class MultiTenancyTests : IClassFixture<TestApp>
{
    private readonly TestApp _app;
    public MultiTenancyTests(TestApp app) => _app = app;

    [Fact]
    public async Task Creating_a_college_provisions_its_own_database()
    {
        var (college, _) = await _app.CreateCollegeAsync("alpha");

        Assert.Equal("college_alpha", college.GetProperty("databaseName").GetString());
        Assert.True(File.Exists(Path.Combine(_app.DataDir, "colleges", "college_alpha.db")));
    }

    [Fact]
    public async Task Colleges_cannot_see_each_others_data()
    {
        var (_, tokenA) = await _app.CreateCollegeAsync("iso_a");
        var (_, tokenB) = await _app.CreateCollegeAsync("iso_b");

        await _app.Client(tokenA).PostAsJsonAsync("/api/t/iso_a/students",
            new { rollNo = "A-001", firstName = "Asha", lastName = "Rao" }).ContinueWith(t => t.Result.JsonAsync()).Unwrap();

        var a = await (await _app.Client(tokenA).GetAsync("/api/t/iso_a/students")).JsonAsync();
        var b = await (await _app.Client(tokenB).GetAsync("/api/t/iso_b/students")).JsonAsync();
        Assert.Equal(1, a.GetProperty("total").GetInt32());
        Assert.Equal(0, b.GetProperty("total").GetInt32());

        // Same roll number is fine in another college: separate databases, separate unique indexes.
        var res = await _app.Client(tokenB).PostAsJsonAsync("/api/t/iso_b/students",
            new { rollNo = "A-001", firstName = "Other", lastName = "Person" });
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
    }

    [Fact]
    public async Task Token_from_one_college_is_rejected_by_another()
    {
        var (_, tokenA) = await _app.CreateCollegeAsync("tok_a");
        await _app.CreateCollegeAsync("tok_b");

        var res = await _app.Client(tokenA).GetAsync("/api/t/tok_b/students");
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task College_tokens_cannot_use_platform_api_and_anonymous_is_rejected()
    {
        var (_, token) = await _app.CreateCollegeAsync("plat_x");
        Assert.Equal(HttpStatusCode.Forbidden, (await _app.Client(token).GetAsync("/api/platform/colleges")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _app.Client().GetAsync("/api/t/plat_x/students")).StatusCode);
    }

    [Fact]
    public async Task Suspended_college_is_blocked_until_reactivated()
    {
        var (college, token) = await _app.CreateCollegeAsync("susp");
        var platform = _app.Client(await _app.PlatformTokenAsync());

        await (await platform.PostAsync($"/api/platform/colleges/{college.Id()}/suspend", null)).JsonAsync();
        Assert.Equal(HttpStatusCode.Forbidden, (await _app.Client(token).GetAsync("/api/t/susp/students")).StatusCode);

        await (await platform.PostAsync($"/api/platform/colleges/{college.Id()}/activate", null)).JsonAsync();
        Assert.Equal(HttpStatusCode.OK, (await _app.Client(token).GetAsync("/api/t/susp/students")).StatusCode);
    }

    [Fact]
    public async Task Plan_student_limit_is_enforced()
    {
        var (_, token) = await _app.CreateCollegeAsync("small", maxStudents: 1);
        var c = _app.Client(token);
        Assert.Equal(HttpStatusCode.Created,
            (await c.PostAsJsonAsync("/api/t/small/students", new { rollNo = "1", firstName = "A", lastName = "B" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await c.PostAsJsonAsync("/api/t/small/students", new { rollNo = "2", firstName = "C", lastName = "D" })).StatusCode);
    }

    [Fact]
    public async Task Duplicate_code_and_unknown_college_are_handled()
    {
        await _app.CreateCollegeAsync("dup");
        var platform = _app.Client(await _app.PlatformTokenAsync());
        var res = await platform.PostAsJsonAsync("/api/platform/colleges", new
        {
            code = "dup", name = "Again", adminEmail = "x@y.com", adminFullName = "X", adminPassword = "Passw0rd!",
        });
        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _app.Client().GetAsync("/api/t/nosuchcollege/auth/me")).StatusCode);
    }

    [Fact]
    public async Task Deleting_a_college_can_drop_its_database()
    {
        var (college, _) = await _app.CreateCollegeAsync("gone");
        var platform = _app.Client(await _app.PlatformTokenAsync());

        var res = await platform.DeleteAsync($"/api/platform/colleges/{college.Id()}?dropDatabase=true");
        Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
        Assert.False(File.Exists(Path.Combine(_app.DataDir, "colleges", "college_gone.db")));
        Assert.Equal(HttpStatusCode.NotFound, (await _app.Client().PostAsJsonAsync("/api/t/gone/auth/login",
            new { email = "admin@gone.edu", password = "Passw0rd!" })).StatusCode);
    }
}
