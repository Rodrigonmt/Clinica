using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Clinica.Models
{
    public class Servico : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public string ServicoId { get; set; }
        public string Nome { get; set; }
        public decimal Preco { get; set; }
        public int Duracao { get; set; }
        public DateTime CriadoEm { get; set; }

        private bool _selecionado;
        public bool Selecionado
        {
            get => _selecionado;
            set
            {
                _selecionado = value;
                OnPropertyChanged();
            }
        }

        private bool _habilitado;
        public bool Habilitado
        {
            get => _habilitado;
            set
            {
                _habilitado = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Opacidade));
            }
        }

        public double Opacidade => Habilitado ? 1 : 0.4;

        protected void OnPropertyChanged([CallerMemberName] string prop = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    }
}
