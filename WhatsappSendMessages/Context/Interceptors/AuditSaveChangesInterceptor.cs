using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using WhatsappSendMessages.Entities.Auditing;
using WhatsappSendMessages.Services.Auditing;

namespace WhatsappSendMessages.Context.Interceptors
{
    /// <summary>
    /// Setea CreatedAt/CreatedBy/UpdatedAt/UpdatedBy en cada entidad IAuditable
    /// antes de SaveChanges. En Modified solo toca Updated*; Created* se marca
    /// como no-modificado para que EF no intente sobreescribirlas.
    /// Cualquier nueva entidad que implemente IAuditable queda auditada
    /// automaticamente (OCP).
    /// </summary>
    public class AuditSaveChangesInterceptor(
        ICurrentActorProvider actorProvider,
        IClock clock) : SaveChangesInterceptor
    {
        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData, InterceptionResult<int> result)
        {
            StampAuditFields(eventData.Context);
            return base.SavingChanges(eventData, result);
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            StampAuditFields(eventData.Context);
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        private void StampAuditFields(DbContext? context)
        {
            if (context is null) return;

            var now = clock.UtcNow;
            var actor = actorProvider.GetActor();

            foreach (EntityEntry<IAuditable> entry in context.ChangeTracker.Entries<IAuditable>())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.CreatedAt = now;
                        entry.Entity.CreatedBy = actor;
                        entry.Entity.UpdatedAt = now;
                        entry.Entity.UpdatedBy = actor;
                        break;

                    case EntityState.Modified:
                        entry.Entity.UpdatedAt = now;
                        entry.Entity.UpdatedBy = actor;
                        // Protege los campos de creacion contra updates accidentales.
                        entry.Property(nameof(IAuditable.CreatedAt)).IsModified = false;
                        entry.Property(nameof(IAuditable.CreatedBy)).IsModified = false;
                        break;
                }
            }
        }
    }
}
