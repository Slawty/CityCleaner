public interface IVacuumable
{
    bool CanVacuum { get; }
    string VacuumPrompt { get; }
    void VacuumStart();
    void VacuumEnd();
}
