using CleanVander.Client.Models; using CleanVander.Client.Services; using Microsoft.Extensions.DependencyInjection;
namespace CleanVander.Client.Views;
public partial class FrmSignUp : ContentPage
{
 private readonly CleanVanderApi _api; public FrmSignUp() { InitializeComponent(); _api=App.Current!.Handler!.MauiContext!.Services.GetRequiredService<CleanVanderApi>(); TipoPessoa.SelectedIndex=0; }
 private async void Cadastrar(object? sender,EventArgs e) { if(TipoPessoa.SelectedIndex<0 || string.IsNullOrWhiteSpace(Nome.Text) || string.IsNullOrWhiteSpace(Documento.Text) || string.IsNullOrWhiteSpace(Telefone.Text) || string.IsNullOrWhiteSpace(Endereco.Text)) { await DisplayAlertAsync("Cadastro incompleto","Preencha tipo, nome, CPF/CNPJ, telefone e endereço.","OK"); return; } try { await _api.CadastrarAsync(new CadastroClienteDto { TipoPessoa=TipoPessoa.SelectedIndex==0?"Fisica":"Juridica",Nome=Nome.Text.Trim(),Documento=Documento.Text.Trim(),Email=Email.Text?.Trim()??"",Telefone=Telefone.Text.Trim(),Endereco=Endereco.Text.Trim() }); await DisplayAlertAsync("Cadastro concluído","Agora você já pode consultar produtos e acompanhar pedidos neste aparelho.","Continuar"); await Navigation.PopAsync(); } catch(Exception ex) { await DisplayAlertAsync("Não foi possível cadastrar",ex.Message,"OK"); } }
}

