using Microsoft.Extensions.Logging;
using Trinetra.Core.Entities;
using Trinetra.Core.Interfaces;
using Trinetra.Infrastructure.Data;

namespace Trinetra.Infrastructure.Services;

public class DbAuditEventWriter : IAuditEventWriter
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<DbAuditEventWriter> _logger;

    public DbAuditEventWriter(ApplicationDbContext dbContext, ILogger<DbAuditEventWriter> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<IncidentEvent> WriteEventAsync(IncidentEvent incidentEvent, CancellationToken cancellationToken = default)
    {
        _dbContext.IncidentEvents.Add(incidentEvent);
        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogDebug("Audit event written to DB: {EventType} by {AgentName} for incident {IncidentId}",
            incidentEvent.EventType, incidentEvent.AgentName, incidentEvent.IncidentId);
        return incidentEvent;
    }
}
