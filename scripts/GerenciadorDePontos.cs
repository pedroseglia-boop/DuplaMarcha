using UnityEngine;

public class GerenciadorDePontos : MonoBehaviour
{
    public int pontuacaoTotal = 0;

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
        // Aqui você pode depois conectar com um TextMeshPro no VR para o paciente ver!
        Debug.Log($"PONTUAÇÃO ATUAL: {pontuacaoTotal}");
    }
}