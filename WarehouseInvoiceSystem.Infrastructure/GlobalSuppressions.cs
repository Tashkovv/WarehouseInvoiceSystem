// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Security", "EF1002:Risk of vulnerability to SQL injection.", Justification = "SqlQueryRaw — sequence name is internal enum-derived, not user input", Scope = "member", Target = "~M:WarehouseInvoiceSystem.Infrastructure.Repositories.InvoiceRepository.GenerateInvoiceNumberAsync(WarehouseInvoiceSystem.Domain.Enums.InvoiceType,System.Threading.CancellationToken)~System.Threading.Tasks.Task{System.String}")]
