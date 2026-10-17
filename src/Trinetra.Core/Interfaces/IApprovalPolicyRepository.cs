using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Trinetra.Core.Entities;

namespace Trinetra.Core.Interfaces;

public interface IApprovalPolicyRepository
{
    Task<List<ApprovalPolicy>> GetActivePoliciesAsync(CancellationToken cancellationToken = default);

    Task<ApprovalPolicy?> FindMatchingPolicyAsync(
        string? command,
        string? service,
        string? riskLevel,
        string? severity,
        CancellationToken cancellationToken = default);
}
