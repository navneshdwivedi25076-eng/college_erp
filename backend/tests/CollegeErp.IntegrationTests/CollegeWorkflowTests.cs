using System.Net;
using System.Net.Http.Json;

namespace CollegeErp.IntegrationTests;

public sealed class CollegeWorkflowTests : IClassFixture<TestApp>
{
    private readonly TestApp _app;
    public CollegeWorkflowTests(TestApp app) => _app = app;

    [Fact]
    public async Task Full_semester_workflow_including_roles_and_student_portal()
    {
        const string code = "flow";
        var (_, adminToken) = await _app.CreateCollegeAsync(code);
        var admin = _app.Client(adminToken);
        string U(string p) => $"/api/t/{code}{p}";

        var dept = await (await admin.PostAsJsonAsync(U("/departments"), new { code = "cs", name = "Computer Science" })).JsonAsync();
        Assert.Equal("CS", dept.GetProperty("code").GetString());

        var prof = await (await admin.PostAsJsonAsync(U("/faculty"), new
            { employeeNo = "E1", firstName = "Ada", lastName = "Lovelace", departmentId = dept.Id() })).JsonAsync();
        var other = await (await admin.PostAsJsonAsync(U("/faculty"), new
            { employeeNo = "E2", firstName = "Alan", lastName = "Turing", departmentId = dept.Id() })).JsonAsync();
        var course = await (await admin.PostAsJsonAsync(U("/courses"), new
            { code = "cs101", title = "Programming", credits = 4, departmentId = dept.Id(), facultyId = prof.Id() })).JsonAsync();
        var s1 = await (await admin.PostAsJsonAsync(U("/students"), new
            { rollNo = "R1", firstName = "Ravi", lastName = "K", departmentId = dept.Id(), currentSemester = 1 })).JsonAsync();
        var s2 = await (await admin.PostAsJsonAsync(U("/students"), new
            { rollNo = "R2", firstName = "Meera", lastName = "S", departmentId = dept.Id(), currentSemester = 1 })).JsonAsync();

        // Unknown department is a 400, not a DB crash
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync(U("/students"),
            new { rollNo = "R9", firstName = "X", lastName = "Y", departmentId = 999 })).StatusCode);

        var enrolled = await (await admin.PostAsJsonAsync(U("/enrollments"), new
            { courseId = course.Id(), studentIds = new[] { s1.Id(), s2.Id() }, academicYear = "2026-27" })).JsonAsync();
        Assert.Equal(2, enrolled.GetProperty("added").GetInt32());

        // Logins for faculty & student
        await (await admin.PostAsJsonAsync(U("/users"), new
            { email = "ada@flow.edu", fullName = "Ada", password = "Passw0rd!", role = "faculty", facultyId = prof.Id() })).JsonAsync();
        await (await admin.PostAsJsonAsync(U("/users"), new
            { email = "alan@flow.edu", fullName = "Alan", password = "Passw0rd!", role = "faculty", facultyId = other.Id() })).JsonAsync();
        await (await admin.PostAsJsonAsync(U("/users"), new
            { email = "ravi@flow.edu", fullName = "Ravi", password = "Passw0rd!", role = "student", studentId = s1.Id() })).JsonAsync();
        var ada = _app.Client(await _app.CollegeTokenAsync(code, "ada@flow.edu", "Passw0rd!"));
        var alan = _app.Client(await _app.CollegeTokenAsync(code, "alan@flow.edu", "Passw0rd!"));
        var ravi = _app.Client(await _app.CollegeTokenAsync(code, "ravi@flow.edu", "Passw0rd!"));

        // Attendance: assigned faculty may mark, others may not
        var attendance = new
        {
            courseId = course.Id(), date = "2026-11-02",
            entries = new[] { new { studentId = s1.Id(), status = "present" }, new { studentId = s2.Id(), status = "absent" } },
        };
        await (await ada.PostAsJsonAsync(U("/attendance"), attendance)).JsonAsync();
        Assert.Equal(HttpStatusCode.Forbidden, (await alan.PostAsJsonAsync(U("/attendance"), attendance)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await ravi.PostAsJsonAsync(U("/attendance"), attendance)).StatusCode);

        var summary = await (await ada.GetAsync(U($"/attendance/summary?courseId={course.Id()}"))).JsonAsync();
        Assert.Equal(100m, summary[0].GetProperty("percentage").GetDecimal());
        Assert.Equal(0m, summary[1].GetProperty("percentage").GetDecimal());

        // Exams & marks
        var exam = await (await ada.PostAsJsonAsync(U("/exams"), new { courseId = course.Id(), name = "Midterm", maxMarks = 50 })).JsonAsync();
        Assert.Equal(HttpStatusCode.BadRequest, (await ada.PostAsJsonAsync(U($"/exams/{exam.Id()}/marks"),
            new { entries = new[] { new { studentId = s1.Id(), marksObtained = 60 } } })).StatusCode);
        await (await ada.PostAsJsonAsync(U($"/exams/{exam.Id()}/marks"), new
        {
            entries = new[] { new { studentId = s1.Id(), marksObtained = 45 }, new { studentId = s2.Id(), marksObtained = 30 } },
        })).JsonAsync();

        // Fees: bulk-bill semester 1, pay part of one invoice
        var invoices = await (await admin.PostAsJsonAsync(U("/fees/invoices"), new
            { semester = 1, description = "Tuition 2026", amount = 1000 })).JsonAsync();
        Assert.Equal(2, invoices.GetArrayLength());
        var raviInvoice = invoices.EnumerateArray().First(i => i.GetProperty("studentId").GetInt32() == s1.Id());
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync(U($"/fees/invoices/{raviInvoice.Id()}/payments"),
            new { amount = 5000, method = "cash" })).StatusCode);
        await (await admin.PostAsJsonAsync(U($"/fees/invoices/{raviInvoice.Id()}/payments"),
            new { amount = 400, method = "upi", reference = "TX1" })).JsonAsync();
        Assert.Equal(HttpStatusCode.Forbidden, (await ada.GetAsync(U("/fees/invoices"))).StatusCode);

        var dash = await (await admin.GetAsync(U("/dashboard"))).JsonAsync();
        Assert.Equal(2, dash.GetProperty("students").GetInt32());
        Assert.Equal(2000m, dash.GetProperty("feesBilled").GetDecimal());
        Assert.Equal(400m, dash.GetProperty("feesCollected").GetDecimal());

        // Student portal shows only the student's own data
        var myCourses = await (await ravi.GetAsync(U("/me/courses"))).JsonAsync();
        Assert.Equal(100m, myCourses[0].GetProperty("attendancePercentage").GetDecimal());
        var myMarks = await (await ravi.GetAsync(U("/me/marks"))).JsonAsync();
        Assert.Equal(1, myMarks.GetArrayLength());
        Assert.Equal(90m, myMarks[0].GetProperty("percentage").GetDecimal());
        var myInvoices = await (await ravi.GetAsync(U("/me/invoices"))).JsonAsync();
        Assert.Equal("partial", myInvoices[0].GetProperty("status").GetString());
        Assert.Equal(600m, myInvoices[0].GetProperty("balance").GetDecimal());
        Assert.Equal(HttpStatusCode.Forbidden, (await ravi.GetAsync(U("/students"))).StatusCode);

        // Settings are per-college JSON
        await (await admin.PutAsJsonAsync(U("/settings"), new { academicYear = "2026-27", passPercentage = 40 })).JsonAsync();
        var settings = await (await ravi.GetAsync(U("/settings"))).JsonAsync();
        Assert.Equal(40, settings.GetProperty("passPercentage").GetInt32());

        // Department in use cannot be deleted
        Assert.Equal(HttpStatusCode.Conflict, (await admin.DeleteAsync(U($"/departments/{dept.Id()}"))).StatusCode);
    }
}
