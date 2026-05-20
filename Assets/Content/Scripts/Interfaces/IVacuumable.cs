public interface IVacuumable
{
    bool CanVacuum { get; }
    void VacuumStart();
    void VacuumEnd();
}
