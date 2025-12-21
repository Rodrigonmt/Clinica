namespace Clinica.View;
using Clinica.Services;

public partial class LoadingPage : ContentPage
{
    public LoadingPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (EmpresaContext.Empresa != null)
            Title = EmpresaContext.Empresa.NomeEmpresa;
    }

}