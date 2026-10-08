namespace WhatsappSendMessages.Entities.Auditing
{
    /// <summary>
    /// Marca una entidad como auditada. El AuditSaveChangesInterceptor setea
    /// estos campos automaticamente; ninguna entidad debe asignarlos a mano.
    /// "UpdatedAt/UpdatedBy" no son null: en un insert reciben los mismos
    /// valores que "CreatedAt/CreatedBy" (asi no hay que cambiar la nulabilidad
    /// de columnas que ya tenian UpdatedAt no-null, como WhatsAppAccessTokens).
    /// </summary>
    public interface IAuditable
    {
        DateTime CreatedAt { get; set; }
        string CreatedBy { get; set; }
        DateTime UpdatedAt { get; set; }
        string UpdatedBy { get; set; }
    }
}
