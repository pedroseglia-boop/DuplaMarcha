using UnityEngine;
using System.Collections;

public class PerguntasPeriodicas : MonoBehaviour
{
    private AudioSource audioSource;

    [Header("Perguntas durante o trajeto (30s, 60s, 90s, 120s, 150s)")]
    public AudioClip[] listaDePerguntas;

    [Header("Fase Final (Aos 180 segundos)")]
    [Tooltip("Áudio 1: Toca exatamente aos 180 segundos para avisar que o tempo do percurso acabou.")]
    public AudioClip audioAvisoFimPercurso;

    [Tooltip("Áudio 2: Toca logo em seguida para dizer que acabou tudo antes de fechar o jogo.")]
    public AudioClip audioEncerramentoTotal;

    [Header("Configurações de Tempo")]
    public float intervaloSegundos = 10f; // Toca a cada 30 segundos
    public float tempoMaximo = 50f; // Limite de 180 segundos

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        StartCoroutine(RotinaDePerguntas());
    }

    IEnumerator RotinaDePerguntas()
    {
        float tempoDecorrido = 0f;
        int indicePergunta = 0;

        // Roda as perguntas periódicas até faltar um ciclo para os 180s
        while (tempoDecorrido < tempoMaximo - intervaloSegundos)
        {
            yield return new WaitForSeconds(intervaloSegundos);
            tempoDecorrido += intervaloSegundos;

            if (listaDePerguntas.Length > 0 && indicePergunta < listaDePerguntas.Length)
            {
                audioSource.clip = listaDePerguntas[indicePergunta];
                audioSource.Play();
                indicePergunta++;
            }
        }

        // Aguarda o restinho do tempo até bater exatamente os 180 segundos
        float tempoRestante = tempoMaximo - tempoDecorrido;
        if (tempoRestante > 0)
        {
            yield return new WaitForSeconds(tempoRestante);
        }

        // ==========================================
        // EXATAMENTE AOS 180 SEGUNDOS
        // ==========================================
        Debug.Log("180 segundos atingidos!");

        // 1. Toca o áudio avisando que o tempo do percurso acabou
        if (audioAvisoFimPercurso != null)
        {
            audioSource.clip = audioAvisoFimPercurso;
            audioSource.Play();
            // Espera este áudio terminar inteirinho
            yield return new WaitForSeconds(audioAvisoFimPercurso.length);
        }

        // 2. Toca o áudio de encerramento total (dizendo que acabou tudo)
        if (audioEncerramentoTotal != null)
        {
            audioSource.clip = audioEncerramentoTotal;
            audioSource.Play();
            // Espera o segundo áudio terminar
            yield return new WaitForSeconds(audioEncerramentoTotal.length);
        }

        // 3. Fecha o jogo de vez
        EncerrarJogo();
    }

    void EncerrarJogo()
    {
        Debug.Log("Encerrando aplicação...");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }
}