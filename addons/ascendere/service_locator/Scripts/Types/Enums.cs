// === ENUMS ===

public enum ServiceLifetime
{
    Singleton, // One instance for the entire application
    Transient, // New instance each time requested
    Scoped // One instance per scope
    ,
}
