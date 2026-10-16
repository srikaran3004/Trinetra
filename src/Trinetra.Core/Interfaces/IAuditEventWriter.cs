using Trinetra.Core.Entities;

namespace Trinetra.Core.Interfaces;

public interface IAuditEventWriter
{
    Task<IncidentEvent> WriteEventAsync(IncidentEvent incidentEvent, CancellationToken cancellationToken = default);
}
