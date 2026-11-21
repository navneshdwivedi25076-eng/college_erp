using System.ComponentModel.DataAnnotations;
using CollegeErp.Application.Abstractions;
using CollegeErp.Application.Common;
using CollegeErp.Domain.Campus;
using Microsoft.EntityFrameworkCore;

namespace CollegeErp.Application.Campus.Finance;

public sealed class CreateInvoiceRequest
{
    /// <summary>Bill one student...</summary>
    public int? StudentId { get; init; }
    /// <summary>...or every active student of a semester (bulk billing).</summary>
    [Range(1, 20)] public int? Semester { get; init; }
    [Required, StringLength(255)] public string Description { get; init; } = "";
    [Range(0.01, 100_000_000)] public decimal Amount { get; init; }
    public DateOnly? DueDate { get; init; }
}

public sealed class RecordPaymentRequest
{
    [Range(0.01, 100_000_000)] public decimal Amount { get; init; }
    [Required, RegularExpression("^(cash|card|bank_transfer|upi|cheque|online|other)$")] public string Method { get; init; } = "cash";
    [StringLength(100)] public string? Reference { get; init; }
}

public sealed record InvoiceDto(
    int Id, int StudentId, string RollNo, string StudentName, string Description, decimal Amount,
    decimal AmountPaid, decimal Balance, DateOnly? DueDate, string Status, DateTime CreatedAt);

public sealed record PaymentDto(int Id, int InvoiceId, decimal Amount, string Method, string? Reference, DateTime PaidOn);

public interface IFeeService
{
    Task<PagedResult<InvoiceDto>> ListInvoicesAsync(ListQuery query, CancellationToken ct = default);
    Task<IReadOnlyList<InvoiceDto>> CreateInvoicesAsync(CreateInvoiceRequest request, CancellationToken ct = default);
    Task DeleteInvoiceAsync(int id, CancellationToken ct = default);
    Task<PaymentDto> RecordPaymentAsync(int invoiceId, RecordPaymentRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<PaymentDto>> ListPaymentsAsync(int invoiceId, CancellationToken ct = default);
}

public sealed class FeeService(ICollegeDbContext db, ICurrentUser user, TimeProvider clock) : IFeeService
{
    public async Task<PagedResult<InvoiceDto>> ListInvoicesAsync(ListQuery query, CancellationToken ct = default)
    {
        var q = from i in db.FeeInvoices join s in db.Students on i.StudentId equals s.Id select new { i, s };
        if (query.IntFilter("studentId") is { } sid) q = q.Where(x => x.i.StudentId == sid);
        if (query.StringFilter("status") is { } status) q = q.Where(x => x.i.Status == status);
        if (query.Search is { Length: > 0 } term)
        {
            term = term.ToLower();
            q = q.Where(x => x.s.RollNo.ToLower().Contains(term) || x.s.FirstName.ToLower().Contains(term)
                             || x.s.LastName.ToLower().Contains(term) || x.i.Description.ToLower().Contains(term));
        }
        var total = await q.CountAsync(ct);
        var rows = await q.OrderByDescending(x => x.i.Id).Skip(query.SafeSkip).Take(query.SafeTake).ToListAsync(ct);
        return new PagedResult<InvoiceDto>(rows.Select(x => ToDto(x.i, x.s)).ToList(), total, query.SafeSkip, query.SafeTake);
    }

    public async Task<IReadOnlyList<InvoiceDto>> CreateInvoicesAsync(CreateInvoiceRequest r, CancellationToken ct = default)
    {
        if ((r.StudentId is null) == (r.Semester is null))
            throw new InvalidInputException("studentId", "Provide either studentId or semester (not both).");

        var students = r.StudentId is { } sid
            ? await db.Students.Where(s => s.Id == sid).ToListAsync(ct)
            : await db.Students.Where(s => s.CurrentSemester == r.Semester && s.Status == "active").ToListAsync(ct);
        if (students.Count == 0) throw new NotFoundException("No matching students to bill.");

        var now = clock.GetUtcNow().UtcDateTime;
        var invoices = students.Select(s => (Invoice: new FeeInvoice
        {
            StudentId = s.Id, Description = r.Description.Trim(), Amount = r.Amount, DueDate = r.DueDate, CreatedAt = now,
        }, Student: s)).ToList();
        db.FeeInvoices.AddRange(invoices.Select(x => x.Invoice));
        await db.SaveChangesAsync(ct);
        return invoices.Select(x => ToDto(x.Invoice, x.Student)).ToList();
    }

    public async Task DeleteInvoiceAsync(int id, CancellationToken ct = default)
    {
        var inv = await db.FeeInvoices.FindAsync([id], ct) ?? throw NotFoundException.For<FeeInvoice>(id);
        if (await db.Payments.AnyAsync(p => p.InvoiceId == id, ct))
            throw new ConflictException("Invoices with recorded payments cannot be deleted.");
        db.FeeInvoices.Remove(inv);
        await db.SaveChangesAsync(ct);
    }

    public async Task<PaymentDto> RecordPaymentAsync(int invoiceId, RecordPaymentRequest r, CancellationToken ct = default)
    {
        var inv = await db.FeeInvoices.FindAsync([invoiceId], ct) ?? throw NotFoundException.For<FeeInvoice>(invoiceId);
        try { inv.ApplyPayment(r.Amount); }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentOutOfRangeException)
        {
            throw new InvalidInputException("amount", ex.Message);
        }
        var payment = new Payment
        {
            InvoiceId = invoiceId, Amount = r.Amount, Method = r.Method, Reference = r.Reference,
            PaidOn = clock.GetUtcNow().UtcDateTime, ReceivedByUserId = user.UserId,
        };
        db.Payments.Add(payment);
        await db.SaveChangesAsync(ct);
        return ToDto(payment);
    }

    public async Task<IReadOnlyList<PaymentDto>> ListPaymentsAsync(int invoiceId, CancellationToken ct = default) =>
        (await db.Payments.AsNoTracking().Where(p => p.InvoiceId == invoiceId).OrderBy(p => p.Id).ToListAsync(ct))
        .Select(ToDto).ToList();

    internal static InvoiceDto ToDto(FeeInvoice i, Student s) => new(
        i.Id, s.Id, s.RollNo, $"{s.FirstName} {s.LastName}", i.Description, i.Amount, i.AmountPaid, i.Balance,
        i.DueDate, i.Status, i.CreatedAt);

    private static PaymentDto ToDto(Payment p) => new(p.Id, p.InvoiceId, p.Amount, p.Method, p.Reference, p.PaidOn);
}
