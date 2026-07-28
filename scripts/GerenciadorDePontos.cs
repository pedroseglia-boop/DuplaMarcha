using UnityEngine;
using TMPro;

public class GerenciadorDePontos : MonoBehaviour
{
    [Header("Configurações")]
    public int pontuacaoTotal = 0;

    [Header("Interface Visual (VR)")]
    // Essa variável vai guardar o texto que o paciente vai ver
    public TextMeshProUGUI textoPlacar;

    void Start()
    {
        AtualizarInterface();
        
    }
    public void AdicionarPontos(int pontos)
    {
        pontuacaoTotal += pontos;
        AtualizarInterface();
    }

    public void RemoverPontos(int pontos)
    {
        pontuacaoTotal -= pontos;

        // Impede que a pontuação fique negativa (opcional)
        if (pontuacaoTotal < 0)
        {
            pontuacaoTotal = 0;
        }

        AtualizarInterface();
    }

    private void AtualizarInterface()
    {
        if (textoPlacar != null)
        {
            textoPlacar.text = $"Pontos: {pontuacaoTotal}";
        }

        // Aqui você pode depois conectar com um TextMeshPro no VR para o paciente ver!
        Debug.Log($"PONTUAÇÃO ATUAL: {pontuacaoTotal}");
    }
}
