public class CalendarioProfissional
{
    public string CalendarioId { get; set; }
    public string ProfissionalId { get; set; }
    public Dictionary<string, bool> DiasSemana { get; set; }
    public Dictionary<string, HorarioDia> Horarios { get; set; }
}